import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../services/api';
import { ProjectContextService } from '../../services/project-context';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-workflow-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  styles: [`
    :host { display: block; }
    .page-container { min-height: calc(100vh - 64px); background: #0b0f1a; color: #e2e8f0; font-family: 'Inter', sans-serif; overflow-y: auto; padding-bottom: 4rem; }

    /* ── Hero ──────────────────────────────── */
    .hero { position: relative; padding: 4rem 3rem; overflow: hidden; background: radial-gradient(circle at 10% 20%, rgba(16,185,129,0.05) 0%, transparent 40%); border-bottom: 1px solid rgba(255,255,255,0.03); }
    .hero-content { position: relative; z-index: 1; max-width: 800px; }
    .hero-title { font-size: 2.5rem; font-weight: 900; letter-spacing: -0.04em; margin: 0 0 1rem; color: #fff; line-height: 1.1; }
    .hero-title span { background: linear-gradient(135deg, #34d399, #10b981); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }
    .hero-sub { font-size: 1rem; color: #64748b; line-height: 1.6; margin: 0; max-width: 600px; }

    .toolbar { position: sticky; top: 0; padding: 1.5rem 3rem; background: rgba(11,15,26,0.8); backdrop-filter: blur(20px); border-bottom: 1px solid rgba(255,255,255,0.05); z-index: 20; display: flex; justify-content: space-between; align-items: center; }
    .search-box { position: relative; width: 300px; }
    .search-input { width: 100%; background: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.1); border-radius: 12px; padding: 0.625rem 1rem 0.625rem 2.5rem; color: #fff; font-size: 0.85rem; outline: none; }
    .search-input:focus { border-color: #10b981; }
    .search-icon { position: absolute; left: 0.875rem; top: 50%; transform: translateY(-50%); color: #475569; font-size: 1.1rem; }

    .btn-primary { display: flex; align-items: center; gap: 0.5rem; padding: 0.625rem 1.25rem; border-radius: 12px; border: none; background: #10b981; color: #fff; font-size: 0.8rem; font-weight: 800; cursor: pointer; transition: all 0.2s; box-shadow: 0 8px 20px rgba(16,185,129,0.2); }
    .btn-primary:hover { background: #34d399; transform: translateY(-1px); }

    /* ── Grid ──────────────────────────────── */
    .grid-section { padding: 3rem; }
    .workflow-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 1.5rem; }

    /* ── Card ──────────────────────────────── */
    .wf-card { background: rgba(15,23,42,0.6); border: 1px solid rgba(255,255,255,0.06); border-radius: 24px; padding: 1.5rem; transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1); cursor: pointer; display: flex; flex-direction: column; gap: 1.25rem; }
    .wf-card:hover { border-color: rgba(16,185,129,0.4); transform: translateY(-4px); box-shadow: 0 20px 40px rgba(0,0,0,0.4); }
    
    .card-head { display: flex; justify-content: space-between; align-items: flex-start; }
    .icon-box { width: 48px; height: 48px; border-radius: 14px; background: rgba(16,185,129,0.1); display: flex; align-items: center; justify-content: center; color: #34d399; }
    .wf-card:hover .icon-box { background: #10b981; color: #fff; }

    .status-pill { font-size: 0.6rem; font-weight: 900; text-transform: uppercase; padding: 0.25rem 0.625rem; border-radius: 999px; background: rgba(16,185,129,0.1); color: #34d399; border: 1px solid rgba(16,185,129,0.1); }

    .card-body { display: flex; flex-direction: column; gap: 0.5rem; }
    .card-name { font-size: 1.1rem; font-weight: 800; color: #f1f5f9; margin: 0; }
    .card-stats { display: flex; align-items: center; gap: 1rem; margin-top: 0.5rem; }
    .stat { display: flex; align-items: center; gap: 0.375rem; font-size: 0.7rem; color: #475569; font-weight: 600; }
    .stat i { font-size: 1rem; opacity: 0.5; }

    .card-foot { display: flex; align-items: center; justify-content: space-between; padding-top: 1.25rem; border-top: 1px solid rgba(255,255,255,0.04); }
    .time { font-size: 0.7rem; color: #334155; font-weight: 600; }
    .actions { display: flex; gap: 0.5rem; opacity: 0; transition: opacity 0.2s; }
    .wf-card:hover .actions { opacity: 1; }
    .action-btn { width: 32px; height: 32px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.05); background: transparent; color: #475569; display: flex; align-items: center; justify-content: center; cursor: pointer; transition: all 0.2s; }
    .action-btn:hover { background: rgba(239,68,68,0.1); color: #f87171; border-color: rgba(239,68,68,0.2); }

    .empty-state { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 8rem 0; opacity: 0.15; text-align: center; }
    .empty-state i { font-size: 5rem; margin-bottom: 1.5rem; }
    .empty-state p { font-size: 1.25rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.3em; }

    /* Modal */
    .modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(8px); z-index: 100; display: flex; align-items: center; justify-content: center; padding: 1rem; }
    .modal-card { background: #0f172a; border: 1px solid rgba(255,255,255,0.1); border-radius: 32px; width: 100%; max-width: 480px; padding: 2.5rem; }
    .modal-title { font-size: 1.5rem; font-weight: 900; margin-bottom: 2rem; color: #fff; }
    .input-group { display: flex; flex-direction: column; gap: 0.5rem; margin-bottom: 1.5rem; }
    .i-label { font-size: 0.7rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; color: #475569; }
    .i-field { background: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.1); border-radius: 14px; padding: 1rem; color: #fff; font-size: 1rem; outline: none; }
    .modal-actions { display: flex; justify-content: flex-end; gap: 1rem; margin-top: 2rem; }
    .btn-cancel { background: transparent; border: none; color: #475569; font-weight: 700; cursor: pointer; }
  `],
  template: `
    <div class="page-container">
      <div class="hero">
        <div class="hero-content">
          <h1 class="hero-title">Process <span>Orchestration</span></h1>
          <p class="hero-sub">Design, manage and monitor complex business logic workflows using Elsa 3.0 distributed runtime.</p>
        </div>
      </div>

      <div class="toolbar">
        <div class="search-box">
          <i class="material-icons-outlined search-icon">search</i>
          <input type="text" placeholder="Search workflows..." class="search-input" [(ngModel)]="searchQuery">
        </div>
        <button class="btn-primary" (click)="showNewModal = true">
          <i class="material-icons-outlined">add</i>
          New Workflow
        </button>
      </div>

      <div class="grid-section">
        <div class="workflow-grid" *ngIf="filteredWorkflows.length > 0; else empty">
          <div *ngFor="let wf of filteredWorkflows" class="wf-card" (click)="openDesigner(wf.id)">
            <div class="card-head">
              <div class="icon-box">
                <i class="material-icons-outlined">account_tree</i>
              </div>
              <span class="status-pill">Active</span>
            </div>

            <div class="card-body">
              <h3 class="card-name">{{ wf.name }}</h3>
              <div class="card-stats">
                <div class="stat">
                  <i class="material-icons-outlined">settings_input_component</i>
                  {{ wf.nodes?.length || 0 }} Steps
                </div>
                <div class="stat">
                  <i class="material-icons-outlined">timeline</i>
                  Production
                </div>
              </div>
            </div>

            <div class="card-foot">
              <span class="time">Modified 2h ago</span>
              <div class="actions">
                <button class="action-btn" (click)="deleteWorkflow($event, wf.id)">
                  <i class="material-icons-outlined" style="font-size:1.1rem">delete</i>
                </button>
              </div>
            </div>
          </div>
        </div>

        <ng-template #empty>
          <div class="empty-state">
            <i class="material-icons-outlined">folder_open</i>
            <p>No Workflows Found</p>
          </div>
        </ng-template>
      </div>

      <!-- New Workflow Modal -->
      <div class="modal-overlay" *ngIf="showNewModal" (click)="showNewModal = false">
        <div class="modal-card" (click)="$event.stopPropagation()">
          <h2 class="modal-title">Create Workflow</h2>
          <div class="input-group">
            <label class="i-label">Workflow Name</label>
            <input type="text" [(ngModel)]="newName" class="i-field" placeholder="e.g. Patient Onboarding" autofocus>
          </div>
          <div class="modal-actions">
            <button class="btn-cancel" (click)="showNewModal = false">Discard</button>
            <button class="btn-primary" (click)="createWorkflow()" [disabled]="!newName.trim()">Create & Open</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class WorkflowDashboard implements OnInit {
  projectId: string | null = null;
  workflows: any[] = [];
  searchQuery = '';
  showNewModal = false;
  newName = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly api: ApiService,
    private readonly projectContext: ProjectContextService
  ) {
    this.projectId = this.route.snapshot.paramMap.get('projectId');
    if (this.projectId) {
      this.projectContext.setProjectId(this.projectId);
    }
  }

  ngOnInit() {
    this.loadWorkflows();
  }

  loadWorkflows() {
    if (!this.projectId) return;
    this.api.getWorkflows(this.projectId).subscribe({
      next: (artifacts) => {
        this.workflows = artifacts.map(a => {
          const content = JSON.parse(a.content);
          return { ...content, id: a.id };
        });
      }
    });
  }

  get filteredWorkflows() {
    return this.workflows.filter(w => 
      w.name.toLowerCase().includes(this.searchQuery.toLowerCase())
    );
  }

  createWorkflow() {
    if (!this.newName.trim() || !this.projectId) return;

    const newWf = {
      name: this.newName,
      nodes: [],
      connections: []
    };

    this.api.createWorkflow(this.projectId, newWf).subscribe({
      next: (artifact) => {
        this.showNewModal = false;
        this.openDesigner(artifact.id);
      }
    });
  }

  openDesigner(id: string) {
    this.router.navigate(['/projects', this.projectId, 'workflows', id]);
  }

  deleteWorkflow(event: Event, id: string) {
    event.stopPropagation();
    if (!confirm('Delete this workflow permanently?')) return;
    this.api.deleteWorkflow(this.projectId!, id).subscribe(() => {
      this.loadWorkflows();
    });
  }
}
