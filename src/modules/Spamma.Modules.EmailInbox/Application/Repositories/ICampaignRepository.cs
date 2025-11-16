using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.EmailInbox.Domain.CampaignAggregate;

namespace Spamma.Modules.EmailInbox.Application.Repositories;

public interface ICampaignRepository : IRepository<Campaign>;