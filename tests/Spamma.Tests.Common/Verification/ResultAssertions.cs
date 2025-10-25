using FluentAssertions;
using ResultMonad;

namespace Spamma.Tests.Common.Verification;

/// <summary>
/// Verification extensions for Result monad types.
/// Provides fluent API for verifying Result monad returns without assertions.
/// </summary>
public static class ResultAssertions
{
    /// <summary>
    /// Verifies that a Result is in the Ok state and extracts the value.
    /// </summary>
    public static T ShouldBeOk<T, TError>(this Result<T, TError> result)
        where TError : class
    {
        result.IsFailure.Should().BeFalse("Expected result to be Ok, but was Failed");
        return result.Value;
    }

    /// <summary>
    /// Verifies that a Result is in the Ok state with optional custom verification.
    /// </summary>
    public static void ShouldBeOk<T, TError>(
        this Result<T, TError> result,
        Action<T>? verify = null)
        where TError : class
    {
        result.IsFailure.Should().BeFalse("Expected result to be Ok, but was Failed");
        verify?.Invoke(result.Value);
    }

    /// <summary>
    /// Verifies that a Result is in the Failed state.
    /// </summary>
    public static TError ShouldBeFailed<T, TError>(this Result<T, TError> result)
        where TError : class
    {
        result.IsFailure.Should().BeTrue("Expected result to be Failed, but was Ok");
        return result.Error;
    }

    /// <summary>
    /// Verifies that a Result is in the Failed state with optional custom verification.
    /// </summary>
    public static void ShouldBeFailed<T, TError>(
        this Result<T, TError> result,
        Action<TError>? verify = null)
        where TError : class
    {
        result.IsFailure.Should().BeTrue("Expected result to be Failed, but was Ok");
        verify?.Invoke(result.Error);
    }

    /// <summary>
    /// Verifies that a ResultWithError is in the Ok state.
    /// </summary>
    public static void ShouldBeOk<TError>(this ResultWithError<TError> result)
        where TError : class
    {
        result.IsFailure.Should().BeFalse("Expected result to be Ok, but was Failed");
    }

    /// <summary>
    /// Verifies that a ResultWithError is in the Failed state.
    /// </summary>
    public static TError ShouldBeFailed<TError>(this ResultWithError<TError> result)
        where TError : class
    {
        result.IsFailure.Should().BeTrue("Expected result to be Failed, but was Ok");
        return result.Error;
    }

    /// <summary>
    /// Verifies that a ResultWithError is in the Failed state with optional custom verification.
    /// </summary>
    public static void ShouldBeFailed<TError>(
        this ResultWithError<TError> result,
        Action<TError>? verify = null)
        where TError : class
    {
        result.IsFailure.Should().BeTrue("Expected result to be Failed, but was Ok");
        verify?.Invoke(result.Error);
    }
}
