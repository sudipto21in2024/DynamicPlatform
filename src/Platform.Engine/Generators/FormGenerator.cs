using System;
using System.IO;
using Platform.Engine.Models;
using Scriban;

namespace Platform.Engine.Generators;

public class FormGenerator
{
    private readonly Template _backendTemplate;
    private readonly Template _frontendTemplate;

    public FormGenerator()
    {
        _backendTemplate = LoadTemplate("Backend", "Form.scriban");
        _frontendTemplate = LoadTemplate("Frontend", "FormComponent.scriban");
    }

    private Template LoadTemplate(string type, string filename)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", type, filename);
        
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", type, filename);
        }

        if (File.Exists(templatePath))
        {
            var content = File.ReadAllText(templatePath);
            return Template.Parse(content);
        }
        
        throw new FileNotFoundException($"Template not found at {templatePath}");
    }

    public string GenerateBackend(FormMetadata metadata, string rootNamespace)
    {
        return _backendTemplate.Render(new { 
            name = metadata.Name,
            root_namespace = rootNamespace,
            fields = metadata.Fields,
            sections = metadata.Sections,
            layout = metadata.Layout.ToString(),
            entity_target = metadata.EntityTarget
        }, member => member.Name);
    }

    public string GenerateFrontend(FormMetadata metadata)
    {
        return _frontendTemplate.Render(new { 
            name = metadata.Name,
            fields = metadata.Fields,
            sections = metadata.Sections,
            layout = metadata.Layout.ToString(),
            entity_target = metadata.EntityTarget
        }, member => member.Name);
    }
}
