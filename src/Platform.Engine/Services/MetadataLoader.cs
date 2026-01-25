using System;
using System.Text.Json;
using Platform.Core.Domain.Entities;
using Platform.Engine.Models;

namespace Platform.Engine.Services;

public class MetadataLoader
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EntityMetadata? LoadEntityMetadata(Artifact artifact)
    {
        if (artifact.Type != ArtifactType.Entity)
        {
            throw new ArgumentException("Artifact is not an Entity type", nameof(artifact));
        }

        try
        {
            return JsonSerializer.Deserialize<EntityMetadata>(artifact.Content, _options);
        }
        catch (JsonException)
        {
            // Logging or error handling
            return null;
        }
    public ConnectorMetadata? LoadConnectorMetadata(Artifact artifact)
    {
        if (artifact.Type != ArtifactType.Connector)
        {
            throw new ArgumentException("Artifact is not a Connector type", nameof(artifact));
        }

        try
        {
            return JsonSerializer.Deserialize<ConnectorMetadata>(artifact.Content, _options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
