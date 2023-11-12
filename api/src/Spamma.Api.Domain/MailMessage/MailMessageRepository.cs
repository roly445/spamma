using Fluxera.Repository;

namespace Spamma.Api.Domain.MailMessage
{
    public class MailMessageRepository : Repository<Aggregate.MailMessage, Guid>, IMailMessageRepository
    {
        public MailMessageRepository(IRepository<Aggregate.MailMessage, Guid> innerRepository) : base(innerRepository)
        {
        }
    }
}