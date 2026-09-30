using EBikeManager.Application.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EBikeManager.IntegrationTests.Fixtures;

internal sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestDatabase()
    {
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public EBikeManagerDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<EBikeManagerDbContext>();
        options.UseSqlite(_connection).ConfigureWarnings(warnings => warnings.Throw(
            CoreEventId.FirstWithoutOrderByAndFilterWarning,
            CoreEventId.RowLimitingOperationWithoutOrderByWarning));
        return new EBikeManagerDbContext(options.Options);
    }

    public void Dispose() => _connection.Dispose();
}
