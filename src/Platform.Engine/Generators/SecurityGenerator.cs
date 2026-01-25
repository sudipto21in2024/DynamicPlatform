using System;
using System.IO;
using Platform.Engine.Models;
using Scriban;

namespace Platform.Engine.Generators;

public class SecurityGenerator
{
    private readonly Template _template;

    public SecurityGenerator()
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Backend", "SecurityConfig.scriban");
        
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", "Backend", "SecurityConfig.scriban");
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

    public string GenerateXml(SecurityMetadata metadata, AppUserMetadata users)
    {
        if (_template.HasErrors)
        {
            throw new InvalidOperationException("Security Template has errors: " + string.Join(", ", _template.Messages));
        }

        return _template.Render(new { 
            Roles = metadata.Roles,
            Menus = metadata.Menus,
            Users = users.Users
        }, member => member.Name);
    }
}
