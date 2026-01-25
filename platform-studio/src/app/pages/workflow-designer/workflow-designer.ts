import { Component, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api';
import { FormsModule } from '@angular/forms';
import Konva from 'konva';

@Component({
   selector: 'app-workflow-designer',
   standalone: true,
   imports: [CommonModule, RouterLink, FormsModule],
   template: `
    <div class="flex flex-col h-[calc(100vh-64px)] bg-slate-950 text-slate-200">
      <!-- Toolbar -->
      <div class="h-14 border-b border-white/10 flex items-center justify-between px-6 bg-slate-900/80 backdrop-blur-xl z-20 shadow-2xl">
        <div class="flex items-center space-x-6">
          <button [routerLink]="['/projects', projectId, 'designer']" class="group p-2 hover:bg-white/5 rounded-xl text-slate-400 hover:text-white transition-all">
            <span class="material-icons-outlined group-hover:-translate-x-1 transition-transform">arrow_back</span>
          </button>
          <div class="flex items-center space-x-3">
             <div class="p-2 bg-green-500/10 rounded-lg">
                <span class="material-icons-outlined text-green-400 text-lg">account_tree</span>
             </div>
             <div>
                <h2 class="text-sm font-semibold text-white">Workflow Designer</h2>
                <div class="text-[10px] text-slate-500 font-mono">Elsa v3 Runtime</div>
             </div>
          </div>
        </div>

        <div class="flex items-center space-x-3">
           <button class="flex items-center space-x-2 text-sm text-slate-400 hover:text-white px-4 py-2 rounded-lg transition-colors hover:bg-white/5">
             <span class="material-icons-outlined text-lg">save</span>
             <span>Save Flow</span>
           </button>
           <div class="w-px h-6 bg-white/10 mx-2"></div>
           <button (click)="addNode('http')" class="bg-blue-600 hover:bg-blue-500 text-white px-4 py-2 rounded-lg text-sm font-medium transition-all shadow-lg shadow-blue-900/20 active:scale-95">
             Add Trigger
           </button>
           <button (click)="runSimulation()" class="bg-slate-800 hover:bg-slate-700 text-slate-200 border border-white/10 px-4 py-2 rounded-lg text-sm font-medium transition-all flex items-center space-x-2">
             <span class="material-icons-outlined text-sm">play_arrow</span>
             <span>Run Test</span>
           </button>
        </div>
      </div>

      <div class="flex-1 flex overflow-hidden relative">
        <!-- Palette Sidebar -->
        <aside class="w-64 bg-slate-900/50 backdrop-blur-md border-r border-white/5 p-6 flex flex-col space-y-8 z-10">
           <div class="space-y-4">
              <label class="text-[10px] uppercase tracking-widest font-bold text-slate-500">Activities</label>
              <nav class="space-y-1">
                 <button (click)="addNode('db')" class="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl hover:bg-white/5 text-slate-400 hover:text-blue-400 transition-all group">
                    <span class="material-icons-outlined text-lg">storage</span>
                    <span class="text-sm font-medium">Database</span>
                 </button>
                 <button (click)="addNode('logic')" class="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl hover:bg-white/5 text-slate-400 hover:text-amber-400 transition-all">
                    <span class="material-icons-outlined text-lg">psychology</span>
                    <span class="text-sm font-medium">Logic</span>
                 </button>
                 <button (click)="addNode('http')" class="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl hover:bg-white/5 text-slate-400 hover:text-green-400 transition-all">
                    <span class="material-icons-outlined text-lg">public</span>
                    <span class="text-sm font-medium">HTTP</span>
                 </button>
                 <button (click)="addNode('notify')" class="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl hover:bg-white/5 text-slate-400 hover:text-purple-400 transition-all">
                    <span class="material-icons-outlined text-lg">notifications</span>
                    <span class="text-sm font-medium">Notifications</span>
                 </button>
              </nav>
           </div>

           <div class="mt-auto p-4 bg-blue-600/5 border border-blue-500/10 rounded-2xl">
              <p class="text-[11px] text-slate-500 leading-relaxed italic">
                 Drag and drop nodes to define the business process for your clinic app.
              </p>
           </div>
        </aside>

        <!-- Canvas Area -->
        <div #canvasContainer class="flex-1 bg-[#0B1120] relative cursor-crosshair overflow-hidden">
           <div id="workflow-holder" class="absolute inset-0"></div>
           
           <!-- Canvas Controls -->
           <div class="absolute bottom-6 right-6 flex space-x-2">
              <button class="w-10 h-10 rounded-full bg-slate-800 border border-white/10 flex items-center justify-center text-slate-400 hover:text-white transition-all shadow-xl">
                 <span class="material-icons-outlined">zoom_in</span>
              </button>
              <button class="w-10 h-10 rounded-full bg-slate-800 border border-white/10 flex items-center justify-center text-slate-400 hover:text-white transition-all shadow-xl">
                 <span class="material-icons-outlined">zoom_out</span>
              </button>
           </div>
        </div>
      </div>
    </div>
  `,
   styles: [`
    #workflow-holder {
      background-image: 
        linear-gradient(to right, rgba(255,255,255,0.05) 1px, transparent 1px),
        linear-gradient(to bottom, rgba(255,255,255,0.05) 1px, transparent 1px);
      background-size: 40px 40px;
    }
  `]
})
export class WorkflowDesigner implements AfterViewInit, OnDestroy {
   @ViewChild('canvasContainer') canvasContainer!: ElementRef;

   projectId: string | null = null;
   stage!: Konva.Stage;
   layer!: Konva.Layer;
   nodes: any[] = [];
   resizeHandler = this.onResize.bind(this);

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

         this.stage = new Konva.Stage({
            container: 'workflow-holder',
            width: container.offsetWidth,
            height: container.offsetHeight,
            draggable: true
         });

         this.layer = new Konva.Layer();
         this.stage.add(this.layer);

         globalThis.addEventListener('resize', this.resizeHandler);

         // Seed initial overlap check nodes to match the mockup
         this.seedClinicWorkflow();
      }, 100);
   }

   onResize() {
      if (!this.stage || !this.canvasContainer) return;
      this.stage.width(this.canvasContainer.nativeElement.offsetWidth);
      this.stage.height(this.canvasContainer.nativeElement.offsetHeight);
   }

   seedClinicWorkflow() {
      this.addNodeAt('http', 150, 100, 'HTTP Request: /validate-appointment', '#3b82f6');
      this.addNodeAt('logic', 150, 250, 'JS: Query Appointment Overlap', '#f59e0b');
      this.addNodeAt('logic', 450, 250, 'Is Overlap?', '#6366f1', true); // Diamond
      this.addNodeAt('notify', 700, 150, '409 Conflict', '#ef4444');
      this.addNodeAt('notify', 700, 350, '200 OK', '#22c55e');

      this.drawConnections();
   }

   addNode(type: string) {
      const x = 100 + Math.random() * 200;
      const y = 100 + Math.random() * 200;
      this.addNodeAt(type, x, y, `New ${type} node`, '#94a3b8');
   }

   addNodeAt(type: string, x: number, y: number, label: string, color: string, isDiamond = false) {
      const group = new Konva.Group({ x, y, draggable: true });

      if (isDiamond) {
         const poly = new Konva.RegularPolygon({
            sides: 4,
            radius: 60,
            fill: '#0f172a',
            stroke: color,
            strokeWidth: 2,
            shadowBlur: 10,
            shadowColor: color,
            shadowOpacity: 0.3,
            rotation: 0
         });
         group.add(poly);
      } else {
         const rect = new Konva.Rect({
            width: 220,
            height: 60,
            fill: '#1e293b',
            stroke: color,
            strokeWidth: 2,
            cornerRadius: 12,
            shadowBlur: 15,
            shadowColor: 'black',
            shadowOpacity: 0.4
         });
         group.add(rect);
      }

      const text = new Konva.Text({
         text: label,
         fontSize: 12,
         fontFamily: 'Inter, sans-serif',
         fill: 'white',
         width: isDiamond ? 100 : 220,
         padding: isDiamond ? 0 : 20,
         align: 'center',
         y: isDiamond ? -10 : 0,
         x: isDiamond ? -50 : 0
      });

      group.add(text);
      this.layer.add(group);
      this.layer.batchDraw();
   }

   drawConnections() {
      // Simplified static lines for visualization of the mockup
      this.drawLine(370, 130, 270, 250); // HTTP to JS
      this.drawLine(370, 280, 400, 280); // JS to Diamond
      this.drawLine(510, 250, 700, 180); // Diamond to Conflict
      this.drawLine(510, 310, 700, 380); // Diamond to OK
   }

   drawLine(x1: number, y1: number, x2: number, y2: number) {
      const line = new Konva.Arrow({
         points: [x1, y1, x2, y2],
         pointerLength: 10,
         pointerWidth: 10,
         fill: '#ffffff33',
         stroke: '#ffffff33',
         strokeWidth: 2,
         tension: 0.5
      });
      this.layer.add(line);
   }

   runSimulation() {
      alert('Starting Workflow Simulation for Clinic Appointment...');
   }

   ngOnDestroy() {
      globalThis.removeEventListener('resize', this.resizeHandler);
      if (this.stage) this.stage.destroy();
   }
}
