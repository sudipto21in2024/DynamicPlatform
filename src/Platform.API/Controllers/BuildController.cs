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
        }

        // 2. Normalize Relations (Handle M:N by creating middle entities)
        var entities = _relationService.Normalize(initialEntities);

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // 3. Generate Infrastructure (Full Code Export)
            var apiNamespace = $"{baseNamespace}.API";
            
            // .csproj
            var csprojCode = _projectGen.GenerateCsproj(apiNamespace, workflows.Any());
            AddFileToZip(archive, $"{apiNamespace}/{apiNamespace}.csproj", csprojCode);

            // Program.cs (Pass both entities and connectors for DI registration)
            var programCode = _projectGen.GenerateProgram(apiNamespace, entities, connectors, workflows);
            AddFileToZip(archive, $"{apiNamespace}/Program.cs", programCode);

            foreach (var workflow in workflows)
            {
                AddFileToZip(archive, $"{apiNamespace}/Workflows/{workflow.Name}.json", workflow.DefinitionJson);
            }

            foreach (var connector in connectors)
            {
                var connectorCode = _connectorGen.Generate(connector);
                AddFileToZip(archive, $"{apiNamespace}/Connectors/{connector.Name}Connector.cs", connectorCode);
            }

            foreach (var entity in entities)
            {
                // 4. Generate Entity
                entity.Namespace = $"{apiNamespace}.Entities";
                var entityCode = _entityGen.Generate(entity);
                AddFileToZip(archive, $"{apiNamespace}/Entities/{entity.Name}.cs", entityCode);

                // 5. Generate Repository
                entity.Namespace = $"{apiNamespace}.Repositories";
                var repoCode = _repoGen.Generate(entity);
                AddFileToZip(archive, $"{apiNamespace}/Repositories/{entity.Name}Repository.cs", repoCode);

                // 6. Generate Controller
                entity.Namespace = $"{apiNamespace}.Controllers";
                var controllerCode = _controllerGen.Generate(entity);
                AddFileToZip(archive, $"{apiNamespace}/Controllers/{entity.Name}Controller.cs", controllerCode);

                // 7. Generate Frontend Component
                var frontendCode = _frontendGen.Generate(entity);
                AddFileToZip(archive, $"Frontend/src/app/pages/{entity.Name.ToLower()}/{entity.Name.ToLower()}.component.ts", frontendCode);
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
