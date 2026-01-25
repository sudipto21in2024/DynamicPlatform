using System;
using System.IO;
using Platform.Engine.Models;
using Scriban;

namespace Platform.Engine.Generators;

public class RepositoryGenerator
{
    private readonly Template _template;

    public RepositoryGenerator()
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Backend", "Repository.scriban");
        
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", "Backend", "Repository.scriban");
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

    public string Generate(EntityMetadata metadata)
    {
        if (_template.HasErrors)
        {
            throw new InvalidOperationException("Template has errors: " + string.Join(", ", _template.Messages));
        }

        return _template.Render(new { 
            Name = metadata.Name,
            Namespace = metadata.Namespace,
            Fields = metadata.Fields,
            Relations = metadata.Relations,
            Events = metadata.Events
        }, member => member.Name);
    }
}
