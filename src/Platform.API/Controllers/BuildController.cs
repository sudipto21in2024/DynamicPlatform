using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Platform.Core.Domain.Entities;
using Platform.Core.Interfaces;
using Platform.Engine.Generators;
using Platform.Engine.Models;
using Platform.Engine.Services;

namespace Platform.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/build")]
public class BuildController : ControllerBase
{
    private readonly IArtifactRepository _repo;
    private readonly EntityGenerator _entityGen;
    private readonly DbContextGenerator _dbGen;
    private readonly RepositoryGenerator _repoGen;
    private readonly ControllerGenerator _controllerGen;
    private readonly ProjectGenerator _projectGen;
    private readonly ConnectorGenerator _connectorGen;
    private readonly SecurityGenerator _securityGen;
    private readonly FrontendGenerator _frontendLayoutGen;
    private readonly AngularComponentGenerator _frontendGen;
    private readonly CustomObjectGenerator _customObjectGen;
    private readonly EnumGenerator _enumGen;
    private readonly MetadataLoader _loader;
    private readonly RelationNormalizationService _relationService;

    public BuildController(
        IArtifactRepository repo, 
        EntityGenerator entityGen, 
        DbContextGenerator dbGen, 
        RepositoryGenerator repoGen,
        ControllerGenerator controllerGen,
        ProjectGenerator projectGen,
        ConnectorGenerator connectorGen,
        SecurityGenerator securityGen,
        FrontendGenerator frontendLayoutGen,
        AngularComponentGenerator frontendGen,
        CustomObjectGenerator customObjectGen,
        EnumGenerator enumGen,
        MetadataLoader loader,
        RelationNormalizationService relationService)
    {
        _repo = repo;
        _entityGen = entityGen;
        _dbGen = dbGen;
        _repoGen = repoGen;
        _controllerGen = controllerGen;
        _projectGen = projectGen;
        _connectorGen = connectorGen;
        _securityGen = securityGen;
        _frontendLayoutGen = frontendLayoutGen;
        _frontendGen = frontendGen;
        _customObjectGen = customObjectGen;
        _enumGen = enumGen;
        _loader = loader;
        _relationService = relationService;
    }

    [HttpPost]
    public async Task<IActionResult> BuildProject(Guid projectId)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        var initialEntities = new List<EntityMetadata>();
        var connectors = new List<ConnectorMetadata>();
        var workflows = new List<WorkflowMetadata>();
        SecurityMetadata? security = null;
        AppUserMetadata? users = null;
        var pages = new List<PageMetadata>();
        var customObjects = new List<CustomObjectMetadata>();
        var enums = new List<EnumMetadata>();
        var project = await _repo.GetProjectByIdAsync(projectId);
        var baseNamespace = project?.Name.Replace(" ", "") ?? "GeneratedApp";

        // 1. Load Metadata
        foreach (var artifact in artifacts)
        {
            if (artifact.Type == ArtifactType.Entity)
            {
                var metadata = _loader.LoadEntityMetadata(artifact);
                if (metadata != null)
                {
                    metadata.Namespace = metadata.Namespace ?? $"{baseNamespace}.Entities";
                    initialEntities.Add(metadata);
                }
            }
            else if (artifact.Type == ArtifactType.Connector)
            {
                var connMetadata = _loader.LoadConnectorMetadata(artifact);
                if (connMetadata != null)
                {
                    connMetadata.Namespace = connMetadata.Namespace ?? $"{baseNamespace}.Connectors";
                    connectors.Add(connMetadata);
                }
            }
            else if (artifact.Type == ArtifactType.SecurityConfig)
            {
                security = _loader.LoadSecurityMetadata(artifact);
            }
            else if (artifact.Type == ArtifactType.UsersConfig)
            {
                users = _loader.LoadAppUserMetadata(artifact);
            }
            else if (artifact.Type == ArtifactType.Workflow)
            {
                var wfMetadata = _loader.LoadWorkflowMetadata(artifact);
                if (wfMetadata != null) workflows.Add(wfMetadata);
            }
            else if (artifact.Type == ArtifactType.Page)
            {
                var pageMetadata = _loader.LoadPageMetadata(artifact);
                if (pageMetadata != null) pages.Add(pageMetadata);
            }
            else if (artifact.Type == ArtifactType.CustomObject)
            {
                var coMetadata = _loader.LoadCustomObjectMetadata(artifact);
                if (coMetadata != null) customObjects.Add(coMetadata);
            }
            else if (artifact.Type == ArtifactType.Enum)
            {
                var enumMetadata = _loader.LoadEnumMetadata(artifact);
                if (enumMetadata != null)
                {
                    enumMetadata.Namespace = enumMetadata.Namespace ?? $"{baseNamespace}.Entities";
                    enums.Add(enumMetadata);
                }
            }
        }

        // 2. Normalize Relations (Handle M:N by creating middle entities)
        var entities = _relationService.Normalize(initialEntities);

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // 3. Generate Infrastructure (Full Code Export)
            var apiNamespace = $"{baseNamespace}.API";
            
            // ... (existing generation logic)

            foreach (var entity in entities)
            {
                // ... (generation for entities)
            }

            foreach (var co in customObjects)
            {
                var coCode = _customObjectGen.Generate(co);
                AddFileToZip(archive, $"{apiNamespace}/Models/{co.Name}.cs", coCode);
            }

            foreach (var @enum in enums)
            {
                var enumCode = _enumGen.Generate(@enum);
                AddFileToZip(archive, $"{apiNamespace}/Entities/{@enum.Name}.cs", enumCode);
            }

            // 7. Generate Dashboards/Pages
            foreach (var page in pages)
            {
                var pageCode = _frontendLayoutGen.GenerateDashboard(page);
                AddFileToZip(archive, $"Frontend/src/app/pages/dashboards/{page.Name.ToLower()}.component.ts", pageCode);
            }

            // 7b. Generate Layout/Navigation (Reflecting Security Features)
            var navCode = _frontendLayoutGen.GenerateNavigation(baseNamespace, security ?? new SecurityMetadata());
            AddFileToZip(archive, "Frontend/src/app/components/navigation/navigation.component.ts", navCode);

            // 7c. Generate UI Logging Service
            var logServiceCode = _frontendLayoutGen.GenerateLoggingService("DEBUG", true);
            AddFileToZip(archive, "Frontend/src/app/services/logging.service.ts", logServiceCode);

            // 8. Generate DbContext
            if (entities.Any())
            {
                var dbNamespace = $"{apiNamespace}.Data";
                var dbCode = _dbGen.Generate(dbNamespace, entities);
                AddFileToZip(archive, $"{apiNamespace}/Data/GeneratedDbContext.cs", dbCode);
            }

            // 9. Add appsettings.json for Data Isolation
            if (project != null)
            {
                var appSettings = $@"{{
  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""{project.IsolatedConnectionString}""
  }},
  ""Serilog"": {{
    ""MinimumLevel"": {{
      ""Default"": ""Information"",
      ""Override"": {{
        ""Microsoft"": ""Warning"",
        ""System"": ""Warning""
      }}
    }},
    ""WriteTo"": [
      {{ ""Name"": ""Console"" }},
      {{
        ""Name"": ""File"",
        ""Args"": {{
          ""path"": ""logs/log-.txt"",
          ""rollingInterval"": ""Day""
        }}
      }}
    ]
  }},
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }}
}}";
                AddFileToZip(archive, $"{apiNamespace}/appsettings.json", appSettings);
            }

            // 10. Add Standalone Dockerfile for the exported app
            var standaloneDockerfile = $@"FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish {apiNamespace}/{apiNamespace}.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT [""dotnet"", ""{apiNamespace}.dll""]";
            
            AddFileToZip(archive, "Dockerfile", standaloneDockerfile);

            // 11. Add Azure Deployment Script
            if (project != null)
            {
                var azureDeployScript = _projectGen.GenerateAzureDeploy(project.Name, project.IsolatedConnectionString ?? "", project.Id.ToString());
                AddFileToZip(archive, "deploy-azure.ps1", azureDeployScript);

                var azureReadme = _projectGen.GenerateAzureReadme();
                AddFileToZip(archive, "README_AZURE.md", azureReadme);
            }

            // 12. Add Security Configuration (XML)
            if (security != null)
            {
                var securityXml = _securityGen.GenerateXml(security, users ?? new AppUserMetadata());
                AddFileToZip(archive, $"{apiNamespace}/security.xml", securityXml);
            }
        }

        memoryStream.Position = 0;
        return File(memoryStream.ToArray(), "application/zip", $"{baseNamespace}_Standalone_Export.zip");
    }

    [HttpPost("publish")]
    public IActionResult PublishProject(Guid projectId)
    {
        // This is a stub for the shared environment publishing logic.
        // In a real scenario, this would trigger a CI/CD pipeline or 
        // deploy the container to a shared K8s/AppService cluster.
        return Ok(new { message = "Project scheduled for publication to the shared environment." });
    }

    private void AddFileToZip(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
