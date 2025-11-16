using FluentAssertions;
using ResultMonad;

namespace Spamma.Tests.Common.Verification;

public static class ResultAssertions
{
    public static T ShouldBeOk<T, TError>(this Result<T, TError> result)
        where TError : class
    {
        result.IsFailure.Should().BeFalse("Expected result to be Ok, but was Failed");
        return result.Value;
    }

    public static void ShouldBeOk<T, TError>(
        this Result<T, TError> result,
        Action<T>? verify = null)
        where TError : class
    {
        result.IsFailure.Should().BeFalse("Expected result to be Ok, but was Failed");
        verify?.Invoke(result.Value);
    }

    public static TError ShouldBeFailed<T, TError>(this Result<T, TError> result)
        where TError : class
    {
        result.IsFailure.Should().BeTrue("Expected result to be Failed, but was Ok");
        return result.Error;
    }

    public static void ShouldBeFailed<T, TError>(
        this Result<T, TError> result,
        Action<TError>? verify = null)
        where TError : class
    {
        result.IsFailure.Should().BeTrue("Expected result to be Failed, but was Ok");
        verify?.Invoke(result.Error);
    }

    public static void ShouldBeOk<TError>(this ResultWithError<TError> result)
        where TError : class
    {
        result.IsFailure.Should().BeFalse("Expected result to be Ok, but was Failed");
    }

    public static TError ShouldBeFailed<TError>(this ResultWithError<TError> result)
        where TError : class
    {
        result.IsFailure.Should().BeTrue("Expected result to be Failed, but was Ok");
        return result.Error;
    }

    public static void ShouldBeFailed<TError>(
        this ResultWithError<TError> result,
        Action<TError>? verify = null)
        where TError : class
    {
        result.IsFailure.Should().BeTrue("Expected result to be Failed, but was Ok");
        verify?.Invoke(result.Error);
    }
}
