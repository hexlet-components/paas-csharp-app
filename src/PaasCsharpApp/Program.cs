using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using PaasCsharpApp;
using Sentry;

const string heading = "Привет от Хекслета!";

var builder = WebApplication.CreateSlimBuilder(args);

// Пустое значение приравнивается к незаданному.
string Setting(string name, string fallback)
{
    var value = builder.Configuration[name];
    return string.IsNullOrEmpty(value) ? fallback : value;
}

var port = Setting("PORT", "3000");
var message = Setting("SERVER_MESSAGE", "Hello, PaaS!");

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
// Без SENTRY_DSN SDK ничего не отправляет.
builder.WebHost.UseSentry(options => options.Dsn = builder.Configuration["SENTRY_DSN"] ?? string.Empty);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
// Запросы EF Core к базе в лог не идут, сообщения о миграциях остаются.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Migrations", LogLevel.Information);

var databaseUrl = builder.Configuration["DATABASE_URL"];
var hasDatabase = !string.IsNullOrEmpty(databaseUrl);
if (hasDatabase)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(DatabaseUrl.ToConnectionString(databaseUrl!)));
}

var app = builder.Build();

// С DATABASE_URL приложение сначала применяет миграции и только потом начинает
// принимать запросы. Перед применением EF Core берёт блокировку в базе, поэтому
// второй экземпляр приложения ждёт, пока первый закончит.
if (hasDatabase)
{
    var scope = app.Services.CreateAsyncScope();
    await using (scope.ConfigureAwait(false))
    {
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync().ConfigureAwait(false);
    }
}

app.Use(async (context, next) =>
{
    await next(context).ConfigureAwait(false);
    Console.WriteLine($"{context.Request.Method} {context.Request.Path} {context.Response.StatusCode}");
});

const string errorText = "Oops! Something went wrong!";
var failure = Results.Text($"{errorText}\n", "text/plain; charset=utf-8", statusCode: 500);

// Без базы контекст не зарегистрирован, и главная страница выходит без счётчика посещений.
app.MapGet("/", async (HttpContext context, IHub hub) =>
{
    var visits = string.Empty;
    var db = context.RequestServices.GetService<AppDbContext>();
    if (db is not null)
    {
        try
        {
            var total = await db.CountVisitAsync(context.RequestAborted).ConfigureAwait(false);
            visits = string.Create(CultureInfo.InvariantCulture, $"<p>Посещений: {total}</p>");
        }
        catch (Exception error) when (error is Npgsql.NpgsqlException or InvalidOperationException)
        {
            // Сбой базы обрабатывается как ошибка /error: событие в Sentry и ответ 500.
            hub.CaptureException(error);
            return failure;
        }
    }

    return Results.Content(
        $"<h1>{heading}</h1><p>{WebUtility.HtmlEncode(message)}</p>{visits}\n",
        "text/html; charset=utf-8");
});

app.MapGet("/error", (IHub hub) =>
{
    try
    {
        throw new InvalidOperationException(errorText);
    }
    catch (InvalidOperationException error)
    {
        hub.CaptureException(error);
    }

    return failure;
});

app.MapFallback(() => Results.Text("not found\n", "text/plain; charset=utf-8", statusCode: 404));

await app.RunAsync().ConfigureAwait(false);

// Нужен тестам: WebApplicationFactory<Program> запускает приложение целиком.
internal partial class Program;
