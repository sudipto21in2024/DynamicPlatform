using System;
using System.IO;
using System.Threading.Tasks;
using Platform.Engine.Models;
using Scriban;

namespace Platform.Engine.Generators;

public class EntityGenerator
{
    private readonly Template _template;

    public EntityGenerator()
    {
        // Load template from embedded resource or file system
        // For MVP, we presume file system relative path
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Backend", "Entity.scriban");
        
        // Fallback for dev environment if not copied to bin
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", "Backend", "Entity.scriban");
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
            Relations = metadata.Relations
        }, member => member.Name);
    }
}
