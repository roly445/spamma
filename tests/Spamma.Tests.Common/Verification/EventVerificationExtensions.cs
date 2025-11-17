using FluentAssertions;
using Spamma.Modules.Common.Domain.Contracts;

namespace Spamma.Tests.Common.Verification;

public static class EventVerificationExtensions
{
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

    public static void ShouldHaveNoEvents(this AggregateRoot aggregate)
    {
        var uncommittedEvents = aggregate.GetUncommittedEvents();
        uncommittedEvents.Should().BeEmpty("Expected no events to be raised, but found {0}", uncommittedEvents.Count());
    }

    public static void ShouldHaveRaisedEventCount(
        this AggregateRoot aggregate,
        int expectedCount)
    {
        var uncommittedEvents = aggregate.GetUncommittedEvents();
        uncommittedEvents.Should().HaveCount(expectedCount, "Expected {0} events to be raised", expectedCount);
    }

    public static void ShouldNotHaveRaisedEvent<TEvent>(this AggregateRoot aggregate)
        where TEvent : class
    {
        var uncommittedEvents = aggregate.GetUncommittedEvents();
        var @event = uncommittedEvents.OfType<TEvent>().FirstOrDefault();

        @event.Should().BeNull($"Expected no event of type {typeof(TEvent).Name}, but it was raised");
    }

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
