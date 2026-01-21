import { Component, ElementRef, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import Konva from 'konva';
import { ApiService } from '../../services/api';

@Component({
  selector: 'app-entity-designer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="flex flex-col h-[calc(100vh-120px)]">
      <!-- Toolbar -->
      <div class="h-12 border-b border-slate-700/50 flex items-center justify-between px-4 bg-slate-900/50">
        <div class="flex items-center space-x-4">
          <button (click)="addEntity()" class="flex items-center space-x-2 text-sm bg-blue-600 hover:bg-blue-500 px-3 py-1.5 rounded-md transition-all">
            <span class="material-icons-outlined text-sm">add</span>
            <span>Add Entity</span>
          </button>
          <div class="w-px h-6 bg-slate-700 mx-2"></div>
          <span class="text-xs text-slate-500">Project ID: {{ projectId }}</span>
        </div>
        <div class="flex items-center space-x-2">
          <button (click)="save()" class="flex items-center space-x-2 text-sm border border-blue-500/50 text-blue-400 hover:bg-blue-500/10 px-3 py-1.5 rounded-md transition-all">
            <span class="material-icons-outlined text-sm">save</span>
            <span>Save Changes</span>
          </button>
          <button (click)="build()" class="flex items-center space-x-2 text-sm bg-indigo-600 hover:bg-indigo-500 px-3 py-1.5 rounded-md transition-all">
             <span class="material-icons-outlined text-sm">bolt</span>
             <span>Build</span>
          </button>
        </div>
      </div>

      <div class="flex flex-1 overflow-hidden">
        <!-- Toolbox (Left) -->
        <aside class="w-16 border-r border-slate-700/50 flex flex-col items-center py-4 space-y-6 bg-slate-900/30">
          <div class="cursor-pointer p-2 rounded-lg hover:bg-slate-800 text-blue-400" title="Entity">
            <span class="material-icons-outlined text-3xl">token</span>
          </div>
          <div class="cursor-pointer p-2 rounded-lg hover:bg-slate-800 text-slate-500" title="Relationship">
            <span class="material-icons-outlined text-3xl">rebase_edit</span>
          </div>
        </aside>

        <!-- Canvas (Center) -->
        <div #canvasContainer class="flex-1 bg-[slate-950] relative overflow-hidden">
           <div id="konva-holder" class="absolute inset-0"></div>
        </div>

        <!-- Property Panel (Right) -->
        <aside class="w-80 border-l border-slate-700/50 bg-slate-900/50 p-6 overflow-y-auto">
          <h3 class="text-lg font-bold mb-4 flex items-center space-x-2">
            <span class="material-icons-outlined text-slate-400">settings</span>
            <span>Properties</span>
          </h3>
          
          <div *ngIf="!selectedNode" class="flex flex-col items-center justify-center h-64 text-slate-600 border-2 border-dashed border-slate-800 rounded-xl">
             <span class="material-icons-outlined text-4xl mb-2">touch_app</span>
             <p class="text-sm">Select a node to edit</p>
          </div>

          <div *ngIf="selectedNode" class="space-y-6">
            <div>
              <label class="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">Entity Name</label>
              <input type="text" [(ngModel)]="selectedNode.name" class="w-full bg-slate-800 border border-slate-700 rounded-lg px-4 py-2 focus:outline-none focus:border-blue-500 transition-all">
            </div>

            <!-- Fields Section -->
            <!-- Fields Section -->
            <div>
              <div class="flex items-center justify-between mb-2">
                 <label class="block text-xs font-semibold text-slate-500 uppercase tracking-wider">Fields</label>
                 <button (click)="addField()" class="text-xs text-blue-400 hover:underline">+ Add Field</button>
              </div>
              <div class="space-y-3">
                 <div *ngFor="let field of selectedNode.fields; let i = index" class="p-3 bg-slate-800/50 rounded-xl border border-slate-700/30 group">
                    <!-- Field Header -->
                    <div class="flex items-center justify-between mb-2">
                      <input type="text" [(ngModel)]="field.name" class="bg-transparent border-none text-sm font-medium focus:outline-none focus:ring-1 focus:ring-blue-500 rounded p-1 w-2/3">
                      <div class="flex items-center space-x-2">
                          <button (click)="field.isRulesOpen = !field.isRulesOpen" [class.text-blue-400]="field.rules?.length > 0" class="text-slate-500 hover:text-blue-400 transition-colors" title="Validation Rules">
                            <span class="material-icons-outlined text-sm">gavel</span>
                          </button>
                          <button (click)="removeField(i)" class="text-slate-500 hover:text-red-400 transition-colors opacity-0 group-hover:opacity-100">
                            <span class="material-icons-outlined text-sm">delete</span>
                          </button>
                      </div>
                    </div>
                    
                    <!-- Field Properties -->
                    <div class="flex items-center space-x-2 mb-2">
                      <select [(ngModel)]="field.type" class="bg-slate-700 text-xs rounded border-none px-2 py-1 focus:ring-1 focus:ring-blue-500 outline-none">
                        <option value="string">String</option>
                        <option value="int">Integer</option>
                        <option value="guid">Guid</option>
                        <option value="datetime">DateTime</option>
                        <option value="decimal">Decimal</option>
                        <option value="bool">Boolean</option>
                      </select>
                      <label class="flex items-center space-x-1 cursor-pointer">
                        <input type="checkbox" [(ngModel)]="field.isRequired" class="rounded border-slate-600 bg-slate-800 text-blue-600 focus:ring-0">
                        <span class="text-[10px] text-slate-500 uppercase">Required</span>
                      </label>
                    </div>

                    <!-- Rules Section (Collapsible) -->
                    <div *ngIf="field.isRulesOpen" class="mt-3 pt-3 border-t border-slate-700/50">
                        <div class="flex items-center justify-between mb-2">
                             <label class="text-[10px] text-slate-500 uppercase">Validation Rules</label>
                             <button (click)="addRule(field)" class="text-[10px] text-green-400 hover:underline">+ Add Rule</button>
                        </div>
                        <div class="space-y-2">
                            <div *ngFor="let rule of field.rules; let ri = index" class="flex flex-col space-y-2 bg-slate-900/50 p-2 rounded border border-slate-700">
                                <div class="flex items-center justify-between">
                                    <select [(ngModel)]="rule.type" class="bg-slate-800 text-xs rounded border-none px-1 py-0.5 w-24">
                                        <option value="Regex">Regex</option>
                                        <option value="Range">Range</option>
                                        <option value="Email">Email</option>
                                        <option value="Phone">Phone</option>
                                    </select>
                                    <button (click)="removeRule(field, ri)" class="text-slate-600 hover:text-red-400">
                                        <span class="material-icons-outlined text-xs">close</span>
                                    </button>
                                </div>
                                <div *ngIf="rule.type === 'Regex' || rule.type === 'Range'" class="flex flex-col">
                                    <input type="text" [(ngModel)]="rule.value" placeholder="Value (e.g. ^[0-9]*$ or 1,100)" class="bg-slate-800 text-xs rounded border border-slate-700 px-2 py-1 w-full text-slate-300">
                                </div>
                                <input type="text" [(ngModel)]="rule.errorMessage" placeholder="Error Message" class="bg-slate-800 text-xs rounded border border-slate-700 px-2 py-1 w-full text-slate-400 italic">
                            </div>
                            <div *ngIf="!field.rules?.length" class="text-center py-2 text-xs text-slate-600 italic">
                                No rules defined
                            </div>
                        </div>
                    </div>
                 </div>
              </div>
            </div>

            <!-- Relationships Section -->
            <div>
              <div class="flex items-center justify-between mb-2">
                 <label class="block text-xs font-semibold text-slate-500 uppercase tracking-wider">Relationships</label>
                 <button (click)="addRelation()" class="text-xs text-indigo-400 hover:underline">+ Add Relation</button>
              </div>
              <div class="space-y-3">
                 <div *ngFor="let rel of selectedNode.relations; let i = index" class="p-3 bg-indigo-900/10 rounded-xl border border-indigo-500/20 group">
                    <div class="flex items-center justify-between mb-2">
                      <select [(ngModel)]="rel.targetEntity" class="bg-transparent border-none text-sm font-medium focus:outline-none focus:ring-1 focus:ring-indigo-500 rounded p-1 w-2/3">
                         <option *ngFor="let target of entities" [value]="target.name">{{ target.name }}</option>
                      </select>
                      <button (click)="removeRelation(i)" class="text-slate-500 hover:text-red-400 transition-colors opacity-0 group-hover:opacity-100">
                        <span class="material-icons-outlined text-sm">delete</span>
                      </button>
                    </div>
                    <div class="flex flex-col space-y-2">
                      <select [ngModel]="rel.type" (ngModelChange)="rel.type = +$event" class="bg-slate-700 text-xs rounded border-none px-2 py-1 focus:ring-1 focus:ring-indigo-500 outline-none w-full">
                        <option [value]="0">One to Many</option>
                        <option [value]="1">Many to One</option>
                        <option [value]="2">Many to Many</option>
                      </select>
                      <input type="text" [(ngModel)]="rel.navPropName" placeholder="Navigation Property" class="bg-slate-800 text-xs rounded border border-slate-700 px-2 py-1 w-full outline-none focus:border-indigo-500">
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

  build() {
    if (!this.projectId) return;
    this.api.buildProject(this.projectId).subscribe({
      next: (blob) => {
        const url = globalThis.URL.createObjectURL(blob);
        const a = globalThis.document.createElement('a');
        a.href = url;
        a.download = `Build_${this.projectId}.zip`;
        a.click();
        globalThis.URL.revokeObjectURL(url);
      },
      error: (err) => alert('Build failed: ' + err.message)
    });
  }

  ngOnDestroy() {
    globalThis.removeEventListener('resize', this.resizeHandler);
  }
}
