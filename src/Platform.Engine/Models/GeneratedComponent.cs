namespace Platform.Engine.Models;

/// <summary>
/// Holds the generated files for a single component: the TypeScript file and its accompanying independent CSS file.
/// </summary>
public class GeneratedComponent
{
    public string TypeScript { get; set; } = string.Empty;
    public string Css { get; set; } = string.Empty;
}
