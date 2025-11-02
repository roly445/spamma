using BluQube.Commands;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands;

public record CheckMxRecordCommandResult(MxStatus MxStatus, DateTime WhenChecked) : ICommandResult;