import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, AiProvider } from '../../../services/api';

const PROVIDER_PRESETS: Record<string, { baseUrl: string; model: string }> = {
  'OpenAI':   { baseUrl: 'https://api.openai.com/v1',               model: 'gpt-4o' },
  'NVIDIA':   { baseUrl: 'https://integrate.api.nvidia.com/v1',      model: 'minimaxai/minimax-m2.7' },
  'Groq':     { baseUrl: 'https://api.groq.com/openai/v1',           model: 'llama-3.3-70b-versatile' },
  'Mistral':  { baseUrl: 'https://api.mistral.ai/v1',               model: 'mistral-large-latest' },
  'Ollama':   { baseUrl: 'http://localhost:11434/v1',                model: 'llama3' },
  'Custom':   { baseUrl: '',                                          model: '' }
};

@Component({
  selector: 'app-ai-providers',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  styles: [`
    .page { padding: 2rem; max-width: 860px; margin: 0 auto; }
    .page-header { display: flex; align-items: center; gap: 1rem; margin-bottom: 2rem; }
    .header-icon { width: 44px; height: 44px; border-radius: 12px; background: rgba(139,92,246,0.15); border: 1px solid rgba(139,92,246,0.3); display: flex; align-items: center; justify-content: center; color: #a78bfa; font-size: 1.25rem; }
    .header-title { font-size: 1.25rem; font-weight: 900; color: #fff; }
    .header-sub { font-size: 0.7rem; color: #64748b; margin-top: 2px; }

    .warning-banner { display: flex; align-items: flex-start; gap: 0.75rem; padding: 1rem; background: rgba(245,158,11,0.05); border: 1px solid rgba(245,158,11,0.2); border-radius: 16px; margin-bottom: 1.5rem; color: #fbbf24; }
    .warning-banner p { margin: 0; font-size: 0.8rem; color: #94a3b8; }
    .warning-banner strong { display: block; font-size: 0.875rem; color: #fbbf24; margin-bottom: 2px; }

    .provider-card { background: rgba(255,255,255,0.02); border: 1px solid rgba(255,255,255,0.06); border-radius: 20px; padding: 1.25rem; margin-bottom: 0.75rem; transition: border-color 0.2s; }
    .provider-card.is-default { background: rgba(139,92,246,0.05); border-color: rgba(139,92,246,0.3); }
    .provider-card-inner { display: flex; align-items: flex-start; justify-content: space-between; }
    .provider-icon { width: 40px; height: 40px; border-radius: 10px; background: rgba(255,255,255,0.04); display: flex; align-items: center; justify-content: center; margin-right: 0.875rem; flex-shrink: 0; }
    .provider-icon.default { background: rgba(139,92,246,0.15); color: #a78bfa; }
    .provider-icon.normal { color: #475569; }
    .provider-name { font-size: 0.875rem; font-weight: 800; color: #f1f5f9; display: flex; align-items: center; gap: 0.5rem; }
    .default-badge { font-size: 0.65rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.08em; background: rgba(139,92,246,0.2); color: #a78bfa; border: 1px solid rgba(139,92,246,0.3); padding: 2px 8px; border-radius: 999px; }
    .provider-url { font-size: 0.72rem; color: #475569; font-family: monospace; margin-top: 2px; }
    .provider-meta { display: flex; gap: 0.75rem; margin-top: 6px; font-size: 0.68rem; color: #334155; }
    .status-badge { display: inline-flex; align-items: center; gap: 4px; font-size: 0.65rem; font-weight: 700; padding: 3px 10px; border-radius: 999px; }
    .status-ok { background: rgba(16,185,129,0.1); color: #34d399; }
    .status-warn { background: rgba(245,158,11,0.1); color: #fbbf24; }
    .status-error { background: rgba(239,68,68,0.1); color: #f87171; }
    .provider-actions { display: flex; align-items: center; gap: 0.5rem; margin-top: 1rem; padding-top: 1rem; border-top: 1px solid rgba(255,255,255,0.04); }
    .btn-danger { display: flex; align-items: center; gap: 4px; font-size: 0.7rem; font-weight: 800; color: #475569; background: none; border: none; cursor: pointer; padding: 6px 12px; border-radius: 8px; transition: all 0.15s; }
    .btn-danger:hover { color: #f87171; background: rgba(239,68,68,0.08); }

    .add-btn { width: 100%; padding: 1rem; border-radius: 20px; border: 2px dashed rgba(255,255,255,0.08); background: none; color: #475569; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 0.5rem; font-size: 0.875rem; font-weight: 700; transition: all 0.2s; margin-top: 0.5rem; }
    .add-btn:hover { border-color: rgba(139,92,246,0.4); color: #a78bfa; }

    /* Dialog */
    .overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.75); backdrop-filter: blur(4px); z-index: 100; display: flex; align-items: center; justify-content: center; padding: 1rem; }
    .dialog { background: #0f172a; border: 1px solid rgba(255,255,255,0.08); border-radius: 24px; width: 100%; max-width: 520px; max-height: 90vh; overflow-y: auto; }
    .dialog-header { display: flex; align-items: center; justify-content: space-between; padding: 1.5rem; border-bottom: 1px solid rgba(255,255,255,0.05); }
    .dialog-title { font-size: 1rem; font-weight: 900; color: #fff; display: flex; align-items: center; gap: 0.75rem; }
    .dialog-body { padding: 1.5rem; }
    .close-btn { background: none; border: none; color: #475569; cursor: pointer; padding: 4px; border-radius: 8px; transition: color 0.15s; }
    .close-btn:hover { color: #fff; }

    .presets { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 1.25rem; }
    .preset-btn { font-size: 0.7rem; font-weight: 800; padding: 5px 14px; border-radius: 999px; border: 1px solid rgba(255,255,255,0.1); background: none; cursor: pointer; color: #64748b; transition: all 0.15s; }
    .preset-btn:hover, .preset-btn.active { border-color: #7c3aed; background: rgba(124,58,237,0.15); color: #c4b5fd; }

    .form-group { margin-bottom: 1rem; }
    .form-label { display: block; font-size: 0.65rem; text-transform: uppercase; letter-spacing: 0.12em; font-weight: 900; color: #475569; margin-bottom: 6px; }
    .form-input { width: 100%; background: rgba(0,0,0,0.4); border: 1px solid rgba(255,255,255,0.08); border-radius: 12px; padding: 10px 14px; font-size: 0.875rem; color: #f1f5f9; outline: none; transition: border-color 0.15s; box-sizing: border-box; }
    .form-input:focus { border-color: #7c3aed; box-shadow: 0 0 0 3px rgba(124,58,237,0.1); }
    .form-input.mono { font-family: monospace; }
    .input-wrap { position: relative; }
    .eye-btn { position: absolute; right: 10px; top: 50%; transform: translateY(-50%); background: none; border: none; color: #475569; cursor: pointer; padding: 4px; }
    .eye-btn:hover { color: #94a3b8; }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
    .form-check { display: flex; align-items: center; gap: 0.5rem; cursor: pointer; margin-top: 0.25rem; }
    .form-check input { width: 14px; height: 14px; accent-color: #7c3aed; }
    .form-check span { font-size: 0.8rem; color: #94a3b8; }

    .test-result { display: flex; align-items: center; gap: 0.5rem; padding: 10px 14px; border-radius: 12px; margin-top: 0.75rem; font-size: 0.8rem; font-weight: 700; }
    .test-ok { background: rgba(16,185,129,0.08); border: 1px solid rgba(16,185,129,0.2); color: #34d399; }
    .test-fail { background: rgba(239,68,68,0.08); border: 1px solid rgba(239,68,68,0.2); color: #f87171; }

    .dialog-footer { display: flex; align-items: center; justify-content: space-between; padding: 1.25rem 1.5rem; border-top: 1px solid rgba(255,255,255,0.05); }
    .btn-ghost { background: none; border: none; color: #475569; font-size: 0.8rem; font-weight: 800; cursor: pointer; padding: 8px 14px; border-radius: 10px; transition: color 0.15s; }
    .btn-ghost:hover { color: #fff; }
    .btn-outline { display: flex; align-items: center; gap: 6px; background: none; border: 1px solid rgba(255,255,255,0.1); color: #94a3b8; font-size: 0.75rem; font-weight: 800; cursor: pointer; padding: 8px 16px; border-radius: 10px; transition: all 0.15s; }
    .btn-outline:hover:not(:disabled) { border-color: rgba(255,255,255,0.2); color: #fff; }
    .btn-outline:disabled { opacity: 0.4; cursor: not-allowed; }
    .btn-primary { display: flex; align-items: center; gap: 6px; background: #7c3aed; color: #fff; border: none; font-size: 0.8rem; font-weight: 900; cursor: pointer; padding: 8px 20px; border-radius: 10px; transition: background 0.15s; }
    .btn-primary:hover:not(:disabled) { background: #6d28d9; }
    .btn-primary:disabled { background: #1e293b; color: #475569; cursor: not-allowed; }
    .btn-actions { display: flex; align-items: center; gap: 0.5rem; }

    .loading-text { text-align: center; padding: 3rem; color: #475569; font-size: 0.875rem; }
    .empty-state { display: flex; flex-direction: column; align-items: center; padding: 2rem; color: #334155; font-size: 0.8rem; }

    .spin { animation: spin 1s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }

    .section-label { font-size: 0.65rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.12em; color: #475569; margin-bottom: 0.5rem; }
  `],
  template: `
    <div class="page">

      <!-- Header -->
      <div class="page-header">
        <div class="header-icon">
          <span class="material-icons-outlined">key</span>
        </div>
        <div>
          <div class="header-title">AI Provider Keys</div>
          <div class="header-sub">Bring Your Own Key — your usage billed directly to your account</div>
        </div>
      </div>

      <!-- No Provider Warning -->
      <div class="warning-banner" *ngIf="!loading && providers.length === 0">
        <span class="material-icons-outlined" style="font-size:1.25rem;margin-top:2px">warning_amber</span>
        <div>
          <strong>No AI provider configured</strong>
          AI features are disabled. Add your API key below to enable schema generation, connector logic, and more.
        </div>
      </div>

      <!-- Loading -->
      <div class="loading-text" *ngIf="loading">
        <span class="material-icons-outlined spin">sync</span>
        <span style="margin-left:8px">Loading providers...</span>
      </div>

      <!-- Provider Cards -->
      <div *ngIf="!loading">
        <div *ngFor="let p of providers"
             class="provider-card"
             [class.is-default]="p.isDefault">
          <div class="provider-card-inner">
            <div style="display:flex;align-items:flex-start">
              <div class="provider-icon" [class.default]="p.isDefault" [class.normal]="!p.isDefault">
                <span class="material-icons-outlined">smart_toy</span>
              </div>
              <div>
                <div class="provider-name">
                  {{ p.name }}
                  <span class="default-badge" *ngIf="p.isDefault">⭐ Default</span>
                </div>
                <div class="provider-url">{{ p.baseUrl }}</div>
                <div class="provider-meta">
                  <span>Key: {{ p.apiKeyPreview }}</span>
                  <span>·</span>
                  <span>Model: {{ p.defaultModel }}</span>
                </div>
              </div>
            </div>
            <div>
              <span class="status-badge"
                    [class.status-ok]="p.lastTestStatus === 'OK'"
                    [class.status-warn]="p.lastTestStatus === 'Untested'"
                    [class.status-error]="p.lastTestStatus === 'Error' || p.lastTestStatus === 'AuthError'">
                <span class="material-icons-outlined" style="font-size:0.75rem">
                  {{ p.lastTestStatus === 'OK' ? 'check_circle' : p.lastTestStatus === 'Untested' ? 'schedule' : 'error_outline' }}
                </span>
                {{ p.lastTestStatus }}
              </span>
            </div>
          </div>
          <div class="provider-actions">
            <button class="btn-danger" (click)="deleteProvider(p)">
              <span class="material-icons-outlined" style="font-size:1rem">delete_outline</span>
              Remove
            </button>
          </div>
        </div>

        <!-- Add Button -->
        <button class="add-btn" (click)="showAddDialog = true">
          <span class="material-icons-outlined">add_circle_outline</span>
          Add AI Provider
        </button>
      </div>
    </div>

    <!-- Dialog Overlay -->
    <div class="overlay" *ngIf="showAddDialog" (click)="closeDialog($event)">
      <div class="dialog" (click)="$event.stopPropagation()">

        <div class="dialog-header">
          <div class="dialog-title">
            <div class="header-icon" style="width:36px;height:36px">
              <span class="material-icons-outlined" style="font-size:1.1rem">add_link</span>
            </div>
            Add AI Provider
          </div>
          <button class="close-btn" (click)="showAddDialog = false">
            <span class="material-icons-outlined">close</span>
          </button>
        </div>

        <div class="dialog-body">
          <div class="section-label" style="margin-bottom:0.5rem">Quick Setup</div>
          <div class="presets">
            <button *ngFor="let key of presetKeys"
                    class="preset-btn"
                    [class.active]="selectedPreset === key"
                    (click)="applyPreset(key)">{{ key }}</button>
          </div>

          <div class="form-group">
            <label class="form-label">Provider Label</label>
            <input class="form-input" [(ngModel)]="form.name" placeholder="e.g., My NVIDIA Key" />
          </div>
          <div class="form-group">
            <label class="form-label">Base URL</label>
            <input class="form-input mono" [(ngModel)]="form.baseUrl" placeholder="https://integrate.api.nvidia.com/v1" />
          </div>
          <div class="form-group">
            <label class="form-label">API Key</label>
            <div class="input-wrap">
              <input class="form-input mono" [type]="showKey ? 'text' : 'password'" [(ngModel)]="form.apiKey" placeholder="nvapi-••••••••" style="padding-right:40px" />
              <button class="eye-btn" (click)="showKey = !showKey">
                <span class="material-icons-outlined" style="font-size:1.1rem">{{ showKey ? 'visibility_off' : 'visibility' }}</span>
              </button>
            </div>
          </div>
          <div class="form-group">
            <label class="form-label">Default Model</label>
            <input class="form-input mono" [(ngModel)]="form.defaultModel" placeholder="e.g., minimaxai/minimax-m2.7" />
          </div>
          <div class="form-row">
            <div class="form-group">
              <label class="form-label">Max Tokens</label>
              <input class="form-input" type="number" [(ngModel)]="form.maxTokens" />
            </div>
            <div class="form-group">
              <label class="form-label">Timeout (s)</label>
              <input class="form-input" type="number" [(ngModel)]="form.timeoutSeconds" />
            </div>
          </div>
          <label class="form-check">
            <input type="checkbox" [(ngModel)]="form.isDefault" />
            <span>Set as default provider</span>
          </label>

          <div class="test-result test-ok" *ngIf="testResult?.status === 'OK'">
            <span class="material-icons-outlined" style="font-size:1rem">check_circle</span>
            Connected — {{ testResult?.response }}
          </div>
          <div class="test-result test-fail" *ngIf="testResult && testResult.status !== 'OK'">
            <span class="material-icons-outlined" style="font-size:1rem">error_outline</span>
            {{ testResult?.message || 'Connection failed' }}
          </div>
        </div>

        <div class="dialog-footer">
          <button class="btn-outline" (click)="testConnection()" [disabled]="testing || !form.baseUrl || !form.apiKey">
            <span class="material-icons-outlined" style="font-size:1rem" [class.spin]="testing">{{ testing ? 'sync' : 'wifi_tethering' }}</span>
            {{ testing ? 'Testing...' : 'Test Connection' }}
          </button>
          <div class="btn-actions">
            <button class="btn-ghost" (click)="showAddDialog = false">Cancel</button>
            <button class="btn-primary" (click)="saveProvider()" [disabled]="saving || !form.name || !form.apiKey">
              <span class="material-icons-outlined" style="font-size:1rem" [class.spin]="saving">{{ saving ? 'sync' : 'save' }}</span>
              Save Provider
            </button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class AiProvidersComponent implements OnInit {
  providers: AiProvider[] = [];
  loading = true;
  showAddDialog = false;
  showKey = false;
  testing = false;
  saving = false;
  testResult: any = null;
  selectedPreset = 'NVIDIA';
  presetKeys = Object.keys(PROVIDER_PRESETS);

  form = {
    name: 'NVIDIA Fast',
    baseUrl: PROVIDER_PRESETS['NVIDIA'].baseUrl,
    apiKey: '',
    defaultModel: PROVIDER_PRESETS['NVIDIA'].model,
    maxTokens: 8192,
    timeoutSeconds: 120,
    isDefault: true
  };

  constructor(private api: ApiService) {}

  ngOnInit() { this.loadProviders(); }

  loadProviders() {
    this.loading = true;
    this.api.getAiProviders().subscribe({
      next: (p: AiProvider[]) => { this.providers = p; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  applyPreset(preset: string) {
    this.selectedPreset = preset;
    const p = PROVIDER_PRESETS[preset];
    this.form.baseUrl = p.baseUrl;
    this.form.defaultModel = p.model;
    this.form.name = preset === 'Custom' ? '' : preset + ' Key';
  }

  testConnection() {
    this.testing = true;
    this.testResult = null;
    this.api.testAiProvider(this.form.baseUrl, this.form.apiKey, this.form.defaultModel).subscribe({
      next: (r: any) => { this.testResult = r; this.testing = false; },
      error: (e: any) => { this.testResult = { status: 'Error', message: e.message }; this.testing = false; }
    });
  }

  saveProvider() {
    this.saving = true;
    this.api.addAiProvider(this.form).subscribe({
      next: () => { this.saving = false; this.showAddDialog = false; this.resetForm(); this.loadProviders(); },
      error: (e: any) => { this.saving = false; alert('Failed: ' + e.message); }
    });
  }

  deleteProvider(p: AiProvider) {
    if (!confirm(`Remove "${p.name}"?`)) return;
    this.api.deleteAiProvider(p.id).subscribe({ next: () => this.loadProviders() });
  }

  closeDialog(e: Event) { if (e.target === e.currentTarget) this.showAddDialog = false; }

  private resetForm() {
    this.form = { name: '', baseUrl: '', apiKey: '', defaultModel: '', maxTokens: 8192, timeoutSeconds: 120, isDefault: false };
    this.testResult = null;
    this.selectedPreset = '';
  }
}
