import { Component, ViewChild, ElementRef, AfterViewInit, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api';
import { FormsModule } from '@angular/forms';
import Konva from 'konva';
import { ProjectContextService } from '../../services/project-context';

interface WorkflowNode {
  id: string;
  type: string;
  x: number;
  y: number;
  label: string;
  color: string;
  config: any;
}

interface WorkflowConnection {
  fromId: string;
  toId: string;
}

interface WorkflowMetadata {
  id?: string;
  name: string;
  nodes: WorkflowNode[];
  connections: WorkflowConnection[];
}

@Component({
  selector: 'app-workflow-designer',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  styles: [`
    :host { display: block; }
    .studio-container { display: flex; flex-direction: column; height: calc(100vh - 64px); background: #0b0f1a; color: #e2e8f0; font-family: 'Inter', sans-serif; overflow: hidden; }

    /* ── Toolbar ──────────────────────────────── */
    .toolbar { height: 56px; border-bottom: 1px solid rgba(255,255,255,0.05); display: flex; align-items: center; justify-content: space-between; padding: 0 1.5rem; background: rgba(15,23,42,0.8); backdrop-filter: blur(16px); z-index: 20; }
    .toolbar-left { display: flex; align-items: center; gap: 1rem; }
    .back-btn { display: flex; align-items: center; justify-content: center; width: 32px; height: 32px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.05); background: transparent; color: #64748b; cursor: pointer; transition: all 0.2s; }
    .back-btn:hover { background: rgba(255,255,255,0.05); color: #fff; }
    
    .brand { display: flex; align-items: center; gap: 0.75rem; }
    .brand-icon-wrap { width: 36px; height: 36px; background: rgba(16,185,129,0.1); border-radius: 10px; display: flex; align-items: center; justify-content: center; }
    .brand-icon { color: #10b981; font-size: 1.25rem; }
    .brand-title { font-size: 0.85rem; font-weight: 800; color: #fff; margin: 0; }
    .brand-sub { font-size: 9px; color: #475569; font-family: 'JetBrains Mono', monospace; text-transform: uppercase; letter-spacing: 0.1em; }

    .toolbar-right { display: flex; align-items: center; gap: 0.75rem; }
    .btn-ghost { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 1rem; border-radius: 10px; border: 1px solid transparent; background: transparent; color: #94a3b8; font-size: 0.75rem; font-weight: 700; cursor: pointer; transition: all 0.2s; }
    .btn-ghost:hover { background: rgba(255,255,255,0.05); color: #fff; }
    
    .btn-primary { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 1.25rem; border-radius: 10px; border: none; background: #2563eb; color: #fff; font-size: 0.75rem; font-weight: 800; cursor: pointer; transition: all 0.2s; box-shadow: 0 4px 12px rgba(37,99,235,0.2); }
    .btn-primary:hover { background: #3b82f6; transform: translateY(-1px); }
    
    .btn-success { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 1.25rem; border-radius: 10px; border: none; background: #10b981; color: #fff; font-size: 0.75rem; font-weight: 800; cursor: pointer; transition: all 0.2s; box-shadow: 0 4px 12px rgba(16,185,129,0.2); }
    .btn-success:hover { background: #34d399; transform: translateY(-1px); }

    /* ── Main Layout ───────────────────────────── */
    .viewport { display: flex; flex: 1; overflow: hidden; position: relative; }
    
    .sidebar { width: 260px; background: rgba(15,23,42,0.4); border-right: 1px solid rgba(255,255,255,0.05); display: flex; flex-direction: column; z-index: 10; backdrop-filter: blur(10px); padding: 1.5rem; }
    .sidebar-section { display: flex; flex-direction: column; gap: 1.5rem; }

    .palette-grid { display: flex; flex-direction: column; gap: 0.5rem; }
    .p-node { display: flex; align-items: center; gap: 0.75rem; padding: 0.75rem; background: rgba(255,255,255,0.02); border: 1px solid rgba(255,255,255,0.05); border-radius: 12px; color: #94a3b8; font-size: 0.75rem; font-weight: 700; cursor: pointer; transition: all 0.2s; text-align: left; }
    .p-node:hover { background: rgba(255,255,255,0.05); color: #fff; transform: translateX(4px); }
    .p-node .material-icons-outlined { font-size: 1.1rem; }

    .canvas-area { flex: 1; background: #0b0f1a; position: relative; overflow: hidden; }
    #workflow-holder { position: absolute; inset: 0; background-image: radial-gradient(circle, rgba(255,255,255,0.03) 1px, transparent 1px); background-size: 40px 40px; }

    .inspector { width: 320px; background: rgba(15,23,42,0.8); border-left: 1px solid rgba(255,255,255,0.05); display: flex; flex-direction: column; backdrop-filter: blur(20px); z-index: 10; overflow-y: auto; scrollbar-width: thin; }
    .inspector-head { padding: 1.25rem; border-bottom: 1px solid rgba(255,255,255,0.05); display: flex; align-items: center; gap: 0.75rem; background: rgba(16,185,129,0.03); }
    .inspector-title { font-size: 0.7rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.15em; color: #cbd5e1; }
    
    .inspector-body { padding: 1.5rem; display: flex; flex-direction: column; gap: 1.5rem; }
    .i-group { display: flex; flex-direction: column; gap: 0.5rem; }
    .i-label { font-size: 0.65rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; color: #334155; }
    .i-input { background: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.05); border-radius: 10px; padding: 0.625rem 0.875rem; color: #fff; font-size: 0.8rem; outline: none; }
    .i-input:focus { border-color: #10b981; }
    textarea.i-input { resize: none; min-height: 100px; font-family: 'JetBrains Mono', monospace; }

    .empty-state { display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100%; opacity: 0.2; text-align: center; padding: 2rem; }
    .empty-state .material-icons-outlined { font-size: 3rem; margin-bottom: 1rem; }
    .empty-state p { font-size: 0.75rem; font-weight: 800; text-transform: uppercase; letter-spacing: 0.1em; line-height: 1.6; }

    .node-selected { stroke: #10b981 !important; stroke-width: 3 !important; }

    .i-url-preview { background: rgba(0,0,0,0.5); border: 1px solid rgba(16,185,129,0.2); border-radius: 8px; padding: 0.75rem; color: #34d399; font-family: 'JetBrains Mono', monospace; font-size: 0.7rem; word-break: break-all; margin-top: 0.25rem; }

    .btn-danger { display: flex; align-items: center; justify-content: center; gap: 0.5rem; padding: 0.75rem; border-radius: 12px; border: 1px solid rgba(239,68,68,0.2); background: rgba(239,68,68,0.05); color: #f87171; font-size: 0.75rem; font-weight: 700; cursor: pointer; transition: all 0.2; width: 100%; margin-top: 1rem; }
    .btn-danger:hover { background: rgba(239,68,68,0.1); border-color: rgba(239,68,68,0.4); }
  `],
  template: `
    <div class="studio-container">
      <header class="toolbar">
        <div class="toolbar-left">
          <button [routerLink]="['/projects', projectId, 'workflows']" class="back-btn">
            <span class="material-icons-outlined" style="font-size:1.25rem">arrow_back</span>
          </button>
          <div class="brand">
            <div class="brand-icon-wrap">
              <span class="material-icons-outlined brand-icon">account_tree</span>
            </div>
            <div>
              <h2 class="brand-title">Workflow Studio</h2>
              <div class="brand-sub">{{ selectedWorkflow ? selectedWorkflow.name : 'Low-Code Engine' }}</div>
            </div>
          </div>
        </div>

        <div class="toolbar-right">
          <button (click)="saveWorkflow()" class="btn-ghost">
            <i class="material-icons-outlined">save</i>
            Draft Sync
          </button>
          <button (click)="publishWorkflow()" class="btn-success">
            <i class="material-icons-outlined">rocket_launch</i>
            Publish to Engine
          </button>
        </div>
      </header>

      <div class="viewport">
        <!-- Sidebar: Explorer + Palette -->
        <aside class="sidebar">
          <div class="sidebar-section">
            <label class="palette-label">Activity Palette</label>
            <div class="palette-grid">
              <button (click)="addNode('http')" class="p-node">
                <span class="material-icons-outlined" style="color:#10b981">public</span>
                HTTP Trigger
              </button>
              <button (click)="addNode('db')" class="p-node">
                <span class="material-icons-outlined" style="color:#3b82f6">storage</span>
                Database Query
              </button>
              <button (click)="addNode('logic')" class="p-node">
                <span class="material-icons-outlined" style="color:#fbbf24">psychology</span>
                Custom Logic
              </button>
              <button (click)="addNode('notify')" class="p-node">
                <span class="material-icons-outlined" style="color:#a78bfa">notifications</span>
                User Notification
              </button>
              <button (click)="addNode('attachment')" class="p-node">
                <span class="material-icons-outlined" style="color:#f43f5e">attach_file</span>
                Attachment Manager
              </button>
            </div>
          </div>
        </aside>

        <!-- Canvas -->
        <main #canvasContainer class="canvas-area">
          <div id="workflow-holder"></div>
          
          <div *ngIf="!selectedWorkflow" class="empty-state">
             <span class="material-icons-outlined">account_tree</span>
             <p>Select a workflow from the explorer<br>to start designing your logic</p>
          </div>
        </main>

        <!-- Inspector -->
        <aside class="inspector">
          <div class="inspector-head">
            <span class="material-icons-outlined" style="color:#10b981">tune</span>
            <span class="inspector-title">Activity Settings</span>
          </div>

          <div class="inspector-body" *ngIf="selectedNode; else noNode">
            <div class="i-group">
              <label class="i-label">Display Name</label>
              <input type="text" [(ngModel)]="selectedNode.label" (ngModelChange)="updateNodeVisual()" class="i-input">
            </div>

            <hr style="border: none; border-top: 1px solid rgba(255,255,255,0.05);">

            <!-- HTTP Config -->
             <div *ngIf="selectedNode.type === 'http'" style="display:flex; flex-direction:column; gap:1.25rem;">
               <div class="i-group">
                 <label class="i-label">Trigger Path</label>
                 <input type="text" [(ngModel)]="selectedNode.config.path" class="i-input" placeholder="e.g. /v1/orders/new">
               </div>
               <div class="i-group" *ngIf="selectedNode.config.path">
                 <label class="i-label">Full Execution URL</label>
                 <div class="i-url-preview">
                    http://localhost:5018/workflows{{ selectedNode.config.path.startsWith('/') ? '' : '/' }}{{ selectedNode.config.path }}
                 </div>
               </div>
               <div class="i-group">
                 <label class="i-label">Method</label>
                 <select [(ngModel)]="selectedNode.config.method" class="i-input" style="background:#000">
                    <option value="GET">GET</option>
                    <option value="POST">POST</option>
                    <option value="PUT">PUT</option>
                    <option value="DELETE">DELETE</option>
                 </select>
               </div>
            </div>

            <!-- DB Config -->
            <div *ngIf="selectedNode.type === 'db'" style="display:flex; flex-direction:column; gap:1.25rem;">
               <div class="i-group">
                 <label class="i-label">SQL / Data Query</label>
                 <textarea [(ngModel)]="selectedNode.config.query" class="i-input" placeholder="SELECT * FROM Table WHERE ID = @id"></textarea>
               </div>
            </div>

            <!-- Logic Config -->
            <div *ngIf="selectedNode.type === 'logic'" style="display:flex; flex-direction:column; gap:1.25rem;">
               <div class="i-group">
                 <label class="i-label">JavaScript Payload</label>
                 <textarea [(ngModel)]="selectedNode.config.script" class="i-input" placeholder="return { success: true };"></textarea>
               </div>
            </div>

            <!-- Notify Config -->
            <div *ngIf="selectedNode.type === 'notify'" style="display:flex; flex-direction:column; gap:1.25rem;">
               <div class="i-group">
                 <label class="i-label">Recipient Channel</label>
                 <input type="text" [(ngModel)]="selectedNode.config.channel" class="i-input" placeholder="email, sms, push">
               </div>
               <div class="i-group">
                 <label class="i-label">Message Template</label>
                 <textarea [(ngModel)]="selectedNode.config.template" class="i-input" placeholder="Hello {name}, your request is approved."></textarea>
               </div>
            </div>

            <!-- Attachment Config -->
            <div *ngIf="selectedNode.type === 'attachment'" style="display:flex; flex-direction:column; gap:1.25rem;">
               <div class="i-group">
                 <label class="i-label">Storage Provider</label>
                 <select [(ngModel)]="selectedNode.config.provider" class="i-input" style="background:#000">
                    <option value="local">Local Filesystem</option>
                    <option value="s3">Amazon S3</option>
                    <option value="blob">Azure Blob Storage</option>
                 </select>
               </div>
               <div class="i-group">
                 <label class="i-label">Allowed Extensions</label>
                 <input type="text" [(ngModel)]="selectedNode.config.extensions" class="i-input" placeholder=".pdf, .jpg, .png">
               </div>
               <div class="i-group">
                 <label class="i-label">Max Size (MB)</label>
                 <input type="number" [(ngModel)]="selectedNode.config.maxSize" class="i-input">
               </div>
            </div>

            <hr style="border: none; border-top: 1px solid rgba(255,255,255,0.05);">
            
            <button class="btn-danger" (click)="deleteSelectedNode()">
               <span class="material-icons-outlined" style="font-size:1.1rem">delete_forever</span>
               Delete Activity
            </button>
          </div>

          <ng-template #noNode>
             <div class="empty-state">
                <span class="material-icons-outlined">settings_input_component</span>
                <p>Select a node on the canvas<br>to configure its properties</p>
             </div>
          </ng-template>
        </aside>
      </div>
    </div>
  `
})
export class WorkflowDesigner implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('canvasContainer') canvasContainer!: ElementRef;

  projectId: string | null = null;
  workflowId: string | null = null;
  selectedWorkflow: WorkflowMetadata | null = null;
  selectedNode: WorkflowNode | null = null;

  stage!: Konva.Stage;
  layer!: Konva.Layer;
  nodeGroups: Map<string, Konva.Group> = new Map();
  
  resizeHandler = this.onResize.bind(this);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: ApiService,
    private readonly projectContext: ProjectContextService
  ) {
    this.projectId = this.route.snapshot.paramMap.get('projectId');
    this.workflowId = this.route.snapshot.paramMap.get('workflowId');
    if (this.projectId) {
      this.projectContext.setProjectId(this.projectId);
    }
  }

  ngOnInit() {
    this.loadWorkflow();
  }

  ngAfterViewInit() {
    this.initCanvas();
  }

  initCanvas() {
    setTimeout(() => {
      if (!this.canvasContainer) return;
      const container = this.canvasContainer.nativeElement;

      this.stage = new Konva.Stage({
        container: 'workflow-holder',
        width: container.offsetWidth,
        height: container.offsetHeight,
        draggable: true
      });

      this.layer = new Konva.Layer();
      this.stage.add(this.layer);

      globalThis.addEventListener('resize', this.resizeHandler);
    }, 100);
  }

  loadWorkflow() {
    if (!this.projectId || !this.workflowId) return;
    this.api.getWorkflows(this.projectId).subscribe({
      next: (artifacts) => {
        const artifact = artifacts.find(a => a.id === this.workflowId);
        if (artifact) {
          const content = JSON.parse(artifact.content);
          this.selectedWorkflow = { ...content, id: artifact.id };
          this.renderWorkflow();
        }
      }
    });
  }

  renderWorkflow() {
    if (!this.layer) return;
    this.layer.destroyChildren();
    this.nodeGroups.clear();

    if (!this.selectedWorkflow) {
      this.layer.batchDraw();
      return;
    }

    this.selectedWorkflow.nodes.forEach(node => {
      this.drawNode(node);
    });

    this.drawConnections();
    this.layer.batchDraw();
  }

  addNode(type: string) {
    if (!this.selectedWorkflow) return;

    const newNode: WorkflowNode = {
      id: Math.random().toString(36).substring(2, 9),
      type,
      x: 100 + Math.random() * 50,
      y: 100 + Math.random() * 50,
      label: `New ${type} Activity`,
      color: this.getColorForType(type),
      config: this.getDefaultConfig(type)
    };

    this.selectedWorkflow.nodes.push(newNode);
    this.drawNode(newNode);
    this.selectNode(newNode);
  }

  deleteSelectedNode() {
    if (!this.selectedNode || !this.selectedWorkflow) return;
    
    // Remove from metadata
    this.selectedWorkflow.nodes = this.selectedWorkflow.nodes.filter(n => n.id !== this.selectedNode?.id);
    
    // Remove from canvas
    const group = this.nodeGroups.get(this.selectedNode.id);
    if (group) {
      group.destroy();
      this.nodeGroups.delete(this.selectedNode.id);
    }
    
    this.selectedNode = null;
    this.drawConnections();
    this.layer.batchDraw();
  }

  drawNode(node: WorkflowNode) {
    const group = new Konva.Group({
      x: node.x,
      y: node.y,
      draggable: true,
      id: node.id
    });

    const rect = new Konva.Rect({
      width: 200,
      height: 50,
      fill: '#1e293b',
      stroke: node.color,
      strokeWidth: 1,
      cornerRadius: 10,
      shadowBlur: 5,
      shadowOpacity: 0.2
    });

    const icon = new Konva.Text({
      text: this.getIconTextForType(node.type),
      fontFamily: 'Material Icons Outlined',
      fontSize: 18,
      fill: node.color,
      x: 12,
      y: 16
    });

    const label = new Konva.Text({
      text: node.label,
      fontSize: 12,
      fontFamily: 'Inter, sans-serif',
      fill: 'white',
      x: 40,
      y: 18,
      width: 150,
      wrap: 'none',
      ellipsis: true
    });

    group.add(rect, icon, label);

    group.on('dragend', () => {
      node.x = group.x();
      node.y = group.y();
      this.drawConnections();
    });

    group.on('click', () => {
      this.selectNode(node);
    });

    this.nodeGroups.set(node.id, group);
    this.layer.add(group);
    this.layer.batchDraw();
  }

  selectNode(node: WorkflowNode) {
    this.selectedNode = node;
    // Highlight visual
    this.nodeGroups.forEach((g, id) => {
      const r = g.findOne('Rect') as Konva.Rect;
      if (id === node.id) {
        r.stroke('#10b981');
        r.strokeWidth(3);
      } else {
        const n = this.selectedWorkflow?.nodes.find(v => v.id === id);
        r.stroke(n?.color || '#94a3b8');
        r.strokeWidth(1);
      }
    });
    this.layer.batchDraw();
  }

  updateNodeVisual() {
    if (!this.selectedNode) return;
    const group = this.nodeGroups.get(this.selectedNode.id);
    if (group) {
      const label = group.findOne('Text') as Konva.Text; // Need to be careful which text
      // Actually finding the specific one:
      const texts = group.find('Text');
      const labelText = texts.find(t => (t as Konva.Text).fontFamily() !== 'Material Icons Outlined') as Konva.Text;
      if (labelText) labelText.text(this.selectedNode.label);
      this.layer.batchDraw();
    }
  }

  drawConnections() {
    // Basic auto-connector for demo purposes (sequential)
    // Real implementation would need drag-and-drop connectors
    const lines = this.layer.find('.connection');
    lines.forEach(l => l.destroy());

    if (!this.selectedWorkflow) return;
    
    // Placeholder: just connect nodes in order of array
    for (let i = 0; i < this.selectedWorkflow.nodes.length - 1; i++) {
      const n1 = this.selectedWorkflow.nodes[i];
      const n2 = this.selectedWorkflow.nodes[i+1];
      
      const arrow = new Konva.Arrow({
        points: [n1.x + 200, n1.y + 25, n2.x, n2.y + 25],
        pointerLength: 10,
        pointerWidth: 10,
        fill: '#ffffff11',
        stroke: '#ffffff11',
        strokeWidth: 2,
        tension: 0.5,
        name: 'connection'
      });
      this.layer.add(arrow);
    }
    this.layer.batchDraw();
  }

  saveWorkflow() {
    if (!this.selectedWorkflow || !this.selectedWorkflow.id) return;
    this.api.updateWorkflow(this.projectId!, this.selectedWorkflow.id, this.selectedWorkflow).subscribe(() => {
      alert('Workflow draft synced successfully.');
    });
  }

  publishWorkflow() {
    if (!this.selectedWorkflow || !this.selectedWorkflow.id) return;
    const workflowId = this.selectedWorkflow.id;
    
    // First save the draft
    this.api.updateWorkflow(this.projectId!, workflowId, this.selectedWorkflow).subscribe(() => {
      // Then call the publish endpoint (I will add this to ApiService)
      this.api.publishWorkflow(this.projectId!, workflowId).subscribe({
        next: () => alert('Workflow successfully published to Elsa 3.0 Engine! Trigger it via its configured path.'),
        error: (err) => alert('Publishing failed: ' + err.message)
      });
    });
  }

  private getColorForType(type: string): string {
    switch (type) {
      case 'http': return '#10b981';
      case 'db': return '#3b82f6';
      case 'logic': return '#fbbf24';
      case 'notify': return '#a78bfa';
      case 'attachment': return '#f43f5e';
      default: return '#94a3b8';
    }
  }

  private getIconTextForType(type: string): string {
    switch (type) {
      case 'http': return 'public';
      case 'db': return 'storage';
      case 'logic': return 'psychology';
      case 'notify': return 'notifications';
      case 'attachment': return 'attach_file';
      default: return 'settings';
    }
  }

  private getDefaultConfig(type: string): any {
    switch (type) {
      case 'http': return { path: '', method: 'POST', headers: {}, body: '' };
      case 'db': return { query: '', params: [] };
      case 'logic': return { script: 'return { success: true };' };
      case 'notify': return { channel: 'email', template: '' };
      case 'attachment': return { provider: 'local', extensions: '.pdf,.docx', maxSize: 10 };
      default: return {};
    }
  }

  onResize() {
    if (!this.stage || !this.canvasContainer) return;
    this.stage.width(this.canvasContainer.nativeElement.offsetWidth);
    this.stage.height(this.canvasContainer.nativeElement.offsetHeight);
  }

  ngOnDestroy() {
    globalThis.removeEventListener('resize', this.resizeHandler);
    if (this.stage) this.stage.destroy();
  }
}
