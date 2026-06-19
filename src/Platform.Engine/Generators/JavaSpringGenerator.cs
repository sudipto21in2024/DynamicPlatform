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
/// Exporter for generating Java Spring Boot / Hibernate backends.
/// </summary>
public class JavaSpringGenerator : ILanguageGenerator
{
    private readonly Template _pomTemplate;
    private readonly Template _appTemplate;
    private readonly Template _entityTemplate;
    private readonly Template _repoTemplate;
    private readonly Template _controllerTemplate;
    private readonly Template _propertiesTemplate;
    private readonly Template _dockerfileTemplate;

    public TargetLanguage Language => TargetLanguage.Java;

    public JavaSpringGenerator()
    {
        _pomTemplate = LoadTemplate("PomXml.scriban");
        _appTemplate = LoadTemplate("Application.scriban");
        _entityTemplate = LoadTemplate("Entity.scriban");
        _repoTemplate = LoadTemplate("Repository.scriban");
        _controllerTemplate = LoadTemplate("Controller.scriban");
        _propertiesTemplate = LoadTemplate("ApplicationProperties.scriban");
        _dockerfileTemplate = LoadTemplate("Dockerfile.scriban");
    }

    private Template LoadTemplate(string fileName)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Backend", "Java", fileName);
        
        if (!File.Exists(templatePath))
        {
             templatePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Platform.Engine", "Templates", "Backend", "Java", fileName);
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
        var baseNamespace = project?.Name.Replace(" ", "").ToLower() ?? "generatedapp";
        var packageName = $"com.generated.{baseNamespace}";
        var packagePath = packageName.Replace(".", "/");

        // 1. Generate pom.xml
        var pomCode = _pomTemplate.Render(new {
            ArtifactId = baseNamespace,
            ProjectName = project?.Name ?? "Generated App"
        }, member => member.Name);
        AddFileToZip(archive, "pom.xml", pomCode);

        // 2. Generate Dockerfile
        var dockerfileCode = _dockerfileTemplate.Render(new { }, member => member.Name);
        AddFileToZip(archive, "Dockerfile", dockerfileCode);

        // 3. Generate application.properties
        var dbConfig = ParseConnectionString(project?.IsolatedConnectionString);
        var propertiesCode = _propertiesTemplate.Render(new {
            ConnectionUrl = dbConfig.Url,
            Username = dbConfig.Username,
            Password = dbConfig.Password,
            IsSqlServer = dbConfig.IsSqlServer
        }, member => member.Name);
        AddFileToZip(archive, "src/main/resources/application.properties", propertiesCode);

        // 4. Generate Main Application Starter
        var appCode = _appTemplate.Render(new {
            PackageName = packageName
        }, member => member.Name);
        AddFileToZip(archive, $"src/main/java/{packagePath}/Application.java", appCode);

        // Map C# types to Java types for entities
        var mappedEntities = entities.Select(e => new {
            e.Name,
            PackageName = packageName,
            Fields = e.Fields.Select(f => new {
                f.Name,
                f.IsRequired,
                f.MaxLength,
                f.Rules,
                JavaType = MapToJavaType(f.CsharpType)
            }).ToList(),
            e.Relations
        }).ToList();

        // 5. Generate Entities, Repositories, and Controllers
        foreach (var entity in mappedEntities)
        {
            var entityCode = _entityTemplate.Render(entity, member => member.Name);
            AddFileToZip(archive, $"src/main/java/{packagePath}/entities/{entity.Name}.java", entityCode);

            var repoCode = _repoTemplate.Render(new {
                PackageName = packageName,
                Name = entity.Name
            }, member => member.Name);
            AddFileToZip(archive, $"src/main/java/{packagePath}/repositories/{entity.Name}Repository.java", repoCode);

            var controllerCode = _controllerTemplate.Render(new {
                PackageName = packageName,
                Name = entity.Name
            }, member => member.Name);
            AddFileToZip(archive, $"src/main/java/{packagePath}/controllers/{entity.Name}Controller.java", controllerCode);
        }
    }

    private string MapToJavaType(string csharpType)
    {
        return csharpType switch
        {
            "Guid" => "java.util.UUID",
            "string" => "String",
            "int" => "Integer",
            "long" => "Long",
            "bool" => "Boolean",
            "double" => "Double",
            "decimal" => "java.math.BigDecimal",
            "DateTime" => "java.time.LocalDateTime",
            _ => "String"
        };
    }

    private (string Url, string Username, string Password, bool IsSqlServer) ParseConnectionString(string? connStr)
    {
        if (string.IsNullOrEmpty(connStr))
        {
            return ("jdbc:h2:mem:db;DB_CLOSE_DELAY=-1", "sa", "", false);
        }

        if (connStr.Contains("Server=") || connStr.Contains("Database=") || connStr.Contains("User Id="))
        {
            var parts = connStr.Split(';')
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim().ToLower(), p => p[1].Trim());

            parts.TryGetValue("server", out var server);
            parts.TryGetValue("database", out var database);
            parts.TryGetValue("user id", out var user);
            parts.TryGetValue("password", out var pwd);

            if (server != null && database != null)
            {
                if (!server.Contains(",")) server += ",1433";
                var hostPort = server.Replace(",", ":");
                var jdbcUrl = $"jdbc:sqlserver://{hostPort};databaseName={database};encrypt=true;trustServerCertificate=true;";
                return (jdbcUrl, user ?? "", pwd ?? "", true);
            }
        }

        return ("jdbc:h2:mem:db;DB_CLOSE_DELAY=-1", "sa", "", false);
    }

    private void AddFileToZip(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
