using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace Spamma.Browser.Tests;

internal sealed class SetupSmtpCapture : IAsyncDisposable
{
    private readonly CancellationTokenSource cancellation = new();
    private readonly TaskCompletionSource<MimeMessage> message = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ServiceProvider services;
    private readonly Task serverTask;

    private SetupSmtpCapture(int port)
    {
        var store = new CaptureMessageStore(this.message);
        this.services = new ServiceCollection()
            .AddSingleton<IMessageStore>(store)
            .BuildServiceProvider();

        var options = new SmtpServerOptionsBuilder()
            .ServerName("Spamma setup test SMTP")
            .Endpoint(endpoint => endpoint.Port(port, false))
            .Build();
        var server = new SmtpServer.SmtpServer(options, this.services);
        this.serverTask = server.StartAsync(this.cancellation.Token);
    }

    public static async Task<SetupSmtpCapture> StartAsync(int port)
    {
        var capture = new SetupSmtpCapture(port);
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (capture.serverTask.IsFaulted)
            {
                await capture.serverTask;
            }

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port);
                return capture;
            }
            catch (SocketException) when (attempt < 29)
            {
                await Task.Delay(100);
            }
        }

        await capture.DisposeAsync();
        throw new TimeoutException($"Setup SMTP capture did not start on port {port}.");
    }

    public Task<MimeMessage> WaitForMessageAsync() => this.message.Task.WaitAsync(TimeSpan.FromSeconds(40));

    public async ValueTask DisposeAsync()
    {
        await this.cancellation.CancelAsync();
        try
        {
            await this.serverTask;
        }
        catch (OperationCanceledException)
        {
        }

        this.services.Dispose();
        this.cancellation.Dispose();
    }

    private sealed class CaptureMessageStore(TaskCompletionSource<MimeMessage> message) : MessageStore
    {
        public override async Task<SmtpResponse> SaveAsync(
            ISessionContext context,
            IMessageTransaction transaction,
            ReadOnlySequence<byte> buffer,
            CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream(buffer.ToArray());
            var captured = await MimeMessage.LoadAsync(stream, cancellationToken);
            message.TrySetResult(captured);
            return SmtpResponse.Ok;
        }
    }
}
