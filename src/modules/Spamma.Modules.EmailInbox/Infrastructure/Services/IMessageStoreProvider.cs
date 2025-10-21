using MaybeMonad;
using MimeKit;
using ResultMonad;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public interface IMessageStoreProvider
{
    ValueTask<Result> StoreMessageContentAsync(Guid messageId, MimeMessage messageContent, CancellationToken cancellationToken = default);

    ValueTask<Result> DeleteMessageContentAsync(Guid messageId, CancellationToken cancellationToken = default);

    ValueTask<Maybe<MimeMessage>> LoadMessageContentAsync(Guid messageId, CancellationToken cancellationToken = default);
}