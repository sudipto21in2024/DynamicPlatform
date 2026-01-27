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
            return null;
        }
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

    public SecurityMetadata? LoadSecurityMetadata(Artifact artifact)
    {
        if (artifact.Type != ArtifactType.SecurityConfig)
        {
            throw new ArgumentException("Artifact is not a SecurityConfig type", nameof(artifact));
        }

        try
        {
            return JsonSerializer.Deserialize<SecurityMetadata>(artifact.Content, _options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public AppUserMetadata? LoadAppUserMetadata(Artifact artifact)
    {
        if (artifact.Type != ArtifactType.UsersConfig)
        {
            throw new ArgumentException("Artifact is not a UsersConfig type", nameof(artifact));
        }

        try
        {
            return JsonSerializer.Deserialize<AppUserMetadata>(artifact.Content, _options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public WorkflowMetadata? LoadWorkflowMetadata(Artifact artifact)
    {
        if (artifact.Type != ArtifactType.Workflow)
        {
            throw new ArgumentException("Artifact is not a Workflow type", nameof(artifact));
        }

        return new WorkflowMetadata
        {
            Name = artifact.Name,
            DefinitionJson = artifact.Content
        };
    }

    public PageMetadata? LoadPageMetadata(Artifact artifact)
    {
        if (artifact.Type != ArtifactType.Page)
        {
            throw new ArgumentException("Artifact is not a Page type", nameof(artifact));
        }

        try
        {
            return JsonSerializer.Deserialize<PageMetadata>(artifact.Content, _options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public CustomObjectMetadata? LoadCustomObjectMetadata(Artifact artifact)
    {
        if (artifact.Type != ArtifactType.CustomObject)
        {
            throw new ArgumentException("Artifact is not a CustomObject type", nameof(artifact));
        }

        try
        {
            return JsonSerializer.Deserialize<CustomObjectMetadata>(artifact.Content, _options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public EnumMetadata? LoadEnumMetadata(Artifact artifact)
    {
        if (artifact.Type != ArtifactType.Enum)
        {
            throw new ArgumentException("Artifact is not an Enum type", nameof(artifact));
        }

        try
        {
            return JsonSerializer.Deserialize<EnumMetadata>(artifact.Content, _options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public FormMetadata? LoadFormMetadata(Artifact artifact)
    {
        if (artifact.Type != ArtifactType.Form)
        {
            throw new ArgumentException("Artifact is not a Form type", nameof(artifact));
        }

        try
        {
            return JsonSerializer.Deserialize<FormMetadata>(artifact.Content, _options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public WidgetDefinition? LoadWidgetDefinition(Artifact artifact)
    {
        if (artifact.Type != ArtifactType.Widget)
        {
            throw new ArgumentException("Artifact is not a Widget type", nameof(artifact));
        }

        try
        {
            return JsonSerializer.Deserialize<WidgetDefinition>(artifact.Content, _options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
