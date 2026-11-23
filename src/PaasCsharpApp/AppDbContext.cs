using Microsoft.EntityFrameworkCore;

namespace PaasCsharpApp;

/// <summary>Контекст EF Core. По нему dotnet ef создаёт миграции в каталоге Migrations.</summary>
internal sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PageVisit> PageVisits => Set<PageVisit>();

    /// <summary>Увеличивает счётчик главной страницы и возвращает новое значение.</summary>
    /// <remarks>Один запрос и создаёт строку, и увеличивает её, поэтому одновременные запросы не теряют посещения.</remarks>
    public async Task<long> CountVisitAsync(CancellationToken cancellationToken)
    {
        var totals = await Database.SqlQuery<long>($"""
            INSERT INTO page_visits (path, total) VALUES ('/', 1)
            ON CONFLICT (path) DO UPDATE SET total = page_visits.total + 1
            RETURNING total AS "Value"
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
        return totals[0];
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PageVisit>(entity =>
        {
            entity.ToTable("page_visits");
            entity.HasKey(visit => visit.Path);
            entity.Property(visit => visit.Path).HasColumnName("path");
            entity.Property(visit => visit.Total).HasColumnName("total");
        });
    }
}
