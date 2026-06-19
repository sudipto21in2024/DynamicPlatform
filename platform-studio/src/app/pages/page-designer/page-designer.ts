import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api';
import { ProjectContextService } from '../../services/project-context';

interface GridDimension {
  colStart: number;
  colSpan: number;
  rowStart: number;
  rowSpan: number;
}

interface WidgetMetadata {
  id: string;
  type: string;
  properties: { [key: string]: any };
  layout: {
    desktop: GridDimension;
    tablet: GridDimension;
    mobile: GridDimension;
  };
  bindings: {
    provider: string;
    source: string;
    params: any;
    mapping?: any;
    pagination?: {
      enabled: boolean;
      pageSize: number;
    }
  };
}

@Component({
  selector: 'app-page-designer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styles: [`
    :host { display: block; }
    .studio-container { display: flex; flex-direction: column; height: calc(100vh - 64px); background: #0b0f1a; color: #e2e8f0; font-family: 'Inter', sans-serif; overflow: hidden; }

    /* ── Toolbar ──────────────────────────────── */
    .toolbar { height: 56px; border-bottom: 1px solid rgba(255,255,255,0.05); display: flex; align-items: center; justify-content: space-between; padding: 0 1.5rem; background: rgba(15,23,42,0.8); backdrop-filter: blur(16px); z-index: 20; }
    .toolbar-left { display: flex; align-items: center; gap: 1rem; }
    .brand { display: flex; align-items: center; gap: 0.75rem; }
    .brand-icon-wrap { width: 36px; height: 36px; background: rgba(59,130,246,0.1); border-radius: 10px; display: flex; align-items: center; justify-content: center; }
    .brand-icon { color: #60a5fa; font-size: 1.25rem; }
    .brand-title { font-size: 0.85rem; font-weight: 800; color: #fff; margin: 0; }
    .brand-sub { font-size: 9px; color: #475569; font-family: 'JetBrains Mono', monospace; text-transform: uppercase; letter-spacing: 0.1em; }

    .toolbar-right { display: flex; align-items: center; gap: 0.75rem; }
    .btn-ghost { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 1rem; border-radius: 10px; border: 1px solid transparent; background: transparent; color: #94a3b8; font-size: 0.75rem; font-weight: 700; cursor: pointer; transition: all 0.2s; }
    .btn-ghost:hover { background: rgba(255,255,255,0.05); color: #fff; }
    .btn-primary { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 1.25rem; border-radius: 10px; border: none; background: #2563eb; color: #fff; font-size: 0.75rem; font-weight: 800; cursor: pointer; transition: all 0.2s; box-shadow: 0 4px 12px rgba(37,99,235,0.2); }
    .btn-primary:hover { background: #3b82f6; transform: translateY(-1px); }

    /* ── Main Layout ───────────────────────────── */
    .viewport { display: flex; flex: 1; overflow: hidden; position: relative; }
    
    .palette { width: 240px; background: rgba(15,23,42,0.4); border-right: 1px solid rgba(255,255,255,0.05); display: flex; flex-direction: column; padding: 1.25rem; gap: 1.5rem; z-index: 10; backdrop-filter: blur(10px); overflow-y: auto; }
    .palette-section { display: flex; flex-direction: column; gap: 0.75rem; }
    .palette-label { font-size: 0.65rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.2em; color: #334155; }
    .palette-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0.5rem; }
    
    .w-item { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 0.75rem; background: rgba(255,255,255,0.02); border: 1px solid rgba(255,255,255,0.05); border-radius: 12px; color: #64748b; cursor: pointer; transition: all 0.2s; }
    .w-item:hover { background: rgba(59,130,246,0.1); border-color: rgba(59,130,246,0.3); color: #60a5fa; transform: translateY(-2px); }
    .w-item .material-icons-outlined { font-size: 1.5rem; margin-bottom: 0.5rem; }
    .w-item span:not(.material-icons-outlined) { font-size: 0.65rem; font-weight: 700; text-align: center; text-transform: uppercase; letter-spacing: 0.05em; }

    .canvas-container { flex: 1; background: #0b0f1a; position: relative; overflow-y: auto; padding: 2rem; scrollbar-width: thin; scrollbar-color: #1e293b transparent; }
    .dashboard-grid { display: grid; grid-template-columns: repeat(12, 1fr); gap: 1.5rem; min-height: 100%; border: 1px dashed rgba(255,255,255,0.03); border-radius: 2rem; padding: 1.5rem; }

    .widget-box { position: relative; background: rgba(15,23,42,0.6); border: 1px solid rgba(255,255,255,0.06); border-radius: 24px; padding: 1.25rem; transition: all 0.3s; cursor: pointer; display: flex; flex-direction: column; min-height: 120px; }
    .widget-box:hover { border-color: #3b82f6; box-shadow: 0 20px 40px rgba(0,0,0,0.4); transform: translateY(-2px); }
    .widget-box.selected { border-color: #3b82f6; ring: 2px solid #3b82f6; box-shadow: 0 0 0 4px rgba(59,130,246,0.1); }

    .widget-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 1rem; }
    .widget-identity { display: flex; align-items: center; gap: 0.75rem; }
    .widget-icon-wrap { width: 32px; height: 32px; border-radius: 10px; display: flex; align-items: center; justify-content: center; }
    .widget-title { font-size: 0.75rem; font-weight: 800; color: #f1f5f9; text-transform: uppercase; letter-spacing: 0.05em; margin: 0; }
    .widget-sub { font-size: 9px; color: #475569; font-weight: 600; margin-top: 2px; }
    
    .widget-actions { opacity: 0; transition: opacity 0.2s; }
    .widget-box:hover .widget-actions { opacity: 1; }
    .delete-btn { background: transparent; border: none; color: #334155; cursor: pointer; transition: color 0.2s; }
    .delete-btn:hover { color: #f87171; }

    .widget-preview { flex: 1; border-top: 1px solid rgba(255,255,255,0.03); margin-top: 1rem; padding-top: 1rem; display: flex; flex-direction: column; align-items: center; justify-content: center; opacity: 0.4; }
    .preview-type { font-size: 0.65rem; font-weight: 900; color: #1e293b; text-transform: uppercase; letter-spacing: 0.3em; }

    .inspector { width: 320px; background: rgba(15,23,42,0.8); border-left: 1px solid rgba(255,255,255,0.05); display: flex; flex-direction: column; backdrop-filter: blur(20px); z-index: 10; }
    .inspector-header { height: 56px; border-bottom: 1px solid rgba(255,255,255,0.05); display: flex; align-items: center; padding: 0 1.25rem; gap: 0.75rem; background: rgba(59,130,246,0.03); }
    .inspector-icon { color: #60a5fa; font-size: 1.1rem; }
    .inspector-title { font-size: 0.7rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.15em; color: #cbd5e1; }

    .inspector-body { flex: 1; overflow-y: auto; padding: 1.5rem; display: flex; flex-direction: column; gap: 2rem; scrollbar-width: thin; }
    .i-section { display: flex; flex-direction: column; gap: 1rem; }
    .i-label { font-size: 0.65rem; font-weight: 900; text-transform: uppercase; letter-spacing: 0.15em; color: #3b82f6; opacity: 0.6; }
    
    .i-input-group { display: flex; flex-direction: column; gap: 0.5rem; }
    .i-field-label { font-size: 0.7rem; font-weight: 700; color: #64748b; }
    .i-input { background: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.05); border-radius: 10px; padding: 0.625rem 0.875rem; color: #fff; font-size: 0.8rem; outline: none; transition: all 0.2s; }
    .i-input:focus { border-color: #3b82f6; background: rgba(0,0,0,0.5); }
    
    .i-select { background: rgba(0,0,0,0.3); border: 1px solid rgba(255,255,255,0.05); border-radius: 10px; padding: 0.625rem 0.875rem; color: #fff; font-size: 0.8rem; outline: none; appearance: none; cursor: pointer; }
    .i-select option { background: #0f172a; color: #fff; }

    .i-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100%; opacity: 0.3; text-align: center; }
    .i-empty .material-icons-outlined { font-size: 3rem; margin-bottom: 1rem; }
    .i-empty p { font-size: 0.7rem; font-weight: 800; text-transform: uppercase; letter-spacing: 0.1em; line-height: 1.6; }

    /* Theme colors */
    .theme-primary { background: rgba(59,130,246,0.1); color: #60a5fa; }
    .theme-success { background: rgba(16,185,129,0.1); color: #34d399; }
    .theme-danger { background: rgba(239,68,68,0.1); color: #f87171; }
    .theme-glass { background: rgba(255,255,255,0.05); color: #fff; backdrop-filter: blur(10px); }

    .col-span-1 { grid-column: span 1; }
    .col-span-2 { grid-column: span 2; }
    .col-span-3 { grid-column: span 3; }
    .col-span-4 { grid-column: span 4; }
    .col-span-5 { grid-column: span 5; }
    .col-span-6 { grid-column: span 6; }
    .col-span-7 { grid-column: span 7; }
    .col-span-8 { grid-column: span 8; }
    .col-span-9 { grid-column: span 9; }
    .col-span-10 { grid-column: span 10; }
    .col-span-11 { grid-column: span 11; }
    .col-span-12 { grid-column: span 12; }
  `],
  template: `
    <div class="studio-container">
      <header class="toolbar">
        <div class="toolbar-left">
          <div class="brand">
            <div class="brand-icon-wrap">
              <span class="material-icons-outlined brand-icon">web</span>
            </div>
            <div>
              <h2 class="brand-title">Page Architect</h2>
              <div class="brand-sub">Fluid Grid v4 // {{ projectId | slice:0:8 }}</div>
            </div>
          </div>
        </div>

        <div class="toolbar-right">
          <button (click)="savePage()" class="btn-ghost">
            <span class="material-icons-outlined">save</span>
            Sync Design
          </button>
          <button class="btn-ghost">
            <span class="material-icons-outlined" style="color:#fbbf24">visibility</span>
            Preview
          </button>
          <div style="width:1px; height:20px; background:rgba(255,255,255,0.1); margin:0 0.5rem"></div>
          <button class="btn-primary">
            <span class="material-icons-outlined">bolt</span>
            Generate UI
          </button>
        </div>
      </header>

      <div class="viewport">
        <!-- Palette -->
        <aside class="palette">
          <div class="palette-section" *ngFor="let cat of widgetCategories">
            <label class="palette-label">{{cat.name}}</label>
            <div class="palette-grid">
              <div *ngFor="let w of cat.widgets" 
                   (click)="addWidget(w.type)"
                   class="w-item">
                <span class="material-icons-outlined">{{w.icon}}</span>
                <span>{{w.label}}</span>
              </div>
            </div>
          </div>
        </aside>

        <!-- Canvas -->
        <main class="canvas-container">
          <div class="dashboard-grid">
            <div *ngFor="let widget of widgets" 
                 (click)="selectWidget(widget)"
                 [class.selected]="selectedWidget?.id === widget.id"
                 [ngClass]="getWidgetClasses(widget)"
                 class="widget-box">
              
              <div class="widget-header">
                <div class="widget-identity">
                  <div class="widget-icon-wrap" [ngClass]="getThemeClass(widget.properties['theme'])">
                    <span class="material-icons-outlined" style="font-size:1.1rem">{{widget.properties['icon']}}</span>
                  </div>
                  <div>
                    <h3 class="widget-title">{{widget.properties['title']}}</h3>
                    <div class="widget-sub">{{widget.properties['subTitle']}}</div>
                  </div>
                </div>
                <div class="widget-actions">
                  <button (click)="removeWidget($event, widget.id)" class="delete-btn">
                    <span class="material-icons-outlined" style="font-size:1.1rem">close</span>
                  </button>
                </div>
              </div>

              <div class="widget-preview">
                 <span class="preview-type">{{widget.type}}</span>
                 <div style="font-size:9px; color:#334155; margin-top:4px; font-family:monospace">{{widget.bindings.source}}</div>
              </div>
            </div>

            <!-- Empty State -->
            <div *ngIf="widgets.length === 0" style="grid-column: span 12; height: 300px; display: flex; flex-direction: column; align-items: center; justify-content: center; opacity: 0.2;">
              <span class="material-icons-outlined" style="font-size:4rem; margin-bottom: 1rem;">add_to_photos</span>
              <p style="font-size: 0.8rem; font-weight: 800; text-transform: uppercase; letter-spacing: 0.3em;">Drag widgets to begin</p>
            </div>
          </div>
        </main>

        <!-- Inspector -->
        <aside class="inspector">
          <div class="inspector-header">
            <span class="material-icons-outlined inspector-icon">tune</span>
            <span class="inspector-title">Properties</span>
          </div>

          <div class="inspector-body">
            <div *ngIf="!selectedWidget" class="i-empty">
               <span class="material-icons-outlined">settings_applications</span>
               <p>Select a widget<br>to configure its properties</p>
            </div>

            <div *ngIf="selectedWidget" style="display:flex; flex-direction:column; gap:2rem;">
              <section class="i-section">
                <label class="i-label">Identity</label>
                <div class="i-input-group">
                  <label class="i-field-label">Header Title</label>
                  <input type="text" [(ngModel)]="selectedWidget.properties['title']" class="i-input">
                </div>
                <div class="i-input-group">
                  <label class="i-field-label">Visual Theme</label>
                  <select [(ngModel)]="selectedWidget.properties['theme']" class="i-select">
                    <option value="primary">Enterprise Blue</option>
                    <option value="success">Success Green</option>
                    <option value="danger">Warning Red</option>
                    <option value="glass">Glass Morphic</option>
                  </select>
                </div>
              </section>

              <section class="i-section">
                <label class="i-label">Layout</label>
                <div class="i-input-group">
                  <label class="i-field-label">Grid Span (1-12)</label>
                  <input type="number" [(ngModel)]="selectedWidget.layout.desktop.colSpan" min="1" max="12" class="i-input">
                </div>
              </section>

              <section class="i-section">
                <label class="i-label">Data Binding</label>
                <div class="i-input-group">
                  <label class="i-field-label">Source Context</label>
                  <input type="text" [(ngModel)]="selectedWidget.bindings.source" class="i-input" placeholder="Entity or API endpoint">
                </div>
              </section>
            </div>
          </div>
        </aside>
      </div>
    </div>
  `
})
export class PageDesigner implements OnInit {
  projectId: string | null = null;
  widgets: WidgetMetadata[] = [];
  selectedWidget: WidgetMetadata | null = null;

  widgetCategories = [
    {
      name: 'Summaries',
      widgets: [
        { type: 'StatCard', icon: 'filter_1', label: 'Stat Card' },
        { type: 'StatCard', icon: 'trending_up', label: 'Trend Box' },
      ]
    },
    {
      name: 'Visuals',
      widgets: [
        { type: 'Chart', icon: 'bar_chart', label: 'Analytics' },
        { type: 'Calendar', icon: 'calendar_today', label: 'Timeline' },
      ]
    },
    {
      name: 'Lists',
      widgets: [
        { type: 'DataGrid', icon: 'table_rows', label: 'Data Grid' },
      ]
    },
    {
      name: 'Content',
      widgets: [
        { type: 'Hero', icon: 'view_quilt', label: 'Hero Section' },
        { type: 'RichText', icon: 'subject', label: 'Rich Text' },
        { type: 'Image', icon: 'image', label: 'Image Box' },
      ]
    }
  ];

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
    this.loadSample();
  }

  loadSample() {
    this.widgets = [
      {
        id: 'w1',
        type: 'Hero',
        properties: { title: 'Clinic Portal', icon: 'medical_services', theme: 'primary', subTitle: 'Excellence in Care' },
        layout: {
          desktop: { colStart: 0, colSpan: 12, rowStart: 0, rowSpan: 3 },
          tablet: { colStart: 0, colSpan: 12, rowStart: 0, rowSpan: 3 },
          mobile: { colStart: 0, colSpan: 12, rowStart: 0, rowSpan: 2 }
        },
        bindings: { provider: 'Static', source: 'System', params: {} }
      },
      {
        id: 'w2',
        type: 'StatCard',
        properties: { title: 'Total Patients', icon: 'groups', theme: 'glass', subTitle: 'Active database' },
        layout: {
          desktop: { colStart: 0, colSpan: 4, rowStart: 3, rowSpan: 1 },
          tablet: { colStart: 0, colSpan: 4, rowStart: 3, rowSpan: 1 },
          mobile: { colStart: 0, colSpan: 12, rowStart: 2, rowSpan: 1 }
        },
        bindings: { provider: 'Entity', source: 'Patient', params: { aggregate: 'count' } }
      }
    ];
  }

  addWidget(type: string) {
    const newWidget: WidgetMetadata = {
      id: Math.random().toString(36).slice(2, 11),
      type: type,
      properties: {
        title: 'New ' + type,
        icon: this.getIconForType(type),
        theme: 'primary',
        subTitle: ''
      },
      layout: {
        desktop: { colStart: 0, colSpan: type === 'Hero' ? 12 : 4, rowStart: 0, rowSpan: 2 },
        tablet: { colStart: 0, colSpan: 6, rowStart: 0, rowSpan: 2 },
        mobile: { colStart: 0, colSpan: 12, rowStart: 0, rowSpan: 2 }
      },
      bindings: {
        provider: 'Entity',
        source: 'EntityName',
        params: { aggregate: 'count' }
      }
    };
    this.widgets.push(newWidget);
    this.selectWidget(newWidget);
  }

  getIconForType(type: string): string {
    switch (type) {
      case 'Chart': return 'show_chart';
      case 'DataGrid': return 'list_alt';
      case 'Hero': return 'view_quilt';
      case 'RichText': return 'subject';
      case 'Calendar': return 'calendar_today';
      case 'Image': return 'image';
      default: return 'widgets';
    }
  }

  selectWidget(widget: WidgetMetadata) {
    this.selectedWidget = widget;
  }

  removeWidget(event: Event, id: string) {
    event.stopPropagation();
    this.widgets = this.widgets.filter(w => w.id !== id);
    if (this.selectedWidget?.id === id) this.selectedWidget = null;
  }

  getWidgetClasses(widget: WidgetMetadata) {
    return {
      [`col-span-${widget.layout.desktop.colSpan}`]: true,
    };
  }

  getThemeClass(theme: string) {
    switch (theme) {
      case 'primary': return 'theme-primary';
      case 'success': return 'theme-success';
      case 'danger': return 'theme-danger';
      case 'glass': return 'theme-glass';
      default: return '';
    }
  }

  savePage() {
    alert('Page design committed.');
  }
}
