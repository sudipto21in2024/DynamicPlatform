using System;

namespace Platform.Engine.Models;

/// <summary>
/// Configuration options for the project build and export process.
/// </summary>
public class BuildOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to include the frontend UI in the generated project.
    /// </summary>
    public bool IncludeUI { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to generate AI-optimized documentation and manifests.
    /// </summary>
    public bool EnableAIEnabledDocs { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the project should be treated as a standalone API service.
    /// </summary>
    public bool StandaloneAPI { get; set; } = false;

    /// <summary>
    /// Gets or sets the target environment (e.g., "Production", "Development").
    /// </summary>
    public string Environment { get; set; } = "Development";
}
