using System;
using Platform.Engine.Models;
using Platform.Engine.Services;
using Scriban;

namespace Platform.Engine.Generators;

public class FormGenerator
{
    public FormGenerator()
    {
    }

    public string GenerateBackend(FormMetadata metadata, string rootNamespace)
    {
        // Backend templates don't depend on frontend style libraries
        var templateContent = TemplateResolver.LoadTemplate("Backend", "Default", "Form.scriban");
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            throw new InvalidOperationException("Form Backend template has errors: " + string.Join(", ", template.Messages));
        }

        return template.Render(new { 
            name = metadata.Name,
            root_namespace = rootNamespace,
            fields = metadata.Fields,
            sections = metadata.Sections,
            layout = metadata.Layout.ToString(),
            entity_target = metadata.EntityTarget
        }, member => member.Name);
    }

    public GeneratedComponent GenerateFrontend(FormMetadata metadata, string styleLibrary)
    {
        var tsContent = TemplateResolver.LoadTemplate("Frontend", styleLibrary, "FormComponent.scriban");
        var cssContent = TemplateResolver.LoadTemplate("Frontend", styleLibrary, "FormComponent.css.scriban");

        var tsTemplate = Template.Parse(tsContent);
        var cssTemplate = Template.Parse(cssContent);

        if (tsTemplate.HasErrors)
        {
            throw new InvalidOperationException("Form Frontend TypeScript template has errors: " + string.Join(", ", tsTemplate.Messages));
        }
        if (cssTemplate.HasErrors)
        {
            throw new InvalidOperationException("Form Frontend CSS template has errors: " + string.Join(", ", cssTemplate.Messages));
        }

        var data = new { 
            name = metadata.Name,
            fields = metadata.Fields,
            sections = metadata.Sections,
            layout = metadata.Layout.ToString(),
            entity_target = metadata.EntityTarget
        };

        return new GeneratedComponent
        {
            TypeScript = tsTemplate.Render(data, member => member.Name),
            Css = cssTemplate.Render(data, member => member.Name)
        };
    }

    public GeneratedComponent GeneratePremiumFrontend(FormMetadata metadata, string styleLibrary)
    {
        var tsContent = TemplateResolver.LoadTemplate("Frontend", styleLibrary, "PremiumFormComponent.scriban");
        var cssContent = TemplateResolver.LoadTemplate("Frontend", styleLibrary, "PremiumFormComponent.css.scriban");

        var tsTemplate = Template.Parse(tsContent);
        var cssTemplate = Template.Parse(cssContent);

        if (tsTemplate.HasErrors)
        {
            throw new InvalidOperationException("Premium Form Frontend TypeScript template has errors: " + string.Join(", ", tsTemplate.Messages));
        }
        if (cssTemplate.HasErrors)
        {
            throw new InvalidOperationException("Premium Form Frontend CSS template has errors: " + string.Join(", ", cssTemplate.Messages));
        }

        var data = new { 
            name = metadata.Name,
            fields = metadata.Fields,
            sections = metadata.Sections,
            layout = metadata.Layout.ToString(),
            entity_target = metadata.EntityTarget
        };

        return new GeneratedComponent
        {
            TypeScript = tsTemplate.Render(data, member => member.Name),
            Css = cssTemplate.Render(data, member => member.Name)
        };
    }
}
