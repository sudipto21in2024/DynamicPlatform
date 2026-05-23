import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api';
import { ProjectContextService } from '../../services/project-context';

@Component({
  selector: 'app-projects-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  styles: [`
    :host { display: block; }
    .page-container { min-height: calc(100vh - 64px); background: #0b1120; color: #e2e8f0; font-family: 'Inter', sans-serif; overflow-y: auto; scrollbar-width: thin; }

    /* ── Hero ──────────────────────────────── */
    .hero { position: relative; padding: 4rem 3rem 6rem; overflow: hidden; background: radial-gradient(circle at 10% 20%, rgba(37,99,235,0.05) 0%, transparent 40%); }
    .hero-content { position: relative; z-index: 1; max-width: 800px; }
    .hero-title { font-size: 3rem; font-weight: 900; letter-spacing: -0.04em; margin: 0 0 1rem; color: #fff; line-height: 1; }
    .hero-title span { background: linear-gradient(135deg, #60a5fa, #a78bfa); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }
    .hero-sub { font-size: 1.125rem; color: #64748b; line-height: 1.6; margin: 0; max-width: 600px; }

    /* ── Grid ──────────────────────────────── */
    .grid-section { padding: 0 3rem 4rem; margin-top: -3rem; position: relative; z-index: 10; }
    .project-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(340px, 1fr)); gap: 1.5rem; }

    /* ── Project Card ────────────────────────── */
    .card { background: rgba(15,23,42,0.6); border: 1px solid rgba(255,255,255,0.06); border-radius: 24px; padding: 1.5rem; text-decoration: none; transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1); display: flex; flex-direction: column; gap: 1.25rem; backdrop-filter: blur(20px); }
    .card:hover { border-color: rgba(59,130,246,0.4); transform: translateY(-4px); box-shadow: 0 20px 40px rgba(0,0,0,0.4); }
    
    .card-header { display: flex; justify-content: space-between; align-items: flex-start; }
    .card-icon-box { width: 52px; height: 52px; border-radius: 16px; background: rgba(59,130,246,0.1); display: flex; align-items: center; justify-content: center; color: #60a5fa; transition: all 0.3s; }
    .card:hover .card-icon-box { background: #2563eb; color: #fff; box-shadow: 0 8px 16px rgba(37,99,235,0.3); }
    
    .card-badges { display: flex; flex-direction: column; align-items: flex-end; gap: 0.5rem; }
    .badge-status { font-size: 0.65rem; font-weight: 900; text-transform: uppercase; color: #34d399; background: rgba(16,185,129,0.1); padding: 0.25rem 0.75rem; border-radius: 9999px; letter-spacing: 0.05em; border: 1px solid rgba(16,185,129,0.1); }
    .card-ver { font-size: 0.7rem; color: #334155; font-family: 'JetBrains Mono', monospace; font-weight: 700; }

    .card-body { display: flex; flex-direction: column; gap: 0.5rem; }
    .card-name { font-size: 1.25rem; font-weight: 800; color: #f1f5f9; margin: 0; transition: color 0.2s; }
    .card:hover .card-name { color: #60a5fa; }
    .card-desc { font-size: 0.875rem; color: #64748b; line-height: 1.5; margin: 0; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; height: 2.6rem; }

    .card-footer { display: flex; justify-content: space-between; align-items: center; padding-top: 1.25rem; border-top: 1px solid rgba(255,255,255,0.04); }
    .meta-item { display: flex; align-items: center; gap: 0.375rem; font-size: 0.75rem; color: #475569; font-weight: 600; }
    .meta-item .material-icons-outlined { font-size: 1rem; opacity: 0.6; }

    /* ── New Project Card ────────────────────── */
    .new-btn { background: transparent; border: 2px dashed rgba(255,255,255,0.06); border-radius: 24px; padding: 2rem; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 1rem; cursor: pointer; transition: all 0.2s; color: #334155; min-height: 200px; }
    .new-btn:hover { border-color: #3b82f6; background: rgba(59,130,246,0.02); color: #60a5fa; }
    .new-icon { width: 64px; height: 64px; border-radius: 50%; background: rgba(255,255,255,0.02); display: flex; align-items: center; justify-content: center; font-size: 2rem; transition: all 0.3s; }
    .new-btn:hover .new-icon { background: rgba(59,130,246,0.1); transform: scale(1.1); }
    .new-label { font-size: 1rem; font-weight: 800; }

    /* ── Modal ───────────────────────────────── */
    .modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(8px); z-index: 100; display: flex; align-items: center; justify-content: center; padding: 1rem; }
    .modal-card { background: #0f172a; border: 1px solid rgba(255,255,255,0.1); border-radius: 32px; width: 100%; max-width: 500px; padding: 2.5rem; box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5); }
    .modal-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 2rem; }
    .modal-title { font-size: 1.5rem; font-weight: 900; color: #fff; display: flex; align-items: center; gap: 1rem; }
    .modal-title .material-icons-outlined { color: #60a5fa; font-size: 2rem; }
    .close-modal { background: transparent; border: none; color: #475569; cursor: pointer; transition: color 0.2s; }
    .close-modal:hover { color: #fff; }

    .input-group { display: flex; flex-direction: column; gap: 0.5rem; margin-bottom: 1.5rem; }
    .input-label { font-size: 0.7rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; color: #475569; }
    .input-field { background: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.08); border-radius: 14px; padding: 0.875rem 1.125rem; color: #fff; font-size: 1rem; outline: none; transition: all 0.2s; }
    .input-field:focus { border-color: #3b82f6; box-shadow: 0 0 0 4px rgba(59,130,246,0.1); }
    .input-field::placeholder { color: #1e293b; }

    .modal-foot { display: flex; justify-content: flex-end; gap: 1rem; margin-top: 2rem; }
    .btn-cancel { background: transparent; border: none; color: #475569; font-size: 0.9rem; font-weight: 700; cursor: pointer; padding: 0.75rem 1.5rem; border-radius: 12px; transition: color 0.2s; }
    .btn-cancel:hover { color: #fff; }
    .btn-create { background: #2563eb; color: #fff; border: none; font-size: 0.9rem; font-weight: 800; cursor: pointer; padding: 0.75rem 2rem; border-radius: 14px; box-shadow: 0 8px 16px rgba(37,99,235,0.2); transition: all 0.2s; display: flex; align-items: center; gap: 0.5rem; }
    .btn-create:hover:not(:disabled) { background: #3b82f6; transform: translateY(-1px); box-shadow: 0 12px 20px rgba(37,99,235,0.3); }
    .btn-create:disabled { opacity: 0.5; cursor: not-allowed; }

    .spin { animation: spin 1s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
  `],
  template: `
    <div class="page-container">
      <div class="hero">
        <div class="hero-content">
          <h1 class="hero-title">Studio <span>Workspace</span></h1>
          <p class="hero-sub">Engineered for architectural precision. Create, manage, and deploy autonomous application clusters from a single command center.</p>
        </div>
      </div>

      <div class="grid-section">
        <div class="project-grid">
          <!-- Create New -->
          <div class="new-btn" (click)="openNewProjectModal()">
            <div class="new-icon"><span class="material-icons-outlined">add</span></div>
            <span class="new-label">Initialize New Stack</span>
          </div>

          <!-- Project Cards -->
          <a *ngFor="let project of projects" [routerLink]="['/projects', project.id, 'designer']" class="card">
            <div class="card-header">
              <div class="card-icon-box">
                <span class="material-icons-outlined" style="font-size:1.75rem">rocket_launch</span>
              </div>
              <div class="card-badges">
                <span class="badge-status">Production Ready</span>
                <span class="card-ver">v{{ project.version || '1.0.0' }}</span>
              </div>
            </div>
            
            <div class="card-body">
              <h3 class="card-name">{{ project.name }}</h3>
              <p class="card-desc">{{ project.description || 'Enterprise-grade microservice architecture awaiting definition.' }}</p>
            </div>

            <div class="card-footer">
              <div class="meta-item">
                <span class="material-icons-outlined">calendar_today</span>
                {{ project.updatedAt | date:'MMM d, y' }}
              </div>
              <div class="meta-item">
                <span class="material-icons-outlined">layers</span>
                {{ project.entitiesCount || 0 }} Objects
              </div>
            </div>
          </a>
        </div>

        <!-- Empty State -->
        <div *ngIf="projects.length === 0 && !loading" style="padding:4rem; text-align:center; opacity:0.1;">
          <span class="material-icons-outlined" style="font-size:5rem; margin-bottom:1rem;">folder_open</span>
          <p style="font-size:1.25rem; font-weight:800; text-transform:uppercase; letter-spacing:0.4em;">Workspace Empty</p>
        </div>
      </div>

      <!-- New Project Modal -->
      <div class="modal-overlay" *ngIf="showModal" (click)="closeOnBackdrop($event)">
        <div class="modal-card" (click)="$event.stopPropagation()">
          <div class="modal-head">
            <div class="modal-title">
              <span class="material-icons-outlined">create_new_folder</span>
              New Workspace
            </div>
            <button class="close-modal" (click)="showModal = false">
              <span class="material-icons-outlined">close</span>
            </button>
          </div>

          <div class="input-group">
            <label class="input-label">Workspace Name</label>
            <input type="text" [(ngModel)]="newProject.name" class="input-field" placeholder="e.g. Clinic Core" autofocus>
          </div>

          <div class="input-group">
            <label class="input-label">Description (Optional)</label>
            <textarea [(ngModel)]="newProject.description" class="input-field" rows="3" placeholder="Define the primary objective of this stack..."></textarea>
          </div>

          <div class="modal-foot">
            <button class="btn-cancel" (click)="showModal = false">Discard</button>
            <button class="btn-create" (click)="createProject()" [disabled]="creating || !newProject.name.trim()">
              <span class="material-icons-outlined" [class.spin]="creating">{{ creating ? 'sync' : 'rocket_launch' }}</span>
              {{ creating ? 'Initializing...' : 'Create Stack' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ProjectsList implements OnInit {
  projects: any[] = [];
  loading = true;
  showModal = false;
  creating = false;
  newProject = { name: '', description: '' };

  constructor(
    private readonly api: ApiService,
    private readonly router: Router,
    private readonly projectContext: ProjectContextService
  ) {}

  ngOnInit() {
    this.projectContext.setProjectId(null); // Reset active project
    this.loading = true;
    this.api.getProjects().subscribe({
      next: (data) => {
        this.loading = false;
        this.projects = data || [];
      },
      error: () => {
        this.loading = false;
        this.projects = this.getMocks();
      }
    });
  }

  openNewProjectModal() {
    this.newProject = { name: '', description: '' };
    this.showModal = true;
  }

  createProject() {
    if (!this.newProject.name.trim() || this.creating) return;
    this.creating = true;
    this.api.createProject({
      name: this.newProject.name.trim(),
      description: this.newProject.description.trim(),
      tenantId: '00000000-0000-0000-0000-000000000000'
    }).subscribe({
      next: (project: any) => {
        this.creating = false;
        this.showModal = false;
        if (project?.id) {
          this.router.navigate(['/projects', project.id, 'designer']);
        } else {
          this.ngOnInit();
        }
      },
      error: (e: any) => {
        this.creating = false;
        alert('Failed to create project: ' + (e.error?.message || e.message));
      }
    });
  }

  closeOnBackdrop(e: Event) {
    if (e.target === e.currentTarget) this.showModal = false;
  }

  private getMocks(): any[] {
    return [
      { id: '11111111-1111-1111-1111-111111111111', name: 'Healthcare Core', description: 'Patient records, appointment scheduling, and automated billing engine.', updatedAt: new Date(), entitiesCount: 12, version: '2.4.0' },
      { id: '22222222-2222-2222-2222-222222222222', name: 'Logistics Engine', description: 'Fleet management, real-time tracking, and warehouse optimization tools.', updatedAt: new Date(), entitiesCount: 8, version: '1.2.1' }
    ];
  }
}
