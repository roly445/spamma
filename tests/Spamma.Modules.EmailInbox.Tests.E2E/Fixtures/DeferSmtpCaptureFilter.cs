using DotNetCore.CAP.Filter;

namespace Spamma.Modules.EmailInbox.Tests.E2E.Fixtures;

public sealed class DeferSmtpCaptureFilter : SubscribeFilter
{
    private static int deferCapture;

    public static void DeferCapture() => Volatile.Write(ref deferCapture, 1);

    public static void AllowCapture() => Volatile.Write(ref deferCapture, 0);

    public override Task OnSubscribeExecutingAsync(ExecutingContext context)
    {
        if (Volatile.Read(ref deferCapture) == 1)
        {
            throw new InvalidOperationException("SMTP capture deferred until host restart");
        }

        return Task.CompletedTask;
    }
}
