using System.Collections.Generic;
using System.IO.Compression;
using Platform.Core.Domain.Entities;
using Platform.Engine.Models;

namespace Platform.Engine.Generators;

/// <summary>
/// Interface for programming language-specific backend generator implementations.
/// </summary>
public interface ILanguageGenerator
{
    /// <summary>
    /// Gets the target programming language this generator handles.
    /// </summary>
    TargetLanguage Language { get; }

    /// <summary>
    /// Generates all backend code and packages it into the provided ZipArchive.
    /// </summary>
    void GenerateBackend(
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
        BuildOptions options);
}
