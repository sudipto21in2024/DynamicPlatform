import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-workflow-designer',
    standalone: true,
    imports: [CommonModule, RouterLink, FormsModule],
    template: `
    <div class="flex flex-col h-[calc(100vh-64px)] bg-slate-950 text-slate-200">
      <!-- Toolbar -->
      <div class="h-14 border-b border-white/10 flex items-center justify-between px-6 bg-slate-900/50 backdrop-blur-sm z-20">
        <div class="flex items-center space-x-4">
          <button [routerLink]="['/projects', projectId, 'designer']" class="p-2 hover:bg-white/5 rounded-lg text-slate-400 hover:text-white transition-all">
            <span class="material-icons-outlined">arrow_back</span>
          </button>
          <div class="flex items-center space-x-2">
            <span class="material-icons-outlined text-green-400">account_tree</span>
            <h2 class="text-sm font-semibold text-white">Workflow Automation (Elsa 3.x)</h2>
          </div>
        </div>
        <div class="flex items-center space-x-3">
           <button (click)="createWorkflow()" class="bg-blue-600 hover:bg-blue-500 text-white px-4 py-2 rounded-lg text-sm font-medium transition-all shadow-lg active:scale-95">
             Create Workflow
           </button>
        </div>
      </div>

      <div class="flex-1 p-8 overflow-y-auto">
        <div class="max-w-6xl mx-auto space-y-8">
          
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <div *ngFor="let wf of workflows" class="bg-slate-900/50 border border-white/5 p-6 rounded-2xl hover:border-green-500/30 transition-all group">
               <div class="flex justify-between items-start mb-4">
                  <div class="p-3 bg-green-500/10 rounded-xl text-green-400">
                     <span class="material-icons-outlined">route</span>
                  </div>
                  <span class="px-2 py-1 bg-blue-500/10 text-blue-400 text-[10px] font-bold rounded uppercase">Elsa v3</span>
               </div>
               <h3 class="text-lg font-bold mb-1">{{ wf.name }}</h3>
               <p class="text-slate-500 text-xs mb-6 line-clamp-2">Automated business process with logic triggers and actions.</p>
               
               <div class="flex items-center space-x-2 pt-4 border-t border-white/5">
                  <button class="text-xs text-blue-400 hover:text-blue-300 font-medium px-2 py-1 bg-blue-400/10 rounded">Open Designer</button>
                  <button class="text-xs text-slate-500 hover:text-white font-medium px-2 py-1">View History</button>
               </div>
            </div>

            <!-- Empty State / Placeholder -->
            <div *ngIf="workflows.length === 0" class="col-span-full py-12 flex flex-col items-center justify-center border-2 border-dashed border-white/5 rounded-3xl">
               <div class="w-20 h-20 bg-white/5 rounded-full flex items-center justify-center mb-4">
                  <span class="material-icons-outlined text-4xl text-slate-700">alt_route</span>
               </div>
               <h4 class="text-lg font-bold">No workflows defined yet</h4>
               <p class="text-slate-500 text-sm max-w-xs text-center">Workflows allow you to automate business logic across entities and connectors.</p>
            </div>
          </div>

          <div class="bg-gradient-to-br from-indigo-500/5 to-purple-500/5 border border-white/5 p-8 rounded-3xl">
             <div class="flex items-center space-x-3 mb-4">
                <span class="material-icons-outlined text-indigo-400">info</span>
                <h4 class="font-bold underline">Elsa 3.x Integration Details</h4>
             </div>
             <p class="text-sm text-slate-400 leading-relaxed mb-6">
               The platform is pre-wired with **Elsa Workflows 3.1**. Every project now supports:
             </p>
             <ul class="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs text-slate-500">
                <li class="flex items-center space-x-2">
                   <span class="material-icons-outlined text-green-500 text-sm">check_circle</span>
                   <span>Native Entity Triggers (OnCreate, OnUpdate)</span>
                </li>
                <li class="flex items-center space-x-2">
                   <span class="material-icons-outlined text-green-500 text-sm">check_circle</span>
                   <span>Custom Connector Invocation from Workflow Nodes</span>
                </li>
                <li class="flex items-center space-x-2">
                   <span class="material-icons-outlined text-green-500 text-sm">check_circle</span>
                   <span>HTTP Incoming Webhooks per Project</span>
                </li>
                <li class="flex items-center space-x-2">
                   <span class="material-icons-outlined text-green-500 text-sm">check_circle</span>
                   <span>Multi-Tenant Persistence via PostgreSQL</span>
                </li>
             </ul>
          </div>

        </div>
      </div>
    </div>
  `,
    styles: []
})
export class WorkflowDesigner implements OnInit {
    projectId: string | null = null;
    workflows: any[] = [];

    constructor(
        private readonly route: ActivatedRoute,
        private readonly api: ApiService
    ) {
        this.projectId = this.route.snapshot.paramMap.get('projectId');
    }

    ngOnInit() {
        if (this.projectId) {
            this.loadWorkflows();
        }
    }

    loadWorkflows() {
        this.api.getWorkflows(this.projectId!).subscribe({
            next: (data) => this.workflows = data
        });
    }

    createWorkflow() {
        if (!this.projectId) return;
        const name = prompt('Workflow Name', 'New Order Process');
        if (!name) return;

        const metadata = {
            name: name,
            nodes: [],
            connections: []
        };

        this.api.createWorkflow(this.projectId, metadata).subscribe({
            next: () => this.loadWorkflows()
        });
    }
}
