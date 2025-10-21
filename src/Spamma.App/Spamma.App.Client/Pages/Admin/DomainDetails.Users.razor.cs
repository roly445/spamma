using Spamma.App.Client.Components.UserControls;

namespace Spamma.App.Client.Pages.Admin;

/// <summary>
/// Backing code for the domain details page - user functionality.
/// </summary>
public partial class DomainDetails
{
    private AddUser? _addModerator;

    private Task HandleModeratorAdded()
    {
        return this.LoadAsync();
    }

    private void OpenAssignUserModal()
    {
        this._addModerator?.Open();
    }
}