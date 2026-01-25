using System;
using System.IO;
using Platform.Engine.Models;
using Scriban;

namespace Platform.Engine.Generators;

public class DashboardGenerator
{
    private readonly Template _template;

    public DashboardGenerator()
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Frontend", "DashboardComponent.scriban");
        
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", "Frontend", "DashboardComponent.scriban");
        }

        if (File.Exists(templatePath))
        {
            var content = File.ReadAllText(templatePath);
            _template = Template.Parse(content);
        }
        else 
        {
            throw new FileNotFoundException($"Template not found at {templatePath}");
        }
    }

    public string Generate(PageMetadata metadata)
    {
        if (_template.HasErrors)
        {
            throw new InvalidOperationException("Template has errors: " + string.Join(", ", _template.Messages));
        }

        return _template.Render(new { 
            Name = metadata.Name,
            NameLowered = metadata.Name.ToLower(),
            Route = metadata.Route,
            Widgets = metadata.Widgets
        }, member => member.Name);
    }
}
