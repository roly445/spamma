using BluQube.Commands;

namespace Spamma.App.Client.Infrastructure.Contracts.Services;

public interface IErrorMessageMapperService
{
    string GetErrorMessage(CommandResult result);

    string GetErrorMessageForCode(string errorCode);
}