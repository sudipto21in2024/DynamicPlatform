import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="flex h-screen bg-slate-900 text-white overflow-hidden">
      <!-- Sidebar -->
      <aside class="w-64 bg-slate-800/50 backdrop-blur-xl border-r border-slate-700/50 flex flex-col">
        <div class="p-6">
          <h1 class="text-2xl font-bold bg-gradient-to-r from-blue-400 to-indigo-400 bg-clip-text text-transparent">
            DynamicPlatform
          </h1>
          <p class="text-xs text-slate-500 mt-1 uppercase tracking-widest font-semibold">Low-Code Studio</p>
        </div>

        <nav class="flex-1 px-4 space-y-2">
          <a routerLink="/projects" routerLinkActive="bg-blue-600/20 text-blue-400 border-blue-600/50" 
             class="flex items-center space-x-3 p-3 rounded-lg border border-transparent transition-all hover:bg-slate-700/50">
            <span class="material-icons-outlined text-xl">folder</span>
            <span class="font-medium">Projects</span>
          </a>
          <a class="flex items-center space-x-3 p-3 rounded-lg border border-transparent text-slate-400 cursor-not-allowed">
            <span class="material-icons-outlined text-xl">schema</span>
            <span class="font-medium">Models (Soon)</span>
          </a>
          <a class="flex items-center space-x-3 p-3 rounded-lg border border-transparent text-slate-400 cursor-not-allowed">
            <span class="material-icons-outlined text-xl">analytics</span>
            <span class="font-medium">KPIs (Soon)</span>
          </a>
        </nav>

        <div class="p-4 border-t border-slate-700/50">
          <div class="flex items-center space-x-3 p-2">
            <div class="w-10 h-10 rounded-full bg-gradient-to-tr from-blue-500 to-indigo-600 flex items-center justify-center font-bold">
              JD
            </div>
            <div>
              <p class="text-sm font-medium">John Developer</p>
              <p class="text-xs text-slate-500">Professional Plan</p>
            </div>
          </div>
        </div>
      </aside>

      <!-- Main Content -->
      <main class="flex-1 overflow-y-auto bg-[radial-gradient(ellipse_at_top_right,_var(--tw-gradient-stops))] from-slate-800 via-slate-900 to-slate-950">
        <header class="h-16 border-b border-slate-700/50 flex items-center justify-between px-8 backdrop-blur-md sticky top-0 z-10">
          <div class="flex items-center space-x-4">
             <span class="text-slate-400">Workspace</span>
             <span class="text-slate-600">/</span>
             <span class="font-medium">All Projects</span>
          </div>
          <div class="flex items-center space-x-4">
            <button class="p-2 text-slate-400 hover:text-white transition-colors">
              <span class="material-icons-outlined">notifications</span>
            </button>
            <button class="bg-blue-600 hover:bg-blue-500 text-white px-4 py-2 rounded-lg font-medium transition-all shadow-lg shadow-blue-600/20">
              New Project
            </button>
          </div>
        </header>

        <div class="flex-1">
          <router-outlet></router-outlet>
        </div>
      </main>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      height: 100vh;
    }
  `],
})
export class Dashboard { }
