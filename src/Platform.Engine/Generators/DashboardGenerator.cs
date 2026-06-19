using System;
using Platform.Engine.Models;
using Platform.Engine.Services;
using Scriban;

namespace Platform.Engine.Generators;

public class DashboardGenerator
{
    public DashboardGenerator()
    {
    }

    public GeneratedComponent Generate(PageMetadata metadata, string styleLibrary)
    {
        var tsContent = TemplateResolver.LoadTemplate("Frontend", styleLibrary, "DashboardComponent.scriban");
        var cssContent = TemplateResolver.LoadTemplate("Frontend", styleLibrary, "DashboardComponent.css.scriban");

        var tsTemplate = Template.Parse(tsContent);
        var cssTemplate = Template.Parse(cssContent);

        if (tsTemplate.HasErrors)
        {
            throw new InvalidOperationException("Dashboard TypeScript template has errors: " + string.Join(", ", tsTemplate.Messages));
        }
        if (cssTemplate.HasErrors)
        {
            throw new InvalidOperationException("Dashboard CSS template has errors: " + string.Join(", ", cssTemplate.Messages));
        }

        var data = new { 
            Name = metadata.Name,
            NameLowered = metadata.Name.ToLower(),
            Route = metadata.Route,
            Widgets = metadata.Widgets
        };

        return new GeneratedComponent
        {
            TypeScript = tsTemplate.Render(data, member => member.Name),
            Css = cssTemplate.Render(data, member => member.Name)
        };
    }
}
