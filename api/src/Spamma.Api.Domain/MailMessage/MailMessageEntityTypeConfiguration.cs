using Fluxera.Repository.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Spamma.Api.Domain.MailMessage
{
    public class MailMessageEntityTypeConfiguration : IEntityTypeConfiguration<Aggregate.MailMessage>
    {
        public void Configure(EntityTypeBuilder<Aggregate.MailMessage> builder)
        {
            builder.UseRepositoryDefaults();
            builder.ToTable("MailMessage");
            builder.HasKey(x => x.ID);
            builder.Property(x => x.ID).ValueGeneratedNever();
            builder.OwnsMany(x => x.Addresses, addresses =>
            {
                addresses.ToTable("Address");
                addresses.HasKey(x => x.ID);
                addresses.Property(x => x.ID).ValueGeneratedNever();
            }).Navigation(user => user.Addresses)
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .AutoInclude();
        }
    }
}