import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api';

@Component({
    selector: 'app-security-designer',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterLink],
    template: `
    <div class="flex flex-col h-[calc(100vh-64px)] bg-slate-950 text-slate-200">
      <!-- Toolbar -->
      <div class="h-14 border-b border-white/10 flex items-center justify-between px-6 bg-slate-900/50 backdrop-blur-sm z-20">
        <div class="flex items-center space-x-4">
          <button [routerLink]="['/projects', projectId, 'designer']" class="p-2 hover:bg-white/5 rounded-lg text-slate-400 hover:text-white transition-all">
            <span class="material-icons-outlined">arrow_back</span>
          </button>
          <div class="flex items-center space-x-2">
            <span class="material-icons-outlined text-purple-400">admin_panel_settings</span>
            <h2 class="text-sm font-semibold text-white">Security & Access Control</h2>
          </div>
          <div class="h-6 w-px bg-white/10"></div>
          <div class="flex space-x-1 p-1 bg-black/20 rounded-lg">
            <button (click)="activeTab = 'roles'" [class.bg-white/10]="activeTab === 'roles'" class="px-4 py-1.5 rounded-md text-xs font-medium transition-all hover:bg-white/5">Roles & Permissions</button>
            <button (click)="activeTab = 'menus'" [class.bg-white/10]="activeTab === 'menus'" class="px-4 py-1.5 rounded-md text-xs font-medium transition-all hover:bg-white/5">Navigation Menu</button>
            <button (click)="activeTab = 'users'" [class.bg-white/10]="activeTab === 'users'" class="px-4 py-1.5 rounded-md text-xs font-medium transition-all hover:bg-white/5">User Management</button>
          </div>
        </div>
        <button (click)="save()" [disabled]="isSaving" class="flex items-center space-x-2 text-sm bg-blue-600 hover:bg-blue-500 disabled:bg-slate-700 text-white px-5 py-2 rounded-lg shadow-lg transition-all active:scale-95">
          <span class="material-icons-outlined text-lg">{{ isSaving ? 'sync' : 'save' }}</span>
          <span>{{ isSaving ? 'Saving...' : 'Save Configuration' }}</span>
        </button>
      </div>

      <div class="flex-1 overflow-hidden flex">
        <!-- Main Content Area -->
        <main class="flex-1 overflow-y-auto p-8">
          
          <!-- Roles Designer -->
          <div *ngIf="activeTab === 'roles'" class="max-w-5xl mx-auto space-y-8 animate-fadeIn">
            <div class="flex items-center justify-between mb-4">
               <div>
                 <h3 class="text-xl font-bold">Role Definitions</h3>
                 <p class="text-slate-400 text-sm">Define user roles and map their permissions to system entities.</p>
               </div>
               <button (click)="addRole()" class="flex items-center space-x-2 bg-purple-600/20 text-purple-400 border border-purple-500/20 px-4 py-2 rounded-xl hover:bg-purple-600 hover:text-white transition-all">
                 <span class="material-icons-outlined">add</span>
                 <span>Add Role</span>
               </button>
            </div>

            <div class="grid grid-cols-1 gap-6">
              <div *ngFor="let role of security.roles; let ri = index" class="bg-slate-900/50 border border-white/5 rounded-2xl overflow-hidden shadow-xl">
                <div class="p-4 bg-white/5 border-b border-white/5 flex items-center justify-between">
                  <div class="flex items-center space-x-3">
                    <span class="material-icons-outlined text-slate-500">group</span>
                    <input type="text" [(ngModel)]="role.name" class="bg-transparent border-none text-lg font-bold text-white focus:ring-0 w-64" placeholder="Role Name (e.g., Admin)">
                  </div>
                  <button (click)="removeRole(ri)" class="text-slate-500 hover:text-red-400 transition-colors">
                    <span class="material-icons-outlined">delete_outline</span>
                  </button>
                </div>
                
                <div class="p-6">
                  <table class="w-full text-sm text-left">
                    <thead>
                      <tr class="text-slate-500 border-b border-white/5 uppercase text-[10px] tracking-wider font-bold">
                        <th class="pb-3 pl-2">Entity Name</th>
                        <th class="pb-3 text-center">Read</th>
                        <th class="pb-3 text-center">Create</th>
                        <th class="pb-3 text-center">Update</th>
                        <th class="pb-3 text-center">Delete</th>
                        <th class="pb-3 text-right">Actions</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-white/5">
                      <tr *ngFor="let perm of role.permissions; let pi = index" class="group transition-colors hover:bg-white/5">
                        <td class="py-4 pl-2 font-mono text-blue-400">{{ perm.entityName }}</td>
                        <td class="py-4 text-center">
                          <input type="checkbox" [(ngModel)]="perm.canRead" class="rounded bg-slate-800 border-white/10 text-blue-600">
                        </td>
                        <td class="py-4 text-center">
                          <input type="checkbox" [(ngModel)]="perm.canCreate" class="rounded bg-slate-800 border-white/10 text-blue-600">
                        </td>
                        <td class="py-4 text-center">
                          <input type="checkbox" [(ngModel)]="perm.canUpdate" class="rounded bg-slate-800 border-white/10 text-blue-600">
                        </td>
                        <td class="py-4 text-center">
                          <input type="checkbox" [(ngModel)]="perm.canDelete" class="rounded bg-slate-800 border-white/10 text-blue-600">
                        </td>
                        <td class="py-4 text-right">
                          <button (click)="removePermission(role, pi)" class="text-slate-600 hover:text-red-400 opacity-0 group-hover:opacity-100 transition-all">
                            <span class="material-icons-outlined text-lg">close</span>
                          </button>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                  <div class="mt-4 flex justify-start">
                    <button (click)="addPermission(role)" class="flex items-center space-x-1 text-xs text-blue-400 hover:text-blue-300 transition-colors">
                      <span class="material-icons-outlined text-sm">add</span>
                      <span>Manage Permissions for Entities</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Menus Designer -->
          <div *ngIf="activeTab === 'menus'" class="max-w-5xl mx-auto space-y-8 animate-fadeIn">
             <div class="flex items-center justify-between">
                <div>
                   <h3 class="text-xl font-bold">Navigation Menu</h3>
                   <p class="text-slate-400 text-sm">Configure the application menu and restrict visibility by roles.</p>
                </div>
                <button (click)="addMenu()" class="flex items-center space-x-2 bg-indigo-600/20 text-indigo-400 border border-indigo-500/20 px-4 py-2 rounded-xl hover:bg-indigo-600 hover:text-white transition-all">
                  <span class="material-icons-outlined">add</span>
                  <span>Add Menu Item</span>
                </button>
             </div>

             <div class="space-y-4">
                <div *ngFor="let menu of security.menus; let mi = index" class="p-4 bg-slate-900/50 border border-white/5 rounded-xl flex items-center space-x-6">
                   <div class="p-3 bg-white/5 rounded-lg text-slate-400">
                      <span class="material-icons-outlined">{{ menu.icon || 'menu' }}</span>
                   </div>
                   <div class="flex-1 grid grid-cols-4 gap-4">
                      <div class="space-y-1">
                        <label class="text-[10px] uppercase font-bold text-slate-500">Label</label>
                        <input type="text" [(ngModel)]="menu.label" class="w-full bg-black/20 border border-white/10 rounded-lg px-3 py-2 text-sm text-white focus:ring-0 outline-none" placeholder="e.g., Dashboard">
                      </div>
                      <div class="space-y-1">
                        <label class="text-[10px] uppercase font-bold text-slate-500">Route</label>
                        <input type="text" [(ngModel)]="menu.route" class="w-full bg-black/20 border border-white/10 rounded-lg px-3 py-2 text-sm text-white focus:ring-0 outline-none" placeholder="/dashboard">
                      </div>
                      <div class="space-y-1">
                        <label class="text-[10px] uppercase font-bold text-slate-500">Icon</label>
                        <input type="text" [(ngModel)]="menu.icon" class="w-full bg-black/20 border border-white/10 rounded-lg px-3 py-2 text-sm text-white focus:ring-0 outline-none" placeholder="material-icon-name">
                      </div>
                      <div class="space-y-1">
                         <label class="text-[10px] uppercase font-bold text-slate-500">Allowed Roles</label>
                         <div class="flex flex-wrap gap-1">
                            <span *ngFor="let role of security.roles" 
                                  (click)="toggleRole(menu, role.name)"
                                  [class.bg-blue-600]="menu.allowedRoles.includes(role.name)"
                                  [class.text-white]="menu.allowedRoles.includes(role.name)"
                                  [class.bg-white/5]="!menu.allowedRoles.includes(role.name)"
                                  [class.text-slate-500]="!menu.allowedRoles.includes(role.name)"
                                  class="px-2 py-1 rounded-md text-[10px] cursor-pointer hover:bg-white/10 transition-all">
                              {{ role.name }}
                            </span>
                         </div>
                      </div>
                   </div>
                   <button (click)="removeMenu(mi)" class="text-slate-600 hover:text-red-400 p-2">
                     <span class="material-icons-outlined">delete_outline</span>
                   </button>
                </div>
             </div>
          </div>
          <!-- User Management Designer -->
          <div *ngIf="activeTab === 'users'" class="max-w-5xl mx-auto space-y-8 animate-fadeIn">
             <div class="flex items-center justify-between">
                <div>
                   <h3 class="text-xl font-bold">App Users</h3>
                   <p class="text-slate-400 text-sm">Manage user identities and their assigned security roles for the generated application.</p>
                </div>
                <button (click)="addUser()" class="flex items-center space-x-2 bg-emerald-600/20 text-emerald-400 border border-emerald-500/20 px-4 py-2 rounded-xl hover:bg-emerald-600 hover:text-white transition-all">
                  <span class="material-icons-outlined">person_add</span>
                  <span>Add User</span>
                </button>
             </div>

             <div class="space-y-4">
                <div *ngFor="let user of userConfig.users; let ui = index" class="p-6 bg-slate-900/50 border border-white/5 rounded-2xl flex flex-col space-y-4">
                   <div class="flex items-center justify-between">
                      <div class="flex items-center space-x-4">
                         <div class="w-12 h-12 rounded-full bg-slate-800 flex items-center justify-center text-slate-400">
                            <span class="material-icons-outlined">person</span>
                         </div>
                         <div>
                            <input type="text" [(ngModel)]="user.username" class="bg-transparent border-none text-lg font-bold text-white focus:ring-0 w-64 p-0" placeholder="Username">
                            <input type="text" [(ngModel)]="user.email" class="bg-transparent border-none text-xs text-slate-500 focus:ring-0 w-64 p-0 block" placeholder="user@example.com">
                         </div>
                      </div>
                      <button (click)="removeUser(ui)" class="text-slate-600 hover:text-red-400 p-2">
                        <span class="material-icons-outlined">delete_outline</span>
                      </button>
                   </div>
                   
                   <div class="grid grid-cols-2 gap-6">
                      <div class="space-y-2">
                         <label class="text-[10px] uppercase font-bold text-slate-500">Security Password (Plain)</label>
                         <div class="relative">
                            <input type="password" [(ngModel)]="user.password" class="w-full bg-black/20 border border-white/10 rounded-lg px-3 py-2 text-sm text-white focus:ring-0 outline-none pr-10" placeholder="••••••••">
                            <span class="material-icons-outlined absolute right-3 top-2 text-slate-600 text-sm">lock</span>
                         </div>
                      </div>
                      <div class="space-y-2">
                         <label class="text-[10px] uppercase font-bold text-slate-500">Assigned Roles</label>
                         <div class="flex flex-wrap gap-1">
                            <span *ngFor="let role of security.roles" 
                                  (click)="toggleUserRole(user, role.name)"
                                  [class.bg-purple-600]="user.assignedRoles.includes(role.name)"
                                  [class.text-white]="user.assignedRoles.includes(role.name)"
                                  [class.bg-white/5]="!user.assignedRoles.includes(role.name)"
                                  [class.text-slate-500]="!user.assignedRoles.includes(role.name)"
                                  class="px-2 py-1 rounded-md text-[10px] cursor-pointer hover:bg-white/10 transition-all">
                              {{ role.name }}
                            </span>
                         </div>
                      </div>
                   </div>
                </div>
             </div>
          </div>
        </main>

        <!-- Context Sidebar -->
        <aside class="w-72 bg-slate-900 border-l border-white/10 p-6 space-y-8 hidden xl:block">
           <div class="space-y-4">
             <h4 class="text-xs uppercase font-bold text-slate-500 tracking-wider">Quick Actions</h4>
             <div class="space-y-2">
                <button class="w-full text-left p-3 rounded-lg bg-white/5 hover:bg-white/10 text-sm flex items-center space-x-3 transition-all">
                   <span class="material-icons-outlined text-green-400 text-lg">verified_user</span>
                   <span>Audit Log Settings</span>
                </button>
                <button class="w-full text-left p-3 rounded-lg bg-white/5 hover:bg-white/10 text-sm flex items-center space-x-3 transition-all">
                   <span class="material-icons-outlined text-orange-400 text-lg">key</span>
                   <span>AD / LDAP Integration</span>
                </button>
             </div>
           </div>
           
           <div class="p-6 bg-gradient-to-br from-blue-600/10 to-purple-600/10 border border-blue-500/20 rounded-2xl">
              <span class="material-icons-outlined text-blue-400 mb-2">info</span>
              <p class="text-xs text-slate-400 leading-relaxed">
                Roles and Permissions are automatically enforced at the **API Level** using the Generated XML configuration during the build process.
              </p>
           </div>
        </aside>
      </div>
    </div>
  `,
    styles: [`
    .animate-fadeIn {
      animation: fadeIn 0.4s ease-out;
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(10px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
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
        private readonly api: ApiService
    ) {
        this.projectId = this.route.snapshot.paramMap.get('projectId');
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
                // Ensure defaults
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
        // Permission sync usually handled on addRole or on refresh, but we allows adding missing ones
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
                    alert('Security and User configurations saved!');
                },
                error: (err) => {
                    this.isSaving = false;
                    alert('Save failed: ' + err.message);
                }
            });
        });
    }
}
