using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Spamma.Api.Domain.MailMessage;

namespace Spamma.Api.Data
{
    public abstract class SpammaContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MailMessageEntityTypeConfiguration).Assembly);
        }
    }
}