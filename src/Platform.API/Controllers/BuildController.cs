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
    private readonly MetadataLoader _loader;
    private readonly RelationNormalizationService _relationService;
    private readonly LanguageGeneratorFactory _generatorFactory;

    public BuildController(
        IArtifactRepository repo, 
        MetadataLoader loader,
        RelationNormalizationService relationService,
        LanguageGeneratorFactory generatorFactory)
    {
        _repo = repo;
        _loader = loader;
        _relationService = relationService;
        _generatorFactory = generatorFactory;
    }

    [HttpPost]
    public async Task<IActionResult> BuildProject(Guid projectId, [FromBody] BuildOptions? options = null)
    {
        options ??= new BuildOptions();
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        var initialEntities = new List<EntityMetadata>();
        var connectors = new List<ConnectorMetadata>();
        var workflows = new List<WorkflowMetadata>();
        SecurityMetadata? security = null;
        AppUserMetadata? users = null;
        var pages = new List<PageMetadata>();
        var customObjects = new List<CustomObjectMetadata>();
        var enums = new List<EnumMetadata>();
        var forms = new List<FormMetadata>();
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
            else if (artifact.Type == ArtifactType.Form)
            {
                var formMetadata = _loader.LoadFormMetadata(artifact);
                if (formMetadata != null) forms.Add(formMetadata);
            }
        }

        // 2. Normalize Relations (Handle M:N by creating middle entities)
        var entities = _relationService.Normalize(initialEntities);

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Resolve generator based on chosen target language
            var generator = _generatorFactory.GetGenerator(options.Language);
            generator.GenerateBackend(
                archive,
                project,
                entities,
                connectors,
                workflows,
                security,
                users,
                customObjects,
                enums,
                forms,
                pages,
                options);
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
