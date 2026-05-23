# Graphify Integration Guide - DynamicPlatform

Graphify is a structural intelligence layer integrated into the **DynamicPlatform** development environment to assist the **Antigravity** AI assistant in understanding complex architectural patterns and cross-module dependencies.

## 🏗️ Architecture Overview

Graphify works by building a persistent knowledge graph of the codebase. It uses:
1.  **AST Pass**: A deterministic extraction of classes, functions, and imports (Deterministic).
2.  **Semantic Pass**: An optional LLM-powered extraction that infers design rationale and architectural intent.
3.  **Community Detection**: Uses the Leiden algorithm to cluster related files into "functional communities" based on edge density.

## 🛠️ Installation Details

### 1. Python Environment
Graphify is installed as a Python module:
- **Package**: `graphifyy` (Official PyPI package)
- **Dependency**: `mcp` (Model Context Protocol for server functionality)

### 2. Antigravity Skill
The integration for the Antigravity AI agent was initialized via:
```bash
python -m graphify antigravity install
```
This created the following files in the workspace:
- `.agents/rules/graphify.md`: Rules for the agent on when to use the graph.
- `.agents/workflows/graphify.md`: Workflow definitions for graph commands.

## ⚙️ Configuration

### Project-Level (mcp_servers.json)
The project root contains an `mcp_servers.json` that configures the Graphify server for this specific repository.

```json
"graphify": {
    "command": "python",
    "args": [
        "-m",
        "graphify.serve",
        "c:/Sudipto/Antigravity/DynamicPlatform/graphify-out/graph.json"
    ]
}
```

### Global-Level (mcp_config.json)
Graphify is also configured globally in the user's Antigravity data directory:
`C:\Users\sudip\.gemini\antigravity\mcp_config.json`

This uses `${workspace.path}` to dynamically point to the current project's graph:
```json
"graphify": {
    "command": "python",
    "args": [
        "-m",
        "graphify.serve",
        "${workspace.path}/graphify-out/graph.json"
    ]
}
```

## 📂 Output Artifacts

All Graphify outputs are stored in `graphify-out/` (excluded from Git):
- `graph.json`: The raw knowledge graph used by the MCP server.
- `graph.html`: An interactive 3D/2D visualization of the codebase.
- `GRAPH_REPORT.md`: A plain-language audit report detailing "God Nodes" and "Surprising Connections".

## 🚀 How to Use

### For the Developer
- **View Graph**: Open `graphify-out/graph.html` in any browser.
- **Read Report**: Consult `graphify-out/GRAPH_REPORT.md` for a quick architectural summary.
- **Manual Update**: If you've made significant changes, run:
  ```powershell
  python -m graphify update .
  ```

### For the AI (Antigravity)
The AI assistant is trained to use the following commands automatically:
- `/graphify .`: Rebuild the graph (AST-only by default).
- `/graphify . --mode deep`: Deep semantic extraction (requires API key).
- `graphify query "..."`: Query the knowledge graph for architectural insights.
- `graphify path "A" "B"`: Find the shortest dependency path between two modules.

## 🧩 Ignore Patterns
A `.graphifyignore` file has been created to prevent the indexer from processing noise:
- `node_modules/`
- `bin/` / `obj/`
- `build/` / `logs/`
- `graphify-out/` (Recursion prevention)

---
*Created on 2026-05-01 by Antigravity AI.*
