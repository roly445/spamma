using MaybeMonad;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using ResultMonad;
using Spamma.Modules.Common.Application.Contracts;

namespace Spamma.Modules.EmailInbox.Infrastructure.Services;

public class LocalMessageStoreProvider(
    IHostEnvironment hostEnvironment,
    ILogger<LocalMessageStoreProvider> logger,
    IDirectoryWrapper directoryWrapper,
    IFileWrapper fileWrapper)
    : IMessageStoreProvider
{
    public async ValueTask<Result> StoreMessageContentAsync(Guid messageId, MimeMessage messageContent, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(hostEnvironment.ContentRootPath, "messages");
        if (!directoryWrapper.Exists(path))
        {
            try
            {
                directoryWrapper.CreateDirectory(path);
            }
            catch (Exception e)
            {
                if (e is not (UnauthorizedAccessException or PathTooLongException
                    or DirectoryNotFoundException))
                {
                    throw;
                }

                logger.LogError(e, "Failed to create directory for message storage.");
                return Result.Fail();
            }
        }

        await messageContent.WriteToAsync(Path.Combine(path, $"{messageId}.eml"), cancellationToken);
        return Result.Ok();
    }

    public ValueTask<Result> DeleteMessageContentAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(hostEnvironment.ContentRootPath, "messages");
        if (!directoryWrapper.Exists(path))
        {
            return new ValueTask<Result>(Result.Fail());
        }

        fileWrapper.Delete(Path.Combine(path, $"{messageId}.eml"));
        return new ValueTask<Result>(Result.Ok());
    }

    public async ValueTask<Maybe<MimeMessage>> LoadMessageContentAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(hostEnvironment.ContentRootPath, "messages");
        if (!directoryWrapper.Exists(path))
        {
            return Maybe<MimeMessage>.Nothing;
        }

        try
        {
            var message = await MimeMessage.LoadAsync(Path.Combine(path, $"{messageId}.eml"), cancellationToken);
            return Maybe.From(message);
        }
        catch (Exception e)
        {
            if (e is not (UnauthorizedAccessException or PathTooLongException
                or DirectoryNotFoundException or FileNotFoundException or FormatException))
            {
                throw;
            }

            logger.LogError(e, "Failed to create directory for message storage.");
            return Maybe<MimeMessage>.Nothing;
        }
    }
}