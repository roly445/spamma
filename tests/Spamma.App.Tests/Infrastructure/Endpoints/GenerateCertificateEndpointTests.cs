using Microsoft.Extensions.Logging;
using Moq;
using ResultMonad;
using Spamma.App.Infrastructure.Endpoints.Setup;
using Spamma.Modules.Common.Infrastructure.Contracts;
using Xunit;

namespace Spamma.App.Tests.Infrastructure.Endpoints;

public class GenerateCertificateEndpointTests
{
    [Fact]
    public void GenerateCertificateRequest_ValidInput_CreatesSuccessfully()
    {
        // Arrange
        var domain = "example.com";
        var email = "admin@example.com";

        // Act
        var request = new GenerateCertificateRequest
        {
            Domain = domain,
            Email = email
        };

        // Verify
        Assert.Equal(domain, request.Domain);
        Assert.Equal(email, request.Email);
    }

    [Fact]
    public void GenerateCertificateRequest_ValidDomainAndEmail_CreatesSuccessfully()
    {
        // Arrange
        var domain = "example.com";
        var email = "admin@example.com";

        // Act
        var request = new GenerateCertificateRequest
        {
            Domain = domain,
            Email = email
        };

        // Verify
        Assert.Equal(domain, request.Domain);
        Assert.Equal(email, request.Email);
    }

    [Fact]
    public void GenerateCertificateResponse_SuccessfulGeneration_CreatesSuccessfully()
    {
        // Arrange
        var domain = "example.com";
        var certificatePath = "/app/certs/example.com.pfx";
        var generatedAt = DateTime.UtcNow;
        var message = "Certificate generated successfully";

        // Act
        var response = new GenerateCertificateResponse
        {
            Success = true,
            Domain = domain,
            CertificatePath = certificatePath,
            GeneratedAt = generatedAt,
            Message = message
        };

        // Verify
        Assert.True(response.Success);
        Assert.Equal(domain, response.Domain);
        Assert.Equal(certificatePath, response.CertificatePath);
        Assert.Equal(generatedAt, response.GeneratedAt);
        Assert.Equal(message, response.Message);
    }

    [Fact]
    public void GenerateCertificateResponse_FailedGeneration_CreatesSuccessfully()
    {
        // Arrange
        var domain = "invalid-domain";
        var errorMessage = "Domain validation failed";

        // Act
        var response = new GenerateCertificateResponse
        {
            Success = false,
            Domain = domain,
            CertificatePath = string.Empty,
            GeneratedAt = DateTime.UtcNow,
            Message = errorMessage
        };

        // Verify
        Assert.False(response.Success);
        Assert.Equal(domain, response.Domain);
        Assert.Equal(errorMessage, response.Message);
    }

    [Fact]
    public void GenerateCertificateRequest_EmptyDomain_CreatesWithEmptyValue()
    {
        // Arrange
        var domain = string.Empty;
        var email = "admin@example.com";

        // Act
        var request = new GenerateCertificateRequest
        {
            Domain = domain,
            Email = email
        };

        // Verify
        Assert.Equal(string.Empty, request.Domain);
        Assert.Equal(email, request.Email);
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("sub.example.com")]
    [InlineData("deep.sub.example.com")]
    [InlineData("example-with-dash.com")]
    [InlineData("example.co.uk")]
    public void GenerateCertificateRequest_VariousDomains_CreatesSuccessfully(string domain)
    {
        // Arrange & Act
        var request = new GenerateCertificateRequest
        {
            Domain = domain,
            Email = "admin@example.com"
        };

        // Verify
        Assert.Equal(domain, request.Domain);
    }

    [Theory]
    [InlineData("admin@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("admin@sub.example.com")]
    public void GenerateCertificateRequest_VariousEmails_CreatesSuccessfully(string email)
    {
        // Arrange & Act
        var request = new GenerateCertificateRequest
        {
            Domain = "example.com",
            Email = email
        };

        // Verify
        Assert.Equal(email, request.Email);
    }

    [Fact]
    public void GenerateCertificateResponse_PreservesTimestampPrecision()
    {
        // Arrange
        var expectedTime = DateTime.UtcNow;

        // Act
        var response = new GenerateCertificateResponse
        {
            Success = true,
            Domain = "example.com",
            CertificatePath = "/app/certs/example.com.pfx",
            GeneratedAt = expectedTime,
            Message = "Success"
        };

        // Verify
        Assert.Equal(expectedTime, response.GeneratedAt);
    }

    [Fact]
    public void GenerateCertificateRequest_RecordType_EqualsByValue()
    {
        // Arrange
        var request1 = new GenerateCertificateRequest
        {
            Domain = "example.com",
            Email = "admin@example.com"
        };
        
        var request2 = new GenerateCertificateRequest
        {
            Domain = "example.com",
            Email = "admin@example.com"
        };

        // Act & Verify
        Assert.Equal(request1, request2);
    }

    [Fact]
    public void GenerateCertificateResponse_RecordType_EqualsByValue()
    {
        // Arrange
        var time = DateTime.UtcNow;
        var response1 = new GenerateCertificateResponse
        {
            Success = true,
            Domain = "example.com",
            CertificatePath = "/app/certs/example.com.pfx",
            GeneratedAt = time,
            Message = "Success"
        };
        
        var response2 = new GenerateCertificateResponse
        {
            Success = true,
            Domain = "example.com",
            CertificatePath = "/app/certs/example.com.pfx",
            GeneratedAt = time,
            Message = "Success"
        };

        // Act & Verify
        Assert.Equal(response1, response2);
    }

    [Fact]
    public void GenerateCertificateResponse_NullMessage_CreatesSuccessfully()
    {
        // Arrange & Act
        var response = new GenerateCertificateResponse
        {
            Success = false,
            Domain = "example.com",
            CertificatePath = string.Empty,
            GeneratedAt = DateTime.UtcNow,
            Message = null!
        };

        // Verify
        Assert.False(response.Success);
    }
}
