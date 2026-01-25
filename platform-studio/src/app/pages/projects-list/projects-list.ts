import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../services/api';

@Component({
  selector: 'app-projects-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="space-y-6 p-8">
      <div class="flex items-center justify-between">
        <h2 class="text-3xl font-bold">Projects</h2>
        <div class="flex space-x-2">
           <!-- Filters or Search could go here -->
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        @for (project of projects; track project.id) {
          <div [routerLink]="['/projects', project.id, 'designer']" 
               class="bg-slate-800/40 backdrop-blur-md border border-slate-700/50 p-6 rounded-2xl hover:border-blue-500/50 transition-all group cursor-pointer hover:shadow-2xl hover:shadow-blue-500/10">
            <div class="flex justify-between items-start mb-4">
              <div class="p-3 bg-blue-600/20 rounded-xl text-blue-400 group-hover:bg-blue-600 group-hover:text-white transition-all">
                <span class="material-icons-outlined text-2xl">rocket_launch</span>
              </div>
              <span class="px-3 py-1 bg-green-500/10 text-green-400 text-xs font-bold rounded-full border border-green-500/20">Active</span>
            </div>
            <h3 class="text-xl font-bold mb-2">{{ project.name }}</h3>
            <p class="text-slate-400 text-sm mb-6 line-clamp-2">{{ project.description }}</p>
            
            <div class="flex items-center justify-between text-xs text-slate-500 border-t border-slate-700/50 pt-4">
              <div class="flex items-center space-x-2">
                <span class="material-icons-outlined text-sm">schedule</span>
                <span>{{ project.updatedAt | date:'shortDate' }}</span>
              </div>
              <div class="flex items-center space-x-2">
                <span class="material-icons-outlined text-sm">storage</span>
                <span>{{ project.entitiesCount }} Entities</span>
              </div>
            </div>
          </div>
        }

        <!-- Add New Project Card -->
        <div class="bg-slate-900/40 border-2 border-dashed border-slate-700 p-6 rounded-2xl flex flex-col items-center justify-center text-slate-500 hover:border-blue-500/50 hover:text-blue-400 transition-all cursor-pointer">
          <span class="material-icons-outlined text-4xl mb-2">add_circle_outline</span>
          <span class="font-bold text-lg">Create New Project</span>
        </div>
      </div>
    </div>
  `,
  styles: ``,
})
export class ProjectsList implements OnInit {
  projects: any[] = [];

  constructor(private readonly api: ApiService) { }

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
        name: 'E-Commerce Core (Mock)',
        description: 'The main backend for the retail application including inventory and orders.',
        updatedAt: new Date(),
        entitiesCount: 12
      },
      {
        id: '2',
        name: 'Customer Portal (Mock)',
        description: 'Mobile responsive customer dashboard for tracking loyalty points.',
        updatedAt: new Date(),
        entitiesCount: 5
      }
    ];
  }
}
