namespace Platform.Engine.Models;

public class WorkflowMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = string.Empty;
}
