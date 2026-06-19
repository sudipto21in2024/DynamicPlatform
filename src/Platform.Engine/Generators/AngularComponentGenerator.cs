using System;
using Platform.Engine.Models;
using Platform.Engine.Services;
using Scriban;

namespace Platform.Engine.Generators;

public class AngularComponentGenerator
{
    public AngularComponentGenerator()
    {
    }

    public GeneratedComponent Generate(EntityMetadata metadata, string styleLibrary)
    {
        var tsContent = TemplateResolver.LoadTemplate("Frontend", styleLibrary, "AngularComponent.scriban");
        var cssContent = TemplateResolver.LoadTemplate("Frontend", styleLibrary, "AngularComponent.css.scriban");

        var tsTemplate = Template.Parse(tsContent);
        var cssTemplate = Template.Parse(cssContent);

        if (tsTemplate.HasErrors)
        {
            throw new InvalidOperationException("List component TypeScript template has errors: " + string.Join(", ", tsTemplate.Messages));
        }
        if (cssTemplate.HasErrors)
        {
            throw new InvalidOperationException("List component CSS template has errors: " + string.Join(", ", cssTemplate.Messages));
        }

        var data = new { 
            Name = metadata.Name,
            Namespace = metadata.Namespace,
            Fields = metadata.Fields,
            Relations = metadata.Relations
        };

        return new GeneratedComponent
        {
            TypeScript = tsTemplate.Render(data, member => member.Name),
            Css = cssTemplate.Render(data, member => member.Name)
        };
    }
}
