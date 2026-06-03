import { Component, ElementRef, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import Konva from 'konva';
import { ApiService } from '../../services/api';
import { AiGenerateModalComponent } from '../../components/ai-generate-modal/ai-generate-modal';
import { ProjectContextService } from '../../services/project-context';

@Component({
  selector: 'app-entity-designer',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, AiGenerateModalComponent],
  styles: [`
    :host { display: block; }
    .designer-container { display: flex; flex-direction: column; height: calc(100vh - 64px); background: #0b1120; color: #e2e8f0; overflow: hidden; font-family: 'Inter', sans-serif; }

    /* ── Toolbar ──────────────────────────────── */
    .toolbar { height: 56px; border-bottom: 1px solid rgba(255,255,255,0.05); display: flex; align-items: center; justify-content: space-between; padding: 0 1.5rem; background: rgba(15,23,42,0.8); backdrop-filter: blur(16px); z-index: 20; }
    .toolbar-left { display: flex; align-items: center; gap: 1.5rem; }
    .brand { display: flex; align-items: center; gap: 0.75rem; }
    .brand-icon-wrap { p: 0.5rem; background: rgba(59,130,246,0.1); border-radius: 12px; display: flex; align-items: center; justify-content: center; }
    .brand-icon { color: #60a5fa; font-size: 1.125rem; }
    .brand-title { font-size: 0.875rem; font-weight: 900; color: #fff; letter-spacing: 0.1em; text-transform: uppercase; margin: 0; }
    .brand-sub { font-size: 9px; color: #64748b; font-family: monospace; letter-spacing: -0.025em; opacity: 0.6; }
    .v-divider { width: 1px; height: 2rem; background: rgba(255,255,255,0.1); margin: 0 0.5rem; }

    .btn-group { display: flex; align-items: center; gap: 0.25rem; padding: 0.25rem; background: rgba(0,0,0,0.2); border-radius: 12px; }
    .tool-btn { display: flex; align-items: center; gap: 0.5rem; font-size: 0.75rem; color: #94a3b8; padding: 0.375rem 1rem; border-radius: 8px; border: none; background: transparent; cursor: pointer; transition: all 0.2s; font-weight: 700; }
    .tool-btn:hover { color: #fff; background: rgba(255,255,255,0.05); }
    .tool-btn.blue:hover { color: #60a5fa; background: rgba(59,130,246,0.1); }
    .tool-btn.violet:hover { color: #a78bfa; background: rgba(139,92,246,0.1); }
    .tool-btn.purple:hover { color: #c084fc; background: rgba(168,85,247,0.1); }
    .tool-btn.amber:hover { color: #fbbf24; background: rgba(245,158,11,0.1); }
    .tool-btn.green:hover { color: #4ade80; background: rgba(34,197,94,0.1); }
    .tool-btn .material-icons-outlined { font-size: 1.125rem; }

    .toolbar-right { display: flex; align-items: center; gap: 0.75rem; }
    .btn-commit { display: flex; align-items: center; gap: 0.5rem; font-size: 0.75rem; color: #94a3b8; padding: 0.5rem 1rem; border-radius: 8px; border: none; background: transparent; cursor: pointer; transition: all 0.2s; font-weight: 700; text-transform: uppercase; letter-spacing: 0.1em; }
    .btn-commit:hover { color: #fff; background: rgba(255,255,255,0.05); }
    .btn-commit .material-icons-outlined { font-size: 1.125rem; }

    .options-stack { display: flex; flex-direction: column; gap: 0.25rem; margin-right: 1rem; }
    .opt-label { display: flex; align-items: center; gap: 0.5rem; font-size: 9px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; color: #64748b; cursor: pointer; transition: color 0.2s; }
    .opt-label:hover { color: #fff; }
    .opt-label.blue:hover { color: #60a5fa; }
    .opt-label.purple:hover { color: #c084fc; }
    .opt-checkbox { height: 0.75rem; width: 0.75rem; background: rgba(0,0,0,0.4); border: 1px solid rgba(255,255,255,0.1); border-radius: 3px; }

    .btn-export { display: flex; align-items: center; gap: 0.5rem; font-size: 0.75rem; font-weight: 700; color: #e2e8f0; background: #1e293b; border: 1px solid rgba(255,255,255,0.1); padding: 0.5rem 1rem; border-radius: 12px; cursor: pointer; transition: all 0.2s; }
    .btn-export:hover { background: #334155; }
    .btn-export .material-icons-outlined { font-size: 1.125rem; color: #fbbf24; }

    .btn-deploy { display: flex; align-items: center; gap: 0.5rem; font-size: 0.75rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; font-style: italic; color: #fff; background: #2563eb; border: none; padding: 0.5rem 1.5rem; border-radius: 12px; cursor: pointer; transition: all 0.2s; }
    .btn-deploy:hover:not(:disabled) { background: #3b82f6; }
    .btn-deploy:disabled { background: #334155; cursor: not-allowed; opacity: 0.7; }
    .btn-deploy .material-icons-outlined { font-size: 1.125rem; }

    /* ── Canvas Area ───────────────────────────── */
    .viewport { display: flex; flex: 1; overflow: hidden; position: relative; }
    .canvas-container { flex: 1; background: #0b1120; position: relative; overflow: hidden; cursor: crosshair; }
    #konva-holder { position: absolute; inset: 0; background-image: radial-gradient(circle, rgba(255,255,255,0.05) 1px, transparent 1px); background-size: 32px 32px; }

    .zoom-controls { position: absolute; bottom: 1.5rem; right: 21rem; display: flex; gap: 0.25rem; background: rgba(15,23,42,0.8); backdrop-filter: blur(8px); padding: 0.25rem; border-radius: 9999px; border: 1px solid rgba(255,255,255,0.05); }
    .zoom-btn { width: 2rem; height: 2rem; border-radius: 50%; border: none; background: transparent; color: #94a3b8; display: flex; align-items: center; justify-content: center; cursor: pointer; transition: all 0.2s; }
    .zoom-btn:hover { background: rgba(255,255,255,0.1); color: #fff; }
    .zoom-val { padding: 0 0.5rem; display: flex; align-items: center; font-size: 10px; font-family: monospace; color: #64748b; }

    .canvas-footer { position: absolute; bottom: 1.5rem; left: 1.5rem; font-size: 9px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.2em; color: #64748b; opacity: 0.4; }

    .floating-toolbox { position: absolute; top: 1.5rem; left: 1.5rem; display: flex; flex-direction: column; gap: 0.5rem; z-index: 10; }
    .toolbox-inner { padding: 0.5rem; background: rgba(15,23,42,0.9); backdrop-filter: blur(12px); border-radius: 1rem; border: 1px solid rgba(255,255,255,0.1); display: flex; flex-direction: column; gap: 1rem; box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5); }
    .toolbox-btn { padding: 0.75rem; border-radius: 0.75rem; border: none; background: transparent; cursor: pointer; transition: all 0.2s; display: flex; align-items: center; justify-content: center; }
    .toolbox-btn:hover { background: rgba(255,255,255,0.05); }
    .toolbox-btn.blue { color: #60a5fa; opacity: 0.6; }
    .toolbox-btn.blue:hover { color: #60a5fa; background: rgba(59,130,246,0.2); opacity: 1; }
    .toolbox-btn.emerald { color: #34d399; opacity: 0.6; }
    .toolbox-btn.emerald:hover { color: #34d399; background: rgba(16,185,129,0.2); opacity: 1; }
    .toolbox-btn.purple { color: #c084fc; opacity: 0.6; }
    .toolbox-btn.purple:hover { color: #c084fc; background: rgba(168,85,247,0.2); opacity: 1; }
    .tool-sep { height: 1px; background: rgba(255,255,255,0.05); margin: 0 0.5rem; }

    /* ── Property Panel ────────────────────────── */
    .prop-panel { width: 20rem; background: rgba(15,23,42,0.6); backdrop-filter: blur(12px); border-left: 1px solid rgba(255,255,255,0.05); display: flex; flex-direction: column; z-index: 20; }
    .prop-header { height: 3.5rem; border-bottom: 1px solid rgba(255,255,255,0.05); display: flex; align-items: center; padding: 0 1.5rem; background: rgba(37,99,235,0.05); }
    .prop-header-icon { color: #60a5fa; margin-right: 0.5rem; font-size: 1.125rem; }
    .prop-header-title { font-size: 10px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; color: #cbd5e1; }

    .prop-body { flex: 1; overflow-y: auto; padding: 1.5rem; scrollbar-width: thin; scrollbar-color: #334155 transparent; }
    .prop-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100%; color: #475569; opacity: 0.4; }
    .empty-icon-wrap { width: 5rem; height: 5rem; border-radius: 50%; border: 2px dashed rgba(255,255,255,0.05); display: flex; align-items: center; justify-content: center; margin-bottom: 1.5rem; }
    .empty-icon { font-size: 2.25rem; }
    .empty-text { font-size: 10px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; text-align: center; line-height: 1.5; }

    .prop-section { margin-bottom: 2.5rem; }
    .section-label { font-size: 10px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.2em; color: rgba(59,130,246,0.6); display: block; margin-bottom: 0.75rem; }
    .section-label.purple { color: rgba(168,85,247,0.6); }
    .section-label.slate { color: #64748b; }

    .input-wrap { position: relative; }
    .text-input { width: 100%; background: rgba(0,0,0,0.4); border: 1px solid rgba(255,255,255,0.1); border-radius: 0.75rem; padding: 0.75rem 1rem; font-size: 0.875rem; color: #fff; font-weight: 900; outline: none; transition: all 0.2s; box-sizing: border-box; }
    .text-input:focus { border-color: #3b82f6; box-shadow: 0 0 0 4px rgba(59,130,246,0.1); }
    .input-icon { position: absolute; right: 0.75rem; top: 0.75rem; color: #334155; }
    .input-wrap:hover .input-icon { color: #3b82f6; }

    .flex-row-sb { display: flex; align-items: center; justify-content: space-between; padding-bottom: 0.5rem; border-bottom: 1px solid rgba(255,255,255,0.05); margin-bottom: 1rem; }
    .btn-add { display: flex; align-items: center; gap: 0.25rem; font-size: 9px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; background: rgba(59,130,246,0.1); color: #60a5fa; border: none; padding: 0.375rem 0.75rem; border-radius: 9999px; cursor: pointer; transition: all 0.2s; }
    .btn-add:hover { background: #3b82f6; color: #fff; }
    .btn-add.purple { background: rgba(168,85,247,0.1); color: #c084fc; }
    .btn-add.purple:hover { background: #a855f7; color: #fff; }

    .field-card { p: 1rem; background: rgba(255,255,255,0.02); border-radius: 1rem; border: 1px solid rgba(255,255,255,0.05); transition: all 0.2s; position: relative; overflow: hidden; margin-bottom: 0.75rem; }
    .field-card:hover { border-color: rgba(59,130,246,0.3); }
    .field-accent { position: absolute; top: 0; left: 0; width: 4px; height: 100%; background: rgba(59,130,246,0.2); transition: background 0.2s; }
    .field-card:hover .field-accent { background: #3b82f6; }
    .field-accent.purple { background: rgba(168,85,247,0.2); }
    .field-card:hover .field-accent.purple { background: #a855f7; }

    .field-row { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.75rem; }
    .field-name-input { flex: 1; background: transparent; border: none; font-size: 0.875rem; font-weight: 900; color: #fff; outline: none; text-transform: uppercase; letter-spacing: -0.025em; padding: 0; }
    .field-actions { display: flex; align-items: center; gap: 0.25rem; }
    .action-btn { background: transparent; border: none; padding: 0.375rem; border-radius: 0.5rem; cursor: pointer; transition: all 0.2s; color: #475569; }
    .action-btn:hover { background: rgba(255,255,255,0.05); color: #60a5fa; }
    .action-btn.red:hover { color: #f87171; background: rgba(239,68,68,0.1); }
    .action-btn.active { color: #60a5fa; }

    .field-controls { display: flex; align-items: center; gap: 0.5rem; }
    .type-select { flex: 1; background: #1e293b; font-size: 10px; font-weight: 700; border-radius: 0.5rem; border: 1px solid rgba(255,255,255,0.1); padding: 0.5rem 0.75rem; color: #94a3b8; text-transform: uppercase; letter-spacing: 0.1em; outline: none; cursor: pointer; transition: all 0.2s; }
    .type-select:hover { border-color: rgba(59,130,246,0.5); color: #fff; }
    .type-select option { background: #0f172a; color: #f1f5f9; padding: 10px; }
    .btn-toggle { padding: 0.5rem 0.75rem; border-radius: 0.5rem; font-size: 9px; font-weight: 900; text-transform: uppercase; letter-spacing: -0.05em; border: 1px solid rgba(255,255,255,0.05); cursor: pointer; transition: all 0.2s; }
    .btn-toggle.active { background: #2563eb; color: #fff; }
    .btn-toggle.inactive { background: rgba(255,255,255,0.05); color: #475569; }

    .rules-panel { margin-top: 1rem; padding-top: 1rem; border-top: 1px solid rgba(255,255,255,0.05); }
    .rules-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 1rem; }
    .rules-label { font-size: 9px; color: #60a5fa; font-weight: 700; text-transform: uppercase; letter-spacing: 0.1em; display: flex; align-items: center; gap: 0.25rem; }
    .rules-label .material-icons-outlined { font-size: 0.75rem; }
    .btn-add-rule { background: transparent; border: none; font-size: 9px; font-weight: 900; color: #34d399; cursor: pointer; text-transform: uppercase; letter-spacing: 0.1em; }
    .btn-add-rule:hover { color: #6ee7b7; }

    .rule-item { background: rgba(0,0,0,0.6); padding: 0.75rem; border-radius: 0.75rem; border: 1px solid rgba(255,255,255,0.05); margin-bottom: 0.5rem; box-shadow: inset 0 2px 4px 0 rgba(0, 0, 0, 0.06); }
    .rule-row { display: flex; align-items: center; justify-content: space-between; padding-bottom: 0.5rem; border-bottom: 1px solid rgba(255,255,255,0.05); margin-bottom: 0.5rem; }
    .rule-type { background: transparent; border: none; font-size: 9px; font-weight: 900; color: #93c5fd; text-transform: uppercase; outline: none; }
    .rule-input { width: 100%; background: rgba(0,0,0,0.4); border: 1px solid rgba(255,255,255,0.05); border-radius: 0.5rem; padding: 0.5rem 0.75rem; font-size: 10px; color: #cbd5e1; font-family: monospace; box-sizing: border-box; }
    .rule-error { width: 100%; background: transparent; border: none; font-size: 9px; color: #64748b; font-style: italic; font-weight: 500; outline: none; margin-top: 0.25rem; }

    .rel-header { display: flex; align-items: center; gap: 0.25rem; font-size: 10px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; color: #a78bfa; }
    .rel-header .material-icons-outlined { font-size: 1rem; transform: rotate(90deg) scaleX(-1); }
    .rel-select { width: 100%; background: #1e293b; font-size: 10px; font-weight: 900; text-transform: uppercase; letter-spacing: 0.1em; border-radius: 0.5rem; border: 1px solid rgba(255,255,255,0.1); padding: 0.625rem 0.75rem; color: #fff; outline: none; margin-bottom: 0.75rem; cursor: pointer; }
    .rel-select option { background: #0f172a; color: #f1f5f9; }
    .rel-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }
    .rel-type-select { background: #1e293b; font-size: 9px; font-weight: 900; border-radius: 0.5rem; border: 1px solid rgba(255,255,255,0.1); padding: 0.5rem; color: #c084fc; outline: none; text-transform: uppercase; cursor: pointer; }
    .rel-type-select option { background: #0f172a; color: #f1f5f9; }
    .rel-alias { background: #1e293b; font-size: 9px; font-family: monospace; border-radius: 0.5rem; border: 1px solid rgba(255,255,255,0.1); padding: 0.5rem 0.75rem; color: #fff; outline: none; text-transform: uppercase; }

    .animate-fadeIn { animation: fadeIn 0.3s ease-out; }
    .animate-pulse { animation: pulse 2s infinite; }
    .animate-slideDown { animation: slideDown 0.2s ease-out; }

    @keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }
    @keyframes slideDown { from { opacity: 0; height: 0; } to { opacity: 1; height: auto; } }

    .text-glow { text-shadow: 0 0 8px rgba(59, 130, 246, 0.4); }
    .box-glow-blue { box-shadow: 0 0 20px rgba(59, 130, 246, 0.15); }

    .spin { animation: spin 1s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
  `],
  template: `
    <div class="designer-container">
      <!-- Toolbar -->
      <header class="toolbar">
        <div class="toolbar-left">
          <div class="brand">
            <div class="brand-icon-wrap">
              <span class="material-icons-outlined brand-icon text-glow">schema</span>
            </div>
            <div>
              <h2 class="brand-title">Entity Architect</h2>
              <div class="brand-sub">v4.1.0 // {{ projectId | slice:0:8 }}</div>
            </div>
          </div>
          <div class="v-divider"></div>
          <div class="btn-group box-glow-blue">
            <button (click)="addEntity()" class="tool-btn blue">
              <span class="material-icons-outlined">add_box</span>
              New Entity
            </button>
            <button (click)="openAiSchemaDesigner()" class="tool-btn violet">
              <span class="material-icons-outlined">auto_awesome</span>
              AI Generate
            </button>
            <button [routerLink]="['/projects', projectId, 'security']" class="tool-btn purple">
              <span class="material-icons-outlined">admin_panel_settings</span>
              Security
            </button>
            <button [routerLink]="['/projects', projectId, 'pages']" class="tool-btn blue">
              <span class="material-icons-outlined">dashboard_customize</span>
              Pages
            </button>
            <button [routerLink]="['/projects', projectId, 'enums']" class="tool-btn amber">
              <span class="material-icons-outlined">list_alt</span>
              Enums
            </button>
            <button [routerLink]="['/projects', projectId, 'workflows']" class="tool-btn green">
              <span class="material-icons-outlined">account_tree</span>
              Workflows
            </button>
            
            <!-- Manual UI Toggle Button -->
            <button (click)="toggleUiMode()" class="tool-btn blue" [class.green]="usePremiumUi" style="border: 1px solid rgba(255,255,255,0.05); margin-left: 0.5rem;">
              <span class="material-icons-outlined">{{ usePremiumUi ? 'grid_view' : 'view_headline' }}</span>
              {{ usePremiumUi ? 'Switch to Legacy' : 'Switch to Premium' }}
            </button>
          </div>
        </div>

        <div class="toolbar-right">
          <button (click)="save()" class="btn-commit">
            <span class="material-icons-outlined" [class.animate-pulse]="!isPublishing">save</span>
            Commit
          </button>
          
          <div class="v-divider"></div>

          <div class="options-stack">
            <label class="opt-label blue">
              <input type="checkbox" [(ngModel)]="buildOptions.includeUI" class="opt-checkbox">
              UI
            </label>
            <label class="opt-label purple">
              <input type="checkbox" [(ngModel)]="buildOptions.enableAIEnabledDocs" class="opt-checkbox">
              AI Docs
            </label>
          </div>

          <button (click)="buildAsZip()" class="btn-export">
             <span class="material-icons-outlined">folder_zip</span>
             Export Code
          </button>

          <button (click)="exportSchemaJson()" class="btn-export" style="border-color: rgba(59,130,246,0.3);">
             <span class="material-icons-outlined" style="color: #60a5fa;">download</span>
             Export JSON
          </button>

          <button (click)="triggerSchemaJsonImport()" class="btn-export" style="border-color: rgba(16,185,129,0.3);">
             <span class="material-icons-outlined" style="color: #34d399;">upload</span>
             Import JSON
          </button>
          
          <input type="file" #schemaFileInput (change)="importSchemaJson($event)" accept=".json" style="display: none;">

          <button (click)="publish()" [disabled]="isPublishing" class="btn-deploy">
             <span class="material-icons-outlined" [class.spin]="isPublishing">{{ isPublishing ? 'sync' : 'bolt' }}</span>
             {{ isPublishing ? 'Deploying...' : 'Build & Deploy' }}
          </button>
        </div>
      </header>

      <div class="viewport">
        <!-- Canvas (Center) -->
        <div #canvasContainer class="canvas-container" [class.fullscreen]="isFullScreen">
           <!-- Floating Toolbox (Left) inside Canvas so it displays in Full Screen -->
           <div class="floating-toolbox animate-fadeIn" *ngIf="usePremiumUi">
             <div class="toolbox-inner">
               <button class="toolbox-btn blue" title="Select Tool">
                  <span class="material-icons-outlined">near_me</span>
               </button>
               <button (click)="addEntity()" class="toolbox-btn emerald" title="Entity Tool">
                  <span class="material-icons-outlined">rectangle</span>
               </button>
               <button class="toolbox-btn purple" title="Relation Tool">
                  <span class="material-icons-outlined">timeline</span>
               </button>
               <div class="tool-sep"></div>
               <button class="toolbox-btn" title="Pan Mode (Drag Background)" style="color:#60a5fa">
                  <span class="material-icons-outlined" style="font-size:1.1rem">pan_tool</span>
               </button>
             </div>
           </div>

           <div id="konva-holder"></div>
           
           <!-- Zoom Controls -->
           <div class="zoom-controls" [style.right.px]="isFullScreen ? 24 : (sidebarWidth + 24)">
              <button (click)="zoomOut()" class="zoom-btn" title="Zoom Out"><span class="material-icons-outlined" style="font-size:14px">remove</span></button>
              <div class="zoom-val">{{ zoomPercentage }}%</div>
              <button (click)="zoomIn()" class="zoom-btn" title="Zoom In"><span class="material-icons-outlined" style="font-size:14px">add</span></button>
              <div style="width: 1px; background: rgba(255,255,255,0.1); margin: 0 0.25rem;"></div>
              <button (click)="toggleFullScreen()" class="zoom-btn" title="Toggle Full Screen">
                <span class="material-icons-outlined" style="font-size:14px">
                  {{ isFullScreen ? 'fullscreen_exit' : 'fullscreen' }}
                </span>
              </button>
           </div>

           <div class="canvas-footer">
              Platform Canvas // Accelerated Graphics Ready
           </div>
        </div>

        <!-- Property Panel (Right) -->
        <aside class="prop-panel" [style.width.px]="sidebarWidth">
          <div class="prop-header" style="justify-content: space-between;">
            <div style="display: flex; align-items: center;">
              <span class="material-icons-outlined prop-header-icon text-glow animate-pulse">tune</span>
              <span class="prop-header-title">Property Inspector</span>
            </div>
            <button (click)="toggleSidebar()" class="action-btn" style="color: #60a5fa;" title="Expand/Collapse Sidebar">
              <span class="material-icons-outlined">
                {{ isSidebarExpanded ? 'keyboard_double_arrow_right' : 'keyboard_double_arrow_left' }}
              </span>
            </button>
          </div>

          <div class="prop-body">
            
            <div *ngIf="!selectedNode" class="prop-empty">
               <div class="empty-icon-wrap">
                 <span class="material-icons-outlined empty-icon">fingerprint</span>
               </div>
               <p class="empty-text">Select an entity<br>on the canvas to proceed</p>
            </div>

            <div *ngIf="selectedNode" class="animate-fadeIn">
              <!-- Identity -->
              <div class="prop-section">
                <label class="section-label">Core Identification</label>
                <div class="input-wrap" style="margin-bottom: 0.75rem;">
                   <input type="text" [(ngModel)]="selectedNode.name" (ngModelChange)="redrawCanvas()" class="text-input" placeholder="Entity Name">
                   <span class="material-icons-outlined input-icon" style="font-size:18px">edit</span>
                </div>
                
                <button (click)="openAiEntityDesigner()" class="btn-add purple animate-pulse" style="width: 100%; padding: 0.625rem 1rem; display: flex; justify-content: center; align-items: center; gap: 0.5rem; border-radius: 12px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.05em; font-size: 9px; box-shadow: 0 4px 12px rgba(168, 85, 247, 0.15); border: none;">
                  <span class="material-icons-outlined" style="font-size: 1rem;">auto_awesome</span>
                  AI Assist Entity
                </button>
                <button (click)="generateCrudForms()" class="btn-add blue" style="width: 100%; padding: 0.625rem 1rem; display: flex; justify-content: center; align-items: center; gap: 0.5rem; border-radius: 12px; font-weight: 800; text-transform: uppercase; letter-spacing: 0.05em; font-size: 9px; box-shadow: 0 4px 12px rgba(59, 130, 246, 0.15); border: none; margin-top: 0.5rem;">
                  <span class="material-icons-outlined" style="font-size: 1rem;">dynamic_form</span>
                  Generate Forms
                </button>
              </div>

              <!-- Fields -->
              <div class="prop-section">
                <div class="flex-row-sb">
                   <label class="section-label slate">Atomic Properties</label>
                   <button (click)="addField()" class="btn-add">
                      <span class="material-icons-outlined" style="font-size:12px">add</span>
                      Inject Field
                   </button>
                </div>
                
                <div class="field-stack">
                   <div *ngFor="let field of selectedNode.fields; let i = index" class="field-card">
                      <div class="field-accent" [class.purple]="usePremiumUi"></div>
                      
                      <div class="field-row">
                        <input type="text" [(ngModel)]="field.name" (ngModelChange)="redrawCanvas()" class="field-name-input" placeholder="PROPERTY_NAME">
                        
                        <div class="field-actions">
                            <button (click)="field.isRulesOpen = !field.isRulesOpen" [class.active]="field.rules?.length > 0" class="action-btn" title="Validation Rules">
                              <span class="material-icons-outlined" style="font-size:16px">verified</span>
                            </button>
                            <button (click)="removeField(i)" class="action-btn red">
                              <span class="material-icons-outlined" style="font-size:16px">delete_sweep</span>
                            </button>
                        </div>
                      </div>
                      
                      <div class="field-controls">
                        <select [(ngModel)]="field.type" (ngModelChange)="redrawCanvas()" class="type-select">
                          <option value="string">String</option>
                          <option value="int">Integer</option>
                          <option value="guid">Guid</option>
                          <option value="datetime">DateTime</option>
                          <option value="decimal">Decimal</option>
                          <option value="bool">Boolean</option>
                        </select>
                        <button (click)="field.isRequired = !field.isRequired; redrawCanvas()" 
                                [class.active]="field.isRequired" 
                                [class.inactive]="!field.isRequired"
                                class="btn-toggle">
                           Mandatory
                        </button>
                      </div>

                      <!-- Rules -->
                      <div *ngIf="field.isRulesOpen" class="rules-panel animate-slideDown">
                          <div class="rules-header">
                               <span class="rules-label"><span class="material-icons-outlined">shield</span> Guards</span>
                               <button (click)="addRule(field)" class="btn-add-rule">+ New Guard</button>
                          </div>
                          <div class="rule-stack">
                              <div *ngFor="let rule of field.rules; let ri = index" class="rule-item">
                                  <div class="rule-row">
                                      <select [(ngModel)]="rule.type" (ngModelChange)="redrawCanvas()" class="rule-type">
                                          <option value="Regex">Pattern</option>
                                          <option value="Range">Limit</option>
                                          <option value="Email">Mail</option>
                                          <option value="Phone">Tel</option>
                                      </select>
                                      <button (click)="removeRule(field, ri)" style="background:none;border:none;color:#475569;cursor:pointer">
                                          <span class="material-icons-outlined" style="font-size:16px">remove_circle_outline</span>
                                      </button>
                                  </div>
                                  <div *ngIf="rule.type === 'Regex' || rule.type === 'Range'">
                                      <input type="text" [(ngModel)]="rule.value" (ngModelChange)="redrawCanvas()" placeholder="Definition..." class="rule-input">
                                  </div>
                                  <input type="text" [(ngModel)]="rule.errorMessage" (ngModelChange)="redrawCanvas()" placeholder="Fault message..." class="rule-error">
                              </div>
                          </div>
                      </div>
                   </div>
                </div>
              </div>

              <!-- Relationships -->
              <div class="prop-section">
                <div class="flex-row-sb">
                   <label class="section-label purple">Semantic Links</label>
                   <button (click)="addRelation()" class="btn-add purple">
                      <span class="material-icons-outlined" style="font-size:12px">link</span>
                      Attach Link
                   </button>
                </div>

                <div class="relation-stack">
                   <div *ngFor="let rel of selectedNode.relations; let i = index" class="field-card">
                      <div class="field-accent purple"></div>
                      
                      <div class="field-row">
                        <div class="rel-header">
                            <span class="material-icons-outlined">shortcut</span>
                            <span>Direct To</span>
                        </div>
                        <button (click)="removeRelation(i)" class="action-btn red">
                            <span class="material-icons-outlined" style="font-size:16px">close</span>
                        </button>
                      </div>
                      
                      <select [(ngModel)]="rel.targetEntity" (ngModelChange)="redrawCanvas()" class="rel-select">
                         <option value="" disabled selected>Target Domain</option>
                         <option *ngFor="let target of entities" [value]="target.name">{{ target.name | uppercase }}</option>
                      </select>

                      <div class="rel-grid">
                          <select [ngModel]="rel.type" (ngModelChange)="rel.type = +$event; redrawCanvas()" class="rel-type-select">
                            <option [value]="0">ONE TO MANY</option>
                            <option [value]="1">MANY TO ONE</option>
                            <option [value]="2">MANY TO MANY</option>
                          </select>
                          <input type="text" [(ngModel)]="rel.navPropName" (ngModelChange)="redrawCanvas()" placeholder="ALIAS" class="rel-alias">
                      </div>
                   </div>
                </div>
              </div>

            </div>
          </div>
        </aside>
      </div>
    </div>

    <!-- AI Generate Modal -->
    <app-ai-generate-modal
      *ngIf="showAiModal"
      [title]="aiModalTitle"
      [subtitle]="aiModalSubtitle"
      [placeholder]="aiModalPlaceholder"
      [mode]="aiModalMode"
      [projectId]="projectId!"
      [currentEntity]="selectedNode"
      (accepted)="handleAiModalAccept($event)"
      (cancel)="showAiModal = false">
    </app-ai-generate-modal>
  `
})
export class EntityDesigner implements AfterViewInit, OnDestroy {
  @ViewChild('canvasContainer') canvasContainer!: ElementRef;
  @ViewChild('schemaFileInput') schemaFileInput!: ElementRef;

  projectId: string | null = null;
  stage!: Konva.Stage;
  layer!: Konva.Layer;
  entities: any[] = [];
  selectedNode: any = null;
  resizeHandler = this.onResize.bind(this);
  isPublishing = false;
  showAiModal = false;
  aiModalTitle = '✨ Generate Entities with AI';
  aiModalSubtitle = 'Describe your domain in plain language';
  aiModalPlaceholder = 'e.g., An e-commerce system with products, categories, orders, customers and reviews';
  aiModalMode: 'schema' | 'entity-designer' = 'schema';
  buildOptions = {
    includeUI: true,
    enableAIEnabledDocs: true,
    standaloneAPI: false
  };

  // UI Modes & Zoom Variables
  usePremiumUi = false;
  zoomLevel = 1.0;
  zoomPercentage = 100;
  sidebarWidth = 320;
  isSidebarExpanded = false;
  isFullScreen = false;
  fullscreenChangeHandler = this.onFullscreenChange.bind(this);

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

      // Add wheel listener for mouse wheel zoom (Premium UI only)
      this.stage.on('wheel', (e) => {
        if (!this.usePremiumUi) return;
        e.evt.preventDefault();

        const scaleBy = 1.05;
        const oldScale = this.stage.scaleX();
        const pointer = this.stage.getPointerPosition();
        if (!pointer) return;

        const mousePointTo = {
          x: (pointer.x - this.stage.x()) / oldScale,
          y: (pointer.y - this.stage.y()) / oldScale,
        };

        const direction = e.evt.deltaY > 0 ? -1 : 1;
        const newScale = direction > 0 ? oldScale * scaleBy : oldScale / scaleBy;

        if (newScale < 0.2 || newScale > 3.0) return;

        this.stage.scale({ x: newScale, y: newScale });

        const newPos = {
          x: pointer.x - mousePointTo.x * newScale,
          y: pointer.y - mousePointTo.y * newScale,
        };
        this.stage.position(newPos);
        
        this.zoomLevel = newScale;
        this.zoomPercentage = Math.round(newScale * 100);
        this.stage.batchDraw();
      });

      globalThis.addEventListener('resize', this.resizeHandler);
      document.addEventListener('fullscreenchange', this.fullscreenChangeHandler);
      document.addEventListener('webkitfullscreenchange', this.fullscreenChangeHandler);
      
      this.loadEntities();
    }, 100);
  }

  toggleUiMode() {
    this.usePremiumUi = !this.usePremiumUi;
    this.redrawCanvas();
  }

  toggleSidebar() {
    this.isSidebarExpanded = !this.isSidebarExpanded;
    this.sidebarWidth = this.isSidebarExpanded ? 480 : 320;
  }

  toggleFullScreen() {
    if (!this.canvasContainer) return;
    const element = this.canvasContainer.nativeElement;

    if (!document.fullscreenElement) {
      if (element.requestFullscreen) {
        element.requestFullscreen();
      } else if ((element as any).webkitRequestFullscreen) {
        (element as any).webkitRequestFullscreen();
      }
    } else {
      if (document.exitFullscreen) {
        document.exitFullscreen();
      } else if ((document as any).webkitExitFullscreen) {
        (document as any).webkitExitFullscreen();
      }
    }
  }

  onFullscreenChange() {
    this.isFullScreen = !!document.fullscreenElement || !!(document as any).webkitFullscreenElement;
    setTimeout(() => {
      this.onResize();
    }, 150);
  }

  zoomIn() {
    this.setZoom(this.zoomLevel + 0.1);
  }

  zoomOut() {
    this.setZoom(Math.max(0.2, this.zoomLevel - 0.1));
  }

  setZoom(value: number) {
    this.zoomLevel = value;
    this.zoomPercentage = Math.round(value * 100);
    if (this.stage) {
      this.stage.scale({ x: value, y: value });
      this.layer.batchDraw();
    }
  }

  loadEntities() {
    if (!this.projectId) return;
    this.api.getEntities(this.projectId).subscribe({
      next: (artifacts) => {
        if (!artifacts || artifacts.length === 0) {
          this.loadMocks();
          return;
        }
        this.entities = artifacts.map(art => {
          try {
            const meta = JSON.parse(art.content);
            meta._artifactId = art.id;
            if (!meta.fields) meta.fields = [];
            if (!meta.relations) meta.relations = [];
            if (!meta.events) meta.events = { onCreate: true, onUpdate: true, onDelete: true };
            return meta;
          } catch (e) {
            console.error('Failed to parse entity artifact', e);
            return null;
          }
        }).filter(x => x !== null);

        this.redrawCanvas();
      },
      error: () => this.loadMocks()
    });
  }

  loadMocks() {
    this.entities = [
      {
        name: 'Patient',
        fields: [
          { name: 'Id', type: 'guid', isRequired: true },
          { name: 'FullName', type: 'string', isRequired: true },
          { name: 'DateOfBirth', type: 'datetime', isRequired: false }
        ],
        relations: [],
        x: 100, y: 150
      },
      {
        name: 'Appointment',
        fields: [
          { name: 'Id', type: 'guid', isRequired: true },
          { name: 'ScheduledTime', type: 'datetime', isRequired: true }
        ],
        relations: [
          { targetEntity: 'Patient', type: 1, navPropName: 'Patient' }
        ],
        x: 400, y: 150
      }
    ];
    this.redrawCanvas();
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
      x: 150,
      y: 150
    };
    this.entities.push(newMetadata);
    this.selectedNode = newMetadata;
    this.redrawCanvas();
  }

  openAiSchemaDesigner() {
    this.aiModalTitle = '✨ Generate Entities with AI';
    this.aiModalSubtitle = 'Describe your domain in plain language';
    this.aiModalPlaceholder = 'e.g., An e-commerce system with products, categories, orders, customers and reviews';
    this.aiModalMode = 'schema';
    this.showAiModal = true;
  }

  openAiEntityDesigner() {
    if (!this.selectedNode) return;
    this.aiModalTitle = `✨ Design ${this.selectedNode.name} with AI`;
    this.aiModalSubtitle = 'Propose updates to this entity or suggest related entities';
    this.aiModalPlaceholder = `e.g., Add residential address fields and also create a related Order history entity...`;
    this.aiModalMode = 'entity-designer';
    this.showAiModal = true;
  }

  handleAiModalAccept(generated: any) {
    if (this.aiModalMode === 'entity-designer') {
      this.acceptEntityDesign(generated);
    } else {
      this.acceptGeneratedEntities(generated);
    }
  }

  acceptGeneratedEntities(generated: any) {
    this.showAiModal = false;
    const list = Array.isArray(generated) ? generated : [generated];
    let xOffset = 100;
    for (const entity of list) {
      if (!entity.relations) entity.relations = [];
      if (!entity.events) entity.events = { onCreate: true, onUpdate: true, onDelete: true };
      entity.x = xOffset;
      entity.y = 120;
      xOffset += 240;
      this.entities.push(entity);
      if (this.projectId) {
        this.api.createEntity(this.projectId, entity).subscribe();
      }
    }
    this.selectedNode = list[list.length - 1];
    this.redrawCanvas();
  }

  acceptEntityDesign(result: any) {
    this.showAiModal = false;
    if (!result) return;

    // 1. Update current entity in place
    if (result.updatedEntity && this.selectedNode) {
      this.selectedNode.name = result.updatedEntity.name;
      this.selectedNode.fields = result.updatedEntity.fields || [];
      this.selectedNode.relations = result.updatedEntity.relations || [];
      if (this.projectId) {
        this.api.createEntity(this.projectId, this.selectedNode).subscribe();
      }
    }

    // 2. Spawn and save new entities visually placed nearby
    if (result.newEntities && result.newEntities.length > 0) {
      let xOffset = (this.selectedNode?.x || 100) + 260;
      let yOffset = this.selectedNode?.y || 120;
      for (const newEntity of result.newEntities) {
        newEntity.x = xOffset;
        newEntity.y = yOffset;
        xOffset += 240;
        if (xOffset > 1000) {
          xOffset = 100;
          yOffset += 200;
        }
        if (!newEntity.relations) newEntity.relations = [];
        if (!newEntity.events) newEntity.events = { onCreate: true, onUpdate: true, onDelete: true };
        this.entities.push(newEntity);
        if (this.projectId) {
          this.api.createEntity(this.projectId, newEntity).subscribe();
        }
      }
    }

    this.redrawCanvas();
  }

  redrawCanvas() {
    if (!this.layer) return;

    // Clear any active polling sync intervals on children to prevent leaks
    this.layer.getChildren().forEach(child => {
      const intervalId = (child as any).syncInterval;
      if (intervalId) {
        clearInterval(intervalId);
      }
    });

    this.layer.destroyChildren();

    // 1. Render all Entity Nodes
    this.entities.forEach(meta => this.renderEntity(meta));

    // 2. Render all Relationship Lines
    this.drawRelationships();
  }

  renderEntity(metadata: any) {
    const isPremium = this.usePremiumUi;
    const nodeWidth = isPremium ? 220 : 200;
    
    // Dynamic node height calculation based on fields
    const headerHeight = isPremium ? 35 : 30;
    const fieldHeight = 22;
    const fieldsCount = metadata.fields?.length || 0;
    const nodeHeight = isPremium 
      ? headerHeight + (fieldsCount * fieldHeight) + 15 
      : 70;

    const entityNode = new Konva.Group({
      x: metadata.x || 100,
      y: metadata.y || 100,
      draggable: true
    });

    const rect = new Konva.Rect({
      width: nodeWidth,
      height: nodeHeight,
      fill: '#0f172a',
      stroke: isPremium ? '#8b5cf6' : '#3b82f6',
      strokeWidth: 2,
      cornerRadius: 16,
      shadowBlur: 20,
      shadowColor: isPremium ? '#8b5cf6' : '#3b82f6',
      shadowOpacity: 0.1
    });

    const header = new Konva.Rect({
      width: nodeWidth,
      height: headerHeight,
      fill: isPremium ? '#8b5cf6' : '#3b82f6',
      opacity: 0.1,
      cornerRadius: [16, 16, 0, 0]
    });

    const title = new Konva.Text({
      text: metadata.name.toUpperCase(),
      fontSize: 10,
      fontFamily: 'Inter, sans-serif',
      fill: 'white',
      width: nodeWidth,
      padding: 12,
      align: 'center',
      fontStyle: 'bold',
      letterSpacing: 2
    });

    entityNode.add(rect);
    entityNode.add(header);
    entityNode.add(title);

    if (isPremium) {
      // Premium UI: Render visual field list with data types and key icons
      if (metadata.fields) {
        metadata.fields.forEach((field: any, index: number) => {
          const isRequired = field.isRequired;
          const isPK = field.name.toLowerCase() === 'id';
          const badge = isPK ? '🔑' : (isRequired ? '🔸' : '🔹');
          
          const fieldText = new Konva.Text({
            text: `${badge} ${field.name} : ${field.type}`,
            fontSize: 9,
            fontFamily: 'JetBrains Mono, monospace',
            fill: '#cbd5e1',
            x: 15,
            y: headerHeight + (index * fieldHeight),
            width: nodeWidth - 30,
            align: 'left'
          });
          entityNode.add(fieldText);
        });
      }
    } else {
      // Legacy UI: Render simplified field summary
      const countText = new Konva.Text({
        text: (metadata.fields?.length || 0) + ' FIELDS',
        fontSize: 8,
        fontFamily: 'JetBrains Mono, monospace',
        fill: '#475569',
        width: nodeWidth,
        y: 40,
        align: 'center',
        fontStyle: 'bold'
      });
      entityNode.add(countText);
    }

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

    entityNode.on('dragmove', () => {
      metadata.x = entityNode.x();
      metadata.y = entityNode.y();
      this.drawRelationships();
    });

    entityNode.on('dragend', () => {
      metadata.x = entityNode.x();
      metadata.y = entityNode.y();
      this.drawRelationships();
    });

    this.layer.add(entityNode);
    this.layer.batchDraw();
  }

  drawRelationships() {
    // Clear old connector lines
    this.layer.find('.relationship-line').forEach(line => line.destroy());

    // Only draw relationships in Premium Mode
    if (!this.usePremiumUi) {
      this.layer.batchDraw();
      return;
    }

    this.entities.forEach(sourceEntity => {
      if (!sourceEntity.relations) return;

      sourceEntity.relations.forEach((rel: any) => {
        if (!rel.targetEntity) return;

        const targetEntity = this.entities.find(e => e.name === rel.targetEntity);
        if (!targetEntity) return;

        // Constants matching premium node size dimensions
        const sourceWidth = 220;
        const targetWidth = 220;

        const sourceFieldsCount = sourceEntity.fields?.length || 0;
        const sourceHeight = 35 + (sourceFieldsCount * 22) + 15;

        const targetFieldsCount = targetEntity.fields?.length || 0;
        const targetHeight = 35 + (targetFieldsCount * 22) + 15;

        // Calculate center coordinates of entities
        const x1 = (sourceEntity.x || 100) + sourceWidth / 2;
        const y1 = (sourceEntity.y || 100) + sourceHeight / 2;

        const x2 = (targetEntity.x || 100) + targetWidth / 2;
        const y2 = (targetEntity.y || 100) + targetHeight / 2;

        // Render line connector
        const line = new Konva.Line({
          points: [x1, y1, x2, y2],
          stroke: '#8b5cf6',
          strokeWidth: 2,
          name: 'relationship-line',
          dash: [5, 5],
          opacity: 0.7
        });

        // Add visual link alias label
        const midX = (x1 + x2) / 2;
        const midY = (y1 + y2) / 2;

        const label = new Konva.Text({
          text: rel.navPropName ? rel.navPropName.toUpperCase() : 'RELATION',
          fontSize: 8,
          fontFamily: 'JetBrains Mono, monospace',
          fill: '#a78bfa',
          x: midX - 60,
          y: midY - 6,
          width: 120,
          align: 'center',
          name: 'relationship-line',
          fontStyle: 'bold'
        });

        this.layer.add(line);
        this.layer.add(label);

        // Send line elements to background behind entity group cards
        line.moveToBottom();
        label.moveToBottom();
      });
    });

    this.layer.batchDraw();
  }

  addField() {
    if (this.selectedNode) {
      if (!this.selectedNode.fields) this.selectedNode.fields = [];
      this.selectedNode.fields.push({ name: 'NEW_PROPERTY', type: 'string', isRequired: false });
      this.redrawCanvas();
    }
  }

  removeField(index: number) {
    if (this.selectedNode) {
      this.selectedNode.fields.splice(index, 1);
      this.redrawCanvas();
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
      this.redrawCanvas();
    }
  }

  removeRelation(index: number) {
    if (this.selectedNode?.relations) {
      this.selectedNode.relations.splice(index, 1);
      this.redrawCanvas();
    }
  }

  addRule(field: any) {
    if (!field.rules) field.rules = [];
    field.rules.push({
      type: 'Regex',
      value: '',
      errorMessage: 'Invalid property value'
    });
    this.redrawCanvas();
  }

  removeRule(field: any, index: number) {
    if (field.rules) {
      field.rules.splice(index, 1);
      this.redrawCanvas();
    }
  }

  generateCrudForms() {
    if (!this.projectId || !this.selectedNode) {
      alert('Select an entity to generate forms.');
      return;
    }

    const entity = this.selectedNode;
    const entityName = entity.name;

    // Build fields list
    // Build fields list helper
    const buildFormFields = (mode: string) => {
      return (entity.fields || []).map((field: any, index: number) => {
        const label = field.name
          .replace(/([A-Z])/g, ' $1')
          .trim();

        let placeholder = `Enter ${label}`;
        if (field.type === 'datetime') {
          placeholder = 'Pick a date';
        } else if (field.type === 'bool') {
          placeholder = '';
        }

        let validationPattern = '';
        if (field.rules && field.rules.length > 0) {
          const regexRule = field.rules.find((r: any) => r.type === 'Regex');
          if (regexRule) {
            validationPattern = regexRule.value;
          }
        }

        const isDescription = field.name.toLowerCase().includes('desc') || field.name.toLowerCase().includes('description');

        return {
          Name: field.name,
          Type: field.type,
          Label: label,
          Placeholder: placeholder,
          Tooltip: '',
          DefaultValue: '',
          IsRequired: !!field.isRequired,
          ValidationPattern: validationPattern,
          EnumReference: '',
          ElementId: `${entityName.toLowerCase()}_${mode.toLowerCase()}_${field.name.toLowerCase()}`,
          CssClass: '',
          Style: '',
          GridSpan: isDescription ? 2 : 1,
          Order: index
        };
      });
    };

    const formFieldsCreate = buildFormFields('create');
    const formFieldsEdit = buildFormFields('edit');

    // Build sections based on count: if <= 8, 1 section; if > 8, split.
    const fieldNames = (entity.fields || []).map((f: any) => f.name);
    const sections: any[] = [];
    if (fieldNames.length <= 8) {
      sections.push({
        Title: 'General Information',
        FieldNames: fieldNames,
        Order: 0
      });
    } else {
      sections.push({
        Title: 'Primary Details',
        FieldNames: fieldNames.slice(0, 8),
        Order: 0
      });
      sections.push({
        Title: 'Additional Details',
        FieldNames: fieldNames.slice(8),
        Order: 1
      });
    }

    // Build Create Form Dto
    const createFormMetadata = {
      Name: `${entityName} Create Form`,
      EntityTarget: entityName,
      Layout: 'Vertical',
      Sections: sections,
      Fields: formFieldsCreate,
      Context: {
        Mode: 0, // Create (numeric enum representation)
        ParentEntityId: null,
        AdditionalData: {}
      }
    };

    // Build Edit Form Dto
    const editFormMetadata = {
      Name: `${entityName} Edit Form`,
      EntityTarget: entityName,
      Layout: 'Vertical',
      Sections: JSON.parse(JSON.stringify(sections)),
      Fields: formFieldsEdit,
      Context: {
        Mode: 1, // Edit (numeric enum representation)
        ParentEntityId: null,
        AdditionalData: {}
      }
    };

    // Call api.createForm for both
    this.api.createForm(this.projectId, createFormMetadata).subscribe({
      next: () => {
        this.api.createForm(this.projectId!, editFormMetadata).subscribe({
          next: () => alert(`CRUD Forms for ${entityName} generated successfully!`),
          error: (err) => alert(`Failed to generate Edit Form: ${err.message}`)
        });
      },
      error: (err) => alert(`Failed to generate Create Form: ${err.message}`)
    });
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
    this.api.buildProject(this.projectId, this.buildOptions).subscribe({
      next: (blob) => {
        const url = globalThis.URL.createObjectURL(blob);
        const a = globalThis.document.createElement('a');
        a.href = url;
        const suffix = this.buildOptions.includeUI ? 'Full_Stack' : 'API_Only';
        a.download = `${suffix}_Export.zip`;
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

  exportSchemaJson() {
    if (!this.projectId) return;
    this.api.exportEntities(this.projectId).subscribe({
      next: (blob) => {
        const url = globalThis.URL.createObjectURL(blob);
        const a = globalThis.document.createElement('a');
        a.href = url;
        a.download = `entities-export-${this.projectId}.json`;
        a.click();
        globalThis.URL.revokeObjectURL(url);
      },
      error: (err) => alert('Failed to export entities schema: ' + err.message)
    });
  }

  triggerSchemaJsonImport() {
    if (this.schemaFileInput) {
      this.schemaFileInput.nativeElement.click();
    }
  }

  importSchemaJson(event: any) {
    const file = event.target.files?.[0];
    if (!file || !this.projectId) return;

    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const json = JSON.parse(e.target?.result as string);
        const entitiesArray = Array.isArray(json) ? json : [json];
        
        const executeImport = (confirmedOverwrites: string[]) => {
          this.api.importEntities(this.projectId!, { entities: entitiesArray, confirmedOverwrites }).subscribe({
            next: (res) => {
              if (res.requiresConfirmation) {
                const conflictsList = res.conflicts.join(', ');
                const confirmOverwrite = confirm(
                  `The following entities already exist:\n\n👉  ${conflictsList}\n\nDo you want to proceed and overwrite these entities?`
                );
                if (confirmOverwrite) {
                  executeImport(res.conflicts);
                }
              } else {
                alert(`Import completed successfully! New: ${res.importedCount}, Updated: ${res.updatedCount}`);
                this.loadEntities();
              }
            },
            error: (err) => alert('Import sequence failed: ' + (err.error?.message || err.message))
          });
        };

        executeImport([]);

      } catch (err) {
        alert('Failed to parse JSON file: ' + err);
      }
    };
    reader.readAsText(file);
    event.target.value = '';
  }

  ngOnDestroy() {
    globalThis.removeEventListener('resize', this.resizeHandler);
    document.removeEventListener('fullscreenchange', this.fullscreenChangeHandler);
    document.removeEventListener('webkitfullscreenchange', this.fullscreenChangeHandler);
    if (this.stage) this.stage.destroy();
  }
}

