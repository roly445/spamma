using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;

namespace Spamma.App.Components;

public partial class App
{
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    private IComponentRenderMode? PageRenderMode =>
        this.HttpContext.AcceptsInteractiveRouting() ? new InteractiveWebAssemblyRenderMode(prerender: false) : null;
}