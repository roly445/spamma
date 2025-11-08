namespace Spamma.App.Infrastructure.Contracts;

public record AssertionData(
    string Id,
    string RawId,
    AssertionResponseData Response,
    string Type);