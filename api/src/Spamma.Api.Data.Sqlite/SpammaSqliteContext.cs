using Microsoft.EntityFrameworkCore;

namespace Spamma.Api.Data.Sqlite;

public class SpammaSqliteContext : SpammaContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite("DataSource=spamma.db;");
}