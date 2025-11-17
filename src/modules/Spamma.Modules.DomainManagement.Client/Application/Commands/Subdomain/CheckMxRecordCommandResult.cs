using BluQube.Commands;
using Spamma.Modules.DomainManagement.Client.Contracts;

namespace Spamma.Modules.DomainManagement.Client.Application.Commands.Subdomain;

public record CheckMxRecordCommandResult(MxStatus MxStatus, DateTime LastCheckedAt) : ICommandResult;