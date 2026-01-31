# Task List: Connector Studio UI Implementation

## 📋 Progress Tracking
- [ ] Phase 1: Foundation & Navigation
- [ ] Phase 2: Configuration & Identity Panel
- [ ] Phase 3: Logic Forge (Monaco Editor Integration)
- [ ] Phase 4: Schema Designer (Payload Contracts)
- [ ] Phase 5: Live Test & Debug Console
- [ ] Phase 6: AI-Assisted Logic suggestion

---

## 🏗️ Detailed Tasks

### Phase 1: Foundation & Navigation
- [ ] Create `ConnectorStudioComponent` in Angular.
- [ ] Add route `/studio/connector/:id` to `app-routing.module.ts`.
- [ ] Implement the basic 3-pane glassmorphism layout.

### Phase 2: Configuration Panel (Left)
- [ ] Build dynamic form for `ConfigProperties`.
- [ ] Implement toggle for "Sensitive/Encrypted" properties.
- [ ] Add basic metadata fields (Name, Description, Icon).

### Phase 3: Logic Forge (Center)
- [ ] Integrate `ngx-monaco-editor-v2`.
- [ ] Configure C# syntax highlighting and theme.
- [ ] Implement auto-saving of code to the artifact.

### Phase 4: Schema Designer (Right)
- [ ] Create visual list for `Inputs` and `Outputs`.
- [ ] Add type-picker (String, Int, Decimal, Object).
- [ ] Implement JSON preview for the contract.

### Phase 5: Connectivity & Testing
- [ ] Implement API call to `POST /api/connectivity/execute`.
- [ ] Create the sliding "Debug Console" at the bottom.
- [ ] Show execution metrics (Time, Success/Failure status).

### Phase 6: AI Copilot
- [ ] Add "✨ Suggest Logic" button to the editor toolbar.
- [ ] Integration with Gemini API to generate C# boilerplate.
