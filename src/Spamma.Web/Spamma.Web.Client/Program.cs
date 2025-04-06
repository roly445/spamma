using BluQube.Attributes;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Spamma.Web.Client;

[BluQubeRequester]
public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        await builder.Build().RunAsync();
    }
}