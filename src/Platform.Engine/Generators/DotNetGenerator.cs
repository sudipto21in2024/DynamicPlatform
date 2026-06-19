using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Platform.Core.Domain.Entities;
using Platform.Engine.Models;

namespace Platform.Engine.Generators;

/// <summary>
/// Exporter for generating .NET Core Web API backends and Angular frontends.
/// </summary>
public class DotNetGenerator : ILanguageGenerator
{
    private readonly EntityGenerator _entityGen;
    private readonly DbContextGenerator _dbGen;
    private readonly RepositoryGenerator _repoGen;
    private readonly ControllerGenerator _controllerGen;
    private readonly ProjectGenerator _projectGen;
    private readonly ConnectorGenerator _connectorGen;
    private readonly SecurityGenerator _securityGen;
    private readonly FrontendGenerator _frontendLayoutGen;
    private readonly AngularComponentGenerator _frontendGen;
    private readonly CustomObjectGenerator _customObjectGen;
    private readonly EnumGenerator _enumGen;
    private readonly FormGenerator _formGen;

    public TargetLanguage Language => TargetLanguage.DotNet;

    public DotNetGenerator(
        EntityGenerator entityGen,
        DbContextGenerator dbGen,
        RepositoryGenerator repoGen,
        ControllerGenerator controllerGen,
        ProjectGenerator projectGen,
        ConnectorGenerator connectorGen,
        SecurityGenerator securityGen,
        FrontendGenerator frontendLayoutGen,
        AngularComponentGenerator frontendGen,
        CustomObjectGenerator customObjectGen,
        EnumGenerator enumGen,
        FormGenerator formGen)
    {
        _entityGen = entityGen;
        _dbGen = dbGen;
        _repoGen = repoGen;
        _controllerGen = controllerGen;
        _projectGen = projectGen;
        _connectorGen = connectorGen;
        _securityGen = securityGen;
        _frontendLayoutGen = frontendLayoutGen;
        _frontendGen = frontendGen;
        _customObjectGen = customObjectGen;
        _enumGen = enumGen;
        _formGen = formGen;
    }

    public void GenerateBackend(
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
        BuildOptions options)
    {
        var baseNamespace = project?.Name.Replace(" ", "") ?? "GeneratedApp";
        var styleLibrary = project?.StyleLibrary ?? "Default";
        var apiNamespace = $"{baseNamespace}.API";

        // 3. Generate Infrastructure (Full Code Export)
        var csprojCode = _projectGen.GenerateCsproj(apiNamespace, workflows.Any(), options);
        AddFileToZip(archive, $"{apiNamespace}/{apiNamespace}.csproj", csprojCode);

        var programCode = _projectGen.GenerateProgram(apiNamespace, entities, connectors, workflows, options);
        AddFileToZip(archive, $"{apiNamespace}/Program.cs", programCode);

        foreach (var entity in entities)
        {
            var entityCode = _entityGen.Generate(entity);
            AddFileToZip(archive, $"{apiNamespace}/Entities/{entity.Name}.cs", entityCode);

            var controllerCode = _controllerGen.Generate(entity);
            AddFileToZip(archive, $"{apiNamespace}/Controllers/{entity.Name}Controller.cs", controllerCode);

            var repoCode = _repoGen.Generate(entity);
            AddFileToZip(archive, $"{apiNamespace}/Repositories/{entity.Name}Repository.cs", repoCode);
        }

        foreach (var co in customObjects)
        {
            var coCode = _customObjectGen.Generate(co);
            AddFileToZip(archive, $"{apiNamespace}/Models/{co.Name}.cs", coCode);
        }

        foreach (var @enum in enums)
        {
            var enumCode = _enumGen.Generate(@enum);
            AddFileToZip(archive, $"{apiNamespace}/Entities/{@enum.Name}.cs", enumCode);
        }

        foreach (var form in forms)
        {
            var formBackendCode = _formGen.GenerateBackend(form, baseNamespace);
            AddFileToZip(archive, $"{apiNamespace}/Models/Forms/{form.Name}Form.cs", formBackendCode);

            if (options.IncludeUI)
            {
                var formFrontend = _formGen.GenerateFrontend(form, styleLibrary);
                AddFileToZip(archive, $"Frontend/src/app/forms/{form.Name.ToLower()}-form/{form.Name.ToLower()}-form.component.ts", formFrontend.TypeScript);
                AddFileToZip(archive, $"Frontend/src/app/forms/{form.Name.ToLower()}-form/{form.Name.ToLower()}-form.component.css", formFrontend.Css);

                var premiumFormFrontend = _formGen.GeneratePremiumFrontend(form, styleLibrary);
                AddFileToZip(archive, $"Frontend/src/app/forms/{form.Name.ToLower()}-form/{form.Name.ToLower()}-premium-form.component.ts", premiumFormFrontend.TypeScript);
                AddFileToZip(archive, $"Frontend/src/app/forms/{form.Name.ToLower()}-form/{form.Name.ToLower()}-premium-form.component.css", premiumFormFrontend.Css);
            }
        }

        // 7. Generate Dashboards/Pages
        if (options.IncludeUI)
        {
            foreach (var page in pages)
            {
                var pageComp = _frontendLayoutGen.GenerateDashboard(page, styleLibrary);
                AddFileToZip(archive, $"Frontend/src/app/pages/dashboards/{page.Name.ToLower()}.component.ts", pageComp.TypeScript);
                AddFileToZip(archive, $"Frontend/src/app/pages/dashboards/{page.Name.ToLower()}.component.css", pageComp.Css);
            }

            // 7b. Generate Layout/Navigation (Reflecting Security Features)
            var navComp = _frontendLayoutGen.GenerateNavigation(baseNamespace, security ?? new SecurityMetadata(), styleLibrary);
            AddFileToZip(archive, "Frontend/src/app/components/navigation/navigation.component.ts", navComp.TypeScript);
            AddFileToZip(archive, "Frontend/src/app/components/navigation/navigation.component.css", navComp.Css);

            // 7c. Generate UI Logging Service
            var logServiceCode = _frontendLayoutGen.GenerateLoggingService("DEBUG", true);
            AddFileToZip(archive, "Frontend/src/app/services/logging.service.ts", logServiceCode);
        }

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

    private void AddFileToZip(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
