import { Component, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { JobProgressComponent } from '../job-progress/job-progress';
import { ApiService, AiProvider } from '../../services/api';
import { ProjectContextService } from '../../services/project-context';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, JobProgressComponent, CommonModule],
  styles: [`
    :host { display:block; height:100vh; }
    .shell { display:flex; height:100vh; background:#0b0f1a; color:#e2e8f0; overflow:hidden; font-family:'Inter',sans-serif; }

    .sidebar { width:260px; background:rgba(11,15,26,0.8); border-right:1px solid rgba(255,255,255,0.06); display:flex; flex-direction:column; flex-shrink:0; backdrop-filter:blur(20px); }
    .logo-area { padding:1.5rem; border-bottom:1px solid rgba(255,255,255,0.05); margin-bottom:1rem; }
    .logo-name { font-size:1.1rem; font-weight:900; background:linear-gradient(135deg,#60a5fa,#a78bfa); -webkit-background-clip:text; -webkit-text-fill-color:transparent; background-clip:text; letter-spacing:-0.02em; }
    .logo-sub { font-size:0.65rem; text-transform:uppercase; letter-spacing:0.2em; color:#475569; margin-top:4px; font-weight:800; }

    .nav { flex:1; overflow-y:auto; padding:0 1rem; display:flex; flex-direction:column; gap:1.5rem; }
    .nav-group { display:flex; flex-direction:column; gap:0.25rem; }
    .nav-group-label { font-size:0.65rem; text-transform:uppercase; letter-spacing:0.15em; font-weight:900; color:#334155; padding:0 0.75rem; margin-bottom:0.5rem; }

    .nav-link { display:flex; align-items:center; gap:0.75rem; padding:0.75rem 1rem; border-radius:12px; border:1px solid transparent; text-decoration:none; color:#94a3b8; font-size:0.85rem; font-weight:600; transition:all 0.2s; position:relative; }
    .nav-link:hover { background:rgba(255,255,255,0.04); color:#f1f5f9; }
    .nav-link.active-link { background:rgba(59,130,246,0.1); color:#60a5fa; border-color:rgba(59,130,246,0.15); box-shadow:0 4px 12px rgba(0,0,0,0.1); }
    .nav-link.active-violet { background:rgba(139,92,246,0.1); color:#a78bfa; border-color:rgba(139,92,246,0.15); }
    .nav-link.active-amber { background:rgba(245,158,11,0.1); color:#fbbf24; border-color:rgba(245,158,11,0.15); }
    .nav-link.active-green { background:rgba(16,185,129,0.1); color:#34d399; border-color:rgba(16,185,129,0.15); }
    
    .nav-link .material-icons-outlined { font-size:1.25rem; opacity:0.7; }
    .nav-link.active-link .material-icons-outlined { opacity:1; }

    .nav-ghost { display:flex; align-items:center; gap:0.75rem; padding:0.75rem 1rem; color:#334155; font-size:0.85rem; font-weight:600; border:1px dashed rgba(255,255,255,0.03); border-radius:12px; cursor:not-allowed; }
    .nav-ghost .material-icons-outlined { font-size:1.25rem; opacity:0.3; }

    .status-dot { width:6px; height:6px; border-radius:50%; margin-left:auto; }
    .dot-amber { background:#f59e0b; box-shadow:0 0 8px #f59e0b; animation:pulse-dot 2s infinite; }
    .dot-green { background:#10b981; box-shadow:0 0 8px #10b981; }
    @keyframes pulse-dot { 0%,100%{opacity:1} 50%{opacity:0.4} }

    .ai-widget { margin:1rem; border-radius:16px; padding:1rem; }
    .ai-widget.unconfigured { background:rgba(245,158,11,0.03); border:1px solid rgba(245,158,11,0.1); }
    .ai-widget.connected { background:rgba(16,185,129,0.03); border:1px solid rgba(16,185,129,0.1); }
    .ai-widget-head { display:flex; align-items:center; justify-content:space-between; margin-bottom:0.5rem; }
    .ai-widget-label { display:flex; align-items:center; gap:8px; font-size:0.7rem; font-weight:900; text-transform:uppercase; letter-spacing:0.1em; }
    .ai-label-amber { color:#fbbf24; }
    .ai-label-green { color:#34d399; }
    .ai-widget p { font-size:0.75rem; color:#475569; line-height:1.5; margin:0 0 0.75rem; }
    .ai-name { font-size:0.85rem; font-weight:800; color:#f1f5f9; margin-bottom:2px; }
    .ai-model { font-size:0.7rem; color:#475569; font-family:'JetBrains Mono',monospace; }

    .user-row { display:flex; align-items:center; gap:0.75rem; padding:1.25rem 1.5rem; border-top:1px solid rgba(255,255,255,0.05); background:rgba(0,0,0,0.1); }
    .avatar { width:36px; height:36px; border-radius:12px; background:linear-gradient(135deg,#3b82f6,#8b5cf6); display:flex; align-items:center; justify-content:center; font-size:0.8rem; font-weight:900; color:#fff; }
    .user-name { font-size:0.85rem; font-weight:700; color:#f1f5f9; }
    .user-plan { font-size:0.7rem; color:#475569; }

    .main { flex:1; display:flex; flex-direction:column; overflow:hidden; background:#0b1120; }
    .topbar { height:64px; border-bottom:1px solid rgba(255,255,255,0.05); display:flex; align-items:center; justify-content:space-between; padding:0 2rem; background:rgba(11,17,32,0.8); backdrop-filter:blur(20px); z-index:10; }
    .breadcrumb { display:flex; align-items:center; gap:0.75rem; font-size:0.85rem; }
    .bc-muted { color:#475569; font-weight:500; }
    .bc-sep { color:#1e293b; }
    .bc-current { color:#f1f5f9; font-weight:700; }

    .topbar-actions { display:flex; align-items:center; gap:1rem; }
    .icon-btn { background:none; border:none; color:#475569; cursor:pointer; padding:8px; border-radius:10px; display:flex; transition:all 0.2s; }
    .icon-btn:hover { color:#94a3b8; background:rgba(255,255,255,0.05); }
    
    .btn-primary { background:linear-gradient(135deg,#3b82f6,#6366f1); color:#fff; border:none; padding:10px 20px; border-radius:12px; font-size:0.85rem; font-weight:800; cursor:pointer; text-decoration:none; display:inline-flex; align-items:center; gap:8px; box-shadow:0 8px 20px rgba(59,130,246,0.2); transition:all 0.2s; }
    .btn-primary:hover { transform:translateY(-1px); box-shadow:0 12px 24px rgba(59,130,246,0.3); }

    .content { flex:1; overflow:hidden; }
  `],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="logo-area">
          <div class="logo-name">DynamicPlatform</div>
          <div class="logo-sub">Studio Edition</div>
        </div>

        <nav class="nav">
          <!-- Main -->
          <div class="nav-group">
            <div class="nav-group-label">Global</div>
            <a class="nav-link" routerLink="/projects" routerLinkActive="active-link" [routerLinkActiveOptions]="{exact: true}">
              <span class="material-icons-outlined">apps</span>
              Project Hub
            </a>
            <a class="nav-link" routerLink="/settings/ai-providers" routerLinkActive="active-violet">
              <span class="material-icons-outlined">psychology</span>
              AI Config
              <span class="status-dot dot-amber" *ngIf="!aiLoading && aiStatus === 'none'"></span>
              <span class="status-dot dot-green" *ngIf="!aiLoading && aiStatus === 'ok'"></span>
            </a>
          </div>

          <!-- Designer -->
          <div class="nav-group">
            <div class="nav-group-label">Architecture</div>
            
            <ng-container *ngIf="projectId; else noProject">
              <a class="nav-link" [routerLink]="['/projects', projectId, 'designer']" routerLinkActive="active-link">
                <span class="material-icons-outlined">schema</span>
                Entity Architect
              </a>
              <a class="nav-link" [routerLink]="['/projects', projectId, 'connectors']" routerLinkActive="active-link">
                <span class="material-icons-outlined">bolt</span>
                Connectors
              </a>
              <a class="nav-link" [routerLink]="['/projects', projectId, 'workflows']" routerLinkActive="active-green">
                <span class="material-icons-outlined">account_tree</span>
                Workflows
              </a>
              <a class="nav-link" [routerLink]="['/projects', projectId, 'security']" routerLinkActive="active-violet">
                <span class="material-icons-outlined">security</span>
                Security
              </a>
            </ng-container>

            <ng-template #noProject>
              <div class="nav-ghost">
                <span class="material-icons-outlined">lock</span>
                Select a Project...
              </div>
            </ng-template>
          </div>

          <!-- UI & Content -->
          <div class="nav-group" *ngIf="projectId">
            <div class="nav-group-label">UI Designer</div>
            <a class="nav-link" [routerLink]="['/projects', projectId, 'pages']" routerLinkActive="active-link">
              <span class="material-icons-outlined">web</span>
              Page Layouts
            </a>
            <a class="nav-link" [routerLink]="['/projects', projectId, 'enums']" routerLinkActive="active-amber">
              <span class="material-icons-outlined">list</span>
              Enum Definitions
            </a>
          </div>
        </nav>

        <!-- AI Status -->
        <div class="ai-widget connected" *ngIf="!aiLoading && aiStatus === 'ok' && defaultProvider">
          <div class="ai-widget-head">
            <span class="ai-widget-label ai-label-green">Active Engine</span>
            <span class="status-dot dot-green"></span>
          </div>
          <div class="ai-name">{{ defaultProvider.name }}</div>
          <div class="ai-model">{{ defaultProvider.defaultModel }}</div>
        </div>

        <div class="user-row">
          <div class="avatar">SA</div>
          <div>
            <div class="user-name">Sudipto A.</div>
            <div class="user-plan">Enterprise Access</div>
          </div>
        </div>
      </aside>

      <main class="main">
        <header class="topbar">
          <div class="breadcrumb">
            <span class="bc-muted">Platform</span>
            <span class="bc-sep">/</span>
            <span class="bc-current">Studio Suite</span>
          </div>
          <div class="topbar-actions">
            <app-job-progress></app-job-progress>
            <button class="icon-btn"><span class="material-icons-outlined">search</span></button>
            <button class="icon-btn"><span class="material-icons-outlined">notifications</span></button>
            <a routerLink="/projects" class="btn-primary">
              <span class="material-icons-outlined">add</span>
              New Project
            </a>
          </div>
        </header>

        <div class="content">
          <router-outlet></router-outlet>
        </div>
      </main>
    </div>
  `
})
export class Dashboard implements OnInit, OnDestroy {
  aiLoading = true;
  aiStatus: 'none' | 'ok' = 'none';
  defaultProvider: AiProvider | null = null;
  projectId: string | null = null;
  private sub?: Subscription;

  constructor(
    private api: ApiService,
    private projectContext: ProjectContextService
  ) {}

  ngOnInit() {
    this.sub = this.projectContext.projectId$.subscribe(id => this.projectId = id);

    this.api.getAiProviders().subscribe({
      next: (providers) => {
        this.aiLoading = false;
        this.defaultProvider = providers.find(p => p.isDefault) ?? providers[0] ?? null;
        this.aiStatus = this.defaultProvider ? 'ok' : 'none';
      },
      error: () => { this.aiLoading = false; this.aiStatus = 'none'; }
    });
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
  }
}
