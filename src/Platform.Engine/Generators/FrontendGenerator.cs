using System;
using System.IO;
using Platform.Engine.Models;
using Scriban;

namespace Platform.Engine.Generators;

public class FrontendGenerator
{
    private readonly Template _navTemplate;
    private readonly Template _logTemplate;
    private readonly DashboardGenerator _dashboardGenerator;

    public FrontendGenerator()
    {
        _navTemplate = LoadTemplate("Navigation.scriban");
        _logTemplate = LoadTemplate("LoggingService.scriban");
        _dashboardGenerator = new DashboardGenerator();
    }

    private Template LoadTemplate(string fileName)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Frontend", fileName);
        
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", "Frontend", fileName);
        }

        if (File.Exists(templatePath))
        {
            var content = File.ReadAllText(templatePath);
            return Template.Parse(content);
        }
        
        throw new FileNotFoundException($"Template not found at {templatePath}");
    }

    public string GenerateNavigation(string projectName, SecurityMetadata security)
    {
        return _navTemplate.Render(new { 
            ProjectName = projectName,
            Menus = security.Menus
        }, member => member.Name);
    }

    public string GenerateLoggingService(string level = "INFO", bool enabled = true)
    {
        return _logTemplate.Render(new { 
            LogLevel = level,
            IsLoggingEnabled = enabled.ToString().ToLower()
        }, member => member.Name);
    }

    public string GenerateDashboard(PageMetadata page)
    {
        return _dashboardGenerator.Generate(page);
    }
}
