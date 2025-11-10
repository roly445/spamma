using Microsoft.AspNetCore.Authorization;
using Spamma.Modules.Common.Client;

namespace Spamma.App.Client.Infrastructure.Auth;

public record BitwiseRoleRequirement(SystemRole RequiredRoleValue) : IAuthorizationRequirement;