using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Spamma.Browser.Tests;

internal sealed class DnsTxtCapture : IAsyncDisposable
{
    private readonly UdpClient udp = new(new IPEndPoint(IPAddress.Loopback, 53535));
    private readonly CancellationTokenSource cancellation = new();
    private readonly ConcurrentDictionary<string, string> records = new(StringComparer.OrdinalIgnoreCase);
    private readonly Task serveTask;

    public DnsTxtCapture() => this.serveTask = this.ServeAsync();

    public void Publish(string name, string token) => this.records[name] = $"spamma-verification={token}";

    private async Task ServeAsync()
    {
        while (!this.cancellation.IsCancellationRequested)
        {
            UdpReceiveResult request;
            try { request = await this.udp.ReceiveAsync(this.cancellation.Token); }
            catch (OperationCanceledException) { break; }

            var bytes = request.Buffer;
            if (bytes.Length < 18) continue;
            var offset = 12;
            var labels = new List<string>();
            while (offset < bytes.Length && bytes[offset] != 0)
            {
                var length = bytes[offset++];
                if (offset + length > bytes.Length) break;
                labels.Add(Encoding.ASCII.GetString(bytes, offset, length));
                offset += length;
            }
            if (offset + 5 > bytes.Length) continue;
            offset++;
            var name = string.Join('.', labels);
            var isTxt = bytes[offset] == 0 && bytes[offset + 1] == 16;
            this.records.TryGetValue(name, out var value);
            var found = isTxt && value is not null;
            using var response = new MemoryStream();
            response.Write(bytes, 0, 2);
            response.WriteByte(0x81); response.WriteByte(0x80);
            response.WriteByte(0); response.WriteByte(1);
            response.WriteByte(0); response.WriteByte(found ? (byte)1 : (byte)0);
            response.Write(new byte[4]);
            response.Write(bytes, 12, offset + 4 - 12);
            if (found)
            {
                var text = Encoding.ASCII.GetBytes(value!);
                response.Write([0xc0, 0x0c, 0, 16, 0, 1, 0, 0, 0, 30, 0, (byte)(text.Length + 1), (byte)text.Length]);
                response.Write(text);
            }
            await this.udp.SendAsync(response.ToArray(), request.RemoteEndPoint, this.cancellation.Token);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await this.cancellation.CancelAsync();
        this.udp.Dispose();
        try { await this.serveTask; } catch (ObjectDisposedException) { }
        this.cancellation.Dispose();
    }
}
