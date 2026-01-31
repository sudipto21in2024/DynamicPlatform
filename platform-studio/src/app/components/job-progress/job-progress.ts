import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api';
import { interval, Subscription, switchMap } from 'rxjs';

@Component({
    selector: 'app-job-progress',
    standalone: true,
    imports: [CommonModule],
    template: `
    <div class="relative">
      <button (click)="toggleDropdown()" class="p-2 text-slate-400 hover:text-white transition-colors relative">
        <span class="material-icons-outlined">history</span>
        <span *ngIf="activeJobsCount > 0" class="absolute top-1 right-1 w-2 h-2 bg-blue-500 rounded-full animate-pulse"></span>
      </button>

      <!-- Dropdown Panel -->
      <div *ngIf="isOpen" 
           class="absolute right-0 mt-4 w-96 bg-slate-800/90 backdrop-blur-xl border border-slate-700/50 rounded-2xl shadow-2xl z-50 overflow-hidden transform transition-all animate-in fade-in slide-in-from-top-4 duration-300">
        
        <div class="p-4 border-b border-slate-700/50 flex items-center justify-between bg-slate-800/50">
          <h3 class="font-semibold text-slate-100 flex items-center space-x-2">
            <span class="material-icons-outlined text-blue-400">task_alt</span>
            <span>Data Operations</span>
          </h3>
          <span class="text-xs bg-slate-700 text-slate-400 px-2 py-1 rounded-full">{{jobs.length}} Total</span>
        </div>

        <div class="max-h-[400px] overflow-y-auto">
          <div *ngIf="jobs.length === 0" class="p-12 text-center">
            <span class="material-icons-outlined text-slate-600 text-4xl mb-2">inventory_2</span>
            <p class="text-slate-500 text-sm">No recent operations</p>
          </div>

          <div *ngFor="let job of jobs" class="p-4 border-b border-slate-700/30 hover:bg-slate-700/20 transition-colors">
            <div class="flex items-start justify-between mb-2">
              <div class="flex-1">
                <p class="text-sm font-medium text-slate-200 truncate">{{job.reportTitle || 'Unnamed Operation'}}</p>
                <p class="text-[10px] text-slate-500 font-mono">{{job.jobId}}</p>
              </div>
              <span [class]="getStatusClass(job.status)" class="text-[10px] px-2 py-0.5 rounded-full uppercase tracking-wider font-bold">
                {{job.status}}
              </span>
            </div>

            <div class="space-y-1">
              <div class="flex justify-between text-[10px] text-slate-400 mb-1">
                <span>{{job.progress}}% complete</span>
                <span>{{job.rowsProcessed | number}} / {{job.totalRows | number}} rows</span>
              </div>
              <div class="h-1.5 w-full bg-slate-700 rounded-full overflow-hidden">
                <div [style.width.%]="job.progress" 
                     [class.bg-blue-500]="job.status === 'Running' || job.status === 'Queued'"
                     [class.bg-green-500]="job.status === 'Completed'"
                     [class.bg-red-500]="job.status === 'Failed'"
                     class="h-full transition-all duration-500 ease-out shadow-[0_0_8px_rgba(59,130,246,0.5)]">
                </div>
              </div>
            </div>

            <div class="mt-3 flex items-center justify-between">
              <span class="text-[10px] text-slate-500">
                {{job.createdAt | date:'shortTime'}}
              </span>
              <div class="flex space-x-2">
                <a *ngIf="job.status === 'Completed' && job.downloadUrl" 
                   [href]="job.downloadUrl" target="_blank"
                   class="flex items-center space-x-1 text-[10px] text-blue-400 hover:text-blue-300 transition-colors">
                  <span class="material-icons-outlined text-xs">download</span>
                  <span>DOWNLOAD</span>
                </a>
                <button *ngIf="job.status === 'Running' || job.status === 'Queued'"
                        class="text-[10px] text-red-400/70 hover:text-red-400 transition-colors uppercase font-bold">
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="p-3 bg-slate-800/80 border-t border-slate-700/50 text-center">
          <button class="text-[10px] text-slate-500 hover:text-slate-300 transition-colors uppercase tracking-widest font-bold">
            Clear History
          </button>
        </div>
      </div>
    </div>
  `,
    styles: [`
    ::-webkit-scrollbar {
      width: 4px;
    }
    ::-webkit-scrollbar-track {
      background: transparent;
    }
    ::-webkit-scrollbar-thumb {
      background: #334155;
      border-radius: 10px;
    }
    ::-webkit-scrollbar-thumb:hover {
      background: #475569;
    }
    .animate-in {
      animation: slide-down 0.3s ease-out forwards;
    }
    @keyframes slide-down {
      from { opacity: 0; transform: translateY(-10px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `]
})
export class JobProgressComponent implements OnInit, OnDestroy {
    jobs: any[] = [];
    isOpen = false;
    activeJobsCount = 0;
    private pollSubscription?: Subscription;
    private readonly userId = 'admin-user'; // Mock user id

    constructor(private readonly apiService: ApiService) { }

    ngOnInit() {
        this.loadJobs();

        // Poll for updates every 3 seconds
        this.pollSubscription = interval(3000).pipe(
            switchMap(() => this.apiService.getJobs(this.userId))
        ).subscribe(jobs => {
            this.jobs = jobs;
            this.updateActiveCount();
        });
    }

    ngOnDestroy() {
        this.pollSubscription?.unsubscribe();
    }

    toggleDropdown() {
        this.isOpen = !this.isOpen;
    }

    loadJobs() {
        this.apiService.getJobs(this.userId).subscribe(jobs => {
            this.jobs = jobs;
            this.updateActiveCount();
        });
    }

    updateActiveCount() {
        this.activeJobsCount = this.jobs.filter(j =>
            j.status === 'Running' || j.status === 'Queued'
        ).length;
    }

    getStatusClass(status: string): string {
        switch (status) {
            case 'Completed': return 'bg-green-500/10 text-green-400 border border-green-500/20';
            case 'Running': return 'bg-blue-500/10 text-blue-400 border border-blue-500/20';
            case 'Failed': return 'bg-red-500/10 text-red-400 border border-red-500/20';
            case 'Queued': return 'bg-amber-500/10 text-amber-400 border border-amber-500/20';
            default: return 'bg-slate-500/10 text-slate-400 border border-slate-500/20';
        }
    }
}
