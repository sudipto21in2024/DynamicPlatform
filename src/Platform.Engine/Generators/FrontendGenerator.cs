using System;
using System.IO;
using Platform.Engine.Models;
using Scriban;

namespace Platform.Engine.Generators;

public class FrontendGenerator
{
    private readonly Template _navTemplate;

    public FrontendGenerator()
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Frontend", "Navigation.scriban");
        
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", "Frontend", "Navigation.scriban");
        }

        if (File.Exists(templatePath))
        {
            var content = File.ReadAllText(templatePath);
            _navTemplate = Template.Parse(content);
        }
        else 
        {
            throw new FileNotFoundException($"Template not found at {templatePath}");
        }
    }

    public string GenerateNavigation(string projectName, SecurityMetadata security)
    {
        if (_navTemplate.HasErrors)
        {
            throw new InvalidOperationException("Navigation Template has errors: " + string.Join(", ", _navTemplate.Messages));
        }

        return _navTemplate.Render(new { 
            ProjectName = projectName,
            Menus = security.Menus
        }, member => member.Name);
    }
}
