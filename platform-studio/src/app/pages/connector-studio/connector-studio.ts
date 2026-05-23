import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api';
import { AiGenerateModalComponent } from '../../components/ai-generate-modal/ai-generate-modal';

@Component({
  selector: 'app-connector-studio',
  standalone: true,
  imports: [CommonModule, FormsModule, AiGenerateModalComponent],
  styles: [`
    :host { display:flex;flex-direction:column;height:calc(100vh - 64px);background:#0b1120;color:#e2e8f0;overflow:hidden;font-family:'Inter',sans-serif; }

    /* Toolbar */
    .toolbar { height:56px;border-bottom:1px solid rgba(255,255,255,0.05);display:flex;align-items:center;justify-content:space-between;padding:0 1.5rem;background:rgba(15,23,42,0.8);backdrop-filter:blur(16px);flex-shrink:0; }
    .toolbar-left { display:flex;align-items:center;gap:1rem; }
    .toolbar-brand { display:flex;align-items:center;gap:0.75rem; }
    .brand-icon { width:36px;height:36px;border-radius:10px;background:rgba(245,158,11,0.1);border:1px solid rgba(245,158,11,0.2);display:flex;align-items:center;justify-content:center;color:#fbbf24; }
    .brand-title { font-size:0.8rem;font-weight:900;color:#fff;text-transform:uppercase;letter-spacing:0.12em; }
    .brand-sub { font-size:0.65rem;color:#475569;font-family:monospace; }
    .divider { width:1px;height:32px;background:rgba(255,255,255,0.07); }
    .toolbar-btn { display:flex;align-items:center;gap:6px;padding:6px 14px;border-radius:10px;border:none;background:none;cursor:pointer;font-size:0.78rem;font-weight:700;color:#64748b;transition:all 0.15s; }
    .toolbar-btn:hover { background:rgba(255,255,255,0.05);color:#cbd5e1; }
    .toolbar-btn.violet:hover { background:rgba(139,92,246,0.12);color:#a78bfa; }
    .btn-save { display:flex;align-items:center;gap:6px;padding:7px 20px;border-radius:12px;border:none;background:#d97706;color:#fff;font-size:0.78rem;font-weight:900;cursor:pointer;box-shadow:0 4px 16px rgba(217,119,6,0.25);transition:background 0.15s; }
    .btn-save:hover:not(:disabled) { background:#b45309; }
    .btn-save:disabled { background:#1e293b;color:#334155;cursor:not-allowed;box-shadow:none; }

    /* Body layout */
    .body { display:flex;flex:1;overflow:hidden; }

    /* Sidebar list */
    .sidebar { width:240px;border-right:1px solid rgba(255,255,255,0.05);background:rgba(15,23,42,0.4);display:flex;flex-direction:column;flex-shrink:0; }
    .sidebar-header { padding:0.875rem 1rem;border-bottom:1px solid rgba(255,255,255,0.04); }
    .sidebar-label { font-size:0.65rem;font-weight:900;text-transform:uppercase;letter-spacing:0.12em;color:#334155; }
    .sidebar-list { flex:1;overflow-y:auto;padding:0.5rem; }
    .connector-item { padding:0.75rem;border-radius:12px;cursor:pointer;transition:background 0.15s;margin-bottom:2px; }
    .connector-item:hover { background:rgba(255,255,255,0.04); }
    .connector-item.active { background:rgba(245,158,11,0.08);border:1px solid rgba(245,158,11,0.2); }
    .item-name { display:flex;align-items:center;gap:6px;font-size:0.8rem;font-weight:700;color:#f1f5f9; }
    .item-name .material-icons-outlined { font-size:0.95rem;color:#64748b; }
    .connector-item.active .item-name .material-icons-outlined { color:#fbbf24; }
    .item-desc { font-size:0.7rem;color:#334155;margin-top:3px;padding-left:22px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis; }
    .empty-list { display:flex;flex-direction:column;align-items:center;justify-content:center;padding:2.5rem 1rem;color:#1e293b;text-align:center;gap:0.5rem; }
    .empty-list .material-icons-outlined { font-size:2.5rem; }
    .empty-list p { font-size:0.75rem;font-weight:800;text-transform:uppercase;letter-spacing:0.08em; }
    .empty-list small { font-size:0.68rem;color:#0f172a; }

    /* Main editor */
    .editor { flex:1;overflow-y:auto;padding:1.5rem; }
    .empty-editor { display:flex;flex-direction:column;align-items:center;justify-content:center;height:100%;color:#1e293b;gap:1rem; }
    .empty-editor .material-icons-outlined { font-size:4rem; }
    .empty-editor h3 { font-size:0.9rem;font-weight:900;text-transform:uppercase;letter-spacing:0.1em;color:#1e293b; }
    .empty-editor p { font-size:0.78rem;color:#0f172a; }
    .btn-ai-cta { display:flex;align-items:center;gap:8px;background:rgba(139,92,246,0.1);border:1px solid rgba(139,92,246,0.25);color:#a78bfa;padding:10px 20px;border-radius:14px;cursor:pointer;font-size:0.875rem;font-weight:700;transition:all 0.2s; }
    .btn-ai-cta:hover { background:rgba(139,92,246,0.18); }

    .editor-content { max-width:760px;display:flex;flex-direction:column;gap:1.25rem; }

    /* Form sections */
    .form-grid-2 { display:grid;grid-template-columns:1fr 1fr;gap:1rem; }
    .form-group { display:flex;flex-direction:column;gap:6px; }
    .form-label { font-size:0.65rem;text-transform:uppercase;letter-spacing:0.12em;font-weight:900;color:#334155; }
    .form-input { background:rgba(0,0,0,0.35);border:1px solid rgba(255,255,255,0.07);border-radius:12px;padding:10px 14px;font-size:0.875rem;color:#f1f5f9;outline:none;transition:border-color 0.15s;font-family:inherit; }
    .form-input:focus { border-color:#d97706;box-shadow:0 0 0 3px rgba(217,119,6,0.1); }
    .form-input.mono { font-family:monospace; }

    /* IO panels */
    .io-section { display:grid;grid-template-columns:1fr 1fr;gap:1rem; }
    .io-panel { background:rgba(0,0,0,0.2);border:1px solid rgba(255,255,255,0.04);border-radius:16px;padding:1rem; }
    .io-header { display:flex;align-items:center;justify-content:space-between;margin-bottom:0.75rem; }
    .io-label { font-size:0.65rem;font-weight:900;text-transform:uppercase;letter-spacing:0.12em; }
    .io-label.in-color { color:rgba(96,165,250,0.7); }
    .io-label.out-color { color:rgba(52,211,153,0.7); }
    .btn-add-io { background:none;border:none;cursor:pointer;display:flex;align-items:center;gap:3px;font-size:0.68rem;font-weight:900;transition:color 0.15s; }
    .btn-add-io.in { color:#3b82f6; }
    .btn-add-io.in:hover { color:#60a5fa; }
    .btn-add-io.out { color:#10b981; }
    .btn-add-io.out:hover { color:#34d399; }
    .io-row { display:flex;align-items:center;gap:6px;margin-bottom:6px;background:rgba(0,0,0,0.3);border-radius:8px;padding:6px 8px; }
    .io-input-name { flex:1;background:none;border:none;color:#f1f5f9;font-size:0.78rem;font-family:monospace;outline:none; }
    .io-select { background: #1e293b; border: 1px solid rgba(255,255,255,0.1); border-radius: 6px; padding: 3px 6px; font-size: 0.68rem; font-weight: 700; outline: none; cursor: pointer; }
    .io-select option { background: #0f172a; color: #f1f5f9; }
    .io-select.in-sel { color: #93c5fd; }
    .io-select.out-sel { color: #6ee7b7; }
    .io-del { background:none;border:none;cursor:pointer;color:#1e293b;transition:color 0.15s;padding:2px;display:flex; }
    .io-del:hover { color:#f87171; }

    /* Logic editor */
    .logic-section { background:rgba(0,0,0,0.2);border:1px solid rgba(255,255,255,0.04);border-radius:16px;padding:1rem; }
    .logic-header { display:flex;align-items:center;justify-content:space-between;margin-bottom:0.75rem; }
    .logic-label { font-size:0.65rem;font-weight:900;text-transform:uppercase;letter-spacing:0.12em;color:rgba(251,191,36,0.7); }
    .btn-ai-logic { display:flex;align-items:center;gap:5px;background:rgba(139,92,246,0.08);border:1px solid rgba(139,92,246,0.2);color:#a78bfa;font-size:0.68rem;font-weight:900;padding:5px 12px;border-radius:999px;cursor:pointer;transition:all 0.15s; }
    .btn-ai-logic:hover { background:rgba(139,92,246,0.15); }
    .logic-textarea { width:100%;background:rgba(0,0,0,0.5);border:1px solid rgba(255,255,255,0.04);border-radius:12px;padding:14px;font-size:0.8rem;color:#6ee7b7;font-family:'JetBrains Mono',monospace;resize:none;outline:none;line-height:1.7;transition:border-color 0.15s;box-sizing:border-box; }
    .logic-textarea:focus { border-color:rgba(245,158,11,0.4);box-shadow:0 0 0 3px rgba(217,119,6,0.07); }
    .logic-hint { font-size:0.65rem;color:#1e293b;font-family:monospace;margin-top:6px; }

    /* Explanation box */
    .explain-box { background:rgba(59,130,246,0.05);border:1px solid rgba(59,130,246,0.15);border-radius:14px;padding:1rem; }
    .explain-header { display:flex;align-items:center;justify-content:space-between;margin-bottom:8px; }
    .explain-label { font-size:0.65rem;font-weight:900;text-transform:uppercase;letter-spacing:0.12em;color:#60a5fa; }
    .explain-close { background:none;border:none;color:#334155;cursor:pointer;padding:2px;display:flex; }
    .explain-close:hover { color:#fff; }
    .explain-text { font-size:0.8rem;color:#94a3b8;line-height:1.6;white-space:pre-wrap; }

    /* Bottom actions */
    .editor-actions { display:flex;align-items:center;justify-content:space-between;padding-top:0.5rem; }
    .btn-explain { display:flex;align-items:center;gap:6px;background:none;border:1px solid rgba(255,255,255,0.06);color:#475569;font-size:0.78rem;font-weight:700;padding:8px 14px;border-radius:10px;cursor:pointer;transition:all 0.15s; }
    .btn-explain:hover:not(:disabled) { color:#93c5fd;border-color:rgba(96,165,250,0.2); }
    .btn-explain:disabled { opacity:0.3;cursor:not-allowed; }
    .btn-delete { background:none;border:none;color:rgba(239,68,68,0.3);cursor:pointer;padding:8px 14px;border-radius:10px;font-size:0.78rem;transition:all 0.15s;display:flex;align-items:center;gap:4px; }
    .btn-delete:hover { color:#f87171;background:rgba(239,68,68,0.06); }

    .spin { animation:spin 1s linear infinite; }
    @keyframes spin { to { transform:rotate(360deg); } }

    ::-webkit-scrollbar { width:4px; }
    ::-webkit-scrollbar-track { background:transparent; }
    ::-webkit-scrollbar-thumb { background:rgba(255,255,255,0.08);border-radius:2px; }
  `],
  template: `
    <div class="toolbar">
      <div class="toolbar-left">
        <div class="toolbar-brand">
          <div class="brand-icon">
            <span class="material-icons-outlined" style="font-size:1.1rem">bolt</span>
          </div>
          <div>
            <div class="brand-title">Connector Studio</div>
            <div class="brand-sub">Business Logic Engine</div>
          </div>
        </div>
        <div class="divider"></div>
        <button class="toolbar-btn" (click)="newConnector()">
          <span class="material-icons-outlined" style="font-size:1.1rem">add_box</span>
          New Connector
        </button>
        <button class="toolbar-btn violet" (click)="showAiConnectorModal = true">
          <span class="material-icons-outlined" style="font-size:1.1rem">auto_awesome</span>
          AI Generate
        </button>
      </div>
      <button class="btn-save" (click)="saveConnector()" [disabled]="!selectedConnector || saving">
        <span class="material-icons-outlined" style="font-size:1rem" [class.spin]="saving">{{ saving ? 'sync' : 'save' }}</span>
        {{ saving ? 'Saving...' : 'Save' }}
      </button>
    </div>

    <div class="body">
      <!-- Sidebar -->
      <aside class="sidebar">
        <div class="sidebar-header">
          <div class="sidebar-label">Connectors ({{ connectors.length }})</div>
        </div>
        <div class="sidebar-list">
          <div *ngIf="loading" style="text-align:center;padding:2rem;color:#334155;font-size:0.8rem">Loading...</div>

          <div *ngFor="let c of connectors"
               class="connector-item"
               [class.active]="selectedConnector?.name === c.name"
               (click)="selectConnector(c)">
            <div class="item-name">
              <span class="material-icons-outlined">bolt</span>
              {{ c.name }}
            </div>
            <div class="item-desc">{{ c.description || 'No description' }}</div>
          </div>

          <div *ngIf="!loading && connectors.length === 0" class="empty-list">
            <span class="material-icons-outlined">bolt</span>
            <p>No connectors yet</p>
            <small>Create or AI-generate one</small>
          </div>
        </div>
      </aside>

      <!-- Editor -->
      <main class="editor">
        <!-- Empty state -->
        <div class="empty-editor" *ngIf="!selectedConnector">
          <span class="material-icons-outlined">bolt</span>
          <h3>Select a Connector</h3>
          <p>or create a new one to start editing</p>
          <button class="btn-ai-cta" (click)="showAiConnectorModal = true">
            <span class="material-icons-outlined">auto_awesome</span>
            Generate with AI
          </button>
        </div>

        <!-- Form -->
        <div class="editor-content" *ngIf="selectedConnector">

          <!-- Name & Namespace -->
          <div class="form-grid-2">
            <div class="form-group">
              <label class="form-label">Connector Name</label>
              <input class="form-input" [(ngModel)]="selectedConnector.name" placeholder="e.g., CalculateDiscount" />
            </div>
            <div class="form-group">
              <label class="form-label">Namespace</label>
              <input class="form-input mono" [(ngModel)]="selectedConnector.namespace" placeholder="App.Connectors" />
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Description</label>
            <input class="form-input" [(ngModel)]="selectedConnector.description" placeholder="What does this connector do?" />
          </div>

          <!-- Inputs & Outputs -->
          <div class="io-section">
            <div class="io-panel">
              <div class="io-header">
                <span class="io-label in-color">Inputs</span>
                <button class="btn-add-io in" (click)="addInput()">
                  <span class="material-icons-outlined" style="font-size:0.85rem">add</span> Add
                </button>
              </div>
              <div *ngFor="let inp of selectedConnector.inputs; let i = index" class="io-row">
                <input class="io-input-name" [(ngModel)]="inp.name" placeholder="paramName" />
                <select class="io-select in-sel" [(ngModel)]="inp.type">
                  <option value="string">string</option>
                  <option value="int">int</option>
                  <option value="decimal">decimal</option>
                  <option value="bool">bool</option>
                  <option value="Guid">Guid</option>
                  <option value="DateTime">DateTime</option>
                </select>
                <button class="io-del" (click)="selectedConnector.inputs.splice(i,1)">
                  <span class="material-icons-outlined" style="font-size:0.9rem">close</span>
                </button>
              </div>
            </div>

            <div class="io-panel">
              <div class="io-header">
                <span class="io-label out-color">Outputs</span>
                <button class="btn-add-io out" (click)="addOutput()">
                  <span class="material-icons-outlined" style="font-size:0.85rem">add</span> Add
                </button>
              </div>
              <div *ngFor="let out of selectedConnector.outputs; let i = index" class="io-row">
                <input class="io-input-name" [(ngModel)]="out.name" placeholder="resultName" />
                <select class="io-select out-sel" [(ngModel)]="out.type">
                  <option value="string">string</option>
                  <option value="int">int</option>
                  <option value="decimal">decimal</option>
                  <option value="bool">bool</option>
                  <option value="object">object</option>
                </select>
                <button class="io-del" (click)="selectedConnector.outputs.splice(i,1)">
                  <span class="material-icons-outlined" style="font-size:0.9rem">close</span>
                </button>
              </div>
            </div>
          </div>

          <!-- Business Logic -->
          <div class="logic-section">
            <div class="logic-header">
              <span class="logic-label">Business Logic (C#)</span>
              <button class="btn-ai-logic" (click)="showLogicModal = true">
                <span class="material-icons-outlined" style="font-size:0.8rem">auto_awesome</span>
                AI Generate Logic
              </button>
            </div>
            <textarea class="logic-textarea" rows="14" [(ngModel)]="selectedConnector.businessLogic"
                      placeholder="// C# statements only&#10;var discount = 0m;&#10;if (LoyaltyPoints > 500)&#10;    discount = TotalAmount * 0.10m;&#10;return discount;">
            </textarea>
            <div class="logic-hint">Available: inputs declared above · logger.LogInformation("...") · return value</div>
          </div>

          <!-- Explanation -->
          <div class="explain-box" *ngIf="explanation">
            <div class="explain-header">
              <span class="explain-label">AI Explanation</span>
              <button class="explain-close" (click)="explanation = ''">
                <span class="material-icons-outlined" style="font-size:1rem">close</span>
              </button>
            </div>
            <div class="explain-text">{{ explanation }}</div>
          </div>

          <!-- Actions Row -->
          <div class="editor-actions">
            <button class="btn-explain" (click)="explainLogic()" [disabled]="!selectedConnector.businessLogic || explaining">
              <span class="material-icons-outlined" style="font-size:1rem" [class.spin]="explaining">
                {{ explaining ? 'sync' : 'help_outline' }}
              </span>
              {{ explaining ? 'Analyzing...' : 'Explain This Code' }}
            </button>
            <button class="btn-delete" (click)="deleteConnector()">
              <span class="material-icons-outlined" style="font-size:1rem">delete_outline</span>
              Delete
            </button>
          </div>
        </div>
      </main>
    </div>

    <!-- AI Generate Connector Modal -->
    <app-ai-generate-modal
      *ngIf="showAiConnectorModal"
      title="✨ Generate Connector Logic"
      subtitle="Describe the business logic in plain language"
      placeholder="e.g., Calculate 10% loyalty discount if customer has more than 500 points..."
      mode="connector"
      [projectId]="projectId!"
      (accepted)="acceptGeneratedConnector($event)"
      (cancel)="showAiConnectorModal = false">
    </app-ai-generate-modal>

    <!-- AI Generate Logic (inline) Modal -->
    <app-ai-generate-modal
      *ngIf="showLogicModal"
      title="✨ Generate Business Logic"
      subtitle="AI will write the C# implementation for this connector"
      [placeholder]="logicPromptHint"
      mode="connector"
      [projectId]="projectId!"
      (accepted)="acceptGeneratedLogic($event)"
      (cancel)="showLogicModal = false">
    </app-ai-generate-modal>
  `
})
export class ConnectorStudioComponent implements OnInit {
  projectId: string | null = null;
  connectors: any[] = [];
  selectedConnector: any = null;
  loading = true;
  saving = false;
  explaining = false;
  explanation = '';
  showAiConnectorModal = false;
  showLogicModal = false;

  get logicPromptHint(): string {
    if (!this.selectedConnector) return '';
    const inputs = (this.selectedConnector.inputs || []).map((i: any) => `${i.name} (${i.type})`).join(', ');
    return `Using inputs: ${inputs || 'none yet'}. Describe what the logic should do...`;
  }

  constructor(private route: ActivatedRoute, private api: ApiService) {
    this.projectId = this.route.snapshot.paramMap.get('projectId');
  }

  ngOnInit() { this.loadConnectors(); }

  loadConnectors() {
    if (!this.projectId) return;
    this.loading = true;
    this.api.request('GET', `projects/${this.projectId}/connectors`).subscribe({
      next: (arts: any[]) => {
        this.connectors = (arts || []).map((a: any) => {
          try { return { ...JSON.parse(a.content), _artifactId: a.id }; }
          catch { return null; }
        }).filter(Boolean);
        this.loading = false;
      },
      error: () => this.loading = false
    });
  }

  newConnector() {
    const c = { name: 'NewConnector', namespace: 'GeneratedApp.Connectors', description: '', inputs: [], outputs: [], businessLogic: '' };
    this.connectors.unshift(c);
    this.selectedConnector = { ...c };
    this.explanation = '';
  }

  selectConnector(c: any) { this.selectedConnector = { ...c }; this.explanation = ''; }

  addInput()  { if (this.selectedConnector) { this.selectedConnector.inputs  = this.selectedConnector.inputs  || []; this.selectedConnector.inputs.push({ name: 'param', type: 'string' }); } }
  addOutput() { if (this.selectedConnector) { this.selectedConnector.outputs = this.selectedConnector.outputs || []; this.selectedConnector.outputs.push({ name: 'result', type: 'string' }); } }

  saveConnector() {
    if (!this.projectId || !this.selectedConnector) return;
    this.saving = true;
    const payload = { ...this.selectedConnector };
    delete payload._artifactId;
    this.api.request('POST', `projects/${this.projectId}/connectors`, payload).subscribe({
      next: () => { this.saving = false; this.loadConnectors(); },
      error: (e: any) => { this.saving = false; alert('Save failed: ' + e.message); }
    });
  }

  deleteConnector() {
    if (!this.selectedConnector || !confirm(`Delete "${this.selectedConnector.name}"?`)) return;
    this.selectedConnector = null;
  }

  acceptGeneratedConnector(generated: any) {
    this.showAiConnectorModal = false;
    const c = Array.isArray(generated) ? generated[0] : generated;
    if (!c) return;
    this.connectors.unshift(c);
    this.selectedConnector = { ...c };
  }

  acceptGeneratedLogic(generated: any) {
    this.showLogicModal = false;
    const c = Array.isArray(generated) ? generated[0] : generated;
    if (c && this.selectedConnector) {
      this.selectedConnector.businessLogic = c.businessLogic || c;
      if (c.inputs?.length)  this.selectedConnector.inputs  = c.inputs;
      if (c.outputs?.length) this.selectedConnector.outputs = c.outputs;
    }
  }

  explainLogic() {
    if (!this.selectedConnector?.businessLogic || !this.projectId) return;
    this.explaining = true;
    this.explanation = '';
    this.api.aiExplainLogic(this.selectedConnector.businessLogic, this.projectId).subscribe({
      next: (r: any) => { this.explanation = typeof r === 'string' ? r : JSON.stringify(r); this.explaining = false; },
      error: (e: any) => { this.explaining = false; alert('Explain failed: ' + e.message); }
    });
  }
}
