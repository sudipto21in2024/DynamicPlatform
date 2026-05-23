import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api';

@Component({
  selector: 'app-ai-generate-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styles: [`
    .overlay { position:fixed;inset:0;background:rgba(0,0,0,0.75);backdrop-filter:blur(6px);z-index:200;display:flex;align-items:center;justify-content:center;padding:1rem; }
    .dialog { background:#0f172a;border:1px solid rgba(255,255,255,0.08);border-radius:24px;width:100%;max-width:680px;max-height:90vh;overflow:hidden;display:flex;flex-direction:column; }

    .dialog-header { display:flex;align-items:center;justify-content:space-between;padding:1.5rem;border-bottom:1px solid rgba(255,255,255,0.05); }
    .header-left { display:flex;align-items:center;gap:0.75rem; }
    .header-icon { width:40px;height:40px;border-radius:12px;background:rgba(139,92,246,0.12);border:1px solid rgba(139,92,246,0.25);display:flex;align-items:center;justify-content:center;color:#a78bfa;animation:pulse 2s ease-in-out infinite; }
    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:0.6} }
    .dialog-title { font-size:1rem;font-weight:900;color:#f1f5f9; }
    .dialog-sub { font-size:0.7rem;color:#475569;margin-top:2px; }
    .close-btn { background:none;border:none;color:#475569;cursor:pointer;padding:6px;border-radius:8px;transition:color 0.15s;display:flex; }
    .close-btn:hover { color:#fff; }

    .dialog-body { padding:1.5rem;flex:1;overflow-y:auto; }
    .form-label { font-size:0.65rem;text-transform:uppercase;letter-spacing:0.12em;font-weight:900;color:#475569;display:block;margin-bottom:8px; }
    textarea { width:100%;background:rgba(0,0,0,0.4);border:1px solid rgba(255,255,255,0.08);border-radius:14px;padding:14px;font-size:0.875rem;color:#f1f5f9;outline:none;resize:none;line-height:1.6;transition:border-color 0.15s;font-family:inherit;box-sizing:border-box; }
    textarea:focus { border-color:#7c3aed;box-shadow:0 0 0 3px rgba(124,58,237,0.1); }
    textarea::placeholder { color:#334155; }

    .ctx-hint { display:flex;align-items:center;gap:6px;margin-top:10px;font-size:0.7rem;color:#475569; }
    .ctx-dot { color:#7c3aed; }
    .kbd-hint { font-size:0.65rem;color:#334155;margin-top:6px; }
    kbd { display:inline-block;padding:1px 6px;border-radius:4px;background:rgba(255,255,255,0.04);border:1px solid rgba(255,255,255,0.08);font-family:monospace;font-size:0.65rem; }

    .dialog-footer { display:flex;align-items:center;justify-content:flex-end;gap:0.75rem;padding:1.25rem 1.5rem;border-top:1px solid rgba(255,255,255,0.05); }
    .btn-ghost { background:none;border:none;color:#475569;font-size:0.8rem;font-weight:800;cursor:pointer;padding:8px 14px;border-radius:10px;transition:color 0.15s; }
    .btn-ghost:hover { color:#fff; }
    .btn-generate { display:flex;align-items:center;gap:6px;background:#7c3aed;color:#fff;border:none;font-size:0.8rem;font-weight:900;cursor:pointer;padding:10px 22px;border-radius:12px;box-shadow:0 4px 20px rgba(124,58,237,0.3);transition:background 0.15s; }
    .btn-generate:hover:not(:disabled) { background:#6d28d9; }
    .btn-generate:disabled { background:#1e293b;color:#334155;cursor:not-allowed;box-shadow:none; }
    .btn-accept { display:flex;align-items:center;gap:6px;background:#059669;color:#fff;border:none;font-size:0.8rem;font-weight:900;cursor:pointer;padding:10px 22px;border-radius:12px;box-shadow:0 4px 20px rgba(5,150,105,0.25);transition:background 0.15s; }
    .btn-accept:hover { background:#047857; }
    .btn-discard { background:none;border:none;color:#475569;font-size:0.8rem;font-weight:800;cursor:pointer;padding:8px 14px;border-radius:10px;transition:color 0.15s; }
    .btn-discard:hover { color:#fff; }
    .btn-retry { display:flex;align-items:center;gap:4px;background:none;border:none;color:#475569;font-size:0.75rem;font-weight:800;cursor:pointer;padding:4px 8px;border-radius:8px;transition:color 0.15s; }
    .btn-retry:hover { color:#fff; }

    .error-box { padding:1rem;border-radius:14px;background:rgba(239,68,68,0.06);border:1px solid rgba(239,68,68,0.18);color:#f87171;margin-bottom:1rem; }
    .error-title { font-weight:800;font-size:0.875rem;margin-bottom:4px; }
    .error-msg { font-size:0.78rem;color:rgba(248,113,113,0.7); }

    .result-header { display:flex;align-items:center;justify-content:space-between;margin-bottom:12px; }
    .result-label { font-size:0.7rem;font-weight:900;text-transform:uppercase;letter-spacing:0.1em;color:#34d399; }
    .result-preview { background:rgba(0,0,0,0.5);border:1px solid rgba(255,255,255,0.05);border-radius:16px;overflow:hidden; }

    .entity-row { padding:1rem;border-bottom:1px solid rgba(255,255,255,0.04);transition:background 0.15s; }
    .entity-row:last-child { border-bottom:none; }
    .entity-row:hover { background:rgba(255,255,255,0.01); }
    .entity-name { font-size:0.875rem;font-weight:800;color:#f1f5f9;display:flex;align-items:center;gap:8px;margin-bottom:8px; }
    .entity-icon { color:#60a5fa;font-size:1rem; }
    .field-count { font-size:0.68rem;color:#475569;margin-left:auto; }
    .field-chips { display:flex;flex-wrap:wrap;gap:6px; }
    .field-chip { font-size:0.65rem;font-family:monospace;padding:2px 10px;border-radius:999px;background:rgba(96,165,250,0.08);color:#93c5fd;border:1px solid rgba(96,165,250,0.15); }

    .connector-preview { padding:1rem; }
    .connector-name { font-size:0.875rem;font-weight:800;color:#f1f5f9;display:flex;align-items:center;gap:8px;margin-bottom:6px; }
    .connector-desc { font-size:0.78rem;color:#64748b;margin-bottom:12px; }
    .io-grid { display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:12px; }
    .io-label { font-size:0.65rem;font-weight:900;text-transform:uppercase;letter-spacing:0.1em;margin-bottom:4px; }
    .io-label.in { color:#60a5fa; }
    .io-label.out { color:#34d399; }
    .io-item { font-size:0.75rem;font-family:monospace;color:#94a3b8;padding:2px 0; }
    .logic-label { font-size:0.65rem;font-weight:900;text-transform:uppercase;letter-spacing:0.1em;color:#fbbf24;margin-bottom:6px; }
    .logic-code { background:rgba(0,0,0,0.5);padding:12px;border-radius:10px;border:1px solid rgba(255,255,255,0.04);font-size:0.72rem;color:#6ee7b7;font-family:monospace;white-space:pre-wrap;overflow-x:auto;max-height:180px;overflow-y:auto; }

    .json-preview { font-size:0.75rem;color:#94a3b8;font-family:monospace;white-space:pre-wrap;overflow:auto;max-height:280px;padding:1rem; }

    .spin { animation:spin 1s linear infinite; }
    @keyframes spin { to { transform:rotate(360deg); } }
  `],
  template: `
    <div class="overlay" (click)="onBackdropClick($event)">
      <div class="dialog" (click)="$event.stopPropagation()">

        <!-- Header -->
        <div class="dialog-header">
          <div class="header-left">
            <div class="header-icon">
              <span class="material-icons-outlined">auto_awesome</span>
            </div>
            <div>
              <div class="dialog-title">{{ title }}</div>
              <div class="dialog-sub">{{ subtitle }}</div>
            </div>
          </div>
          <button class="close-btn" (click)="cancel.emit()">
            <span class="material-icons-outlined">close</span>
          </button>
        </div>

        <!-- Prompt Phase -->
        <div class="dialog-body" *ngIf="!result && !error">
          <label class="form-label">Describe what you need</label>
          <textarea rows="6" [(ngModel)]="prompt" [placeholder]="placeholder"
                    (keydown.control.enter)="generate()">
          </textarea>
          <div class="ctx-hint" *ngIf="projectId">
            <span class="material-icons-outlined ctx-dot" style="font-size:0.9rem">schema</span>
            AI has access to your project's entity schema as context.
          </div>
          <div class="kbd-hint">Press <kbd>Ctrl+Enter</kbd> to generate</div>
        </div>

        <!-- Error Phase -->
        <div class="dialog-body" *ngIf="error && !result">
          <div class="error-box">
            <div class="error-title">Generation failed</div>
            <div class="error-msg">{{ error }}</div>
          </div>
        </div>

        <!-- Result Phase -->
        <div class="dialog-body" *ngIf="result">
          <div class="result-header">
            <span class="result-label">✨ Generated — Review before accepting</span>
            <button class="btn-retry" (click)="result = null; error = ''">
              <span class="material-icons-outlined" style="font-size:0.9rem">refresh</span>
              Regenerate
            </button>
          </div>

          <div class="result-preview">
            <!-- Entity array preview -->
            <ng-container *ngIf="isArray(result)">
              <div class="entity-row" *ngFor="let entity of result">
                <div class="entity-name">
                  <span class="material-icons-outlined entity-icon">table_chart</span>
                  {{ entity.name }}
                  <span class="field-count">{{ entity.fields?.length || 0 }} fields</span>
                </div>
                <div class="field-chips">
                  <span class="field-chip" *ngFor="let f of entity.fields">
                    {{ f.name }}: {{ f.type }}{{ f.isRequired ? '*' : '' }}
                  </span>
                </div>
              </div>
            </ng-container>

            <!-- AI Entity Designer preview (updatedEntity & newEntities) -->
            <ng-container *ngIf="!isArray(result) && result.updatedEntity">
              <div class="entity-row" style="border-left: 4px solid #8b5cf6; background: rgba(139,92,246,0.04); margin-bottom: 12px; border-radius: 8px;">
                <div class="entity-name" style="color: #c084fc;">
                  <span class="material-icons-outlined entity-icon" style="color: #c084fc;">edit_note</span>
                  Update Entity: {{ result.updatedEntity.name }}
                  <span class="field-count">{{ result.updatedEntity.fields?.length || 0 }} fields</span>
                </div>
                <div class="field-chips">
                  <span class="field-chip" *ngFor="let f of result.updatedEntity.fields" 
                        [style.background]="isNewField(f) ? 'rgba(52,211,153,0.08)' : 'rgba(96,165,250,0.08)'"
                        [style.color]="isNewField(f) ? '#34d399' : '#93c5fd'"
                        [style.border-color]="isNewField(f) ? 'rgba(52,211,153,0.15)' : 'rgba(96,165,250,0.15)'">
                    {{ f.name }}: {{ f.type }}{{ f.isRequired ? '*' : '' }}{{ isNewField(f) ? ' [NEW]' : '' }}
                  </span>
                </div>
              </div>

              <div *ngIf="result.newEntities && result.newEntities.length > 0">
                <div class="result-label" style="margin-top: 16px; margin-bottom: 8px; color: #34d399; font-size: 0.65rem;">➕ Additional Related Entities to Create</div>
                <div class="entity-row" *ngFor="let entity of result.newEntities" style="border-left: 4px solid #10b981; background: rgba(16,185,129,0.03); border-radius: 8px; margin-bottom: 12px;">
                  <div class="entity-name" style="color: #34d399;">
                    <span class="material-icons-outlined entity-icon" style="color: #34d399;">add_box</span>
                    {{ entity.name }}
                    <span class="field-count">{{ entity.fields?.length || 0 }} fields</span>
                  </div>
                  <div class="field-chips">
                    <span class="field-chip" *ngFor="let f of entity.fields" style="background:rgba(52,211,153,0.08); color:#34d399; border-color:rgba(52,211,153,0.15)">
                      {{ f.name }}: {{ f.type }}{{ f.isRequired ? '*' : '' }}
                    </span>
                  </div>
                  <div *ngIf="entity.relations && entity.relations.length > 0" style="margin-top: 8px; font-size: 0.7rem; color: #64748b; display: flex; flex-direction: column; gap: 4px;">
                    <div *ngFor="let rel of entity.relations" style="display:flex; align-items:center; gap:6px;">
                      <span class="material-icons-outlined" style="font-size:0.85rem">link</span>
                      {{ rel.type === 0 ? 'One-to-Many' : rel.type === 1 ? 'Many-to-One' : 'Many-to-Many' }} link to {{ rel.targetEntity }} (prop: {{ rel.navPropName }})
                    </div>
                  </div>
                </div>
              </div>
            </ng-container>

            <!-- Connector preview -->
            <div class="connector-preview" *ngIf="!isArray(result) && !result.updatedEntity && result.businessLogic">
              <div class="connector-name">
                <span class="material-icons-outlined" style="color:#fbbf24">bolt</span>
                {{ result.name }}
              </div>
              <div class="connector-desc">{{ result.description }}</div>
              <div class="io-grid">
                <div>
                  <div class="io-label in">Inputs</div>
                  <div class="io-item" *ngFor="let i of result.inputs">{{ i.name }}: {{ i.type }}</div>
                </div>
                <div>
                  <div class="io-label out">Outputs</div>
                  <div class="io-item" *ngFor="let o of result.outputs">{{ o.name }}: {{ o.type }}</div>
                </div>
              </div>
              <div class="logic-label">Business Logic</div>
              <div class="logic-code">{{ result.businessLogic }}</div>
            </div>

            <!-- JSON fallback -->
            <div *ngIf="!isArray(result) && !result.updatedEntity && !result.businessLogic" class="json-preview">{{ resultJson }}</div>
          </div>
        </div>

        <!-- Footer -->
        <div class="dialog-footer" *ngIf="!result">
          <button class="btn-ghost" (click)="cancel.emit()">Cancel</button>
          <button class="btn-generate" (click)="generate()" [disabled]="generating || !prompt.trim()">
            <span class="material-icons-outlined" style="font-size:1rem" [class.spin]="generating">
              {{ generating ? 'sync' : 'auto_awesome' }}
            </span>
            {{ generating ? 'Generating...' : 'Generate' }}
          </button>
        </div>

        <div class="dialog-footer" *ngIf="result">
          <button class="btn-discard" (click)="cancel.emit()">Discard</button>
          <button class="btn-accept" (click)="accept()">
            <span class="material-icons-outlined" style="font-size:1rem">check_circle</span>
            Accept & Save
          </button>
        </div>

      </div>
    </div>
  `
})
export class AiGenerateModalComponent {
  @Input() title = '✨ Generate with AI';
  @Input() subtitle = 'Describe your requirement in plain language';
  @Input() placeholder = 'e.g., A product catalog with categories, variants, and inventory...';
  @Input() mode: 'schema' | 'connector' | 'rule' | 'entity-designer' = 'schema';
  @Input() projectId?: string;
  @Input() currentEntity?: any;
  @Output() accepted = new EventEmitter<any>();
  @Output() cancel = new EventEmitter<void>();

  prompt = '';
  generating = false;
  result: any = null;
  resultJson = '';
  error = '';

  constructor(private api: ApiService) {}

  generate() {
    if (!this.prompt.trim() || this.generating) return;
    this.generating = true;
    this.result = null;
    this.error = '';

    const call$ = this.mode === 'connector'
      ? this.api.aiGenerateConnector(this.prompt, this.projectId!)
      : this.mode === 'rule'
        ? this.api.aiGenerateRule(this.prompt, this.projectId!)
        : this.mode === 'entity-designer'
          ? this.api.aiDesignEntity(this.prompt, JSON.stringify(this.currentEntity), this.projectId)
          : this.api.aiGenerateSchema(this.prompt, this.projectId);

    call$.subscribe({
      next: (raw: any) => {
        this.generating = false;
        try {
          this.result = typeof raw === 'string' ? JSON.parse(raw) : raw;
          this.resultJson = JSON.stringify(this.result, null, 2);
        } catch {
          this.result = raw;
          this.resultJson = String(raw);
        }
      },
      error: (e: any) => {
        this.generating = false;
        this.error = e.error?.message || e.message || 'Unknown error';
      }
    });
  }

  accept() { this.accepted.emit(this.result); }
  isArray(v: any): boolean { return Array.isArray(v); }
  onBackdropClick(e: Event) { if (e.target === e.currentTarget) this.cancel.emit(); }

  isNewField(field: any): boolean {
    if (!this.currentEntity || !this.currentEntity.fields) return true;
    return !this.currentEntity.fields.some((f: any) => f.name.toLowerCase() === field.name.toLowerCase());
  }
}
