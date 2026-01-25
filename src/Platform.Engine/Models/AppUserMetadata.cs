using System.Collections.Generic;

namespace Platform.Engine.Models;

public class AppUserMetadata
{
    public List<AppUserDefinition> Users { get; set; } = new();
}

public class AppUserDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // In a real app, this would be a hash or handled by Identity
    public List<string> AssignedRoles { get; set; } = new();
}
