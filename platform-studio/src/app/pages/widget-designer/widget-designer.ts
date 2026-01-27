import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api';

interface WidgetDefinition {
    id?: string;
    name: string;
    category: string;
    template: string;
    propertyDefinitions: WidgetPropertyDef[];
    events: string[]; // List of event names like 'onClick'
}

interface WidgetPropertyDef {
    name: string;
    displayName: string;
    type: string;
    defaultValue: string;
    options: string[]; // For enum types, comma separated string converted to array
}

@Component({
    selector: 'app-widget-designer',
    standalone: true,
    imports: [CommonModule, FormsModule],
    template: `
    <div class="flex flex-col h-[calc(100vh-64px)] bg-[#0B1120] text-slate-200">
      <!-- Toolbar -->
      <div class="h-14 border-b border-white/5 flex items-center justify-between px-6 bg-slate-900/80 backdrop-blur-xl shrink-0">
        <div class="flex items-center space-x-4">
           <div class="p-2 bg-purple-500/10 rounded-xl">
              <span class="material-icons-outlined text-purple-400 text-lg">widgets</span>
           </div>
           <div>
              <h2 class="text-sm font-black text-white tracking-widest uppercase">Custom Widget Studio</h2>
              <div class="text-[9px] text-slate-500 font-mono">Build reusable UI components</div>
           </div>
           
           <div class="h-8 w-px bg-white/10 mx-2"></div>
           
           <select [(ngModel)]="currentWidgetId" (change)="loadWidget()" class="bg-black/20 border border-white/10 rounded px-3 py-1 text-xs text-white focus:outline-none">
              <option value="">Start New Widget...</option>
              <option *ngFor="let w of availableWidgets" [value]="w.id">{{ w.name }}</option>
           </select>
        </div>

        <button (click)="saveWidget()" class="bg-purple-600 hover:bg-purple-500 text-white px-4 py-2 rounded-lg text-xs font-bold uppercase tracking-wider transition-all">
           Save Widget
        </button>
      </div>

      <div class="flex flex-1 overflow-hidden">
         <!-- Configuration (Left) -->
         <div class="w-96 border-r border-white/5 flex flex-col overflow-y-auto bg-slate-900/50">
            <div class="p-6 space-y-6">
               
               <!-- Identity -->
               <div class="space-y-4">
                  <h3 class="text-[10px] font-black uppercase text-purple-400 tracking-widest">Identity</h3>
                  <div class="space-y-3">
                     <div class="space-y-1">
                        <label class="text-[9px] font-bold text-slate-500">Widget Name</label>
                        <input [(ngModel)]="definition.name" class="w-full bg-black/20 border border-white/10 rounded px-3 py-2 text-xs text-white focus:border-purple-500 outline-none" placeholder="e.g., TemperatureGauge">
                     </div>
                     <div class="space-y-1">
                        <label class="text-[9px] font-bold text-slate-500">Category</label>
                        <select [(ngModel)]="definition.category" class="w-full bg-black/20 border border-white/10 rounded px-3 py-2 text-xs text-white outline-none">
                           <option>General</option>
                           <option>Data</option>
                           <option>Forms</option>
                        </select>
                     </div>
                  </div>
               </div>

               <!-- Properties -->
               <div class="space-y-4">
                  <div class="flex justify-between items-center">
                     <h3 class="text-[10px] font-black uppercase text-blue-400 tracking-widest">Public Properties</h3>
                     <button (click)="addProperty()" class="text-[9px] bg-blue-500/10 text-blue-400 px-2 py-1 rounded hover:bg-blue-500/20 font-bold">+ ADD</button>
                  </div>
                  
                  <div class="space-y-2">
                     <div *ngFor="let prop of definition.propertyDefinitions; let i = index" class="bg-white/5 p-3 rounded-lg border border-white/5 space-y-2">
                        <div class="flex justify-between">
                           <input [(ngModel)]="prop.name" class="bg-transparent border-b border-white/10 text-xs font-bold text-white w-1/2 focus:outline-none" placeholder="propName">
                           <button (click)="removeProperty(i)" class="text-slate-600 hover:text-red-400">×</button>
                        </div>
                        <div class="grid grid-cols-2 gap-2">
                            <select [(ngModel)]="prop.type" class="bg-black/20 text-[10px] text-slate-400 rounded px-1 py-1 border border-white/5">
                               <option value="string">String</option>
                               <option value="number">Number</option>
                               <option value="boolean">Boolean</option>
                               <option value="color">Color</option>
                            </select>
                            <input [(ngModel)]="prop.defaultValue" class="bg-black/20 text-[10px] text-slate-400 rounded px-1 py-1 border border-white/5" placeholder="Default">
                        </div>
                     </div>
                  </div>
               </div>

               <!-- Events -->
               <div class="space-y-4">
                  <div class="flex justify-between items-center">
                     <h3 class="text-[10px] font-black uppercase text-emerald-400 tracking-widest">Events</h3>
                     <button (click)="addEvent()" class="text-[9px] bg-emerald-500/10 text-emerald-400 px-2 py-1 rounded hover:bg-emerald-500/20 font-bold">+ ADD</button>
                  </div>
                  <div class="space-y-2">
                     <div *ngFor="let evt of definition.events; let i = index; trackBy: trackByIndex" class="flex items-center space-x-2">
                        <input [ngModel]="definition.events[i]" (ngModelChange)="definition.events[i]=$event"  class="flex-1 bg-white/5 border border-white/5 rounded px-3 py-1.5 text-xs text-white" placeholder="onClick">
                        <button (click)="removeEvent(i)" class="text-slate-600 hover:text-red-400">×</button>
                     </div>
                  </div>
               </div>

            </div>
         </div>

         <!-- Editor Visuals (Center) -->
         <div class="flex-1 flex flex-col bg-[#0f1629]">
            <!-- Template Editor -->
            <div class="flex-1 flex flex-col border-b border-white/5">
               <div class="h-8 bg-black/20 flex items-center px-4 border-b border-white/5 justify-between">
                  <span class="text-[9px] font-mono text-slate-500 uppercase">Template (HTML/Scriban)</span>
                  <div class="flex space-x-2">
                     <button class="text-[9px] text-slate-500 hover:text-white">Insert Prop</button>
                  </div>
               </div>
               <textarea [(ngModel)]="definition.template" 
                         class="flex-1 bg-[#0B1120] text-slate-300 font-mono text-xs p-4 focus:outline-none resize-none"
                         [placeholder]="placeholderText"></textarea>
            </div>

            <!-- Preview Data -->
            <div class="h-1/3 flex flex-col bg-black/20">
               <div class="h-8 bg-black/20 flex items-center px-4 border-b border-white/5 border-t">
                  <span class="text-[9px] font-mono text-slate-500 uppercase">Preview & Styling</span>
               </div>
               <div class="p-8 flex items-center justify-center">
                   <div class="text-slate-500 text-xs italic">
                       Preview rendering coming soon. (Requires Safe HTML Pipe implementation)
                       <br>
                       <span class="font-mono mt-2 block opacity-50">{{ definition.template }}</span>
                   </div>
               </div>
            </div>
         </div>
      </div>
    </div>
  `,
    styles: [`
    ::-webkit-scrollbar { width: 4px; height: 4px; }
    ::-webkit-scrollbar-thumb { background: #334155; border-radius: 2px; }
    textarea { font-family: 'Fira Code', monospace; }
  `]
})
export class WidgetDesignerComponent implements OnInit {
    projectId = '';
    availableWidgets: any[] = [];
    currentWidgetId = '';

    placeholderText = `<div class='my-card'>\n  <h3>{{properties['title']}}</h3>\n  <button (click)="trigger('onClick')">Action</button>\n</div>`;

    definition: WidgetDefinition = {
        name: 'NewWidget',
        category: 'General',
        template: '<div class="card">\n  <h3>{{title}}</h3>\n</div>',
        propertyDefinitions: [
            { name: 'title', displayName: 'Title', type: 'string', defaultValue: 'Card Title', options: [] }
        ],
        events: ['onClick']
    };

    constructor(
        private readonly api: ApiService,
        private readonly route: ActivatedRoute
    ) { }

    ngOnInit() {
        this.route.parent?.paramMap.subscribe(p => {
            this.projectId = p.get('id') || '';
            this.loadList();
        });
    }

    loadList() {
        this.api.getWidgets(this.projectId).subscribe(ws => {
            this.availableWidgets = ws.map(artifact => {
                // Artifact to light object
                return { id: artifact.id, name: artifact.name };
            });
        });
    }

    loadWidget() {
        if (!this.currentWidgetId) {
            this.resetDef();
            return;
        }

        this.api.getWidgets(this.projectId).subscribe(ws => {
            const artifact = ws.find((x: any) => x.id === this.currentWidgetId);
            if (artifact) {
                const content = typeof artifact.content === 'string' ? JSON.parse(artifact.content) : artifact.content;
                this.definition = content;
                // Ensure defaults
                if (!this.definition.propertyDefinitions) this.definition.propertyDefinitions = [];
                if (!this.definition.events) this.definition.events = [];
            }
        });
    }

    resetDef() {
        this.definition = {
            name: 'NewWidget',
            category: 'General',
            template: '',
            propertyDefinitions: [],
            events: []
        };
    }

    addProperty() {
        this.definition.propertyDefinitions.push({
            name: 'newProp',
            displayName: 'New Property',
            type: 'string',
            defaultValue: '',
            options: []
        });
    }

    removeProperty(index: number) {
        this.definition.propertyDefinitions.splice(index, 1);
    }

    addEvent() {
        this.definition.events.push('onEvent');
    }

    removeEvent(index: number) {
        this.definition.events.splice(index, 1);
    }

    trackByIndex(index: number, obj: any): any {
        return index;
    }

    saveWidget() {
        if (this.currentWidgetId) {
            this.api.updateWidget(this.projectId, this.currentWidgetId, this.definition).subscribe(() => {
                alert('Widget updated!');
                this.loadList();
            });
        } else {
            this.api.createWidget(this.projectId, this.definition).subscribe((res) => {
                alert('Widget created!');
                this.currentWidgetId = res.id;
                this.loadList();
            });
        }
    }
}
