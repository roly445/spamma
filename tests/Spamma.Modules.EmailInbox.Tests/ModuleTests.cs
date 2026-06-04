using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmtpServer;
using Spamma.Modules.EmailInbox.Infrastructure.Services;
using Spamma.Modules.EmailInbox.Infrastructure.Settings;

namespace Spamma.Modules.EmailInbox.Tests;

public sealed class ModuleTests
{
    [Fact]
    public void AddEmailInbox_WithCertificate_ConfiguresStandardTlsEndpoints()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var certificatePath = Path.Combine(tempDir, "certificate_test.pfx");
            CreateCertificate(certificatePath);

            var certificateService = new SmtpCertificateService(NullLogger<SmtpCertificateService>.Instance);
            var fieldInfo = typeof(SmtpCertificateService).GetField("_certificatePath", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo?.SetValue(certificateService, tempDir);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddEmailInbox();
            services.RemoveAll<IOptions<EmailInboxSettings>>();
            services.AddSingleton<IOptions<EmailInboxSettings>>(Options.Create(new EmailInboxSettings { Port = 2525 }));
            services.RemoveAll<SmtpCertificateService>();
            services.AddSingleton(certificateService);

            using var provider = services.BuildServiceProvider();

            // Act
            var smtpServer = provider.GetRequiredService<SmtpServer.SmtpServer>();
            var options = GetOptions(smtpServer);

            // Assert
            var endpoints = options.Endpoints.ToDictionary(e => e.Endpoint.Port);
            endpoints.Keys.Should().BeEquivalentTo([2525, 465, 587]);
            endpoints[2525].IsSecure.Should().BeFalse();
            endpoints[2525].CertificateFactory.Should().BeNull();
            endpoints[465].IsSecure.Should().BeTrue();
            endpoints[465].CertificateFactory.Should().NotBeNull();
            endpoints[587].IsSecure.Should().BeFalse();
            endpoints[587].CertificateFactory.Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static ISmtpServerOptions GetOptions(SmtpServer.SmtpServer smtpServer)
    {
        var fieldInfo = typeof(SmtpServer.SmtpServer).GetField("_options", BindingFlags.NonPublic | BindingFlags.Instance);
        return (ISmtpServerOptions)fieldInfo!.GetValue(smtpServer)!;
    }

    private static void CreateCertificate(string certificatePath)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("cn=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        File.WriteAllBytes(certificatePath, certificate.Export(X509ContentType.Pfx));
    }
}
