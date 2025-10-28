using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

/// <summary>
/// Tests for SmtpCertificateService certificate detection.
/// </summary>
public class SmtpCertificateServiceTests
{
    [Fact]
    public void FindCertificate_DirectoryDoesNotExist_ReturnsNothing()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SmtpCertificateService>>();
        var service = new SmtpCertificateService(loggerMock.Object);

        // Act - use a non-existent directory by accessing private field indirectly
        var result = service.FindCertificate();

        // Verify - should return Nothing since certs/ directory doesn't exist in test context
        result.HasValue.Should().BeFalse();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Certificate directory not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void FindCertificate_NoPfxFilesInDirectory_ReturnsNothing()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a non-.pfx file
            File.WriteAllText(Path.Combine(tempDir, "readme.txt"), "Not a certificate");

            var loggerMock = new Mock<ILogger<SmtpCertificateService>>();
            var service = new SmtpCertificateService(loggerMock.Object);

            // Manually set the certificate path (using reflection to access private field)
            var fieldInfo = typeof(SmtpCertificateService).GetField("_certificatePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo?.SetValue(service, tempDir);

            // Act
            var result = service.FindCertificate();

            // Verify
            result.HasValue.Should().BeFalse();
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No .pfx certificates found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void FindCertificate_ValidCertificateExists_ReturnsMaybeWithCertificate()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a self-signed certificate
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("cn=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(365));

            // Save certificate to .pfx file
            var certPath = Path.Combine(tempDir, "test-cert.pfx");
            byte[] pfxData = certificate.Export(X509ContentType.Pfx);
            File.WriteAllBytes(certPath, pfxData);

            var loggerMock = new Mock<ILogger<SmtpCertificateService>>();
            var service = new SmtpCertificateService(loggerMock.Object);

            // Manually set the certificate path
            var fieldInfo = typeof(SmtpCertificateService).GetField("_certificatePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo?.SetValue(service, tempDir);

            // Act
            var result = service.FindCertificate();

            // Verify
            result.HasValue.Should().BeTrue();
            result.Value.Subject.Should().Contain("localhost");
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Found valid SMTP certificate")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void FindCertificate_CorruptedCertificateFile_ReturnsNothing()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a corrupted .pfx file
            var certPath = Path.Combine(tempDir, "corrupt.pfx");
            File.WriteAllText(certPath, "This is not a valid certificate");

            var loggerMock = new Mock<ILogger<SmtpCertificateService>>();
            var service = new SmtpCertificateService(loggerMock.Object);

            // Manually set the certificate path
            var fieldInfo = typeof(SmtpCertificateService).GetField("_certificatePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo?.SetValue(service, tempDir);

            // Act
            var result = service.FindCertificate();

            // Verify
            result.HasValue.Should().BeFalse();
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed to load")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void FindCertificate_MultipleCertificates_LoadsFirstOne()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create two certificates
            for (int i = 1; i <= 2; i++)
            {
                using var rsa = RSA.Create(2048);
                var request = new CertificateRequest($"cn=localhost{i}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(365));

                var certPath = Path.Combine(tempDir, $"cert{i}.pfx");
                byte[] pfxData = certificate.Export(X509ContentType.Pfx);
                File.WriteAllBytes(certPath, pfxData);
            }

            var loggerMock = new Mock<ILogger<SmtpCertificateService>>();
            var service = new SmtpCertificateService(loggerMock.Object);

            // Manually set the certificate path
            var fieldInfo = typeof(SmtpCertificateService).GetField("_certificatePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo?.SetValue(service, tempDir);

            // Act
            var result = service.FindCertificate();

            // Verify - should load one of them (first alphabetically)
            result.HasValue.Should().BeTrue();
            result.Value.Subject.Should().Contain("localhost");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
