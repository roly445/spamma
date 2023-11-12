using Fluxera.Repository.EntityFrameworkCore;

namespace Spamma.Api.Data
{
    public sealed class FluxeraContext : EntityFrameworkCoreContext
    {
        /// <inheritdoc />
        protected override void ConfigureOptions(EntityFrameworkCoreContextOptions options)
        {
            options.UseDbContext<SpammaContext>();
        }
    }
}