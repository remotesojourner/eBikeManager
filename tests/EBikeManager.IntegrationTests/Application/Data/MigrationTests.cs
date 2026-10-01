using EBikeManager.Application.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EBikeManager.IntegrationTests.Application.Data;

public sealed class MigrationTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task ExistingBikesAndRidesKeepBoschsNameAsTheirModel()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _connection.OpenAsync(cancellationToken);
        await using var db = new EBikeManagerDbContext(new DbContextOptionsBuilder<EBikeManagerDbContext>().UseSqlite(_connection).Options);
        var migrator = db.GetService<IMigrator>();

        await migrator.MigrateAsync("AddBikeDocuments", cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO bikes (id, name, addedAt) VALUES ('bike-1', 'TENWAYS (Performance Line)', '2026-09-30 12:00:00');
            INSERT INTO rides (id, bikeId, startTime, firstSeenAt) VALUES
                ('ride-1', 'bike-1', '2026-09-29 08:00:00', '2026-09-30 12:00:00'),
                ('ride-2', 'unticked-bike', '2026-09-28 08:00:00', '2026-09-30 12:00:00');
            """,
            cancellationToken);
        await migrator.MigrateAsync(cancellationToken: cancellationToken);

        Assert.Equal(["TENWAYS (Performance Line)", null], await ColumnAsync(db, "SELECT bikeName AS Value FROM rides ORDER BY id", cancellationToken));
        Assert.Equal(["TENWAYS (Performance Line)", null], await ColumnAsync(db, "SELECT bikeModel AS Value FROM rides ORDER BY id", cancellationToken));
        Assert.Equal(["TENWAYS (Performance Line)"], await ColumnAsync(db, "SELECT model AS Value FROM bikes", cancellationToken));
    }

    private static Task<List<string?>> ColumnAsync(EBikeManagerDbContext db, string sql, CancellationToken cancellationToken) =>
        db.Database.SqlQueryRaw<string?>(sql).ToListAsync(cancellationToken);
}
