import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ApiService } from '../../services/api';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-form-list',
    standalone: true,
    imports: [CommonModule, RouterModule, FormsModule],
    styles: [`
      :host { display: block; }
      .dashboard-container { min-height: calc(100vh - 64px); background: #0b1120; color: #e2e8f0; font-family: 'Inter', sans-serif; p: 3rem; }

      /* ── Header ──────────────────────────────── */
      .header-section { display: flex; justify-content: space-between; align-items: center; margin-bottom: 2.5rem; }
      .title-wrap h1 { font-size: 2.25rem; font-weight: 900; color: #fff; margin: 0 0 0.5rem; letter-spacing: -0.025em; }
      .title-wrap h1 span { background: linear-gradient(135deg, #60a5fa, #a78bfa); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }
      .subtitle { color: #64748b; font-size: 0.95rem; margin: 0; }

      .btn-new { background: #2563eb; color: #fff; border: none; font-size: 0.875rem; font-weight: 800; padding: 0.75rem 1.5rem; border-radius: 14px; cursor: pointer; transition: all 0.2s; box-shadow: 0 8px 16px rgba(37,99,235,0.2); display: flex; align-items: center; gap: 0.5rem; }
      .btn-new:hover { background: #3b82f6; transform: translateY(-1px); box-shadow: 0 12px 20px rgba(37,99,235,0.3); }

      /* ── Filters ─────────────────────────────── */
      .filter-toolbar { display: flex; align-items: center; gap: 1.5rem; background: rgba(15,23,42,0.4); border: 1px solid rgba(255,255,255,0.05); padding: 1rem 1.5rem; border-radius: 20px; margin-bottom: 2rem; backdrop-filter: blur(16px); }
      .filter-label { font-size: 0.75rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; color: #64748b; display: flex; align-items: center; gap: 0.5rem; }
      .filter-select { background: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.08); border-radius: 12px; padding: 0.5rem 1rem; color: #fff; font-size: 0.875rem; outline: none; transition: border-color 0.2s; cursor: pointer; }
      .filter-select:focus { border-color: #3b82f6; }
      .filter-select option { background: #0f172a; color: #fff; }

      /* ── Cards Grid ───────────────────────────── */
      .cards-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 1.5rem; }
      
      .form-card { background: rgba(15,23,42,0.6); border: 1px solid rgba(255,255,255,0.06); border-radius: 24px; padding: 1.75rem; transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1); cursor: pointer; display: flex; flex-direction: column; gap: 1.25rem; backdrop-filter: blur(20px); position: relative; overflow: hidden; }
      .form-card:hover { border-color: rgba(99,102,241,0.4); transform: translateY(-4px); box-shadow: 0 20px 40px rgba(0,0,0,0.4); }
      .card-accent { position: absolute; top: 0; left: 0; width: 4px; height: 100%; background: rgba(99,102,241,0.3); transition: background 0.3s; }
      .form-card:hover .card-accent { background: #6366f1; }

      .card-header { display: flex; justify-content: space-between; align-items: flex-start; }
      .card-title { font-size: 1.2rem; font-weight: 800; color: #f1f5f9; margin: 0; transition: color 0.2s; }
      .form-card:hover .card-title { color: #818cf8; }
      
      .badge-mode { font-size: 0.65rem; font-weight: 900; text-transform: uppercase; padding: 0.25rem 0.75rem; border-radius: 9999px; letter-spacing: 0.05em; border: 1px solid currentColor; }
      .badge-mode.create { color: #34d399; background: rgba(16,185,129,0.05); }
      .badge-mode.edit { color: #60a5fa; background: rgba(59,130,246,0.05); }
      .badge-mode.view { color: #f87171; background: rgba(239,68,68,0.05); }

      .card-body { display: flex; flex-direction: column; gap: 0.5rem; }
      .entity-target-row { display: flex; align-items: center; gap: 0.5rem; font-size: 0.8rem; color: #94a3b8; font-weight: 600; }
      .entity-target-row .material-icons-outlined { font-size: 1rem; color: #818cf8; }
      .field-count { font-size: 0.75rem; color: #475569; font-weight: 500; }

      .card-footer { display: flex; justify-content: space-between; align-items: center; padding-top: 1rem; border-top: 1px solid rgba(255,255,255,0.04); font-size: 0.75rem; color: #475569; font-weight: 600; }

      /* ── Empty State ─────────────────────────── */
      .empty-state { text-align: center; padding: 6rem 2rem; background: rgba(15,23,42,0.3); border: 1px dashed rgba(255,255,255,0.06); border-radius: 32px; color: #64748b; }
      .empty-icon { font-size: 4rem; color: #334155; margin-bottom: 1.5rem; }
      .empty-title { font-size: 1.25rem; font-weight: 800; color: #cbd5e1; margin-bottom: 0.5rem; }

      /* ── Modal ───────────────────────────────── */
      .modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(8px); z-index: 100; display: flex; align-items: center; justify-content: center; padding: 1rem; }
      .modal-card { background: #0f172a; border: 1px solid rgba(255,255,255,0.1); border-radius: 32px; width: 100%; max-width: 500px; padding: 2.5rem; box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5); }
      .modal-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 2rem; }
      .modal-title { font-size: 1.5rem; font-weight: 900; color: #fff; display: flex; align-items: center; gap: 1rem; }
      .modal-title .material-icons-outlined { color: #818cf8; font-size: 2rem; }
      .close-modal { background: transparent; border: none; color: #475569; cursor: pointer; transition: color 0.2s; }
      .close-modal:hover { color: #fff; }

      .input-group { display: flex; flex-direction: column; gap: 0.5rem; margin-bottom: 1.5rem; }
      .input-label { font-size: 0.7rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; color: #64748b; }
      .input-field { background: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.08); border-radius: 14px; padding: 0.875rem 1.125rem; color: #fff; font-size: 1rem; outline: none; transition: all 0.2s; }
      .input-field:focus { border-color: #3b82f6; box-shadow: 0 0 0 4px rgba(59,130,246,0.1); }
      
      .modal-foot { display: flex; justify-content: flex-end; gap: 1rem; margin-top: 2rem; }
      .btn-cancel { background: transparent; border: none; color: #475569; font-size: 0.9rem; font-weight: 700; cursor: pointer; padding: 0.75rem 1.5rem; border-radius: 12px; transition: color 0.2s; }
      .btn-cancel:hover { color: #fff; }
      .btn-create { background: #2563eb; color: #fff; border: none; font-size: 0.9rem; font-weight: 800; cursor: pointer; padding: 0.75rem 2rem; border-radius: 14px; transition: all 0.2s; }
      .btn-create:hover { background: #3b82f6; }
    `],
    template: `
    <div class="dashboard-container">
      <!-- Header -->
      <div class="header-section">
        <div class="title-wrap">
           <h1>Form <span>Dashboard</span></h1>
           <p class="subtitle">Design, generate, and govern high-fidelity dynamic interfaces.</p>
        </div>
        <button (click)="openNewModal()" class="btn-new">
          <span class="material-icons-outlined">add</span>
          New Interface
        </button>
      </div>

      <!-- Filters -->
      <div class="filter-toolbar">
         <div class="filter-label">
            <span class="material-icons-outlined" style="font-size:1.1rem; color:#818cf8;">filter_alt</span>
            Scope View
         </div>
         <select [(ngModel)]="selectedEntityFilter" class="filter-select">
            <option value="">All Domain Entities</option>
            <option *ngFor="let entity of entities" [value]="entity.name">{{ entity.name | uppercase }}</option>
         </select>
      </div>

      <!-- Grid list -->
      <div *ngIf="getFilteredForms().length > 0; else noForms" class="cards-grid">
        <div *ngFor="let form of getFilteredForms()" class="form-card" (click)="editForm(form.id)">
           <div class="card-accent"></div>
           
           <div class="card-header">
              <h3 class="card-title">{{ form.name }}</h3>
              <span class="badge-mode" [ngClass]="getFormModeClass(form)">
                 {{ getFormModeText(form) }}
              </span>
           </div>

           <div class="card-body">
              <div class="entity-target-row">
                 <span class="material-icons-outlined">layers</span>
                 <span>Target: {{ getEntityTarget(form) }}</span>
              </div>
              <div class="field-count">
                 Contains {{ getFieldCount(form) }} mapped atomic fields
              </div>
           </div>

           <div class="card-footer">
              <div>
                 Modified {{ form.lastModified | date:'MMM d, y, h:mm a' }}
              </div>
              <span class="material-icons-outlined" style="font-size: 1.1rem; color: #475569;">arrow_forward</span>
           </div>
        </div>
      </div>

      <!-- Empty State -->
      <ng-template #noForms>
         <div class="empty-state">
            <span class="material-icons-outlined empty-icon">space_dashboard</span>
            <div class="empty-title">No Dynamic Forms Found</div>
            <p style="margin: 0;">Create an entity in the designer or initialize a new form canvas to get started.</p>
         </div>
      </ng-template>

      <!-- New Form Modal -->
      <div *ngIf="showModal" class="modal-overlay" (click)="closeOnBackdrop($event)">
        <div class="modal-card" (click)="$event.stopPropagation()">
          <div class="modal-head">
            <div class="modal-title">
              <span class="material-icons-outlined">dynamic_form</span>
              Initialize Form
            </div>
            <button class="close-modal" (click)="showModal = false">
              <span class="material-icons-outlined">close</span>
            </button>
          </div>

          <div class="input-group">
            <label class="input-label">Form Schema Identifier</label>
            <input type="text" [(ngModel)]="newFormName" placeholder="e.g. PatientRegistrationForm" class="input-field" autofocus/>
          </div>

          <div class="input-group">
            <label class="input-label">Target Domain Entity</label>
            <select [(ngModel)]="targetEntity" class="input-field" style="appearance: none; cursor: pointer;">
               <option value="" disabled selected>Select Entity</option>
               <option *ngFor="let entity of entities" [value]="entity.name">{{ entity.name }}</option>
            </select>
          </div>

          <div class="modal-foot">
            <button class="btn-cancel" (click)="showModal = false">Discard</button>
            <button (click)="createForm()" [disabled]="!newFormName || !targetEntity" class="btn-create">
              Initialize Stack
            </button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class FormListComponent implements OnInit {
    forms: any[] = [];
    entities: any[] = [];
    projectId: string = '';
    showModal = false;
    newFormName = '';
    targetEntity = '';
    selectedEntityFilter = '';

    constructor(
        private readonly api: ApiService,
        private readonly route: ActivatedRoute,
        private readonly router: Router
    ) { }

    ngOnInit() {
        this.route.parent?.paramMap.subscribe(params => {
            this.projectId = params.get('id') || params.get('projectId') || '';
            if (this.projectId) {
                this.loadForms();
                this.loadEntities();
            }
        });
    }

    loadForms() {
        this.api.getForms(this.projectId).subscribe(data => this.forms = data);
    }

    loadEntities() {
        this.api.getEntities(this.projectId).subscribe(data => this.entities = data);
    }

    openNewModal() {
        this.showModal = true;
        this.newFormName = '';
        this.targetEntity = '';
    }

    createForm() {
        if (!this.newFormName || !this.targetEntity) return;

        const metadata = {
            Name: this.newFormName,
            EntityTarget: this.targetEntity,
            Layout: 'Vertical',
            Sections: [],
            Fields: [],
            Context: {
                Mode: 0, // Default to Create
                ParentEntityId: null,
                AdditionalData: {}
            }
        };

        this.api.createForm(this.projectId, metadata).subscribe(res => {
            this.showModal = false;
            this.loadForms();
        });
    }

    editForm(id: string) {
        this.router.navigate([id], { relativeTo: this.route });
    }

    closeOnBackdrop(e: Event) {
        if (e.target === e.currentTarget) this.showModal = false;
    }

    // ── Parsers for UI Badges & Metadata ─────────────────────────────────────
    private getParsedContent(form: any): any {
        try {
            return typeof form.content === 'string' ? JSON.parse(form.content) : form.content;
        } catch {
            return {};
        }
    }

    getEntityTarget(form: any): string {
        const meta = this.getParsedContent(form);
        return meta.EntityTarget || meta.entityTarget || 'Unknown';
    }

    getFieldCount(form: any): number {
        const meta = this.getParsedContent(form);
        return meta.Fields?.length || meta.fields?.length || 0;
    }

    getFormModeText(form: any): string {
        const meta = this.getParsedContent(form);
        const mode = meta.Context?.Mode ?? meta.context?.mode ?? 0;
        switch (mode) {
            case 0: return 'Create';
            case 1: return 'Edit';
            case 2: return 'View';
            case 3: return 'Clone';
            case 4: return 'InlineEdit';
            default: return 'Form';
        }
    }

    getFormModeClass(form: any): string {
        const text = this.getFormModeText(form).toLowerCase();
        if (text === 'create') return 'create';
        if (text === 'edit') return 'edit';
        return 'view';
    }

    getFilteredForms(): any[] {
        if (!this.selectedEntityFilter) return this.forms;
        return this.forms.filter(f => {
            const target = this.getEntityTarget(f);
            return target.toLowerCase() === this.selectedEntityFilter.toLowerCase();
        });
    }
}
