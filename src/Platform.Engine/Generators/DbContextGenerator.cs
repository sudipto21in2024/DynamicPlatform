using System;
using System.IO;
using System.Collections.Generic;
using Platform.Engine.Models;
using Scriban;

namespace Platform.Engine.Generators;

public class DbContextGenerator
{
    private readonly Template _template;

    public DbContextGenerator()
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Backend", "DbContext.scriban");
        
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", "Backend", "DbContext.scriban");
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

    public string Generate(string ns, List<EntityMetadata> entities)
    {
        if (_template.HasErrors)
        {
            throw new InvalidOperationException("Template has errors: " + string.Join(", ", _template.Messages));
        }

        return _template.Render(new { 
            Namespace = ns,
            Entities = entities
        }, member => member.Name);
    }
}
