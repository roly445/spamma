using Microsoft.AspNetCore.Authorization;
using Spamma.Modules.Common.Client;
using Spamma.Modules.UserManagement.Client.Contracts;

namespace Spamma.App.Client.Infrastructure.Auth;

public record BitwiseRoleRequirement(SystemRole RequiredRoleValue) : IAuthorizationRequirement;