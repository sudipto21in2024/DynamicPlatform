using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Platform.Core.Domain.Entities;
using Platform.Engine.Models;
using Scriban;

namespace Platform.Engine.Generators;

/// <summary>
/// Exporter for generating Node.js NestJS / TypeORM backends.
/// </summary>
public class NodeNestGenerator : ILanguageGenerator
{
    private readonly Template _packageTemplate;
    private readonly Template _tsconfigTemplate;
    private readonly Template _mainTemplate;
    private readonly Template _appModuleTemplate;
    private readonly Template _entityTemplate;
    private readonly Template _serviceTemplate;
    private readonly Template _controllerTemplate;
    private readonly Template _moduleTemplate;
    private readonly Template _dockerfileTemplate;

    public TargetLanguage Language => TargetLanguage.NodeJS;

    public NodeNestGenerator()
    {
        _packageTemplate = LoadTemplate("package.json.scriban");
        _tsconfigTemplate = LoadTemplate("tsconfig.json.scriban");
        _mainTemplate = LoadTemplate("main.ts.scriban");
        _appModuleTemplate = LoadTemplate("app.module.ts.scriban");
        _entityTemplate = LoadTemplate("entity.ts.scriban");
        _serviceTemplate = LoadTemplate("service.ts.scriban");
        _controllerTemplate = LoadTemplate("controller.ts.scriban");
        _moduleTemplate = LoadTemplate("module.ts.scriban");
        _dockerfileTemplate = LoadTemplate("Dockerfile.scriban");
    }

    private Template LoadTemplate(string fileName)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Backend", "Node", fileName);
        
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", "Backend", "Node", fileName);
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

    public void GenerateBackend(
        ZipArchive archive,
        Project? project,
        List<EntityMetadata> entities,
        List<ConnectorMetadata> connectors,
        List<WorkflowMetadata> workflows,
        SecurityMetadata? security,
        AppUserMetadata? users,
        List<CustomObjectMetadata> customObjects,
        List<EnumMetadata> enums,
        List<FormMetadata> forms,
        List<PageMetadata> pages,
        BuildOptions options)
    {
        var projectName = project?.Name ?? "NestJS App";
        var baseName = projectName.Replace(" ", "").ToLower();

        // 1. Generate package.json
        var packageCode = _packageTemplate.Render(new {
            ProjectName = baseName
        }, member => member.Name);
        AddFileToZip(archive, "package.json", packageCode);

        // 2. Generate tsconfig.json
        var tsconfigCode = _tsconfigTemplate.Render(new { }, member => member.Name);
        AddFileToZip(archive, "tsconfig.json", tsconfigCode);

        // 3. Generate Dockerfile
        var dockerfileCode = _dockerfileTemplate.Render(new { }, member => member.Name);
        AddFileToZip(archive, "Dockerfile", dockerfileCode);

        // 4. Generate main.ts
        var mainCode = _mainTemplate.Render(new { }, member => member.Name);
        AddFileToZip(archive, "src/main.ts", mainCode);

        // 5. Generate app.module.ts
        var appModuleCode = _appModuleTemplate.Render(new {
            Entities = entities
        }, member => member.Name);
        AddFileToZip(archive, "src/app.module.ts", appModuleCode);

        // 6. Generate entities, services, controllers, modules
        foreach (var entity in entities)
        {
            var mappedFields = entity.Fields.Select(f => new {
                f.Name,
                f.IsRequired,
                f.MaxLength,
                f.Rules,
                TsType = MapToTypeScriptType(f.CsharpType)
            }).ToList();

            var entityCode = _entityTemplate.Render(new {
                entity.Name,
                Fields = mappedFields,
                entity.Relations
            }, member => member.Name);
            AddFileToZip(archive, $"src/entities/{entity.Name.ToLower()}.entity.ts", entityCode);

            var serviceCode = _serviceTemplate.Render(entity, member => member.Name);
            AddFileToZip(archive, $"src/services/{entity.Name.ToLower()}.service.ts", serviceCode);

            var controllerCode = _controllerTemplate.Render(entity, member => member.Name);
            AddFileToZip(archive, $"src/controllers/{entity.Name.ToLower()}.controller.ts", controllerCode);

            var moduleCode = _moduleTemplate.Render(entity, member => member.Name);
            AddFileToZip(archive, $"src/modules/{entity.Name.ToLower()}.module.ts", moduleCode);
        }
    }

    private string MapToTypeScriptType(string csharpType)
    {
        return csharpType switch
        {
            "Guid" => "string",
            "string" => "string",
            "int" => "number",
            "long" => "number",
            "bool" => "boolean",
            "double" => "number",
            "decimal" => "number",
            "DateTime" => "Date",
            _ => "any"
        };
    }

    private void AddFileToZip(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
