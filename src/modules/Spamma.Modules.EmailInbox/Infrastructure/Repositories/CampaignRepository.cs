using Marten;
using Spamma.Modules.Common.Infrastructure;
using Spamma.Modules.EmailInbox.Application.Repositories;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate;

namespace Spamma.Modules.EmailInbox.Infrastructure.Repositories;

public class CampaignRepository(IDocumentSession session) : GenericRepository<Campaign>(session), ICampaignRepository;