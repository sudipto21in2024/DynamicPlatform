using System.Collections.Generic;

namespace Platform.Engine.Models;

public class SecurityMetadata
{
    public List<RoleDefinition> Roles { get; set; } = new();
    public List<MenuDefinition> Menus { get; set; } = new();
}

public class RoleDefinition
{
    public string Name { get; set; } = string.Empty;
    public List<PermissionDefinition> Permissions { get; set; } = new();
}

public class PermissionDefinition
{
    public string EntityName { get; set; } = string.Empty;
    public bool CanRead { get; set; }
    public bool CanCreate { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
}

public class MenuDefinition
{
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public List<string> AllowedRoles { get; set; } = new();
}
