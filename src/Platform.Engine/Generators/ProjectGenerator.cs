using System;
using System.IO;
using System.Collections.Generic;
using Platform.Engine.Models;
using Scriban;

namespace Platform.Engine.Generators;

public class ProjectGenerator
{
    private readonly Template _csprojTemplate;
    private readonly Template _programTemplate;
    private readonly Template _azureDeployTemplate;
    private readonly Template _azureReadmeTemplate;

    public ProjectGenerator()
    {
        _csprojTemplate = LoadTemplate("Csproj.scriban");
        _programTemplate = LoadTemplate("Program.scriban");
        _azureDeployTemplate = LoadTemplate("AzureDeploy.scriban");
        _azureReadmeTemplate = LoadTemplate("AzureReadme.scriban");
    }

    private Template LoadTemplate(string fileName)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Backend", fileName);
        
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", "Backend", fileName);
        }

        if (File.Exists(templatePath))
        {
            var content = File.ReadAllText(templatePath);
            var template = Template.Parse(content);
            if (template.HasErrors)
            {
                throw new InvalidOperationException($"Template {fileName} has errors: " + string.Join(", ", template.Messages));
            }
            return template;
        }
        
        throw new FileNotFoundException($"Template not found at {templatePath}");
    }

    public string GenerateCsproj(string ns, bool hasWorkflows)
    {
        return _csprojTemplate.Render(new { Namespace = ns, HasWorkflows = hasWorkflows }, member => member.Name);
    }

    public string GenerateProgram(string ns, List<EntityMetadata> entities, List<ConnectorMetadata> connectors, List<WorkflowMetadata> workflows)
    {
        return _programTemplate.Render(new { 
            Namespace = ns,
            Entities = entities,
            Connectors = connectors,
            Workflows = workflows
        }, member => member.Name);
    }

    public string GenerateAzureDeploy(string name, string connectionString, string uniqueId)
    {
        return _azureDeployTemplate.Render(new { 
            Name = name,
            NameTrimmed = name.Replace(" ", "").ToLower(),
            ConnectionString = connectionString,
            UniqueId = uniqueId.Substring(0, 8)
        }, member => member.Name);
    }

    public string GenerateAzureReadme()
    {
        return _azureReadmeTemplate.Render(new { }, member => member.Name);
    }
}
