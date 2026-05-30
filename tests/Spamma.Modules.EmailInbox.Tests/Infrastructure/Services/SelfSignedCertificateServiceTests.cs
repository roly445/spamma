using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class SelfSignedCertificateServiceTests
{
    private const string CertificatePassword = "letmein";

    [Fact]
    public void GenerateSelfSignedCertificate_ReturnsNonEmptyByteArray()
    {
        // Arrange
        var service = new SelfSignedCertificateService();

        // Act
        var result = service.GenerateSelfSignedCertificate("mail.test.localhost");

        // Verify
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateSelfSignedCertificate_CanBeLoadedAsX509Certificate2()
    {
        // Arrange
        var service = new SelfSignedCertificateService();

        // Act
        var pfxBytes = service.GenerateSelfSignedCertificate("mail.test.localhost");

        // Verify
        var loadAction = () => X509CertificateLoader.LoadPkcs12(pfxBytes, CertificatePassword);
        loadAction.Should().NotThrow();
    }

    [Fact]
    public void GenerateSelfSignedCertificate_CertificateCnMatchesDomain()
    {
        // Arrange
        var domain = "mail.test.localhost";
        var service = new SelfSignedCertificateService();

        // Act
        var pfxBytes = service.GenerateSelfSignedCertificate(domain);
        using var cert = X509CertificateLoader.LoadPkcs12(pfxBytes, CertificatePassword);

        // Verify
        cert.Subject.Should().Contain(domain);
    }

    [Fact]
    public void GenerateSelfSignedCertificate_CertificateHasAtLeast364DaysValidity()
    {
        // Arrange
        var service = new SelfSignedCertificateService();

        // Act
        var pfxBytes = service.GenerateSelfSignedCertificate("mail.test.localhost");
        using var cert = X509CertificateLoader.LoadPkcs12(pfxBytes, CertificatePassword);

        // Verify
        var remainingDays = (cert.NotAfter - DateTime.Now).TotalDays;
        remainingDays.Should().BeGreaterThanOrEqualTo(364);
    }
}
