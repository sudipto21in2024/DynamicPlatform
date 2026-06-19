using System;
using Platform.Engine.Models;
using Platform.Engine.Services;
using Scriban;

namespace Platform.Engine.Generators;

public class FrontendGenerator
{
    private readonly DashboardGenerator _dashboardGenerator;

    public FrontendGenerator()
    {
        _dashboardGenerator = new DashboardGenerator();
    }

    public GeneratedComponent GenerateNavigation(string projectName, SecurityMetadata security, string styleLibrary)
    {
        var tsContent = TemplateResolver.LoadTemplate("Frontend", styleLibrary, "Navigation.scriban");
        var cssContent = TemplateResolver.LoadTemplate("Frontend", styleLibrary, "Navigation.css.scriban");

        var tsTemplate = Template.Parse(tsContent);
        var cssTemplate = Template.Parse(cssContent);

        if (tsTemplate.HasErrors)
        {
            throw new InvalidOperationException("Navigation TypeScript template has errors: " + string.Join(", ", tsTemplate.Messages));
        }
        if (cssTemplate.HasErrors)
        {
            throw new InvalidOperationException("Navigation CSS template has errors: " + string.Join(", ", cssTemplate.Messages));
        }

        var data = new { 
            ProjectName = projectName,
            Menus = security.Menus
        };

        return new GeneratedComponent
        {
            TypeScript = tsTemplate.Render(data, member => member.Name),
            Css = cssTemplate.Render(data, member => member.Name)
        };
    }

    public string GenerateLoggingService(string level = "INFO", bool enabled = true)
    {
        var tsContent = TemplateResolver.LoadTemplate("Frontend", "Default", "LoggingService.scriban");
        var tsTemplate = Template.Parse(tsContent);

        if (tsTemplate.HasErrors)
        {
            throw new InvalidOperationException("LoggingService template has errors: " + string.Join(", ", tsTemplate.Messages));
        }

        return tsTemplate.Render(new { 
            LogLevel = level,
            IsLoggingEnabled = enabled.ToString().ToLower()
        }, member => member.Name);
    }

    public GeneratedComponent GenerateDashboard(PageMetadata page, string styleLibrary)
    {
        return _dashboardGenerator.Generate(page, styleLibrary);
    }
}
