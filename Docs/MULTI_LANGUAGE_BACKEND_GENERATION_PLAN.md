# Multi-Language Backend Export Implementation Plan

Evaluate architectural changes and design tasks to introduce support for Java (Spring Boot), Python (FastAPI), and Node.js (NestJS) backend code generation alongside the existing ASP.NET Core Web API exporter.

## User Review Required

> [!IMPORTANT]
> **Code Generation Strategy Decision:**
> Rather than writing custom string builders, we should extend the **Scriban** templating engine approach currently used in the C# generation pipeline. We will organize templates under target-specific folders and implement a Strategy/Factory pattern to select the appropriate generator.

## Open Questions

> [!NOTE]
> **Folder Structure and Packaging:**
> 1. Do we want to support building/running the generated projects automatically (e.g., in Docker), or is this plan strictly for *exporting/downloading* zip files?
> 2. How should package/namespace structure be configured? (e.g., C# uses `BaseNamespace.API`, Java uses reverse-domain `com.example.app`, Python uses pythonic snake_case module structures).

---

## Proposed Changes

### [Backend Model Extension]

#### [MODIFY] [BuildOptions.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Models/BuildOptions.cs)
- Add a `TargetLanguage` enum and property to `BuildOptions`:
  ```csharp
  public enum TargetLanguage
  {
      DotNet,
      Java,
      Python,
      NodeJS
  }
  ```
- Expose `TargetLanguage Language { get; set; } = TargetLanguage.DotNet;` to allow clients to select the desired output.

---

### [Architecture & Design: Code Generation Abstractions]

To adhere to **SOLID principles**, we will abstract the code generation process so that `BuildController` does not become a massive conditional branch.

#### [NEW] [ILanguageGenerator.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Generators/Interfaces/ILanguageGenerator.cs)
- Define a unified interface for backend generation:
  ```csharp
  public interface ILanguageGenerator
  {
      TargetLanguage Language { get; }
      
      // Orchestrates the generation of all components and writes them to the zip archive
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
          BuildOptions options
      );
  }
  ```

#### [NEW] [LanguageGeneratorFactory.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.Engine/Generators/LanguageGeneratorFactory.cs)
- Implement a factory to resolve the correct generator class depending on the chosen `TargetLanguage`:
  ```csharp
  public class LanguageGeneratorFactory
  {
      private readonly IEnumerable<ILanguageGenerator> _generators;
      
      public LanguageGeneratorFactory(IEnumerable<ILanguageGenerator> generators)
      {
          _generators = generators;
      }
      
      public ILanguageGenerator GetGenerator(TargetLanguage language)
      {
          return _generators.FirstOrDefault(g => g.Language == language) 
                 ?? throw new NotSupportedException($"Language {language} is not supported.");
      }
  }
  ```

---

### [New Language Exporter Implementations]

For each new target language, we will implement a concrete `ILanguageGenerator` class and define the associated Scriban templates.

#### 1. Java (Spring Boot + Spring Data JPA)
* **Generator:** `JavaSpringGenerator`
* **Templates needed:**
  - `pom.xml.scriban` (Maven dependencies & build plugins)
  - `Application.java.scriban` (Spring Boot main class with `@SpringBootApplication` and `@EnableJpaRepositories`)
  - `Entity.java.scriban` (JPA Entity with annotations mapping relationships `@OneToMany` / `@ManyToOne` and `@NotNull` validations)
  - `Repository.java.scriban` (Interfaces extending `JpaRepository<Entity, UUID>`)
  - `Controller.java.scriban` (`@RestController` with CRUD endpoints mirroring C# API endpoints)
  - `application.yml.scriban` (Spring datasource and server config)
  - `Dockerfile.scriban` (Multi-stage Maven build + JDK run environment)

#### 2. Python (FastAPI + SQLModel)
* **Generator:** `PythonFastApiGenerator`
* **Templates needed:**
  - `pyproject.toml.scriban` (Dependency configuration for Poetry / Pipenv)
  - `main.py.scriban` (FastAPI app bootstrapping, lifespan setup, router inclusions, and middleware)
  - `models.py.scriban` (SQLModel classes combining Pydantic validation fields and SQLAlchemy columns)
  - `database.py.scriban` (Async database engine and session dependency helper)
  - `routers.py.scriban` (APIRouter endpoints matching the CRUD operations)
  - `Dockerfile.scriban` (Python slim base image copying source and installing requirements)

#### 3. Node.js (NestJS + TypeORM)
* **Generator:** `NodeNestGenerator`
* **Templates/Files needed:**
  - `package.json.scriban` & `tsconfig.json.scriban`
  - `main.ts.scriban` (Bootstrap application using `NestFactory`)
  - `app.module.ts.scriban` (Module imports and configuration setup)
  - `entity.ts.scriban` (TypeORM entity classes with column and relationship decorators)
  - `controller.ts.scriban` (NestJS controllers with `@Get()`, `@Post()`, etc.)
  - `service.ts.scriban` (Business logic layer wrapping TypeORM repositories)
  - `Dockerfile.scriban` (Multi-stage Node build)

---

### [Controller Update]

#### [MODIFY] [BuildController.cs](file:///c:/Sudipto/Antigravity/DynamicPlatform/src/Platform.API/Controllers/BuildController.cs)
- Inject `LanguageGeneratorFactory` and delegate backend zip packaging:
  ```csharp
  // Instead of inline .NET-specific generation in BuildProject:
  var generator = _generatorFactory.GetGenerator(options.Language);
  generator.GenerateBackend(archive, baseNamespace, entities, connectors, workflows, security, users, customObjects, enums, forms, options);
  ```

---

### [Artifact Runtime Strategy]

To support fully functional applications in the target languages, metadata artifacts other than entities must be supported:

#### 1. Workflows (Elsa Integration)
* **Approach A: Decoupled Workflow Host (Default)**
  - Keep Elsa running in a standalone .NET container. The generated Java, Python, and Node.js backends interact with it via REST or event streams.
* **Approach B: Native Engine Translation**
  - **Java:** Translate Elsa definitions into BPMN 2.0 (Camunda or Flowable).
  - **Python / NodeJS:** Integrate Temporal.io orchestrator clients.

#### 2. Connectors
* **Java:** Scaffolds Spring `RestClient` / `WebClient` for REST connections and multi-datasource routing configuration for databases.
* **Python:** Scaffolds async clients using `httpx` or SQLAlchemy engine connection pools.
* **NodeJS:** Scaffolds NestJS Axios wrappers (`HttpModule`) or TypeORM dynamic datasource configurations.

#### 3. Custom Objects & Enums
* **Java:** POJOs and enums.
* **Python:** Pydantic models (with auto-serialization) and standard `Enum` subclasses.
* **NodeJS:** TypeScript classes/interfaces and native TypeScript `enum` types.

#### 4. Security Configuration
* **Java:** Configure Spring Security filters to dynamically parse and enforce roles and access lists (RBAC) matching the schema permissions.
* **Python:** Implement a custom FastAPI dependency middleware using `Depends()` to extract JWT claims and validate them against security rules.
* **NodeJS:** Scaffold NestJS Guards (`RolesGuard`) mapping endpoints dynamically to schema access scopes.

---

## Verification Plan

### Automated Tests
- Run unit/integration tests to ensure existing .NET API generation remains functional:
  ```powershell
  dotnet test
  ```
- Build test suites for new generators confirming template parser errors do not occur.

### Manual Verification
- Trigger builds using `BuildOptions` targeting Java, Python, and Node.js.
- Extract the zip files and run local verification builds (e.g. `mvn clean compile`, `pip install -r requirements.txt`, or `npm install && npm run build`) to ensure the generated projects compile successfully.
