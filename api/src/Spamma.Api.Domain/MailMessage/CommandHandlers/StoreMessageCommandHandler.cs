using MediatR;
using ResultMonad;
using Spamma.Api.Domain.MailMessage.Commands;
using Address = Spamma.Api.Domain.MailMessage.Aggregate.Address;

namespace Spamma.Api.Domain.MailMessage.CommandHandlers
{
    public class StoreMessageCommandHandler : IRequestHandler<StoreMessageCommand, Result>
    {
        private readonly IMailMessageRepository _mailMessageRepository;

        public StoreMessageCommandHandler(
            IMailMessageRepository mailMessageRepository)
        {
            this._mailMessageRepository = mailMessageRepository;
        }

        public async Task<Result> Handle(StoreMessageCommand request, CancellationToken cancellationToken)
        {
            await this._mailMessageRepository.AddAsync(
                new Aggregate.MailMessage(
                    request.MailMessageId,
                    request.Subject,
                    request.WhenReceived,
                    request.Addresses.Select(x => new Address(
                            x.Id,
                            x.EmailAddress,
                            x.DisplayName,
                            x.AddressType)).ToList()), cancellationToken);

            return Result.Ok();
        }
    }
}