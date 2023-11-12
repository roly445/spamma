using System.Buffers;
using MediatR;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Spamma.Api.Core;
using Spamma.Api.Domain.MailMessage.Commands;

namespace Spamma.Api.Web
{
    public class SpammaMessageStore : MessageStore
    {
        public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction,
            ReadOnlySequence<byte> buffer,
            CancellationToken cancellationToken)
        {
            var scope = context.ServiceProvider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await using var stream = new MemoryStream();

            var position = buffer.GetPosition(0);
            while (buffer.TryGet(ref position, out var memory))
            {
                stream.Write(memory.Span);
            }

            stream.Position = 0;

            var id = Guid.NewGuid();

            var message = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);

            await mediator.Send(
                new StoreMessageCommand(
                id,
                message.Subject,
                message.Date,
                message.From.Mailboxes
                    .Select(x => new StoreMessageCommand.Address(Guid.NewGuid(), x.Address, x.Name, AddressType.From)).AsEnumerable()
                    .Concat(message.To.Mailboxes
                        .Select(x => new StoreMessageCommand.Address(Guid.NewGuid(), x.Address, x.Name, AddressType.To)).ToList()).AsEnumerable()
                    .Concat(message.Bcc.Mailboxes
                        .Select(x => new StoreMessageCommand.Address(Guid.NewGuid(), x.Address, x.Name, AddressType.Bcc)).ToList()).AsEnumerable()
                    .Concat(message.Cc.Mailboxes
                        .Select(x => new StoreMessageCommand.Address(Guid.NewGuid(), x.Address, x.Name, AddressType.Cc)).ToList()).ToList()), cancellationToken);
            return new SmtpResponse(SmtpReplyCode.Ok);
        }
    }
}