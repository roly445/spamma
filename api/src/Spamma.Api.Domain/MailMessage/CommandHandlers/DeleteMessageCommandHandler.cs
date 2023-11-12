using MediatR;
using ResultMonad;
using Spamma.Api.Domain.MailMessage.Commands;

namespace Spamma.Api.Domain.MailMessage.CommandHandlers
{
    public class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, Result>
    {
        private readonly IMailMessageRepository _mailMessageRepository;

        public DeleteMessageCommandHandler(IMailMessageRepository mailMessageRepository)
        {
            this._mailMessageRepository = mailMessageRepository;
        }

        public async Task<Result> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
        {
            await this._mailMessageRepository.RemoveAsync(request.MailMessageId, cancellationToken);

            return Result.Ok();
        }
    }
}