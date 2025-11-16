using FluentAssertions;
using Spamma.Modules.DomainManagement.Client.Contracts;
using Spamma.Modules.DomainManagement.Domain.DomainAggregate.Events;

namespace Spamma.Modules.DomainManagement.Tests.Domain;

public class DomainAggregateTests
{
    [Fact]
    public void DomainCreatedEvent_WithValidData_IsConstructedCorrectly()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var token = Guid.NewGuid().ToString();

        // Act
        var @event = new DomainCreated(domainId, "example.com", "contact@example.com", "Test domain", token, now);

        // Verify
        @event.DomainId.Should().Be(domainId);
        @event.Name.Should().Be("example.com");
        @event.PrimaryContactEmail.Should().Be("contact@example.com");
        @event.Description.Should().Be("Test domain");
        @event.VerificationToken.Should().Be(token);
        @event.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void DomainCreatedEvent_WithNullContactEmail_IsValid()
    {
        // Arrange
        var domainId = Guid.NewGuid();
        var token = Guid.NewGuid().ToString();

        // Act
        var @event = new DomainCreated(domainId, "example.com", null, null, token, DateTime.UtcNow);

        // Verify
        @event.PrimaryContactEmail.Should().BeNull();
        @event.Description.Should().BeNull();
    }

    [Fact]
    public void DomainVerifiedEvent_WithValidData_IsConstructedCorrectly()
    {
        // Arrange
        var verifyTime = DateTime.UtcNow;

        // Act
        var @event = new DomainVerified(verifyTime);

        // Verify
        @event.VerifiedAt.Should().Be(verifyTime);
    }

    [Fact]
    public void DetailsUpdatedEvent_WithValidData_IsConstructedCorrectly()
    {
        // Arrange
        var newEmail = "new@example.com";
        var newDescription = "Updated description";

        // Act
        var @event = new DetailsUpdated(newEmail, newDescription);

        // Verify
        @event.PrimaryContactEmail.Should().Be(newEmail);
        @event.Description.Should().Be(newDescription);
    }

    [Fact]
    public void DetailsUpdatedEvent_WithNullValues_IsValid()
    {
        // Act
        var @event = new DetailsUpdated(null, null);

        // Verify
        @event.PrimaryContactEmail.Should().BeNull();
        @event.Description.Should().BeNull();
    }

    [Fact]
    public void DomainSuspendedEvent_WithValidReason_IsConstructedCorrectly()
    {
        // Arrange
        var reason = DomainSuspensionReason.PolicyViolation;
        var suspendTime = DateTime.UtcNow;

        // Act
        var @event = new DomainSuspended(reason, "Policy violation", suspendTime);

        // Verify
        @event.Reason.Should().Be(DomainSuspensionReason.PolicyViolation);
        @event.Notes.Should().Be("Policy violation");
        @event.SuspendedAt.Should().Be(suspendTime);
    }

    [Fact]
    public void DomainSuspendedEvent_WithDifferentReasons_IsValid()
    {
        // Test different suspension reasons
        var reasons = new[]
        {
            DomainSuspensionReason.PolicyViolation,
            DomainSuspensionReason.NonPayment,
            DomainSuspensionReason.ViolationOfTermsOfService,
        };

        foreach (var reason in reasons)
        {
            var @event = new DomainSuspended(reason, null, DateTime.UtcNow);
            @event.Reason.Should().Be(reason);
        }
    }

    [Fact]
    public void DomainUnsuspendedEvent_WithValidData_IsConstructedCorrectly()
    {
        // Arrange
        var unsuspendTime = DateTime.UtcNow;

        // Act
        var @event = new DomainUnsuspended(unsuspendTime);

        // Verify
        @event.UnsuspendedAt.Should().Be(unsuspendTime);
    }

    [Fact]
    public void ModerationUserAddedEvent_WithValidData_IsConstructedCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var @event = new ModerationUserAdded(userId, now);

        // Verify
        @event.UserId.Should().Be(userId);
        @event.AddedAt.Should().Be(now);
    }

    [Fact]
    public void ModerationUserRemovedEvent_WithValidData_IsConstructedCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Act
        var @event = new ModerationUserRemoved(userId, now);

        // Verify
        @event.UserId.Should().Be(userId);
        @event.RemovedAt.Should().Be(now);
    }

    [Fact]
    public void EventSequence_CreatedVerifiedSuspended_AllEventsValid()
    {
        // Simulate event sequence: Create -> Verify -> Suspend
        var domainId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;

        var created = new DomainCreated(
            domainId,
            "example.com",
            "contact@example.com",
            "Test domain",
            Guid.NewGuid().ToString(),
            baseTime);

        var verified = new DomainVerified(baseTime.AddSeconds(10));
        var suspended = new DomainSuspended(DomainSuspensionReason.PolicyViolation, null, baseTime.AddSeconds(20));

        // Verify sequence
        created.DomainId.Should().Be(domainId);
        verified.VerifiedAt.Should().BeAfter(created.CreatedAt);
        suspended.SuspendedAt.Should().BeAfter(verified.VerifiedAt);
    }

    [Fact]
    public void AllSuspensionReasons_AreValid()
    {
        // Verify all enum values are accessible
        var allReasons = new[]
        {
            DomainSuspensionReason.ViolationOfTermsOfService,
            DomainSuspensionReason.NonPayment,
            DomainSuspensionReason.Other,
            DomainSuspensionReason.PolicyViolation,
            DomainSuspensionReason.SecurityConcern,
            DomainSuspensionReason.AbuseSpam,
            DomainSuspensionReason.BillingContract,
            DomainSuspensionReason.Administrative,
        };

        allReasons.Should().HaveCount(8);
    }
}