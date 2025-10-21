using Spamma.Modules.Common.Application.Contracts;
using Spamma.Modules.EmailInbox.Domain.EmailAggregate;

namespace Spamma.Modules.EmailInbox.Application.Repositories;

public interface IEmailRepository : IRepository<Email>;