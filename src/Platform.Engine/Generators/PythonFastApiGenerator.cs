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
/// Exporter for generating Python FastAPI backends.
/// </summary>
public class PythonFastApiGenerator : ILanguageGenerator
{
    private readonly Template _pyprojectTemplate;
    private readonly Template _mainTemplate;
    private readonly Template _databaseTemplate;
    private readonly Template _modelsTemplate;
    private readonly Template _routersTemplate;
    private readonly Template _dockerfileTemplate;

    public TargetLanguage Language => TargetLanguage.Python;

    public PythonFastApiGenerator()
    {
        _pyprojectTemplate = LoadTemplate("pyproject.toml.scriban");
        _mainTemplate = LoadTemplate("main.py.scriban");
        _databaseTemplate = LoadTemplate("database.py.scriban");
        _modelsTemplate = LoadTemplate("models.py.scriban");
        _routersTemplate = LoadTemplate("routers.py.scriban");
        _dockerfileTemplate = LoadTemplate("Dockerfile.scriban");
    }

    private Template LoadTemplate(string fileName)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Backend", "Python", fileName);
        
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", "Backend", "Python", fileName);
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
        var projectName = project?.Name ?? "FastAPI App";
        var baseName = projectName.Replace(" ", "").ToLower();

        // 1. Generate pyproject.toml
        var pyprojectCode = _pyprojectTemplate.Render(new {
            ProjectName = baseName
        }, member => member.Name);
        AddFileToZip(archive, "pyproject.toml", pyprojectCode);

        // 2. Generate Dockerfile
        var dockerfileCode = _dockerfileTemplate.Render(new { }, member => member.Name);
        AddFileToZip(archive, "Dockerfile", dockerfileCode);

        // 3. Generate main.py
        var mainCode = _mainTemplate.Render(new {
            ProjectName = projectName,
            Entities = entities
        }, member => member.Name);
        AddFileToZip(archive, "app/main.py", mainCode);

        // 4. Generate database.py
        var dbCode = _databaseTemplate.Render(new {
            ConnectionUrl = "" // Handled fallback to sqlite in database.py
        }, member => member.Name);
        AddFileToZip(archive, "app/database.py", dbCode);

        // 5. Generate app/models.py containing all SQLModels
        var modelsCode = string.Join("\n\n", entities.Select(e => {
            var mappedFields = e.Fields.Select(f => new {
                f.Name,
                f.IsRequired,
                f.MaxLength,
                f.Rules,
                PythonType = MapToPythonType(f.CsharpType)
            }).ToList();

            return _modelsTemplate.Render(new {
                e.Name,
                Fields = mappedFields,
                e.Relations
            }, member => member.Name);
        }));
        AddFileToZip(archive, "app/models.py", modelsCode);

        // 6. Generate app/routers for each entity
        foreach (var entity in entities)
        {
            var routerCode = _routersTemplate.Render(entity, member => member.Name);
            AddFileToZip(archive, $"app/routers/{entity.Name.ToLower()}.py", routerCode);
        }

        // Add __init__.py files to establish packages
        AddFileToZip(archive, "app/__init__.py", "");
        AddFileToZip(archive, "app/routers/__init__.py", "");
    }

    private string MapToPythonType(string csharpType)
    {
        return csharpType switch
        {
            "Guid" => "uuid.UUID",
            "string" => "str",
            "int" => "int",
            "long" => "int",
            "bool" => "bool",
            "double" => "float",
            "decimal" => "float",
            "DateTime" => "datetime",
            _ => "str"
        };
    }

    private void AddFileToZip(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
