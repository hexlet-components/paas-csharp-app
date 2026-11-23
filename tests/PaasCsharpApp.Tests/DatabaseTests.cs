using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace PaasCsharpApp.Tests;

/// <summary>
/// Тесты с базой идут, когда задан TEST_DATABASE_URL, например
/// postgresql://postgres@localhost:5432/paas_test. База очищается.
/// </summary>
public sealed class DatabaseTests
{
    private static readonly string? Url = Environment.GetEnvironmentVariable("TEST_DATABASE_URL");

    private static async Task ResetAsync()
    {
        var connection = new NpgsqlConnection(DatabaseUrl.ToConnectionString(Url!));
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            var command = new NpgsqlCommand("""DROP TABLE IF EXISTS page_visits, "__EFMigrationsHistory" """, connection);
            await using (command.ConfigureAwait(false))
            {
                await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(DatabaseUrl.ToConnectionString(Url!)).Options);

    [Fact]
    public async Task ConcurrentMigrationsApplyOnce()
    {
        Assert.SkipWhen(string.IsNullOrEmpty(Url), "TEST_DATABASE_URL не задан");
        await ResetAsync();

        // У каждого экземпляра свой контекст и подключение, как у отдельных процессов.
        await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            var db = CreateContext();
            await using (db.ConfigureAwait(false))
            {
                await db.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
            }
        }));

        var check = CreateContext();
        await using (check.ConfigureAwait(false))
        {
            var applied = await check.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
            Assert.Single(applied);
        }
    }

    [Fact]
    public async Task HomeCountsVisits()
    {
        Assert.SkipWhen(string.IsNullOrEmpty(Url), "TEST_DATABASE_URL не задан");
        await ResetAsync();

        using var baseFactory = new WebApplicationFactory<Program>();
        using var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SERVER_MESSAGE", "Первая публикация");
            builder.UseSetting("DATABASE_URL", Url);
        });
        using var client = factory.CreateClient();
        for (var visit = 1; visit <= 2; visit++)
        {
            var body = await client.GetStringAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
            Assert.Equal($"<h1>Привет от Хекслета!</h1><p>Первая публикация</p><p>Посещений: {visit}</p>\n", body);
        }
    }

    [Fact]
    public void ConnectionStringComesFromRenderUrl()
    {
        var connectionString = DatabaseUrl.ToConnectionString("postgresql://paas_user@dpg-example-a/paas_demo");

        Assert.Equal("Host=dpg-example-a;Database=paas_demo;Username=paas_user", connectionString);
    }
}
