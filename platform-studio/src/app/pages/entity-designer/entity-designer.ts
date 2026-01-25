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
    <div class="flex flex-col h-[calc(100vh-64px)] bg-[#0B1120] text-slate-200 overflow-hidden font-sans">
      <!-- Toolbar -->
      <div class="h-14 border-b border-white/5 flex items-center justify-between px-6 bg-slate-900/80 backdrop-blur-xl z-20 shadow-2xl">
        <div class="flex items-center space-x-6">
          <div class="flex items-center space-x-3">
             <div class="p-2 bg-blue-500/10 rounded-xl shadow-inner shadow-blue-500/20">
                <span class="material-icons-outlined text-blue-400 text-lg text-glow">schema</span>
             </div>
             <div>
                <h2 class="text-sm font-black text-white tracking-widest uppercase">Entity Architect</h2>
                <div class="text-[9px] text-slate-500 font-mono tracking-tighter opacity-60">v4.1.0 // {{ projectId | slice:0:8 }}</div>
             </div>
          </div>
          <div class="w-px h-8 bg-white/10 mx-2"></div>
          <div class="flex items-center space-x-1 p-1 bg-black/20 rounded-xl box-glow-blue">
            <button (click)="addEntity()" class="group flex items-center space-x-2 text-xs hover:bg-blue-600/20 text-slate-400 hover:text-blue-400 px-4 py-1.5 rounded-lg transition-all active:scale-95">
              <span class="material-icons-outlined text-lg">add_box</span>
              <span class="font-bold">New Entity</span>
            </button>
            <button [routerLink]="['/projects', projectId, 'security']" class="group flex items-center space-x-2 text-xs hover:bg-purple-600/20 text-slate-400 hover:text-purple-400 px-4 py-1.5 rounded-lg transition-all">
              <span class="material-icons-outlined text-lg">admin_panel_settings</span>
              <span class="font-bold">Security</span>
            </button>
            <button [routerLink]="['/projects', projectId, 'pages']" class="group flex items-center space-x-2 text-xs hover:bg-blue-600/20 text-slate-400 hover:text-blue-400 px-4 py-1.5 rounded-lg transition-all">
              <span class="material-icons-outlined text-lg text-blue-500">dashboard_customize</span>
              <span class="font-bold">Pages</span>
            </button>
            <button [routerLink]="['/projects', projectId, 'enums']" class="group flex items-center space-x-2 text-xs hover:bg-amber-600/20 text-slate-400 hover:text-amber-400 px-4 py-1.5 rounded-lg transition-all">
              <span class="material-icons-outlined text-lg text-amber-500">list_alt</span>
              <span class="font-bold">Enums</span>
            </button>
            <button [routerLink]="['/projects', projectId, 'workflows']" class="group flex items-center space-x-2 text-xs hover:bg-green-600/20 text-slate-400 hover:text-green-400 px-4 py-1.5 rounded-lg transition-all">
              <span class="material-icons-outlined text-lg">account_tree</span>
              <span class="font-bold">Workflows</span>
            </button>
          </div>
        </div>
        <div class="flex items-center space-x-3">
          <button (click)="save()" class="group flex items-center space-x-2 text-xs text-slate-400 hover:text-white px-4 py-2 rounded-lg transition-all hover:bg-white/5 active:scale-95">
            <span class="material-icons-outlined text-lg group-hover:animate-pulse">save</span>
            <span class="font-bold uppercase tracking-widest text-[10px]">Commit</span>
          </button>
          
          <div class="h-6 w-px bg-white/10 mx-2"></div>

          <button (click)="buildAsZip()" class="group flex items-center space-x-2 text-xs bg-slate-800 hover:bg-slate-700 text-slate-200 border border-white/10 px-4 py-2 rounded-xl transition-all shadow-xl active:scale-95">
             <span class="material-icons-outlined text-lg text-amber-400 group-hover:rotate-12 transition-transform">folder_zip</span>
             <span class="font-bold tracking-tight">Export Code</span>
          </button>

          <button (click)="publish()" [disabled]="isPublishing" class="group flex items-center space-x-2 text-xs bg-blue-600 hover:bg-blue-500 disabled:bg-slate-700 text-white px-6 py-2 rounded-xl shadow-lg shadow-blue-900/50 transition-all active:scale-95">
             <span class="material-icons-outlined text-lg group-hover:scale-110 transition-transform">{{ isPublishing ? 'sync' : 'bolt' }}</span>
             <span class="font-black uppercase tracking-widest italic">{{ isPublishing ? 'Deploying...' : 'Build & Deploy' }}</span>
          </button>
        </div>
      </div>

      <div class="flex flex-1 overflow-hidden relative">
        <!-- Floating Toolbox (Left) -->
        <div class="absolute top-6 left-6 flex flex-col space-y-2 z-10 animate-fadeIn">
          <div class="glass-dark p-2 rounded-2xl shadow-2xl flex flex-col space-y-4 border border-white/10">
            <button class="p-3 rounded-xl hover:bg-blue-600/30 text-blue-400/60 hover:text-blue-400 transition-all active:scale-90" title="Select Tool">
               <span class="material-icons-outlined text-xl">near_me</span>
            </button>
            <button (click)="addEntity()" class="p-3 rounded-xl hover:bg-emerald-600/30 text-emerald-400/60 hover:text-emerald-400 transition-all active:scale-90" title="Entity Tool">
               <span class="material-icons-outlined text-xl">rectangle</span>
            </button>
            <button class="p-3 rounded-xl hover:bg-purple-600/30 text-purple-400/60 hover:text-purple-400 transition-all active:scale-90" title="Relation Tool">
               <span class="material-icons-outlined text-xl">timeline</span>
            </button>
            <div class="h-px bg-white/5 mx-2"></div>
            <button class="p-3 rounded-xl hover:bg-white/5 text-slate-600 transition-all" title="Pan Mode">
               <span class="material-icons-outlined text-xl">pan_tool</span>
            </button>
          </div>
        </div>

        <!-- Canvas (Center) -->
        <div #canvasContainer class="flex-1 bg-[#0B1120] relative overflow-hidden cursor-crosshair">
           <div id="konva-holder" class="absolute inset-0"></div>
           
           <!-- Zoom Controls -->
           <div class="absolute bottom-6 right-80 flex space-x-1 glass p-1 rounded-full border border-white/5">
              <button class="w-8 h-8 rounded-full hover:bg-white/10 flex items-center justify-center text-slate-400 transition-all"><span class="material-icons-outlined text-sm">remove</span></button>
              <div class="px-2 flex items-center text-[10px] font-mono text-slate-500 tracking-tighter">100%</div>
              <button class="w-8 h-8 rounded-full hover:bg-white/10 flex items-center justify-center text-slate-400 transition-all"><span class="material-icons-outlined text-sm">add</span></button>
           </div>

           <div class="absolute bottom-6 left-6 text-[9px] text-slate-500 font-black uppercase tracking-[0.2em] opacity-40">
              Platform Canvas // Accelerated Graphics Ready
           </div>
        </div>

        <!-- Property Panel (Right) -->
        <aside class="w-80 glass-dark border-l border-white/5 flex flex-col shadow-2xl z-20 overflow-hidden">
          <div class="h-14 border-b border-white/5 flex items-center px-6 bg-blue-600/5 backdrop-blur-md">
            <span class="material-icons-outlined text-blue-400 mr-2 text-lg text-glow animate-pulse">tune</span>
            <span class="font-black text-[10px] uppercase tracking-widest text-slate-300">Property Inspector</span>
          </div>

          <div class="flex-1 overflow-y-auto p-6 scrollbar-thin scrollbar-thumb-slate-700 scrollbar-track-transparent">
            
            <div *ngIf="!selectedNode" class="flex flex-col items-center justify-center h-full text-slate-600 opacity-40">
               <div class="w-20 h-20 rounded-full border-2 border-dashed border-white/5 flex items-center justify-center mb-6">
                 <span class="material-icons-outlined text-4xl">fingerprint</span>
               </div>
               <p class="text-[10px] font-black uppercase tracking-widest text-center">Select an entity<br>on the canvas to proceed</p>
            </div>

            <div *ngIf="selectedNode" class="space-y-10 animate-fadeIn">
              <!-- Identity -->
              <div class="space-y-3">
                <label class="text-[10px] uppercase tracking-[0.2em] font-black text-blue-500/60 block">Core Identification</label>
                <div class="relative group">
                   <input type="text" [(ngModel)]="selectedNode.name" class="w-full bg-black/40 border border-white/10 rounded-xl px-4 py-3 text-sm text-white font-black focus:outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/10 transition-all placeholder-slate-700 shadow-inner">
                   <div class="absolute right-3 top-3 text-slate-700 group-hover:text-blue-500 transition-colors">
                      <span class="material-icons-outlined text-lg">edit</span>
                   </div>
                </div>
              </div>

              <!-- Fields -->
              <div class="space-y-4">
                <div class="flex items-center justify-between pb-2 border-b border-white/5">
                   <label class="text-[10px] uppercase tracking-[0.2em] font-black text-slate-500">Atomic Properties</label>
                   <button (click)="addField()" class="flex items-center space-x-1 text-[9px] font-black uppercase tracking-widest bg-blue-500/10 text-blue-400 hover:bg-blue-500 hover:text-white px-3 py-1.5 rounded-full transition-all active:scale-95 shadow-lg shadow-blue-900/20">
                      <span class="material-icons-outlined text-xs">add</span>
                      <span>Inject Field</span>
                   </button>
                </div>
                
                <div class="space-y-3">
                   <div *ngFor="let field of selectedNode.fields; let i = index" class="p-4 bg-white/[0.02] rounded-2xl border border-white/5 hover:border-blue-500/30 transition-all group relative overflow-hidden">
                      <div class="absolute top-0 left-0 w-1 h-full bg-blue-500/20 group-hover:bg-blue-500 transition-colors"></div>
                      
                      <!-- Field Row 1 -->
                      <div class="flex items-center justify-between mb-3">
                        <input type="text" [(ngModel)]="field.name" class="flex-1 bg-transparent border-none text-sm font-black text-white focus:outline-none placeholder-slate-700 uppercase tracking-tight" placeholder="PROPERTY_NAME">
                        
                        <div class="flex items-center space-x-1">
                            <button (click)="field.isRulesOpen = !field.isRulesOpen" [class.text-blue-400]="field.rules?.length > 0" class="text-slate-600 hover:text-blue-400 p-1.5 rounded-lg hover:bg-white/5 transition-all" title="Validation Rules">
                              <span class="material-icons-outlined text-base">verified</span>
                            </button>
                            <button (click)="removeField(i)" class="text-slate-600 hover:text-red-500 p-1.5 rounded-lg hover:bg-red-500/10 transition-all opacity-0 group-hover:opacity-100">
                              <span class="material-icons-outlined text-base">delete_sweep</span>
                            </button>
                        </div>
                      </div>
                      
                      <!-- Field Row 2 -->
                      <div class="flex items-center space-x-2">
                        <select [(ngModel)]="field.type" class="flex-1 bg-black/40 text-[10px] font-bold rounded-lg border border-white/5 px-3 py-2 focus:border-blue-500 outline-none text-slate-400 uppercase tracking-widest appearance-none">
                          <option value="string">String</option>
                          <option value="int">Integer</option>
                          <option value="guid">Guid</option>
                          <option value="datetime">DateTime</option>
                          <option value="decimal">Decimal</option>
                          <option value="bool">Boolean</option>
                        </select>
                        <button (click)="field.isRequired = !field.isRequired" 
                                [class.bg-blue-600]="field.isRequired" 
                                [class.text-white]="field.isRequired"
                                [class.bg-white/5]="!field.isRequired"
                                [class.text-slate-600]="!field.isRequired"
                                class="px-3 py-2 rounded-lg text-[9px] font-black uppercase tracking-tighter transition-all border border-white/5">
                           Mandatory
                        </button>
                      </div>

                      <!-- Validation Rules Panel -->
                      <div *ngIf="field.isRulesOpen" class="mt-4 pt-4 border-t border-white/5 animate-slideDown space-y-4">
                          <div class="flex items-center justify-between">
                               <span class="text-[9px] text-blue-400 font-bold uppercase tracking-widest italic flex items-center"><span class="material-icons-outlined text-xs mr-1 text-[10px]">shield</span> Guards</span>
                               <button (click)="addRule(field)" class="text-[9px] font-black text-emerald-400 hover:text-emerald-300 transition-colors uppercase tracking-widest">+ New Guard</button>
                          </div>
                          <div class="space-y-2">
                              <div *ngFor="let rule of field.rules; let ri = index" class="bg-black/60 p-3 rounded-xl border border-white/5 shadow-inner">
                                  <div class="flex items-center justify-between mb-2 pb-2 border-b border-white/5">
                                      <select [(ngModel)]="rule.type" class="bg-transparent text-[9px] font-black text-blue-300 border-none px-0 py-0 uppercase focus:ring-0">
                                          <option value="Regex">Pattern</option>
                                          <option value="Range">Limit</option>
                                          <option value="Email">Mail</option>
                                          <option value="Phone">Tel</option>
                                      </select>
                                      <button (click)="removeRule(field, ri)" class="text-slate-600 hover:text-red-400">
                                          <span class="material-icons-outlined text-base">remove_circle_outline</span>
                                      </button>
                                  </div>
                                  <div *ngIf="rule.type === 'Regex' || rule.type === 'Range'" class="mb-2">
                                      <input type="text" [(ngModel)]="rule.value" placeholder="Definition..." class="w-full bg-black/40 text-[10px] rounded-lg border border-white/5 px-3 py-2 text-slate-300 font-mono">
                                  </div>
                                  <input type="text" [(ngModel)]="rule.errorMessage" placeholder="Fault message..." class="w-full bg-transparent text-[9px] px-0 py-1 text-slate-500 italic font-medium focus:text-slate-300 focus:outline-none transition-colors border-none">
                              </div>
                          </div>
                      </div>

                   </div>
                </div>
              </div>

              <!-- Relationships -->
              <div class="space-y-4">
                <div class="flex items-center justify-between pb-2 border-b border-white/5">
                   <label class="text-[10px] uppercase tracking-[0.2em] font-black text-purple-500/60">Semantic Links</label>
                   <button (click)="addRelation()" class="flex items-center space-x-1 text-[9px] font-black uppercase tracking-widest bg-purple-500/10 text-purple-400 hover:bg-purple-600 hover:text-white px-3 py-1.5 rounded-full transition-all active:scale-95 shadow-lg shadow-purple-900/20">
                      <span class="material-icons-outlined text-xs">link</span>
                      <span>Attach Link</span>
                   </button>
                </div>

                <div class="space-y-3">
                   <div *ngFor="let rel of selectedNode.relations; let i = index" class="p-4 bg-purple-500/[0.02] rounded-2xl border border-purple-500/10 hover:border-purple-500 transition-all group relative overflow-hidden">
                      <div class="absolute top-0 left-0 w-1 h-full bg-purple-500/20 group-hover:bg-purple-500 transition-colors"></div>
                      
                      <div class="flex items-center justify-between mb-3">
                        <div class="flex items-center space-x-1 text-[10px] font-black uppercase tracking-widest text-purple-400">
                            <span class="material-icons-outlined text-base rotate-90 scale-x-[-1]">shortcut</span>
                            <span>Direct To</span>
                        </div>
                        <button (click)="removeRelation(i)" class="text-slate-600 hover:text-red-400 opacity-0 group-hover:opacity-100 transition-all p-1">
                            <span class="material-icons-outlined text-base">close</span>
                        </button>
                      </div>
                      
                      <select [(ngModel)]="rel.targetEntity" class="w-full bg-black/40 text-[10px] font-black uppercase tracking-widest rounded-lg border border-white/5 px-3 py-2.5 focus:border-purple-500 focus:ring-4 focus:ring-purple-500/10 outline-none text-white mb-3 appearance-none">
                         <option value="" disabled selected>Target Domain</option>
                         <option *ngFor="let target of entities" [value]="target.name">{{ target.name | uppercase }}</option>
                      </select>

                      <div class="grid grid-cols-2 gap-3">
                          <select [ngModel]="rel.type" (ngModelChange)="rel.type = +$event" class="bg-black/60 text-[9px] font-black rounded-lg border border-white/10 px-2 py-2 text-purple-300 focus:ring-0 outline-none appearance-none uppercase tracking-tighter">
                            <option [value]="0">ONE TO MANY</option>
                            <option [value]="1">MANY TO ONE</option>
                            <option [value]="2">MANY TO MANY</option>
                          </select>
                          <input type="text" [(ngModel)]="rel.navPropName" placeholder="ALIAS" class="bg-black/60 text-[9px] font-mono rounded-lg border border-white/10 px-3 py-2 text-white focus:border-purple-500 outline-none uppercase placeholder-slate-700">
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
        radial-gradient(circle, rgba(255,255,255,0.05) 1px, transparent 1px);
      background-size: 32px 32px;
    }
    .text-glow {
      filter: drop-shadow(0 0 8px rgba(59, 130, 246, 0.4));
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
  isPublishing = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: ApiService
  ) {
    this.projectId = this.route.snapshot.paramMap.get('projectId');
  }

  ngAfterViewInit() {
    setTimeout(() => {
      if (!this.canvasContainer) return;
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
            if (!meta.relations) meta.relations = [];
            if (!meta.events) meta.events = { onCreate: true, onUpdate: true, onDelete: true };
            return meta;
          } catch (e) {
            console.error('Failed to parse entity artifact', e);
            return null;
          }
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
      name: 'NEW_ENTITY_' + Math.floor(Math.random() * 100),
      fields: [
        { name: 'ID', type: 'guid', isRequired: true },
        { name: 'CREATED_AT', type: 'datetime', isRequired: false }
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
      width: 200,
      height: 70,
      fill: '#0f172a',
      stroke: '#3b82f6',
      strokeWidth: 2,
      cornerRadius: 16,
      shadowBlur: 20,
      shadowColor: '#3b82f6',
      shadowOpacity: 0.1
    });

    const header = new Konva.Rect({
      width: 200,
      height: 30,
      fill: '#3b82f6',
      opacity: 0.1,
      cornerRadius: [16, 16, 0, 0]
    });

    const title = new Konva.Text({
      text: metadata.name.toUpperCase(),
      fontSize: 10,
      fontFamily: 'Inter, sans-serif',
      fill: 'white',
      width: 200,
      padding: 12,
      align: 'center',
      fontStyle: 'bold',
      letterSpacing: 2
    });

    const countText = new Konva.Text({
      text: (metadata.fields?.length || 0) + ' FIELDS',
      fontSize: 8,
      fontFamily: 'JetBrains Mono, monospace',
      fill: '#475569',
      width: 200,
      y: 40,
      align: 'center',
      fontStyle: 'bold'
    });

    entityNode.add(rect);
    entityNode.add(header);
    entityNode.add(title);
    entityNode.add(countText);

    entityNode.on('mouseover', () => {
      rect.strokeWidth(3);
      rect.shadowOpacity(0.3);
      rect.shadowBlur(30);
      this.layer.batchDraw();
    });

    entityNode.on('mouseout', () => {
      rect.strokeWidth(2);
      rect.shadowOpacity(0.1);
      rect.shadowBlur(20);
      this.layer.batchDraw();
    });

    entityNode.on('click', () => {
      this.selectedNode = metadata;
    });

    entityNode.on('dragend', () => {
      metadata.x = entityNode.x();
      metadata.y = entityNode.y();
    });

    // Update visual sync
    const intervalId = setInterval(() => {
      if (title.text() !== metadata.name.toUpperCase()) {
        title.text(metadata.name.toUpperCase());
        countText.text((metadata.fields?.length || 0) + ' FIELDS');
        this.layer.batchDraw();
      }
    }, 500);

    (entityNode as any).syncInterval = intervalId;

    this.layer.add(entityNode);
    this.layer.batchDraw();
  }

  addField() {
    if (this.selectedNode) {
      if (!this.selectedNode.fields) this.selectedNode.fields = [];
      this.selectedNode.fields.push({ name: 'NEW_PROPERTY', type: 'string', isRequired: false });
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
        type: 1,
        navPropName: 'LINKED_OBJECT',
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
      errorMessage: 'Invalid property value'
    });
  }

  removeRule(field: any, index: number) {
    if (field.rules) {
      field.rules.splice(index, 1);
    }
  }

  save() {
    if (!this.projectId || !this.selectedNode) {
      alert('Select an architecture node to commit');
      return;
    }

    this.api.createEntity(this.projectId, this.selectedNode).subscribe({
      next: (res) => alert('Changes committed to metadata store.'),
      error: (err) => alert('Fault detected: ' + (err.error?.message || err.message))
    });
  }

  buildAsZip() {
    if (!this.projectId) return;
    this.api.buildProject(this.projectId).subscribe({
      next: (blob) => {
        const url = globalThis.URL.createObjectURL(blob);
        const a = globalThis.document.createElement('a');
        a.href = url;
        a.download = `Application_Source_Standard.zip`;
        a.click();
        globalThis.URL.revokeObjectURL(url);
      },
      error: (err) => alert('Build sequence failed: ' + err.message)
    });
  }

  publish() {
    if (!this.projectId) return;
    this.isPublishing = true;
    this.api.publishProject(this.projectId).subscribe({
      next: (res) => {
        this.isPublishing = false;
        alert('Application cluster updated successfully.');
      },
      error: (err) => {
        this.isPublishing = false;
        alert('Propagation failed: ' + err.message);
      }
    });
  }

  ngOnDestroy() {
    globalThis.removeEventListener('resize', this.resizeHandler);
    if (this.stage) this.stage.destroy();
  }
}
