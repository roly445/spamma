using System.ComponentModel;

namespace Spamma.Modules.Common.Client;

[Flags]
public enum SystemRole
{
    [Description("User Management")]
    UserManagement = 1,
    [Description("Domain Management")]
    DomainManagement = 2,
}