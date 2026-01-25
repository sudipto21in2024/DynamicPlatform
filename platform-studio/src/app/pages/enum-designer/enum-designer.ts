import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api';

interface EnumValue {
    name: string;
    value: number;
}

interface EnumMetadata {
    id?: string;
    name: string;
    namespace: string;
    values: EnumValue[];
}

@Component({
    selector: 'app-enum-designer',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterLink],
    template: `
    <div class="flex flex-col h-[calc(100vh-64px)] bg-[#0B1120] text-slate-200 overflow-hidden font-sans">
      <!-- Toolbar -->
      <div class="h-14 border-b border-white/5 flex items-center justify-between px-6 bg-slate-900/80 backdrop-blur-xl z-20 shadow-2xl">
        <div class="flex items-center space-x-6">
          <div class="flex items-center space-x-3">
             <div class="p-2 bg-amber-500/10 rounded-xl shadow-inner shadow-amber-500/20">
                <span class="material-icons-outlined text-amber-400 text-lg text-glow text-amber-500">list_alt</span>
             </div>
             <div>
                <h2 class="text-sm font-black text-white tracking-widest uppercase">Enum Architect</h2>
                <div class="text-[9px] text-slate-500 font-mono tracking-tighter opacity-60">Metadata Engine // {{ projectId | slice:0:8 }}</div>
             </div>
          </div>
          <div class="w-px h-8 bg-white/10 mx-2"></div>
          <div class="flex items-center space-x-4">
             <nav class="flex space-x-1 p-1 bg-black/20 rounded-xl">
                <button [routerLink]="['/projects', projectId, 'designer']" class="px-3 py-1.5 rounded-lg text-xs font-bold text-slate-500 hover:text-white transition-all">Entities</button>
                <button [routerLink]="['/projects', projectId, 'pages']" class="px-3 py-1.5 rounded-lg text-xs font-bold text-slate-500 hover:text-white transition-all">Pages</button>
                <div class="bg-amber-600/20 text-amber-400 px-3 py-1.5 rounded-lg text-xs font-bold border border-amber-500/20 shadow-lg shadow-amber-500/10">Enums</div>
                <button [routerLink]="['/projects', projectId, 'workflows']" class="px-3 py-1.5 rounded-lg text-xs font-bold text-slate-500 hover:text-white transition-all">Workflows</button>
             </nav>
          </div>
        </div>
        <div class="flex items-center space-x-3">
          <button (click)="saveEnums()" class="group flex items-center space-x-2 text-xs text-slate-400 hover:text-white px-4 py-2 rounded-lg transition-all hover:bg-white/5 active:scale-95">
            <span class="material-icons-outlined text-lg group-hover:animate-pulse text-amber-500">save</span>
            <span class="font-bold uppercase tracking-widest text-[10px]">Sync Metadata</span>
          </button>
          
          <div class="h-6 w-px bg-white/10 mx-2"></div>

          <button (click)="addNewEnum()" class="group flex items-center space-x-2 text-xs bg-amber-600 hover:bg-amber-500 text-white px-6 py-2 rounded-xl shadow-lg shadow-amber-900/50 transition-all active:scale-95">
             <span class="material-icons-outlined text-lg group-hover:rotate-90 transition-transform">add</span>
             <span class="font-black uppercase tracking-widest text-[10px]">New Enum</span>
          </button>
        </div>
      </div>

      <div class="flex flex-1 overflow-hidden relative">
        <!-- Explorer (Left) -->
        <aside class="w-64 glass-dark border-r border-white/5 flex flex-col shadow-2xl z-20">
          <div class="h-12 border-b border-white/5 flex items-center px-4 bg-white/5 justify-between">
             <span class="text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">Dictionary Explorer</span>
          </div>
          <div class="p-2 space-y-1 overflow-y-auto scrollbar-hide">
              <div *ngFor="let e of enums" 
                   (click)="selectEnum(e)"
                   [class.bg-amber-600/20]="selectedEnum === e"
                   [class.border-amber-500/40]="selectedEnum === e"
                   class="group flex items-center justify-between p-3 rounded-xl hover:bg-white/[0.03] border border-transparent transition-all cursor-pointer">
                 <div class="flex items-center space-x-3">
                    <span class="material-icons-outlined text-sm text-amber-500/60">segment</span>
                    <span class="text-xs font-bold text-slate-300 group-hover:text-white">{{e.name}}</span>
                 </div>
                 <button (click)="deleteEnum($event, e)" class="opacity-0 group-hover:opacity-100 p-1 hover:text-red-400 transition-opacity">
                    <span class="material-icons-outlined text-xs">delete</span>
                 </button>
              </div>

              <div *ngIf="enums.length === 0" class="py-12 text-center opacity-20">
                 <span class="material-icons-outlined text-4xl mb-2">inventory_2</span>
                 <p class="text-[9px] font-black uppercase tracking-widest">No Enums Defined</p>
              </div>
          </div>
        </aside>

        <!-- Canvas (Center) -->
        <main class="flex-1 bg-[#0B1120] relative p-12 overflow-y-auto scrollbar-thin scrollbar-thumb-slate-800 scrollbar-track-transparent">
           <div *ngIf="!selectedEnum" class="h-full flex flex-col items-center justify-center text-slate-700 animate-fadeIn">
              <div class="w-32 h-32 rounded-full border-4 border-dashed border-white/[0.02] flex items-center justify-center mb-8">
                 <span class="material-icons-outlined text-6xl opacity-10">format_list_bulleted</span>
              </div>
              <p class="text-[10px] font-black uppercase tracking-[0.5em] text-center max-w-xs leading-loose">
                 Select an existing enum or create a new definition to manage status codes and constants
              </p>
           </div>

           <div *ngIf="selectedEnum" class="max-w-3xl mx-auto space-y-8 animate-slideUp">
              <!-- Enum Identity Header -->
              <div class="glass-dark p-8 rounded-3xl border border-white/5 shadow-2xl relative overflow-hidden">
                 <div class="absolute top-0 right-0 p-8 opacity-[0.03] pointer-events-none">
                    <span class="material-icons-outlined text-[120px]">segments</span>
                 </div>
                 
                 <div class="flex items-start justify-between relative z-10">
                    <div class="space-y-6 w-full pr-12">
                       <div class="space-y-2">
                          <label class="text-[10px] font-black uppercase tracking-[0.2em] text-amber-500/60">Enum Identity</label>
                          <input type="text" [(ngModel)]="selectedEnum.name" 
                                 class="w-full bg-transparent border-b border-white/10 text-3xl font-black text-white focus:border-amber-500 outline-none pb-2 transition-all"
                                 placeholder="e.g. ProjectStatus">
                       </div>
                       <div class="space-y-2">
                          <label class="text-[10px] font-black uppercase tracking-[0.2em] text-slate-500">Logical Namespace</label>
                          <input type="text" [(ngModel)]="selectedEnum.namespace" 
                                 class="w-full bg-white/5 rounded-xl px-4 py-2 text-xs font-mono text-slate-400 focus:border-amber-500/40 outline-none border border-transparent transition-all"
                                 placeholder="GeneratedApp.Entities">
                       </div>
                    </div>
                 </div>
              </div>

              <!-- Values List -->
              <div class="space-y-4">
                 <div class="flex items-center justify-between px-2">
                    <label class="text-[10px] font-black uppercase tracking-[0.2em] text-slate-500">Defined Values (Constants)</label>
                    <button (click)="addValue()" class="flex items-center space-x-2 text-[10px] font-black uppercase tracking-widest text-amber-400 hover:text-white transition-colors">
                       <span class="material-icons-outlined text-sm">add</span>
                       <span>Add Option</span>
                    </button>
                 </div>

                 <div class="space-y-2">
                    <div *ngFor="let val of selectedEnum.values; let i = index" 
                         class="group flex items-center space-x-3 bg-white/[0.02] hover:bg-white/[0.04] p-3 rounded-2xl border border-white/5 transition-all">
                       <div class="w-8 h-8 rounded-lg bg-black/40 flex items-center justify-center text-[10px] font-black text-slate-600 font-mono">
                          {{i}}
                       </div>
                       <div class="flex-1">
                          <input type="text" [(ngModel)]="val.name" 
                                 class="w-full bg-transparent border-none text-sm font-bold text-slate-200 outline-none focus:text-amber-400" 
                                 placeholder="Value Name (e.g. Active)">
                       </div>
                       <div class="w-32">
                          <input type="number" [(ngModel)]="val.value" 
                                 class="w-full bg-black/20 border border-white/5 rounded-lg px-3 py-1.5 text-xs font-mono text-amber-500/80 outline-none text-right" 
                                 placeholder="Int Value">
                       </div>
                       <button (click)="removeValue(i)" class="p-2 text-slate-600 hover:text-red-400 transition-colors">
                          <span class="material-icons-outlined text-sm">close</span>
                       </button>
                    </div>

                    <div *ngIf="selectedEnum.values.length === 0" 
                         (click)="addValue()"
                         class="py-8 border-2 border-dashed border-white/[0.03] rounded-2xl flex flex-col items-center justify-center group cursor-pointer hover:border-amber-500/20 transition-all">
                       <span class="material-icons-outlined text-2xl text-slate-700 group-hover:text-amber-500/40 mb-2">playlist_add</span>
                       <span class="text-[9px] font-black uppercase tracking-widest text-slate-600">Click to add initial values</span>
                    </div>
                 </div>
              </div>
           </div>
        </main>

        <!-- Inspector (Right) -->
        <aside class="w-80 glass-dark border-l border-white/5 flex flex-col shadow-2xl z-20">
           <div class="h-14 border-b border-white/5 flex items-center px-6 bg-amber-600/5 backdrop-blur-md">
            <span class="material-icons-outlined text-amber-400 mr-2 text-lg text-glow">insights</span>
            <span class="font-black text-[10px] uppercase tracking-widest text-slate-300">Enum Inspector</span>
          </div>

          <div class="p-6 space-y-8 overflow-y-auto scrollbar-hide">
              <div *ngIf="!selectedEnum" class="text-center opacity-40 py-12">
                 <p class="text-[10px] font-black uppercase tracking-[0.2em] leading-loose">Metadata stats will appear here when a definition is selected</p>
              </div>

              <div *ngIf="selectedEnum" class="space-y-6 animate-fadeIn">
                 <div class="bg-black/40 p-5 rounded-2xl border border-white/5 space-y-4">
                    <div class="flex flex-col">
                       <span class="text-[8px] font-black text-slate-500 uppercase tracking-[0.2em]">Total Options</span>
                       <span class="text-2xl font-black text-white">{{selectedEnum.values.length}}</span>
                    </div>
                    <div class="w-full h-1 bg-white/5 rounded-full overflow-hidden">
                       <div class="h-full bg-amber-500 shadow-glow-amber" [style.width.%]="(selectedEnum.values.length / 10) * 100"></div>
                    </div>
                 </div>

                 <div class="space-y-4 pt-4 border-t border-white/5">
                    <label class="text-[10px] font-black text-amber-500/60 uppercase tracking-widest">Compiler Hints</label>
                    <div class="space-y-3">
                       <div class="flex items-center justify-between group cursor-pointer">
                          <span class="text-[10px] font-bold text-slate-400 group-hover:text-white transition-colors">Serialization: Integer</span>
                          <span class="material-icons-outlined text-sm text-green-500/60">check_circle</span>
                       </div>
                       <div class="flex items-center justify-between group cursor-pointer opacity-40">
                          <span class="text-[10px] font-bold text-slate-400">Generate [Flags]</span>
                          <span class="material-icons-outlined text-sm">radio_button_unchecked</span>
                       </div>
                    </div>
                 </div>

                 <div class="pt-8 text-center">
                    <p class="text-[8px] text-slate-700 font-bold uppercase tracking-[0.2em] leading-relaxed">
                       PRO TIP: Enums can be referenced by name in any Entity field for type-safe status handling.
                    </p>
                 </div>
              </div>
          </div>
        </aside>
      </div>
    </div>
  `,
    styles: [`
    .text-glow {
      filter: drop-shadow(0 0 8px rgba(245, 158, 11, 0.4));
    }
    .shadow-glow-amber {
       box-shadow: 0 0 15px rgba(245, 158, 11, 0.3);
    }
  `]
})
export class EnumDesigner implements OnInit {
    projectId: string | null = null;
    enums: EnumMetadata[] = [];
    selectedEnum: EnumMetadata | null = null;

    constructor(
        private readonly route: ActivatedRoute,
        private readonly api: ApiService
    ) {
        this.projectId = this.route.snapshot.paramMap.get('projectId');
    }

    ngOnInit() {
        this.loadEnums();
    }

    loadEnums() {
        this.api.request('GET', `projects/${this.projectId}/artifacts?type=9`).subscribe((res: any) => {
            this.enums = res.map((a: any) => {
                const meta = JSON.parse(a.content);
                return { ...meta, id: a.id };
            });
        });
    }

    selectEnum(e: EnumMetadata) {
        this.selectedEnum = e;
    }

    addNewEnum() {
        const newEnum: EnumMetadata = {
            name: 'NewEnum',
            namespace: 'GeneratedApp.Entities',
            values: [
                { name: 'Default', value: 0 }
            ]
        };
        this.enums.push(newEnum);
        this.selectedEnum = newEnum;
    }

    addValue() {
        if (this.selectedEnum) {
            const nextVal = this.selectedEnum.values.length;
            this.selectedEnum.values.push({ name: '', value: nextVal });
        }
    }

    removeValue(index: number) {
        if (this.selectedEnum) {
            this.selectedEnum.values.splice(index, 1);
        }
    }

    deleteEnum(event: Event, e: EnumMetadata) {
        event.stopPropagation();
        if (confirm(`Delete Enum "${e.name}"?`)) {
            if (e.id) {
                this.api.request('DELETE', `projects/${this.projectId}/artifacts/${e.id}`).subscribe(() => {
                    this.enums = this.enums.filter(item => item !== e);
                    if (this.selectedEnum === e) this.selectedEnum = null;
                });
            } else {
                this.enums = this.enums.filter(item => item !== e);
                if (this.selectedEnum === e) this.selectedEnum = null;
            }
        }
    }

    saveEnums() {
        if (!this.selectedEnum) return;

        const payload = {
            name: this.selectedEnum.name,
            type: 9, // Enum
            content: JSON.stringify(this.selectedEnum)
        };

        if (this.selectedEnum.id) {
            this.api.request('PUT', `projects/${this.projectId}/artifacts/${this.selectedEnum.id}`, payload).subscribe();
        } else {
            this.api.request('POST', `projects/${this.projectId}/artifacts`, payload).subscribe((res: any) => {
                if (this.selectedEnum) this.selectedEnum.id = res.id;
            });
        }
    }
}
