import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api';

@Component({
  selector: 'app-projects-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="min-h-screen bg-[#0B1120] text-slate-200">
      <!-- Gradient Header -->
      <div class="h-80 bg-gradient-to-br from-blue-600/20 via-slate-900 to-slate-950 relative overflow-hidden border-b border-white/5">
        <div class="absolute inset-0 shimmer opacity-30"></div>
        <div class="max-w-7xl mx-auto px-8 h-full flex flex-col justify-center relative z-10">
          <h1 class="text-5xl font-black text-white mb-4 tracking-tight">Dynamic<span class="text-blue-500">Platform</span></h1>
          <p class="text-xl text-slate-400 max-w-2xl leading-relaxed">
            Build enterprise-grade applications with zero code. Designed for doctors, clinics, and modern businesses.
          </p>
          <div class="mt-8 flex space-x-4">
            <button class="bg-blue-600 hover:bg-blue-500 text-white px-6 py-3 rounded-xl font-bold shadow-2xl transition-all active:scale-95">
              Launch Studio
            </button>
            <button class="glass px-6 py-3 rounded-xl font-bold hover:bg-white/10 transition-all border border-white/10">
              Documentation
            </button>
          </div>
        </div>
      </div>

      <div class="max-w-7xl mx-auto p-8 -mt-16 relative z-20">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
          @for (project of projects; track project.id) {
            <div [routerLink]="['/projects', project.id, 'designer']" 
                 class="glass group p-8 rounded-3xl hover:border-blue-500/50 transition-all cursor-pointer hover:shadow-2xl hover:shadow-blue-500/10 animate-fadeIn">
              <div class="flex justify-between items-start mb-6">
                <div class="p-4 bg-blue-600/10 rounded-2xl text-blue-400 group-hover:bg-blue-600 group-hover:text-white transition-all shadow-inner">
                  <span class="material-icons-outlined text-3xl">rocket_launch</span>
                </div>
                <div class="flex flex-col items-end">
                   <span class="px-3 py-1 bg-green-500/10 text-green-400 text-[10px] font-black uppercase rounded-full border border-green-500/20 tracking-widest">Active</span>
                   <span class="text-[10px] text-slate-600 mt-2 font-mono">v{{ project.version || '1.0.4' }}</span>
                </div>
              </div>
              <h3 class="text-2xl font-black text-white mb-3 group-hover:text-blue-400 transition-colors">{{ project.name }}</h3>
              <p class="text-slate-400 text-sm mb-8 leading-relaxed line-clamp-3">{{ project.description }}</p>
              
              <div class="flex items-center justify-between text-[11px] text-slate-500 border-t border-white/5 pt-6 font-bold uppercase tracking-wider">
                <div class="flex items-center space-x-2">
                  <span class="material-icons-outlined text-sm">schedule</span>
                  <span>{{ project.updatedAt | date:'mediumDate' }}</span>
                </div>
                <div class="flex items-center space-x-2">
                  <span class="material-icons-outlined text-sm">layers</span>
                  <span>{{ project.entitiesCount || 12 }} Objects</span>
                </div>
              </div>
            </div>
          }

          <!-- Add New Project Card -->
          <div (click)="createNewProject()" class="glass-dark border-2 border-dashed border-white/10 p-8 rounded-3xl flex flex-col items-center justify-center text-slate-500 hover:border-blue-500/50 hover:text-blue-400 transition-all cursor-pointer group">
            <div class="w-16 h-16 rounded-full bg-white/5 flex items-center justify-center group-hover:bg-blue-600/10 group-hover:scale-110 transition-all mb-4">
               <span class="material-icons-outlined text-4xl">add</span>
            </div>
            <span class="font-black text-xl tracking-tight">Create New Project</span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: ``,
})
export class ProjectsList implements OnInit {
  projects: any[] = [];
  projectId: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: ApiService
  ) {
  }

  ngOnInit() {
    this.api.getProjects().subscribe({
      next: (data) => {
        if (data && data.length > 0) {
          this.projects = data;
        } else {
          this.loadMocks();
        }
      },
      error: () => this.loadMocks()
    });
  }

  loadMocks() {
    this.projects = [
      {
        id: '1',
        name: 'Clinic Management System',
        description: 'Multi-doctor appointment scheduling with patient medical history and automated notifications.',
        updatedAt: new Date(),
        entitiesCount: 14,
        version: '2.1.0'
      },
      {
        id: '2',
        name: 'Inventory Hub Pro',
        description: 'Real-time supply chain tracking with external ERP connectors and AI-driven forecasting.',
        updatedAt: new Date(),
        entitiesCount: 9,
        version: '1.0.8'
      }
    ];
  }

  createNewProject() {
    const name = prompt('Project Name:');
    if (name) {
      this.api.createProject({ name, tenantId: '00000000-0000-0000-0000-000000000000' }).subscribe(() => this.ngOnInit());
    }
  }
}
