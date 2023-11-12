using Fluxera.Repository;

namespace Spamma.Api.Domain.MailMessage
{
    public interface IMailMessageRepository : IRepository<Aggregate.MailMessage, Guid>
    {
    }
}