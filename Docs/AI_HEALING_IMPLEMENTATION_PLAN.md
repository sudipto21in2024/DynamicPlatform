# Implementation Plan: AI-Assisted Metadata Healing

## 1. Objective
While our **Virtualization Middleware** keeps the app running by translating legacy names at runtime, it is better for long-term health to "fix" the source metadata (Workflows, Rules, Pages) so they point to the current schema. This reduces the performance overhead of translation.

The **AI Healing Engine** will proactively suggest these fixes using Gemini.

---

## 2. Architecture

### A. The Healing Request
When a user views a "Breaking Change" in the Migration Plan, a "💡 Suggest Fixes" button will appear.
- **Trigger**: `POST /api/metadata/heal-proposal`
- **Payload**: `{ ProjectId, TargetArtifactId, MigrationPlanId }`

### B. The AI Prompt Strategy
We will send a structured prompt to Gemini:
1. **Context**: Original JSON of the broken artifact (e.g., a workflow).
2. **Changes**: List of renames/type changes from the `MigrationPlan`.
3. **Task**: "Rewrite this JSON so that all property paths match the current schema. Maintain all other logic."

---

## 3. Detailed Tasks

### Task 1: Healing Service Backend 🧠
- [ ] Create `IHealingService` and its implementation `AIHealingService`.
- [ ] Implement `GetProposalsAsync(Guid projectId, Guid artifactId)`:
    - Load the Artifact (JSON).
    - Load the `MigrationPlan`.
    - Retrieve any `ICompatibilityProvider` mapping for that artifact.
    - Interact with `GeminiService` to get the "Healed" JSON.

### Task 2: Review & Approval UI 🖥️
- [ ] Create a "Safe-Diff" viewer in the Studio.
- [ ] Show the "Old Code" vs "Healed Code" side-by-side.
- [ ] Implement "Apply All Fixes" button:
    - This updates the Artifact content in the database.
    - Increments the version and marks it as "Healed".

### Task 3: Automatic Dependency Rewriting
- [ ] Implement a background job that runs after a `Publish` operation.
- [ ] It scans all artifacts and flags those that are "Currently being virtualized" but "Can be healed."

---

## 4. Technical Edge Cases

### 🚩 Partial Healing
If Gemini can only fix 80% of a complex expression, the service must flag the remaining 20% for human review rather than silently failing or providing invalid C#.

### 🚩 Identity Collision
If a rename creates a name that already exists in a different context within the JSON, the AI must be instructed to use specific path-based resolution.

---

## 5. Next Steps
1. [ ] **Step 1**: Implement `GeminiService` wrapper for structured JSON output.
2. [ ] **Step 2**: Build the `ArtifactHealer` logic that uses the `MigrationPlan`.
3. [ ] **Step 3**: Integrate with the API Controllers.
