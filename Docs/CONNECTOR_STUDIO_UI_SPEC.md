# Specification: Connector Studio UI

The **Connector Studio** is the dedicated workspace within the DynamicPlatform for defining, testing, and managing external system integrations as **Artifacts**.

## 1. Visual Design Philosophy
- **Aesthetic**: "Cyber-Premium" dark mode with glassmorphism overlays.
- **Accents**: Indigo (#6366f1) and Electric Cyan (#22d3ee) for interactive elements.
- **Layout**: Three-pane responsive cockpit.

## 2. Key Workspace Areas

### A. The Configuration Panel (Left)
- **Static Settings**: Name, Description, and Icon selector.
- **Configuration Properties**: A dynamic list where developers define environment-specific variables (e.g., `BaseUrl`, `ApiKey`, `MaxRetries`).
- **Encrypted Storage**: Toggle to mark a property as "Secret" (masking it in the UI and encrypting it in the database).

### B. The Logic Forge (Center)
- **C# Code Editor**: Integrated Monaco/VS Code editor with syntax highlighting and IntelliSense for the `IConnector` interface.
- **Auto-Mapping**: The editor pre-populates the Input variables defined in the Schema panel.
- **AI Copilot Integration**: "✨ Suggest Logic" button that uses Gemini to generate boilerplate for REST API calls or data transformations based on the Description.

### C. The Input/Output Schema (Right)
- **Payload Designer**: Define the structure of the `inputs` dictionary.
- **Data Types**: Support for `string`, `number`, `boolean`, `date`, and `dynamic-object`.
- **Output Preview**: Visual representation of the expected JSON response from the connector.

## 3. Interactive Features

### ⚡ Live Test Suite
- Allows developer to provide sample inputs and click **"Run Test"**.
- Real-time console output showing logs, execution time, and raw JSON result.
- Integrated with the **ConnectivityHub** for immediate execution in the dev environment.

### 🛡️ Dependency Graph
- A visual "Web" showing which Workflows and Reports are currently consuming this connector artifact.
- Warning system if a developer tries to modify an Output schema that is currently "in-use."

### 🔄 Auto-Documentation
- Generates a "Swagger-style" documentation page for the connector automatically, including usage examples in C# and JavaScript.

## 4. Metadata Mapping
The UI directly serializes to the `ConnectorMetadata` class:
- `Inputs` -> `List<ConnectorParameter>`
- `Outputs` -> `List<ConnectorParameter>`
- `ConfigProperties` -> `List<ConnectorProperty>`
- `BusinessLogic` -> `string`
