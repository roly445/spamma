using System.CodeDom.Compiler;
using System.Globalization;
using System.Resources;

namespace Spamma.Analyzers;

/// <summary>
/// Resource strings for analyzer diagnostics.
/// </summary>
[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
[System.Diagnostics.DebuggerNonUserCode()]
[System.Runtime.CompilerServices.CompilerGenerated()]
internal class ResourceStrings
{
    private static ResourceManager? _resourceManager;
    private static CultureInfo? _resourceCulture;

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
        get
        {
            if (_resourceManager == null)
            {
                _resourceManager = new ResourceManager(typeof(ResourceStrings).FullName!, typeof(ResourceStrings).Assembly);
            }
            return _resourceManager;
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
    internal static CultureInfo? Culture
    {
        get => _resourceCulture;
        set => _resourceCulture = value;
    }

    
    internal static string? QueryMustHaveValidator =>
        ResourceManager.GetString("QueryMustHaveValidator", _resourceCulture);

    internal static string? QueryMustHaveValidatorMessage =>
        ResourceManager.GetString("QueryMustHaveValidatorMessage", _resourceCulture);

    internal static string? QueryMustHaveValidatorDescription =>
        ResourceManager.GetString("QueryMustHaveValidatorDescription", _resourceCulture);
}
