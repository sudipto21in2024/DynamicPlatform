import { Component, ElementRef, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import Konva from 'konva';
import { ApiService } from '../../services/api';

@Component({
  selector: 'app-entity-designer',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="flex flex-col h-[calc(100vh-64px)] bg-slate-950 text-slate-200">
      <!-- Toolbar -->
      <div class="h-14 border-b border-white/10 flex items-center justify-between px-6 bg-slate-900/50 backdrop-blur-sm z-20 shadow-sm">
        <div class="flex items-center space-x-6">
          <div class="flex items-center space-x-3">
             <div class="p-2 bg-blue-500/10 rounded-lg">
                <span class="material-icons-outlined text-blue-400 text-lg">schema</span>
             </div>
             <div>
                <h2 class="text-sm font-semibold text-white">Entity Model</h2>
                <div class="text-[10px] text-slate-500 font-mono">ID: {{ projectId | slice:0:8 }}...</div>
             </div>
          </div>
          <div class="w-px h-8 bg-white/10"></div>
          <button (click)="addEntity()" class="group flex items-center space-x-2 text-sm bg-white/5 hover:bg-white/10 border border-white/5 active:bg-blue-600 active:border-blue-500 px-4 py-2 rounded-lg transition-all">
            <span class="material-icons-outlined text-blue-400 group-hover:text-blue-300 text-lg">add_box</span>
            <span class="font-medium">New Entity</span>
          </button>
          <button [routerLink]="['/projects', projectId, 'security']" class="group flex items-center space-x-2 text-sm bg-white/5 hover:bg-white/10 border border-white/5 px-4 py-2 rounded-lg transition-all">
            <span class="material-icons-outlined text-purple-400 group-hover:text-purple-300 text-lg">admin_panel_settings</span>
            <span class="font-medium">Security</span>
          </button>
          <button [routerLink]="['/projects', projectId, 'workflows']" class="group flex items-center space-x-2 text-sm bg-white/5 hover:bg-white/10 border border-white/5 px-4 py-2 rounded-lg transition-all">
            <span class="material-icons-outlined text-green-400 group-hover:text-green-300 text-lg">account_tree</span>
            <span class="font-medium">Workflows</span>
          </button>
        </div>
        <div class="flex items-center space-x-3">
          <button (click)="save()" class="flex items-center space-x-2 text-sm text-slate-400 hover:text-white px-4 py-2 rounded-lg transition-colors hover:bg-white/5">
            <span class="material-icons-outlined text-lg">save</span>
            <span>Save</span>
          </button>
          
          <div class="h-6 w-px bg-white/10 mx-2"></div>

          <button (click)="buildAsZip()" class="group flex items-center space-x-2 text-sm bg-slate-800 hover:bg-slate-700 text-slate-200 border border-white/10 px-4 py-2 rounded-lg transition-all shadow-lg active:scale-95">
             <span class="material-icons-outlined text-lg text-amber-400 group-hover:scale-110 transition-transform">folder_zip</span>
             <span>Output as ZIP</span>
          </button>

          <button (click)="publish()" [disabled]="isPublishing" class="flex items-center space-x-2 text-sm bg-indigo-600 hover:bg-indigo-500 disabled:bg-slate-700 disabled:opacity-50 text-white px-5 py-2 rounded-lg shadow-lg shadow-indigo-900/50 transition-all transform active:scale-95">
             <span class="material-icons-outlined text-lg">{{ isPublishing ? 'sync' : 'cloud_upload' }}</span>
             <span>{{ isPublishing ? 'Publishing...' : 'Publish to Cloud' }}</span>
          </button>
        </div>
      </div>

      <div class="flex flex-1 overflow-hidden relative">
        <!-- Floating Toolbox (Left) -->
        <div class="absolute top-6 left-6 flex flex-col space-y-2 z-10">
          <div class="bg-slate-800/90 backdrop-blur border border-white/10 p-2 rounded-xl shadow-xl flex flex-col space-y-4">
            <button class="p-2 rounded-lg hover:bg-blue-600/20 text-slate-400 hover:text-blue-400 transition-colors" title="Select">
               <span class="material-icons-outlined text-xl">near_me</span>
            </button>
            <button class="p-2 rounded-lg hover:bg-blue-600/20 text-slate-400 hover:text-blue-400 transition-colors" title="Entity">
               <span class="material-icons-outlined text-xl">rectangle</span>
            </button>
            <button class="p-2 rounded-lg hover:bg-blue-600/20 text-slate-400 hover:text-blue-400 transition-colors" title="Relation">
               <span class="material-icons-outlined text-xl">timeline</span>
            </button>
          </div>
        </div>

        <!-- Canvas (Center) -->
        <div #canvasContainer class="flex-1 bg-[#0B1120] relative overflow-hidden cursor-crosshair">
           <!-- Grid Background handled by CSS -->
           <div id="konva-holder" class="absolute inset-0"></div>
           
           <div class="absolute bottom-6 left-6 text-xs text-slate-600 bg-slate-900/50 px-2 py-1 rounded border border-white/5">
              Canvas Ready
           </div>
        </div>

        <!-- Property Panel (Right) -->
        <aside class="w-80 bg-slate-900/95 backdrop-blur-xl border-l border-white/10 flex flex-col shadow-2xl z-20">
          <div class="h-14 border-b border-white/10 flex items-center px-6 bg-white/5">
            <span class="material-icons-outlined text-slate-400 mr-2">tune</span>
            <span class="font-semibold text-sm tracking-wide">Inspector</span>
          </div>

          <div class="flex-1 overflow-y-auto p-6 scrollbar-thin scrollbar-thumb-slate-700 scrollbar-track-transparent">
            
            <div *ngIf="!selectedNode" class="flex flex-col items-center justify-center h-full text-slate-500 opacity-60">
               <div class="w-16 h-16 rounded-full bg-white/5 flex items-center justify-center mb-4">
                 <span class="material-icons-outlined text-3xl">touch_app</span>
               </div>
               <p class="text-sm font-medium">Select an entity to edit properties</p>
            </div>

            <div *ngIf="selectedNode" class="space-y-8 animate-fadeIn">
              <!-- Identity -->
              <div class="space-y-1">
                <label class="text-[11px] uppercase tracking-wider font-bold text-slate-500">Entity Name</label>
                <input type="text" [(ngModel)]="selectedNode.name" class="w-full bg-black/20 border border-white/10 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-all placeholder-slate-600">
              </div>

              <!-- Fields -->
              <div class="space-y-3">
                <div class="flex items-center justify-between">
                   <label class="text-[11px] uppercase tracking-wider font-bold text-slate-500">Fields</label>
                   <button (click)="addField()" class="flex items-center space-x-1 text-[11px] bg-blue-500/10 text-blue-400 hover:bg-blue-500/20 px-2 py-1 rounded transition-colors">
                      <span class="material-icons-outlined text-[14px]">add</span>
                      <span>Add</span>
                   </button>
                </div>
                
                <div class="space-y-3">
                   <div *ngFor="let field of selectedNode.fields; let i = index" class="p-3 bg-black/20 rounded-lg border border-white/10 hover:border-white/20 transition-colors group">
                      
                      <!-- Field Row 1 -->
                      <div class="flex items-center space-x-2 mb-2">
                        <input type="text" [(ngModel)]="field.name" class="flex-1 bg-transparent border-none text-sm font-medium text-white focus:outline-none placeholder-slate-600" placeholder="FieldName">
                        
                        <div class="flex items-center">
                            <button (click)="field.isRulesOpen = !field.isRulesOpen" [class.text-blue-400]="field.rules?.length > 0" class="text-slate-500 hover:text-blue-400 p-1 rounded hover:bg-white/5 transition-colors" title="Rules">
                              <span class="material-icons-outlined text-[16px]">gavel</span>
                            </button>
                            <button (click)="removeField(i)" class="text-slate-500 hover:text-red-400 p-1 rounded hover:bg-white/5 transition-colors opacity-0 group-hover:opacity-100">
                              <span class="material-icons-outlined text-[16px]">close</span>
                            </button>
                        </div>
                      </div>
                      
                      <!-- Field Row 2 -->
                      <div class="flex items-center space-x-2">
                        <select [(ngModel)]="field.type" class="flex-1 bg-slate-800 text-[11px] rounded border border-white/5 px-2 py-1 focus:border-blue-500 outline-none text-slate-300">
                          <option value="string">String</option>
                          <option value="int">Integer</option>
                          <option value="guid">Guid</option>
                          <option value="datetime">DateTime</option>
                          <option value="decimal">Decimal</option>
                          <option value="bool">Boolean</option>
                        </select>
                        <label class="flex items-center space-x-1.5 cursor-pointer bg-slate-800 px-2 py-1 rounded border border-white/5 hover:bg-slate-700">
                          <input type="checkbox" [(ngModel)]="field.isRequired" class="rounded border-slate-600 bg-slate-900 text-blue-500 focus:ring-0 w-3 h-3">
                          <span class="text-[10px] text-slate-400 font-medium">REQ</span>
                        </label>
                      </div>

                      <!-- Validation Rules Panel -->
                      <div *ngIf="field.isRulesOpen" class="mt-3 pt-3 border-t border-white/5 animate-slideDown">
                          <div class="flex items-center justify-between mb-2">
                               <span class="text-[10px] text-slate-500 font-bold uppercase">Validations</span>
                               <button (click)="addRule(field)" class="text-[10px] text-green-400 hover:text-green-300 transition-colors">+ Add</button>
                          </div>
                          <div class="space-y-2">
                              <div *ngFor="let rule of field.rules; let ri = index" class="bg-black/40 p-2 rounded border border-white/5">
                                  <div class="flex items-center justify-between mb-1">
                                      <select [(ngModel)]="rule.type" class="bg-transparent text-[11px] font-mono text-blue-300 border-none px-0 py-0 w-20 focus:ring-0">
                                          <option value="Regex">Regex</option>
                                          <option value="Range">Range</option>
                                          <option value="Email">Email</option>
                                          <option value="Phone">Phone</option>
                                      </select>
                                      <button (click)="removeRule(field, ri)" class="text-slate-600 hover:text-red-400">
                                          <span class="material-icons-outlined text-[14px]">remove_circle</span>
                                      </button>
                                  </div>
                                  <div *ngIf="rule.type === 'Regex' || rule.type === 'Range'" class="mb-1">
                                      <input type="text" [(ngModel)]="rule.value" placeholder="Value..." class="w-full bg-slate-800/50 text-[11px] rounded border border-white/5 px-2 py-1 text-slate-300">
                                  </div>
                                  <input type="text" [(ngModel)]="rule.errorMessage" placeholder="Error msg..." class="w-full bg-transparent text-[11px] border-b border-white/5 px-0 py-0.5 text-slate-500 italic focus:border-slate-500 focus:ring-0 placeholder-slate-700">
                              </div>
                          </div>
                      </div>

                   </div>
                </div>
              </div>

              <!-- Workflow Triggers -->
              <div class="space-y-3 pb-6 border-b border-white/5">
                <label class="text-[11px] uppercase tracking-wider font-bold text-slate-500">Workflow Triggers</label>
                <div class="grid grid-cols-1 gap-2">
                   <div class="flex items-center justify-between p-2 bg-slate-800/50 rounded-lg border border-white/5">
                      <span class="text-[11px] text-slate-300">On Created</span>
                      <input type="checkbox" [(ngModel)]="selectedNode.events.onCreate" class="rounded bg-slate-900 border-white/10 text-blue-600 focus:ring-0">
                   </div>
                   <div class="flex items-center justify-between p-2 bg-slate-800/50 rounded-lg border border-white/5">
                      <span class="text-[11px] text-slate-300">On Updated</span>
                      <input type="checkbox" [(ngModel)]="selectedNode.events.onUpdate" class="rounded bg-slate-900 border-white/10 text-blue-600 focus:ring-0">
                   </div>
                   <div class="flex items-center justify-between p-2 bg-slate-800/50 rounded-lg border border-white/5">
                      <span class="text-[11px] text-slate-300">On Deleted</span>
                      <input type="checkbox" [(ngModel)]="selectedNode.events.onDelete" class="rounded bg-slate-900 border-white/10 text-blue-600 focus:ring-0">
                   </div>
                </div>
              </div>

              <!-- Relationships -->
              <div class="space-y-3">
                <div class="flex items-center justify-between">
                   <label class="text-[11px] uppercase tracking-wider font-bold text-slate-500">Relations</label>
                   <button (click)="addRelation()" class="flex items-center space-x-1 text-[11px] bg-indigo-500/10 text-indigo-400 hover:bg-indigo-500/20 px-2 py-1 rounded transition-colors">
                      <span class="material-icons-outlined text-[14px]">add</span>
                      <span>Link</span>
                   </button>
                </div>

                <div class="space-y-2">
                   <div *ngFor="let rel of selectedNode.relations; let i = index" class="p-3 bg-indigo-500/5 rounded-lg border border-indigo-500/10 hover:border-indigo-500/30 transition-colors group">
                      <div class="flex items-center justify-between mb-2">
                        <div class="flex items-center space-x-1 text-xs text-indigo-300">
                            <span class="material-icons-outlined text-[14px] rotate-90">alt_route</span>
                            <span>To:</span>
                        </div>
                        <button (click)="removeRelation(i)" class="text-slate-500 hover:text-red-400 opacity-0 group-hover:opacity-100 transition-opacity">
                            <span class="material-icons-outlined text-[14px]">close</span>
                        </button>
                      </div>
                      
                      <select [(ngModel)]="rel.targetEntity" class="w-full bg-slate-800 text-xs rounded border border-white/5 px-2 py-1.5 focus:border-indigo-500 outline-none text-white mb-2">
                         <option value="" disabled selected>Select Target</option>
                         <option *ngFor="let target of entities" [value]="target.name">{{ target.name }}</option>
                      </select>

                      <div class="grid grid-cols-2 gap-2">
                          <select [ngModel]="rel.type" (ngModelChange)="rel.type = +$event" class="bg-slate-800 text-[10px] rounded border border-white/5 px-1 py-1 text-indigo-200 focus:border-indigo-500 outline-none">
                            <option [value]="0">1 : N</option>
                            <option [value]="1">N : 1</option>
                            <option [value]="2">M : N</option>
                          </select>
                          <input type="text" [(ngModel)]="rel.navPropName" placeholder="PropName" class="bg-slate-800 text-[10px] rounded border border-white/5 px-2 py-1 text-white focus:border-indigo-500 outline-none">
                      </div>
                   </div>
                </div>
              </div>

            </div>
          </div>
        </aside>
      </div>
    </div>
  `,
  styles: [`
    #konva-holder {
      background-image: 
        radial-gradient(circle, #1e293b 1px, transparent 1px);
      background-size: 30px 30px;
    }
  `],
})
export class EntityDesigner implements AfterViewInit, OnDestroy {
  @ViewChild('canvasContainer') canvasContainer!: ElementRef;

  readonly projectId: string | null = null;
  stage!: Konva.Stage;
  layer!: Konva.Layer;
  entities: any[] = [];
  selectedNode: any = null;
  resizeHandler = this.onResize.bind(this);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: ApiService
  ) {
    this.projectId = this.route.snapshot.paramMap.get('projectId');
  }

  ngAfterViewInit() {
    setTimeout(() => {
      const container = this.canvasContainer.nativeElement;
      const width = container.offsetWidth || 800;
      const height = container.offsetHeight || 600;

      this.stage = new Konva.Stage({
        container: 'konva-holder',
        width: width,
        height: height,
        draggable: true
      });

      this.layer = new Konva.Layer();
      this.stage.add(this.layer);

      globalThis.addEventListener('resize', this.resizeHandler);
      this.loadEntities();
    }, 100);
  }

  loadEntities() {
    if (!this.projectId) return;
    this.api.getEntities(this.projectId).subscribe({
      next: (artifacts) => {
        this.entities = artifacts.map(art => {
          try {
            const meta = JSON.parse(art.content);
            meta.id = art.id; // Keep artifact ID
            if (!meta.relations) meta.relations = []; // Ensure relations exist
            if (!meta.events) meta.events = { onCreate: true, onUpdate: true, onDelete: true };
            return meta;
          } catch (e) { return null; }
        }).filter(x => x !== null);

        this.layer.destroyChildren();
        this.entities.forEach(meta => this.renderEntity(meta));
      }
    });
  }

  onResize() {
    if (!this.stage || !this.canvasContainer) return;
    const container = this.canvasContainer.nativeElement;
    this.stage.width(container.offsetWidth);
    this.stage.height(container.offsetHeight);
  }

  addEntity() {
    const newMetadata = {
      name: 'NewEntity' + Math.floor(Math.random() * 100),
      fields: [
        { name: 'Id', type: 'guid', isRequired: true },
        { name: 'CreatedAt', type: 'datetime', isRequired: false }
      ],
      relations: [],
      events: { onCreate: true, onUpdate: true, onDelete: true },
      x: 100,
      y: 100
    };
    this.entities.push(newMetadata);
    this.renderEntity(newMetadata);
    this.selectedNode = newMetadata;
  }

  renderEntity(metadata: any) {
    const entityNode = new Konva.Group({
      x: metadata.x || 100,
      y: metadata.y || 100,
      draggable: true
    });

    const rect = new Konva.Rect({
      width: 180,
      height: 120,
      fill: '#1e293b',
      stroke: '#3b82f6',
      strokeWidth: 2,
      cornerRadius: 12,
      shadowBlur: 10,
      shadowColor: 'black',
      shadowOpacity: 0.3
    });

    const title = new Konva.Text({
      text: metadata.name,
      fontSize: 14,
      fontFamily: 'Inter, sans-serif',
      fill: 'white',
      width: 180,
      padding: 15,
      align: 'center',
      fontStyle: 'bold'
    });

    entityNode.add(rect);
    entityNode.add(title);

    entityNode.on('click', () => {
      this.selectedNode = metadata;
    });

    entityNode.on('dragend', () => {
      metadata.x = entityNode.x();
      metadata.y = entityNode.y();
    });

    // Update text if name changes
    const intervalId = setInterval(() => {
      if (title.text() !== metadata.name) {
        title.text(metadata.name);
        this.layer.batchDraw();
      }
    }, 500);

    // Store interval to clear it later if needed, though this simple app doesn't re-render often
    (entityNode as any).syncInterval = intervalId;

    this.layer.add(entityNode);
    this.layer.batchDraw();
  }

  addField() {
    if (this.selectedNode) {
      if (!this.selectedNode.fields) this.selectedNode.fields = [];
      this.selectedNode.fields.push({ name: 'NewField', type: 'string', isRequired: false });
    }
  }

  removeField(index: number) {
    if (this.selectedNode) {
      this.selectedNode.fields.splice(index, 1);
    }
  }

  addRelation() {
    if (this.selectedNode) {
      if (!this.selectedNode.relations) this.selectedNode.relations = [];
      this.selectedNode.relations.push({
        targetEntity: '',
        type: 1, // ManyToOne default
        navPropName: 'NewRelation',
        foreignKeyName: ''
      });
    }
  }

  removeRelation(index: number) {
    if (this.selectedNode?.relations) {
      this.selectedNode.relations.splice(index, 1);
    }
  }

  addRule(field: any) {
    if (!field.rules) field.rules = [];
    field.rules.push({
      type: 'Regex',
      value: '',
      errorMessage: 'Invalid value'
    });
  }

  removeRule(field: any, index: number) {
    if (field.rules) {
      field.rules.splice(index, 1);
    }
  }

  save() {
    if (!this.projectId || !this.selectedNode) {
      alert('Select an entity to save');
      return;
    }

    this.api.createEntity(this.projectId, this.selectedNode).subscribe({
      next: (res) => alert('Saved successfully!'),
      error: (err) => alert('Error saving: ' + (err.error?.message || err.message))
    });
  }

  isPublishing = false;

  buildAsZip() {
    if (!this.projectId) return;
    this.api.buildProject(this.projectId).subscribe({
      next: (blob) => {
        const url = globalThis.URL.createObjectURL(blob);
        const a = globalThis.document.createElement('a');
        a.href = url;
        const projectName = this.entities[0]?.namespace?.split('.')[0] || 'Project';
        a.download = `${projectName}_Standalone_Export.zip`;
        a.click();
        globalThis.URL.revokeObjectURL(url);
      },
      error: (err) => alert('Build failed: ' + err.message)
    });
  }

  publish() {
    if (!this.projectId) return;
    this.isPublishing = true;
    this.api.publishProject(this.projectId).subscribe({
      next: (res) => {
        this.isPublishing = false;
        alert('Project successfully published to the shared environment!');
      },
      error: (err) => {
        this.isPublishing = false;
        alert('Publication failed: ' + err.message);
      }
    });
  }

  ngOnDestroy() {
    globalThis.removeEventListener('resize', this.resizeHandler);
  }
}
