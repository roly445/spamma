using MediatR;
using ResultMonad;

namespace Spamma.Api.Domain.MailMessage.Commands
{
    public record DeleteMessageCommand(Guid MailMessageId) : IRequest<Result>;
}