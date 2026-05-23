import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api';
import { ProjectContextService } from '../../services/project-context';

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
    styles: [`
    :host { display: block; }
    .studio-container { display: flex; flex-direction: column; height: calc(100vh - 64px); background: #0b0f1a; color: #e2e8f0; font-family: 'Inter', sans-serif; overflow: hidden; }

    /* ── Toolbar ──────────────────────────────── */
    .toolbar { height: 56px; border-bottom: 1px solid rgba(255,255,255,0.05); display: flex; align-items: center; justify-content: space-between; padding: 0 1.5rem; background: rgba(15,23,42,0.8); backdrop-filter: blur(16px); z-index: 20; }
    .toolbar-left { display: flex; align-items: center; gap: 1rem; }
    .brand { display: flex; align-items: center; gap: 0.75rem; }
    .brand-icon-wrap { width: 36px; height: 36px; background: rgba(245,158,11,0.1); border-radius: 10px; display: flex; align-items: center; justify-content: center; }
    .brand-icon { color: #f59e0b; font-size: 1.25rem; }
    .brand-title { font-size: 0.85rem; font-weight: 800; color: #fff; margin: 0; }
    .brand-sub { font-size: 9px; color: #475569; font-family: 'JetBrains Mono', monospace; text-transform: uppercase; letter-spacing: 0.1em; }

    .toolbar-right { display: flex; align-items: center; gap: 0.75rem; }
    .btn-ghost { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 1rem; border-radius: 10px; border: 1px solid transparent; background: transparent; color: #94a3b8; font-size: 0.75rem; font-weight: 700; cursor: pointer; transition: all 0.2s; }
    .btn-ghost:hover { background: rgba(255,255,255,0.05); color: #fff; }
    .btn-primary { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 1.25rem; border-radius: 10px; border: none; background: #d97706; color: #fff; font-size: 0.75rem; font-weight: 800; cursor: pointer; transition: all 0.2s; box-shadow: 0 4px 12px rgba(217,119,6,0.2); }
    .btn-primary:hover { background: #f59e0b; transform: translateY(-1px); }

    /* ── Main Layout ───────────────────────────── */
    .viewport { display: flex; flex: 1; overflow: hidden; position: relative; }
    
    .explorer { width: 260px; background: rgba(15,23,42,0.4); border-right: 1px solid rgba(255,255,255,0.05); display: flex; flex-direction: column; padding: 1.25rem; gap: 1.5rem; z-index: 10; backdrop-filter: blur(10px); }
    .explorer-label { font-size: 0.65rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.2em; color: #334155; }
    .explorer-list { display: flex; flex-direction: column; gap: 0.5rem; overflow-y: auto; }
    
    .e-item { display: flex; align-items: center; justify-content: space-between; padding: 0.75rem 1rem; background: rgba(255,255,255,0.02); border: 1px solid rgba(255,255,255,0.05); border-radius: 12px; color: #64748b; cursor: pointer; transition: all 0.2s; }
    .e-item:hover { background: rgba(255,255,255,0.05); color: #fff; border-color: rgba(255,255,255,0.1); }
    .e-item.active { background: rgba(245,158,11,0.1); border-color: rgba(245,158,11,0.2); color: #f59e0b; }
    .e-name { font-size: 0.8rem; font-weight: 700; }
    .e-delete { background: transparent; border: none; color: #334155; cursor: pointer; opacity: 0; transition: all 0.2s; }
    .e-item:hover .e-delete { opacity: 1; }
    .e-delete:hover { color: #f87171; }

    .canvas { flex: 1; background: #0b0f1a; position: relative; overflow-y: auto; padding: 3rem; scrollbar-width: thin; }
    .canvas-max { max-width: 800px; margin: 0 auto; display: flex; flex-direction: column; gap: 3rem; }

    .identity-card { background: rgba(15,23,42,0.6); border: 1px solid rgba(255,255,255,0.05); border-radius: 24px; padding: 2rem; display: flex; flex-direction: column; gap: 1.5rem; position: relative; overflow: hidden; }
    .identity-card::after { content: 'list_alt'; font-family: 'Material Icons Outlined'; position: absolute; font-size: 10rem; color: #fff; opacity: 0.02; top: -2rem; right: -2rem; pointer-events: none; }
    
    .field-group { display: flex; flex-direction: column; gap: 0.5rem; position: relative; z-index: 1; }
    .field-label { font-size: 0.65rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; color: #f59e0b; opacity: 0.6; }
    .name-input { background: transparent; border: none; border-bottom: 2px solid rgba(255,255,255,0.05); color: #fff; font-size: 2rem; font-weight: 900; outline: none; padding-bottom: 0.5rem; transition: border-color 0.2s; }
    .name-input:focus { border-color: #f59e0b; }
    .ns-input { background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.05); border-radius: 10px; padding: 0.625rem 1rem; color: #64748b; font-family: 'JetBrains Mono', monospace; font-size: 0.75rem; outline: none; }

    .values-section { display: flex; flex-direction: column; gap: 1.5rem; }
    .values-head { display: flex; justify-content: space-between; align-items: center; }
    .values-title { font-size: 0.7rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.15em; color: #334155; }
    .btn-add-val { background: transparent; border: none; color: #f59e0b; font-size: 0.75rem; font-weight: 800; text-transform: uppercase; cursor: pointer; display: flex; align-items: center; gap: 0.375rem; }

    .val-item { display: flex; align-items: center; gap: 1rem; background: rgba(15,23,42,0.4); border: 1px solid rgba(255,255,255,0.03); border-radius: 16px; padding: 1rem; transition: all 0.2s; }
    .val-item:hover { background: rgba(255,255,255,0.02); border-color: rgba(245,158,11,0.2); }
    .val-index { width: 32px; height: 32px; border-radius: 8px; background: rgba(0,0,0,0.3); display: flex; align-items: center; justify-content: center; font-family: 'JetBrains Mono', monospace; font-size: 0.75rem; color: #334155; font-weight: 800; }
    .val-name-input { flex: 1; background: transparent; border: none; color: #f1f5f9; font-size: 0.9rem; font-weight: 700; outline: none; }
    .val-value-input { width: 80px; background: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.05); border-radius: 8px; padding: 0.5rem; color: #f59e0b; font-family: 'JetBrains Mono', monospace; font-size: 0.8rem; text-align: center; outline: none; }
    .val-delete { color: #334155; transition: color 0.2s; }
    .val-delete:hover { color: #f87171; }

    .inspector { width: 280px; background: rgba(15,23,42,0.8); border-left: 1px solid rgba(255,255,255,0.05); display: flex; flex-direction: column; backdrop-filter: blur(20px); z-index: 10; }
    .inspector-header { height: 56px; border-bottom: 1px solid rgba(255,255,255,0.05); display: flex; align-items: center; padding: 0 1.25rem; gap: 0.75rem; background: rgba(245,158,11,0.03); }
    .inspector-icon { color: #f59e0b; font-size: 1.1rem; }
    .inspector-title { font-size: 0.7rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.15em; color: #cbd5e1; }
    
    .inspector-body { flex: 1; padding: 1.5rem; display: flex; flex-direction: column; gap: 2rem; }
    .stat-card { background: rgba(0,0,0,0.4); border: 1px solid rgba(255,255,255,0.05); border-radius: 20px; padding: 1.25rem; display: flex; flex-direction: column; gap: 0.75rem; }
    .stat-label { font-size: 0.65rem; font-weight: 800; color: #475569; text-transform: uppercase; letter-spacing: 0.1em; }
    .stat-value { font-size: 2rem; font-weight: 900; color: #fff; line-height: 1; }
    .stat-bar-bg { height: 4px; background: rgba(255,255,255,0.05); border-radius: 2px; overflow: hidden; }
    .stat-bar-fill { height: 100%; background: #f59e0b; box-shadow: 0 0 10px #f59e0b; }

    .fade-in { animation: fadeIn 0.3s ease-out; }
    @keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
  `],
    template: `
    <div class="studio-container">
      <header class="toolbar">
        <div class="toolbar-left">
          <div class="brand">
            <div class="brand-icon-wrap">
              <span class="material-icons-outlined brand-icon">list_alt</span>
            </div>
            <div>
              <h2 class="brand-title">Enum Architect</h2>
              <div class="brand-sub">Metadata Engine // {{ projectId | slice:0:8 }}</div>
            </div>
          </div>
        </div>

        <div class="toolbar-right">
          <button (click)="saveEnums()" class="btn-ghost">
            <span class="material-icons-outlined">save</span>
            Sync Metadata
          </button>
          <div style="width:1px; height:20px; background:rgba(255,255,255,0.1); margin:0 0.5rem"></div>
          <button (click)="addNewEnum()" class="btn-primary">
            <span class="material-icons-outlined">add</span>
            New Enum
          </button>
        </div>
      </header>

      <div class="viewport">
        <!-- Explorer -->
        <aside class="explorer">
          <label class="explorer-label">Dictionary</label>
          <div class="explorer-list">
             <div *ngFor="let e of enums" 
                  (click)="selectEnum(e)"
                  [class.active]="selectedEnum === e"
                  class="e-item">
               <span class="e-name">{{e.name}}</span>
               <button (click)="deleteEnum($event, e)" class="e-delete">
                  <span class="material-icons-outlined" style="font-size:1rem">delete</span>
               </button>
             </div>

             <div *ngIf="enums.length === 0" style="padding:2rem; text-align:center; opacity:0.2;">
                <p style="font-size:0.7rem; font-weight:800; text-transform:uppercase;">No Enums</p>
             </div>
          </div>
        </aside>

        <!-- Canvas -->
        <main class="canvas">
          <div *ngIf="!selectedEnum" style="height:100%; display:flex; flex-direction:column; align-items:center; justify-content:center; opacity:0.2;">
             <span class="material-icons-outlined" style="font-size:4rem; margin-bottom:1rem;">format_list_bulleted</span>
             <p style="font-size:0.8rem; font-weight:800; text-transform:uppercase; letter-spacing:0.3em;">Select a definition</p>
          </div>

          <div *ngIf="selectedEnum" class="canvas-max fade-in">
             <div class="identity-card">
                <div class="field-group">
                   <label class="field-label">Definition Name</label>
                   <input type="text" [(ngModel)]="selectedEnum.name" class="name-input" placeholder="EnumName">
                </div>
                <div class="field-group">
                   <label class="field-label">Namespace</label>
                   <input type="text" [(ngModel)]="selectedEnum.namespace" class="ns-input" placeholder="App.Entities">
                </div>
             </div>

             <div class="values-section">
                <div class="values-head">
                   <label class="values-title">Member Constants</label>
                   <button (click)="addValue()" class="btn-add-val">
                      <span class="material-icons-outlined">add_circle</span>
                      Add Option
                   </button>
                </div>

                <div style="display:flex; flex-direction:column; gap:0.5rem;">
                   <div *ngFor="let val of selectedEnum.values; let i = index" class="val-item">
                      <div class="val-index">{{i}}</div>
                      <input type="text" [(ngModel)]="val.name" class="val-name-input" placeholder="MemberName">
                      <input type="number" [(ngModel)]="val.value" class="val-value-input">
                      <button (click)="removeValue(i)" style="background:none; border:none; cursor:pointer;" class="val-delete">
                        <span class="material-icons-outlined" style="font-size:1.1rem">close</span>
                      </button>
                   </div>
                </div>
             </div>
          </div>
        </main>

        <!-- Inspector -->
        <aside class="inspector">
           <div class="inspector-header">
              <span class="material-icons-outlined inspector-icon">insights</span>
              <span class="inspector-title">Metadata Stats</span>
           </div>
           <div class="inspector-body">
              <div *ngIf="selectedEnum" class="fade-in">
                 <div class="stat-card">
                    <span class="stat-label">Defined Options</span>
                    <span class="stat-value">{{selectedEnum.values.length}}</span>
                    <div class="stat-bar-bg">
                       <div class="stat-bar-fill" [style.width.%]="(selectedEnum.values.length / 10) * 100"></div>
                    </div>
                 </div>
                 <p style="font-size:0.7rem; color:#334155; margin-top:2rem; font-style:italic; line-height:1.5;">
                    Metadata enums are compiled into strongly typed C# enums and TypeScript unions during the deployment cycle.
                 </p>
              </div>
           </div>
        </aside>
      </div>
    </div>
  `,
})
export class EnumDesigner implements OnInit {
    projectId: string | null = null;
    enums: EnumMetadata[] = [];
    selectedEnum: EnumMetadata | null = null;

    constructor(
        private readonly route: ActivatedRoute,
        private readonly api: ApiService,
        private readonly projectContext: ProjectContextService
    ) {
        this.projectId = this.route.snapshot.paramMap.get('projectId');
        if (this.projectId) {
            this.projectContext.setProjectId(this.projectId);
        }
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
            this.api.request('PUT', `projects/${this.projectId}/artifacts/${this.selectedEnum.id}`, payload).subscribe(() => {
                alert('Enum metadata updated.');
            });
        } else {
            this.api.request('POST', `projects/${this.projectId}/artifacts`, payload).subscribe((res: any) => {
                if (this.selectedEnum) this.selectedEnum.id = res.id;
                alert('Enum metadata created.');
            });
        }
    }
}
