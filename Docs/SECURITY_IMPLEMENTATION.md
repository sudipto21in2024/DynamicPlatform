# Security & Identity Management Implementation

This document describes the **Role-Based Access Control (RBAC)**, **Navigation Security**, and **User Management** architecture within DynamicPlatform.

---

## 1. Overview
The security system follows a **Metadata-First** approach. Security requirements are defined visually in the Platform Studio, stored as JSON metadata, and then transpiled into a standalone XML configuration and adaptive UI components.

---

## 2. The Three Pillars of Security

### 2.1. Roles & Permissions
Defines **Who** can do **What** to **Which** entity.
- **Granularity**: Permissions are per-entity and support all CRUD operations (Read, Create, Update, Delete).
- **Association**: Roles act as a bridge between users and entity access.

### 2.2. Navigation & Menus
Defines the visible structure of the application.
- **Conditional Visibility**: Menu items are only rendered if the current user possesses an authorized role.
- **Metadata**: Includes labels, Material Design icons, and application routes.

### 2.3. User Management
Manages the specific identities within the generated application.
- **Project Isolation**: Users belong to specific projects and are stored in the project's metadata context.
- **Identity Seed**: Provides the initial set of user credentials and their role memberships.

---

## 3. The Security Blueprint (`security.xml`)

When a project is published or exported, the engine generates a unified `security.xml` file. This is the "Source of Truth" for the application's security posture.

```xml
<?xml version="1.0" encoding="utf-8"?>
<SecurityConfiguration>
    <Roles>
        <Role Name="Admin">
            <Permissions>
                <Permission Entity="Product" Read="True" Create="True" Update="True" Delete="True" />
            </Permissions>
        </Role>
    </Roles>
    <Menus>
        <MenuItem Label="Inventory" Icon="inventory" Route="/stock" Roles="Admin,Manager" />
    </Menus>
    <Users>
        <User Id="12345" Username="admin" Email="admin@system.com" Password="encrypted_val" Roles="Admin" />
    </Users>
</SecurityConfiguration>
```

---

## 4. Enforcement Mechanism

### 4.1. UI Layer (Adaptive UI)
The generated Angular application includes a **Navigation Component** that provides client-side enforcement:
- **Role Check**: A `canAccess()` method checks the user's roles against the menu's metadata.
- **Directive**: `*ngIf="canAccess(menu.roles)"` ensures unauthorized menus are never added to the DOM.

### 4.2. API Layer (Backend Verification)
For standalone exports:
- The `security.xml` is placed in the root of the API project.
- Custom Middleware (or standard Identity policies) can read this XML to authorize incoming REST requests.
- **Isolation**: Every request is verified against the specific project's roles, ensuring cross-tenant security.

---

## 5. Developer Guide: Adding Security

1.  **Open Security Designer**: Click the "Security" button in the Studio toolbar.
2.  **Define Roles**: Create roles like `Manager` or `Reader`. Map their permissions to entities.
3.  **Assign Menus**: Add menu items for your entities and link them to roles.
4.  **Create Users**: Add the initial application users and select their roles from the dropdown.
5.  **Build**: Output the code. The `security.xml` and `NavigationComponent` will be automatically generated.
