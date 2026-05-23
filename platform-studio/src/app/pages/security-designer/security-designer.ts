import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api';
import { ProjectContextService } from '../../services/project-context';

@Component({
    selector: 'app-security-designer',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterLink],
    styles: [`
    :host { display: block; }
    .studio-container { display: flex; flex-direction: column; height: calc(100vh - 64px); background: #0b0f1a; color: #e2e8f0; font-family: 'Inter', sans-serif; overflow: hidden; }

    /* ── Toolbar ──────────────────────────────── */
    .toolbar { height: 56px; border-bottom: 1px solid rgba(255,255,255,0.05); display: flex; align-items: center; justify-content: space-between; padding: 0 1.5rem; background: rgba(15,23,42,0.8); backdrop-filter: blur(16px); z-index: 20; }
    .toolbar-left { display: flex; align-items: center; gap: 1rem; }
    .back-btn { display: flex; align-items: center; justify-content: center; width: 32px; height: 32px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.05); background: transparent; color: #64748b; cursor: pointer; transition: all 0.2s; }
    .back-btn:hover { background: rgba(255,255,255,0.05); color: #fff; }
    
    .brand { display: flex; align-items: center; gap: 0.75rem; }
    .brand-icon-wrap { width: 36px; height: 36px; background: rgba(139,92,246,0.1); border-radius: 10px; display: flex; align-items: center; justify-content: center; }
    .brand-icon { color: #a78bfa; font-size: 1.25rem; }
    .brand-title { font-size: 0.85rem; font-weight: 800; color: #fff; margin: 0; }
    .brand-sub { font-size: 9px; color: #475569; font-family: 'JetBrains Mono', monospace; text-transform: uppercase; letter-spacing: 0.1em; }

    .tabs { display: flex; background: rgba(0,0,0,0.2); border-radius: 10px; padding: 0.25rem; gap: 0.25rem; margin-left: 2rem; }
    .tab-btn { padding: 0.375rem 1rem; border-radius: 8px; border: none; background: transparent; color: #64748b; font-size: 0.75rem; font-weight: 700; cursor: pointer; transition: all 0.2s; }
    .tab-btn:hover { color: #fff; background: rgba(255,255,255,0.05); }
    .tab-btn.active { background: rgba(255,255,255,0.1); color: #fff; }

    .toolbar-right { display: flex; align-items: center; gap: 0.75rem; }
    .btn-primary { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 1.25rem; border-radius: 10px; border: none; background: #2563eb; color: #fff; font-size: 0.75rem; font-weight: 800; cursor: pointer; transition: all 0.2s; box-shadow: 0 4px 12px rgba(37,99,235,0.2); }
    .btn-primary:hover { background: #3b82f6; transform: translateY(-1px); }

    /* ── Content ──────────────────────────────── */
    .viewport { flex: 1; overflow-y: auto; padding: 2rem; display: flex; justify-content: center; scrollbar-width: thin; }
    .content-max { width: 100%; max-width: 1000px; display: flex; flex-direction: column; gap: 2rem; }

    .section-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem; }
    .section-title { font-size: 1.25rem; font-weight: 800; color: #f1f5f9; margin: 0; }
    .section-desc { font-size: 0.85rem; color: #64748b; margin-top: 4px; }
    
    .card { background: rgba(15,23,42,0.6); border: 1px solid rgba(255,255,255,0.05); border-radius: 20px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.2); }
    .card-header { padding: 1rem 1.5rem; background: rgba(255,255,255,0.02); border-bottom: 1px solid rgba(255,255,255,0.05); display: flex; justify-content: space-between; align-items: center; }
    .card-title-wrap { display: flex; align-items: center; gap: 0.75rem; }
    .role-input { background: transparent; border: none; color: #fff; font-size: 1rem; font-weight: 800; outline: none; width: 240px; }
    .role-input::placeholder { color: #334155; }

    .table-container { width: 100%; overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; }
    th { text-align: left; padding: 1rem 1.5rem; font-size: 0.65rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; color: #334155; border-bottom: 1px solid rgba(255,255,255,0.05); }
    td { padding: 0.75rem 1.5rem; font-size: 0.85rem; color: #94a3b8; border-bottom: 1px solid rgba(255,255,255,0.02); }
    .entity-name { font-family: 'JetBrains Mono', monospace; font-size: 0.75rem; color: #60a5fa; font-weight: 600; }

    .checkbox-wrap { display: flex; justify-content: center; }
    input[type="checkbox"] { width: 16px; height: 16px; border-radius: 4px; background: rgba(0,0,0,0.4); border: 1px solid rgba(255,255,255,0.1); appearance: none; cursor: pointer; position: relative; transition: all 0.2s; }
    input[type="checkbox"]:checked { background: #3b82f6; border-color: #3b82f6; }
    input[type="checkbox"]:checked::after { content: 'check'; font-family: 'Material Icons Outlined'; position: absolute; font-size: 12px; color: #fff; top: 50%; left: 50%; transform: translate(-50%, -50%); }

    .action-row { padding: 1rem 1.5rem; display: flex; gap: 1rem; }
    .btn-text { background: transparent; border: none; color: #3b82f6; font-size: 0.75rem; font-weight: 800; text-transform: uppercase; letter-spacing: 0.05em; cursor: pointer; display: flex; align-items: center; gap: 0.25rem; }
    .btn-text:hover { color: #60a5fa; }

    .btn-icon-red { background: transparent; border: none; color: #334155; cursor: pointer; transition: color 0.2s; }
    .btn-icon-red:hover { color: #f87171; }

    .menu-item { display: flex; align-items: center; gap: 1.5rem; padding: 1.25rem; background: rgba(255,255,255,0.02); border: 1px solid rgba(255,255,255,0.05); border-radius: 16px; margin-bottom: 0.75rem; transition: all 0.2s; }
    .menu-item:hover { border-color: rgba(99,102,241,0.3); background: rgba(99,102,241,0.03); }
    .menu-icon-box { width: 44px; height: 44px; border-radius: 12px; background: rgba(0,0,0,0.3); display: flex; align-items: center; justify-content: center; color: #64748b; }
    .menu-inputs { flex: 1; display: grid; grid-template-columns: 1fr 1fr 1fr 1.5fr; gap: 1rem; }
    .m-field { display: flex; flex-direction: column; gap: 0.375rem; }
    .m-label { font-size: 0.65rem; font-weight: 900; text-transform: uppercase; color: #334155; }
    .m-input { background: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.05); border-radius: 8px; padding: 0.5rem 0.75rem; color: #fff; font-size: 0.8rem; outline: none; }
    .m-input:focus { border-color: #6366f1; }
    
    .role-badge-list { display: flex; flex-wrap: wrap; gap: 0.25rem; }
    .role-badge { padding: 0.25rem 0.5rem; border-radius: 6px; font-size: 9px; font-weight: 800; text-transform: uppercase; cursor: pointer; transition: all 0.2s; }
    .role-badge.inactive { background: rgba(255,255,255,0.05); color: #475569; }
    .role-badge.active { background: #3b82f6; color: #fff; }
    .role-badge.active.purple { background: #8b5cf6; }

    .user-card { display: flex; flex-direction: column; gap: 1.25rem; padding: 1.5rem; background: rgba(15,23,42,0.6); border: 1px solid rgba(255,255,255,0.05); border-radius: 24px; margin-bottom: 1rem; }
    .user-header { display: flex; justify-content: space-between; align-items: center; }
    .user-info { display: flex; align-items: center; gap: 1rem; }
    .u-avatar { width: 48px; height: 48px; border-radius: 16px; background: #1e293b; display: flex; align-items: center; justify-content: center; color: #475569; }
    .u-name-input { background: transparent; border: none; color: #fff; font-size: 1.1rem; font-weight: 800; outline: none; padding: 0; }
    .u-email-input { background: transparent; border: none; color: #475569; font-size: 0.8rem; outline: none; padding: 0; margin-top: 2px; }
    
    .user-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1.5rem; }
    
    .fade-in { animation: fadeIn 0.3s ease-out; }
    @keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
  `],
    template: `
    <div class="studio-container">
      <!-- Toolbar -->
      <header class="toolbar">
        <div class="toolbar-left">
          <button [routerLink]="['/projects', projectId, 'designer']" class="back-btn">
            <span class="material-icons-outlined" style="font-size:1.25rem">arrow_back</span>
          </button>
          <div class="brand">
            <div class="brand-icon-wrap">
              <span class="material-icons-outlined brand-icon">admin_panel_settings</span>
            </div>
            <div>
              <h2 class="brand-title">Security & Access</h2>
              <div class="brand-sub">Policy Engine v2 // {{ projectId | slice:0:8 }}</div>
            </div>
          </div>
          <div class="tabs">
            <button (click)="activeTab = 'roles'" [class.active]="activeTab === 'roles'" class="tab-btn">Roles</button>
            <button (click)="activeTab = 'menus'" [class.active]="activeTab === 'menus'" class="tab-btn">Navigation</button>
            <button (click)="activeTab = 'users'" [class.active]="activeTab === 'users'" class="tab-btn">Users</button>
          </div>
        </div>

        <div class="toolbar-right">
          <button (click)="save()" [disabled]="isSaving" class="btn-primary">
            <span class="material-icons-outlined">{{ isSaving ? 'sync' : 'save' }}</span>
            {{ isSaving ? 'Saving Changes...' : 'Save Configuration' }}
          </button>
        </div>
      </header>

      <main class="viewport">
        <div class="content-max fade-in">
          
          <!-- Roles View -->
          <div *ngIf="activeTab === 'roles'" style="display:flex; flex-direction:column; gap:2rem;">
            <div class="section-head">
              <div>
                <h3 class="section-title">Role Definitions</h3>
                <p class="section-desc">Map permissions to entities for each security role.</p>
              </div>
              <button (click)="addRole()" class="btn-text">
                <span class="material-icons-outlined">add_circle</span>
                New Role
              </button>
            </div>

            <div class="card" *ngFor="let role of security.roles; let ri = index">
              <div class="card-header">
                <div class="card-title-wrap">
                  <span class="material-icons-outlined" style="color:#64748b">group</span>
                  <input type="text" [(ngModel)]="role.name" class="role-input" placeholder="ROLE_NAME">
                </div>
                <button (click)="removeRole(ri)" class="btn-icon-red">
                  <span class="material-icons-outlined">delete</span>
                </button>
              </div>
              
              <div class="table-container">
                <table>
                  <thead>
                    <tr>
                      <th style="width:30%">Entity Name</th>
                      <th style="text-align:center">Read</th>
                      <th style="text-align:center">Create</th>
                      <th style="text-align:center">Update</th>
                      <th style="text-align:center">Delete</th>
                      <th style="text-align:right"></th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let perm of role.permissions; let pi = index">
                      <td class="entity-name">{{ perm.entityName }}</td>
                      <td><div class="checkbox-wrap"><input type="checkbox" [(ngModel)]="perm.canRead"></div></td>
                      <td><div class="checkbox-wrap"><input type="checkbox" [(ngModel)]="perm.canCreate"></div></td>
                      <td><div class="checkbox-wrap"><input type="checkbox" [(ngModel)]="perm.canUpdate"></div></td>
                      <td><div class="checkbox-wrap"><input type="checkbox" [(ngModel)]="perm.canDelete"></div></td>
                      <td style="text-align:right">
                         <button (click)="removePermission(role, pi)" class="btn-icon-red" style="opacity:0.3">
                           <span class="material-icons-outlined" style="font-size:1rem">close</span>
                         </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <div class="action-row">
                 <button (click)="addPermission(role)" class="btn-text" style="font-size:9px">
                   <span class="material-icons-outlined" style="font-size:12px">sync</span>
                   Sync with Entities
                 </button>
              </div>
            </div>
          </div>

          <!-- Menu View -->
          <div *ngIf="activeTab === 'menus'" style="display:flex; flex-direction:column; gap:2rem;">
            <div class="section-head">
              <div>
                <h3 class="section-title">Navigation Menu</h3>
                <p class="section-desc">Configure the application sidebar and role-based visibility.</p>
              </div>
              <button (click)="addMenu()" class="btn-text">
                <span class="material-icons-outlined">add_circle</span>
                Add Link
              </button>
            </div>

            <div class="menu-stack">
              <div *ngFor="let menu of security.menus; let mi = index" class="menu-item">
                <div class="menu-icon-box">
                  <span class="material-icons-outlined">{{ menu.icon || 'menu' }}</span>
                </div>
                <div class="menu-inputs">
                  <div class="m-field">
                    <label class="m-label">Label</label>
                    <input type="text" [(ngModel)]="menu.label" class="m-input">
                  </div>
                  <div class="m-field">
                    <label class="m-label">Route</label>
                    <input type="text" [(ngModel)]="menu.route" class="m-input">
                  </div>
                  <div class="m-field">
                    <label class="m-label">Icon ID</label>
                    <input type="text" [(ngModel)]="menu.icon" class="m-input">
                  </div>
                  <div class="m-field">
                    <label class="m-label">Visibility</label>
                    <div class="role-badge-list">
                       <span *ngFor="let role of security.roles" 
                             (click)="toggleRole(menu, role.name)"
                             [class.active]="menu.allowedRoles.includes(role.name)"
                             [class.inactive]="!menu.allowedRoles.includes(role.name)"
                             class="role-badge">
                         {{ role.name }}
                       </span>
                    </div>
                  </div>
                </div>
                <button (click)="removeMenu(mi)" class="btn-icon-red">
                  <span class="material-icons-outlined">delete</span>
                </button>
              </div>
            </div>
          </div>

          <!-- User View -->
          <div *ngIf="activeTab === 'users'" style="display:flex; flex-direction:column; gap:2rem;">
             <div class="section-head">
               <div>
                 <h3 class="section-title">User Registry</h3>
                 <p class="section-desc">Seed user accounts for the production environment.</p>
               </div>
               <button (click)="addUser()" class="btn-text">
                 <span class="material-icons-outlined">person_add</span>
                 Create User
               </button>
             </div>

             <div class="user-card" *ngFor="let user of userConfig.users; let ui = index">
               <div class="user-header">
                 <div class="user-info">
                   <div class="u-avatar">
                     <span class="material-icons-outlined">face</span>
                   </div>
                   <div>
                     <input type="text" [(ngModel)]="user.username" class="u-name-input" placeholder="username">
                     <input type="text" [(ngModel)]="user.email" class="u-email-input" placeholder="email@domain.com">
                   </div>
                 </div>
                 <button (click)="removeUser(ui)" class="btn-icon-red">
                   <span class="material-icons-outlined">delete</span>
                 </button>
               </div>

               <div class="user-grid">
                  <div class="m-field">
                    <label class="m-label">Master Password</label>
                    <input type="password" [(ngModel)]="user.password" class="m-input" placeholder="••••••••">
                  </div>
                  <div class="m-field">
                    <label class="m-label">Attached Roles</label>
                    <div class="role-badge-list">
                       <span *ngFor="let role of security.roles" 
                             (click)="toggleUserRole(user, role.name)"
                             [class.active]="user.assignedRoles.includes(role.name)"
                             [class.purple]="true"
                             [class.inactive]="!user.assignedRoles.includes(role.name)"
                             class="role-badge">
                         {{ role.name }}
                       </span>
                    </div>
                  </div>
               </div>
             </div>
          </div>

        </div>
      </main>
    </div>
  `,
})
export class SecurityDesigner implements OnInit {
    projectId: string | null = null;
    activeTab: 'roles' | 'menus' | 'users' = 'roles';
    isSaving = false;
    security: any = { roles: [], menus: [] };
    userConfig: any = { users: [] };
    availableEntities: string[] = [];

    constructor(
        private readonly route: ActivatedRoute,
        private readonly api: ApiService,
        private readonly projectContext: ProjectContextService
    ) {
        this.projectId = this.route.snapshot.paramMap.get('projectId');
        if (this.projectId) {
            this.projectContext.setProjectId(this.projectId);
        }
    }

    ngOnInit() {
        if (this.projectId) {
            this.loadConfig();
            this.loadEntities();
        }
    }

    loadConfig() {
        this.api.getSecurityConfig(this.projectId!).subscribe({
            next: (config) => {
                this.security = config || { roles: [], menus: [] };
                if (!this.security.roles) this.security.roles = [];
                if (!this.security.menus) this.security.menus = [];
            }
        });

        this.api.getUsersConfig(this.projectId!).subscribe({
            next: (config) => {
                this.userConfig = config || { users: [] };
                if (!this.userConfig.users) this.userConfig.users = [];
            }
        });
    }

    loadEntities() {
        this.api.getEntities(this.projectId!).subscribe({
            next: (artifacts) => {
                this.availableEntities = artifacts.map(a => a.name);
            }
        });
    }

    addRole() {
        const role = {
            name: 'NEW_ROLE',
            permissions: this.availableEntities.map(e => ({
                entityName: e,
                canRead: true,
                canCreate: false,
                canUpdate: false,
                canDelete: false
            }))
        };
        this.security.roles.push(role);
    }

    removeRole(index: number) {
        this.security.roles.splice(index, 1);
    }

    addPermission(role: any) {
        this.availableEntities.forEach(e => {
            if (!role.permissions.some((p: any) => p.entityName === e)) {
                role.permissions.push({
                    entityName: e,
                    canRead: true,
                    canCreate: false,
                    canUpdate: false,
                    canDelete: false
                });
            }
        });
    }

    removePermission(role: any, index: number) {
        role.permissions.splice(index, 1);
    }

    addMenu() {
        this.security.menus.push({
            label: 'New Menu',
            icon: 'star',
            route: '/',
            allowedRoles: []
        });
    }

    removeMenu(index: number) {
        this.security.menus.splice(index, 1);
    }

    toggleRole(menu: any, roleName: string) {
        if (!menu.allowedRoles) menu.allowedRoles = [];
        const idx = menu.allowedRoles.indexOf(roleName);
        if (idx > -1) {
            menu.allowedRoles.splice(idx, 1);
        } else {
            menu.allowedRoles.push(roleName);
        }
    }

    addUser() {
        this.userConfig.users.push({
            id: Math.random().toString(36).substring(2),
            username: 'newuser',
            email: '',
            password: '',
            assignedRoles: []
        });
    }

    removeUser(index: number) {
        this.userConfig.users.splice(index, 1);
    }

    toggleUserRole(user: any, roleName: string) {
        if (!user.assignedRoles) user.assignedRoles = [];
        const idx = user.assignedRoles.indexOf(roleName);
        if (idx > -1) {
            user.assignedRoles.splice(idx, 1);
        } else {
            user.assignedRoles.push(roleName);
        }
    }

    save() {
        this.isSaving = true;
        const obs1 = this.api.saveSecurityConfig(this.projectId!, this.security);
        const obs2 = this.api.saveUsersConfig(this.projectId!, this.userConfig);

        import('rxjs').then(({ forkJoin }) => {
            forkJoin([obs1, obs2]).subscribe({
                next: () => {
                    this.isSaving = false;
                    alert('Security configuration synchronized.');
                },
                error: (err) => {
                    this.isSaving = false;
                    alert('Synchronize failed: ' + err.message);
                }
            });
        });
    }
}
