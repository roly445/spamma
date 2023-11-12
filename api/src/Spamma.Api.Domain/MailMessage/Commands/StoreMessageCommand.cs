using MediatR;
using ResultMonad;
using Spamma.Api.Core;

namespace Spamma.Api.Domain.MailMessage.Commands
{
    public record StoreMessageCommand(
        Guid MailMessageId,
        string Subject,
        DateTimeOffset WhenReceived,
        IReadOnlyList<StoreMessageCommand.Address> Addresses
    ) : IRequest<Result>
    {
        public record Address(
            Guid Id,
            string EmailAddress,
            string DisplayName,
            AddressType AddressType);
    }
}