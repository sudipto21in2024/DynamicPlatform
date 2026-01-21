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
    private readonly AngularComponentGenerator _frontendGen;
    private readonly MetadataLoader _loader;
    private readonly RelationNormalizationService _relationService;

    public BuildController(
        IArtifactRepository repo, 
        EntityGenerator entityGen, 
        DbContextGenerator dbGen, 
        RepositoryGenerator repoGen,
        ControllerGenerator controllerGen,
        AngularComponentGenerator frontendGen,
        MetadataLoader loader,
        RelationNormalizationService relationService)
    {
        _repo = repo;
        _entityGen = entityGen;
        _dbGen = dbGen;
        _repoGen = repoGen;
        _controllerGen = controllerGen;
        _frontendGen = frontendGen;
        _loader = loader;
        _relationService = relationService;
    }

    [HttpPost]
    public async Task<IActionResult> BuildProject(Guid projectId)
    {
        var artifacts = await _repo.GetByProjectIdAsync(projectId);
        var initialEntities = new List<EntityMetadata>();

        // 1. Load Metadata
        foreach (var artifact in artifacts.Where(a => a.Type == ArtifactType.Entity))
        {
            var metadata = _loader.LoadEntityMetadata(artifact);
            if (metadata != null)
            {
                if (string.IsNullOrEmpty(metadata.Namespace))
                {
                    metadata.Namespace = "GeneratedApp.Entities";
                }
                initialEntities.Add(metadata);
            }
        }

        // 2. Normalize Relations (Handle M:N by creating middle entities)
        var entities = _relationService.Normalize(initialEntities);

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var entity in entities)
            {
                // 3. Generate Entity
                var entityCode = _entityGen.Generate(entity);
                AddFileToZip(archive, $"Entities/{entity.Name}.cs", entityCode);

                // 4. Generate Repository
                var repoCode = _repoGen.Generate(entity);
                AddFileToZip(archive, $"Repositories/{entity.Name}Repository.cs", repoCode);

                // 5. Generate Controller
                var controllerCode = _controllerGen.Generate(entity);
                AddFileToZip(archive, $"Controllers/{entity.Name}Controller.cs", controllerCode);

                // 6. Generate Frontend Component
                var frontendCode = _frontendGen.Generate(entity);
                AddFileToZip(archive, $"Frontend/metrics/{entity.Name.ToLower()}/{entity.Name.ToLower()}.component.ts", frontendCode);
            }

            // 7. Generate DbContext
            if (entities.Any())
            {
                var dbNamespace = "GeneratedApp.Data";
                var dbCode = _dbGen.Generate(dbNamespace, entities);
                AddFileToZip(archive, "Data/GeneratedDbContext.cs", dbCode);
            }
        }

        memoryStream.Position = 0;
        return File(memoryStream.ToArray(), "application/zip", $"Project_{projectId}_Build.zip");
    }

    private void AddFileToZip(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
