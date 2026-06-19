using System;
using System.IO;

namespace Platform.Engine.Services;

/// <summary>
/// Dynamically resolves and loads template files based on the project's selected Style Library,
/// with automatic fallback to the Default style template directory if the custom stylesheet or template is missing.
/// </summary>
public static class TemplateResolver
{
    public static string LoadTemplate(string type, string styleLibrary, string filename)
    {
        if (string.IsNullOrWhiteSpace(styleLibrary))
        {
            styleLibrary = "Default";
        }

        // 1. Try selected style library path
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", type, styleLibrary, filename);
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", type, styleLibrary, filename);
        }

        // 2. Fallback to "Default" if it doesn't exist and we aren't already looking for Default
        if (!File.Exists(path) && !styleLibrary.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", type, "Default", filename);
            if (!File.Exists(path))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", type, "Default", filename);
            }
        }

        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }

        throw new FileNotFoundException($"Template file not found: type={type}, style={styleLibrary}, file={filename}");
    }
}
