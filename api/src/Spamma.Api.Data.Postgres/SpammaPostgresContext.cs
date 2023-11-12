using Microsoft.EntityFrameworkCore;

namespace Spamma.Api.Data.Postgres
{
    public class SpammaPostgresContext : SpammaContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(@"Host=myserver;Username=mylogin;Password=mypass;Database=mydatabase");
    }
}