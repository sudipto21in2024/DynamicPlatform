using System;
using System.IO;
using Platform.Engine.Models;
using Scriban;

namespace Platform.Engine.Generators;

public class ConnectorGenerator
{
    private readonly Template _template;

    public ConnectorGenerator()
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Backend", "Connector.scriban");
        
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", "Backend", "Connector.scriban");
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

    public string Generate(ConnectorMetadata metadata)
    {
        if (_template.HasErrors)
        {
            throw new InvalidOperationException("Connector Template has errors: " + string.Join(", ", _template.Messages));
        }

        return _template.Render(new { 
            Name = metadata.Name,
            Namespace = metadata.Namespace,
            Description = metadata.Description,
            Inputs = metadata.Inputs,
            Outputs = metadata.Outputs,
            ConfigProperties = metadata.ConfigProperties,
            BusinessLogic = metadata.BusinessLogic
        }, member => member.Name);
    }
}
