using System.ComponentModel;

namespace Spamma.Modules.UserManagement.Client.Contracts;

[Flags]
public enum SystemRole
{
    [Description("User Management")]
    UserManagement = 1,
    [Description("Domain Management")]
    DomainManagement = 2,
}