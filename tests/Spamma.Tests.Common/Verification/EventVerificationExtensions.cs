using FluentAssertions;
using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Tests.Common.Verification;

/// <summary>
/// Verification extensions for domain aggregates using event sourcing.
/// Provides fluent API for asserting events raised by aggregates without assertions.
/// </summary>
public static class EventVerificationExtensions
{
    /// <summary>
    /// Verifies that an event of type TEvent was raised by the aggregate.
    /// Allows optional custom verification logic within the callback.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to verify</typeparam>
    /// <param name="aggregate">The aggregate root instance</param>
    /// <param name="verify">Optional action to perform custom verification on the event</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when event was not found</exception>
    public static void ShouldHaveRaisedEvent<TEvent>(
        this AggregateRoot aggregate,
        Action<TEvent>? verify = null)
        where TEvent : class
    {
        var uncommittedEvents = aggregate.GetUncommittedEvents();
        var @event = uncommittedEvents.OfType<TEvent>().FirstOrDefault();

        @event.Should().NotBeNull($"Expected event of type {typeof(TEvent).Name} to be raised, but it was not found");

        verify?.Invoke(@event!);
    }

    /// <summary>
    /// Verifies that NO events were raised by the aggregate (no side effects).
    /// </summary>
    /// <param name="aggregate">The aggregate root instance</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when events were found</exception>
    public static void ShouldHaveNoEvents(this AggregateRoot aggregate)
    {
        var uncommittedEvents = aggregate.GetUncommittedEvents();
        uncommittedEvents.Should().BeEmpty("Expected no events to be raised, but found {0}", uncommittedEvents.Count());
    }

    /// <summary>
    /// Verifies the exact number of events raised by the aggregate.
    /// </summary>
    /// <param name="aggregate">The aggregate root instance</param>
    /// <param name="expectedCount">Expected number of events</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when event count does not match</exception>
    public static void ShouldHaveRaisedEventCount(
        this AggregateRoot aggregate,
        int expectedCount)
    {
        var uncommittedEvents = aggregate.GetUncommittedEvents();
        uncommittedEvents.Should().HaveCount(expectedCount, "Expected {0} events to be raised", expectedCount);
    }

    /// <summary>
    /// Verifies that an event of type TEvent was NOT raised by the aggregate.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to verify was not raised</typeparam>
    /// <param name="aggregate">The aggregate root instance</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when event was found</exception>
    public static void ShouldNotHaveRaisedEvent<TEvent>(this AggregateRoot aggregate)
        where TEvent : class
    {
        var uncommittedEvents = aggregate.GetUncommittedEvents();
        var @event = uncommittedEvents.OfType<TEvent>().FirstOrDefault();

        @event.Should().BeNull($"Expected no event of type {typeof(TEvent).Name}, but it was raised");
    }

    /// <summary>
    /// Gets all uncommitted (unperisted) events from an aggregate.
    /// </summary>
    private static IEnumerable<object> GetUncommittedEvents(this AggregateRoot aggregate)
    {
        // Marten stores uncommitted events in a private field accessible via reflection
        var field = typeof(AggregateRoot).GetField("_uncommittedEvents", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field?.GetValue(aggregate) is List<object> events)
        {
            return events;
        }

        return Enumerable.Empty<object>();
    }
}
