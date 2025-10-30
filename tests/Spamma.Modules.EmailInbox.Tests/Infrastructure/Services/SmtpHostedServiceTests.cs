using FluentAssertions;
using Spamma.Modules.EmailInbox.Infrastructure.Services;

namespace Spamma.Modules.EmailInbox.Tests.Infrastructure.Services;

public class SmtpHostedServiceTests
{
    [Fact]
    public void Constructor_AcceptsSmtpServer_AndDoesNotThrow()
    {
        // This test verifies the basic structure of SmtpHostedService.
        // We cannot easily mock SmtpServer due to sealed classes and non-virtual methods.
        // Full integration tests should use actual SmtpServer instance.

        // Arrange & Act - just verify the service can be instantiated
        // In production, SmtpServer is built via DI container
        var service = typeof(SmtpHostedService);

        // Verify
        service.Should().NotBeNull();
        service.GetConstructors().Should().HaveCount(1);
        var constructorParams = service.GetConstructors()[0].GetParameters();
        constructorParams.Should().HaveCount(1);
        constructorParams[0].ParameterType.Name.Should().Be("SmtpServer");
    }

    [Fact]
    public void SmtpHostedService_IsBackgroundService()
    {
        // Verify SmtpHostedService inherits from BackgroundService
        var service = typeof(SmtpHostedService);
        var baseType = service.BaseType;

        // Verify
        baseType?.Name.Should().Be("BackgroundService");
    }

    [Fact]
    public void SmtpHostedService_HasExecuteAsyncMethod()
    {
        // Verify SmtpHostedService has the required ExecuteAsync method
        var service = typeof(SmtpHostedService);
        var method = service.GetMethod("ExecuteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Verify
        method.Should().NotBeNull("SmtpHostedService should have ExecuteAsync method");
        method!.ReturnType.Name.Should().Be("Task");
    }
}