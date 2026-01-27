import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api';

interface GridDimension {
  colStart: number;
  colSpan: number;
  rowStart: number;
  rowSpan: number;
}

interface WidgetMetadata {
  id: string;
  type: string;
  properties: { [key: string]: any }; // Generic Key-Value Store
  layout: {
    desktop: GridDimension;
    tablet: GridDimension;
    mobile: GridDimension;
  };
  bindings: {
    provider: string; // Entity, API
    source: string;   // Entity Name
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
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="flex flex-col h-[calc(100vh-64px)] bg-[#0B1120] text-slate-200 overflow-hidden font-sans">
      <!-- Toolbar -->
      <div class="h-14 border-b border-white/5 flex items-center justify-between px-6 bg-slate-900/80 backdrop-blur-xl z-20 shadow-2xl">
        <div class="flex items-center space-x-6">
          <div class="flex items-center space-x-3">
             <div class="p-2 bg-blue-500/10 rounded-xl shadow-inner shadow-blue-500/20">
                <span class="material-icons-outlined text-blue-400 text-lg text-glow text-blue-500">dashboard_customize</span>
             </div>
             <div>
                <h2 class="text-sm font-black text-white tracking-widest uppercase">Page Architect</h2>
                <div class="text-[9px] text-slate-500 font-mono tracking-tighter opacity-60">v1.2.0 // {{ projectId | slice:0:8 }}</div>
             </div>
          </div>
          <div class="w-px h-8 bg-white/10 mx-2"></div>
          <div class="flex items-center space-x-4">
             <nav class="flex space-x-1 p-1 bg-black/20 rounded-xl">
                <button [routerLink]="['/projects', projectId, 'designer']" class="px-3 py-1.5 rounded-lg text-xs font-bold text-slate-500 hover:text-white transition-all">Entities</button>
                <div class="bg-blue-600/20 text-blue-400 px-3 py-1.5 rounded-lg text-xs font-bold border border-blue-500/20 shadow-lg shadow-blue-500/10">Pages</div>
                <button [routerLink]="['/projects', projectId, 'enums']" class="px-3 py-1.5 rounded-lg text-xs font-bold text-slate-500 hover:text-white transition-all">Enums</button>
                <button [routerLink]="['/projects', projectId, 'widgets']" class="px-3 py-1.5 rounded-lg text-xs font-bold text-slate-500 hover:text-white transition-all">Widgets</button>
             </nav>
          </div>
        </div>
        <div class="flex items-center space-x-3">
          <button (click)="savePage()" class="group flex items-center space-x-2 text-xs text-slate-400 hover:text-white px-4 py-2 rounded-lg transition-all hover:bg-white/5 active:scale-95">
            <span class="material-icons-outlined text-lg group-hover:animate-pulse">save</span>
            <span class="font-bold uppercase tracking-widest text-[10px]">Save Design</span>
          </button>
          
          <div class="h-6 w-px bg-white/10 mx-2"></div>

          <button class="group flex items-center space-x-2 text-xs bg-slate-800 hover:bg-slate-700 text-slate-200 border border-white/10 px-4 py-2 rounded-xl transition-all shadow-xl active:scale-95">
             <span class="material-icons-outlined text-lg text-amber-400">visibility</span>
             <span class="font-bold tracking-tight">Preview</span>
          </button>

          <button class="group flex items-center space-x-2 text-xs bg-blue-600 hover:bg-blue-500 text-white px-6 py-2 rounded-xl shadow-lg shadow-blue-900/50 transition-all active:scale-95">
             <span class="material-icons-outlined text-lg group-hover:scale-110 transition-transform">bolt</span>
             <span class="font-black uppercase tracking-widest italic">Generate UI</span>
          </button>
        </div>
      </div>

      <div class="flex flex-1 overflow-hidden relative">
        <!-- Widget Palette (Left) -->
        <aside class="w-64 glass-dark border-r border-white/5 flex flex-col shadow-2xl z-20">
          <div class="h-12 border-b border-white/5 flex items-center px-4 bg-white/5">
             <span class="text-[10px] font-black uppercase tracking-[0.2em] text-slate-400">Component Palette</span>
          </div>
          <div class="p-4 space-y-4 overflow-y-auto scrollbar-hide">
              <div *ngFor="let cat of widgetCategories" class="space-y-2">
                 <h4 class="text-[9px] font-black text-slate-600 uppercase tracking-widest ml-1">{{cat.name}}</h4>
                 <div class="grid grid-cols-2 gap-2">
                    <div *ngFor="let w of cat.widgets" 
                         (click)="addWidget(w.type)"
                         class="flex flex-col items-center justify-center p-3 bg-white/[0.03] border border-white/5 rounded-xl hover:bg-blue-600/10 hover:border-blue-500/30 transition-all cursor-grab active:cursor-grabbing group">
                       <span class="material-icons-outlined text-2xl text-slate-500 group-hover:text-blue-400 mb-2">{{w.icon}}</span>
                       <span class="text-[9px] font-bold text-slate-400 group-hover:text-slate-200 text-center uppercase tracking-tighter">{{w.label}}</span>
                    </div>
                 </div>
              </div>
          </div>
          <div class="mt-auto p-4 border-t border-white/5 bg-black/20">
             <div class="flex items-center space-x-3 opacity-40 hover:opacity-100 transition-opacity cursor-pointer">
                <span class="material-icons-outlined text-lg">help_outline</span>
                <span class="text-[10px] font-bold uppercase tracking-widest">Layout Guide</span>
             </div>
          </div>
        </aside>

        <!-- Canvas (Center) -->
        <main class="flex-1 bg-[#0B1120] relative overflow-y-auto p-8 scrollbar-thin scrollbar-thumb-slate-800 scrollbar-track-transparent">
           <!-- The 12-Column Grid -->
           <div class="dashboard-grid min-h-full border border-dashed border-white/[0.03] rounded-3xl relative p-4 transition-all"
                [class.grid-active]="isDragging">
             
             <!-- Empty Grid Slots for visual orientation -->
             <div *ngFor="let i of [1,2,3,4,5,6,7,8,9,10,11,12]" 
                  class="absolute top-0 bottom-0 border-l border-white/[0.02]"
                  [style.left.%]="(i-1) * (100/12)"></div>

             <!-- Render Widgets -->
             <div *ngFor="let widget of widgets" 
                  (click)="selectWidget(widget)"
                  [ngClass]="getWidgetClasses(widget)"
                  class="relative group glass-dark p-6 rounded-3xl border border-white/10 hover:border-blue-500/40 transition-all hover:shadow-2xl hover:shadow-blue-500/10 cursor-pointer animate-fadeIn"
                  [class.ring-2]="selectedWidget?.id === widget.id"
                  [class.ring-blue-500]="selectedWidget?.id === widget.id">
                
                <div class="flex justify-between items-start mb-4">
                  <div class="flex items-center space-x-3">
                    <div class="p-2 rounded-xl" [ngClass]="getThemeClass(widget.properties['theme'])">
                      <span class="material-icons-outlined text-lg">{{widget.properties['icon']}}</span>
                    </div>
                    <div>
                      <h3 class="text-xs font-black uppercase tracking-widest text-white">{{widget.properties['title']}}</h3>
                      <p class="text-[9px] text-slate-500 font-bold opacity-60">{{widget.properties['subTitle']}}</p>
                    </div>
                  </div>
                  <button (click)="removeWidget($event, widget.id)" class="opacity-0 group-hover:opacity-100 transition-opacity text-slate-600 hover:text-red-500">
                    <span class="material-icons-outlined text-sm">close</span>
                  </button>
                </div>
                
                <!-- Mockup Content based on type -->
                <div class="mt-4 flex flex-col items-center justify-center py-6 border-t border-white/5 opacity-30 group-hover:opacity-60 transition-opacity">
                   <div *ngIf="widget.type === 'Hero'" class="w-full text-center space-y-2">
                       <div class="text-xl font-black text-white glow-blue mb-1 italic">HERO SECTION</div>
                       <div class="h-1 bg-blue-500/20 w-1/4 mx-auto rounded-full"></div>
                   </div>
                   <div *ngIf="widget.type === 'RichText'" class="w-full space-y-2 opacity-50">
                       <div class="h-1.5 bg-white/10 rounded w-full"></div>
                       <div class="h-1.5 bg-white/10 rounded w-full"></div>
                       <div class="h-1.5 bg-white/10 rounded w-2/3"></div>
                   </div>
                   <div *ngIf="widget.type === 'ContactForm'" class="w-full space-y-3">
                       <div class="h-6 bg-white/5 rounded-lg border border-white/10 w-full"></div>
                       <div class="h-6 bg-white/5 rounded-lg border border-white/10 w-full"></div>
                       <div class="h-10 bg-blue-600/30 rounded-lg w-full flex items-center justify-center text-[8px] font-black uppercase">Send Message</div>
                   </div>
                   <div *ngIf="widget.type === 'Map'" class="w-full h-24 bg-blue-500/5 rounded-2xl flex items-center justify-center border border-dashed border-white/10">
                       <span class="material-icons-outlined text-3xl opacity-20">travel_explore</span>
                   </div>
                   <div *ngIf="widget.type === 'Image'" class="w-full h-24 bg-white/5 rounded-2xl flex items-center justify-center overflow-hidden border border-white/5">
                       <span class="material-icons-outlined text-3xl opacity-10">landscape</span>
                   </div>

                   <div *ngIf="widget.type === 'StatCard'" class="text-3xl font-black text-white glow-blue">942</div>
                   <div *ngIf="widget.type === 'Chart'" class="w-full h-20 bg-blue-500/10 rounded-lg flex items-end p-2 space-x-1">
                      <div class="flex-1 bg-blue-500/40 rounded-t h-1/2"></div>
                      <div class="flex-1 bg-blue-500/40 rounded-t h-3/4"></div>
                      <div class="flex-1 bg-blue-500/40 rounded-t h-1/3"></div>
                      <div class="flex-1 bg-blue-500/40 rounded-t h-5/6"></div>
                   </div>
                   <div *ngIf="widget.type === 'Calendar'" class="material-icons-outlined text-4xl">calendar_view_month</div>
                   <div *ngIf="widget.type === 'DataGrid'" class="w-full space-y-2">
                       <div class="h-2 bg-white/10 rounded w-full"></div>
                       <div class="h-2 bg-white/10 rounded w-3/4"></div>
                       <div class="h-2 bg-white/10 rounded w-5/6"></div>
                   </div>
                   <span class="text-[8px] font-black uppercase mt-4 tracking-[0.3em] font-mono">{{widget.bindings.source}} SOURCE</span>
                </div>

                <!-- Resize / Move Handles (Future) -->
                <div class="absolute bottom-2 right-2 opacity-0 group-hover:opacity-40"><span class="material-icons-outlined text-xs">open_in_full</span></div>
             </div>

             <!-- Placeholder when empty -->
             <div *ngIf="widgets.length === 0" class="col-span-12 h-96 flex flex-col items-center justify-center text-slate-700">
                <div class="w-24 h-24 rounded-full border-4 border-dashed border-white/[0.03] flex items-center justify-center mb-6">
                   <span class="material-icons-outlined text-5xl">add_to_photos</span>
                </div>
                <p class="text-xs font-black uppercase tracking-[0.4em] text-center">Drag and Drop widgets<br>from the palette to start</p>
             </div>
           </div>
        </main>

        <!-- Inspector (Right) -->
        <aside class="w-80 glass-dark border-l border-white/5 flex flex-col shadow-2xl z-20 overflow-hidden">
          <div class="h-14 border-b border-white/5 flex items-center px-6 bg-blue-600/5 backdrop-blur-md">
            <span class="material-icons-outlined text-blue-400 mr-2 text-lg text-glow">tune</span>
            <span class="font-black text-[10px] uppercase tracking-widest text-slate-300">Layout Inspector</span>
          </div>

          <div class="flex-1 overflow-y-auto p-6 scrollbar-thin scrollbar-thumb-slate-700 scrollbar-track-transparent">
            <div *ngIf="!selectedWidget" class="space-y-8 animate-fadeIn">
               <div class="flex flex-col items-center justify-center py-6 text-slate-600 opacity-40 border-b border-white/5 mb-6">
                 <div class="w-16 h-16 rounded-full border-2 border-dashed border-white/5 flex items-center justify-center mb-4">
                   <span class="material-icons-outlined text-3xl">settings_applications</span>
                 </div>
                 <p class="text-[9px] font-black uppercase tracking-widest text-center">Global Page Settings</p>
               </div>

               <!-- Routing Section -->
               <section class="space-y-4">
                  <label class="text-[10px] uppercase tracking-[0.2em] font-black text-amber-500/60 block">Route Propagation</label>
                  <div class="space-y-4 bg-amber-500/5 p-4 rounded-2xl border border-amber-500/10">
                    <div class="space-y-1">
                      <span class="text-[8px] font-bold text-amber-900/60 uppercase tracking-tighter">URL Path</span>
                      <div class="flex items-center bg-slate-900/50 border border-white/10 rounded-lg px-3 py-2">
                         <span class="text-[10px] text-slate-600 mr-1">/</span>
                         <input type="text" [(ngModel)]="pageSettings.route" class="w-full bg-transparent border-none text-xs text-white focus:outline-none placeholder-slate-700" placeholder="contact-us">
                      </div>
                    </div>
                    <div class="space-y-3 pt-2">
                       <div class="flex items-center justify-between">
                          <span class="text-[9px] font-bold text-slate-400 uppercase">Include in Navigation</span>
                          <div class="w-8 h-4 bg-blue-600/40 rounded-full relative cursor-pointer" (click)="pageSettings.showInMenu = !pageSettings.showInMenu">
                             <div class="absolute top-1 w-2 h-2 rounded-full bg-white transition-all shadow-glow-blue" [style.left.px]="pageSettings.showInMenu ? 20 : 4"></div>
                          </div>
                       </div>
                       <div *ngIf="pageSettings.showInMenu" class="space-y-1 animate-slideDown">
                          <span class="text-[8px] font-bold text-slate-500 uppercase tracking-tighter">Menu Label</span>
                          <input type="text" [(ngModel)]="pageSettings.menuLabel" class="w-full bg-slate-900/50 border border-white/10 rounded-lg px-3 py-2 text-xs text-white focus:border-blue-500 outline-none">
                       </div>
                    </div>
                  </div>
               </section>

               <!-- SEO & Identification -->
               <section class="space-y-4">
                  <label class="text-[10px] uppercase tracking-[0.2em] font-black text-blue-500/60 block">Identity & Discovery</label>
                  <div class="space-y-4 bg-blue-500/5 p-4 rounded-2xl border border-blue-500/10">
                    <div class="space-y-1">
                      <span class="text-[8px] font-bold text-blue-900/60 uppercase tracking-tighter">Browser Tab Title</span>
                      <input type="text" [(ngModel)]="pageSettings.title" class="w-full bg-slate-900/50 border border-white/10 rounded-lg px-3 py-2 text-xs text-white focus:border-blue-500 outline-none">
                    </div>
                    <div class="space-y-1">
                      <span class="text-[8px] font-bold text-blue-900/60 uppercase tracking-tighter">Access Context</span>
                      <select [(ngModel)]="pageSettings.accessLevel" class="w-full bg-slate-900/50 border border-white/10 rounded-lg px-3 py-2 text-xs text-slate-400 outline-none">
                        <option value="Public">Public (Landing Page)</option>
                        <option value="Authenticated">Authenticated Users</option>
                        <option value="RoleBased">Role-Engine (Dynamic)</option>
                      </select>
                    </div>
                  </div>
               </section>

               <div class="p-6 border-t border-white/5 text-center">
                  <p class="text-[8px] text-slate-600 font-bold uppercase tracking-[0.2em]">Select a widget on the left<br>to configure its properties</p>
               </div>
            </div>

            <div *ngIf="selectedWidget" class="space-y-8 animate-fadeIn">
                <!-- Visuals -->
                <section class="space-y-4">
                  <label class="text-[10px] uppercase tracking-[0.2em] font-black text-blue-500/60 block">Design Config</label>
                  <div class="space-y-4 bg-black/20 p-4 rounded-2xl border border-white/5">
                    <!-- Generic Property Look (Iterate for Custom? For now fixed for standard) -->
                    <div class="space-y-1">
                      <span class="text-[8px] font-bold text-slate-500 uppercase tracking-tighter">Widget Title</span>
                      <input type="text" [(ngModel)]="selectedWidget.properties['title']" class="w-full bg-slate-900/50 border border-white/10 rounded-lg px-3 py-2 text-xs text-white focus:border-blue-500 outline-none">
                    </div>
                    <div class="space-y-1">
                      <span class="text-[8px] font-bold text-slate-500 uppercase tracking-tighter">Visual Theme</span>
                      <select [(ngModel)]="selectedWidget.properties['theme']" class="w-full bg-slate-900/50 border border-white/10 rounded-lg px-3 py-2 text-xs text-slate-400 outline-none">
                        <option value="primary">Enterprise Blue</option>
                        <option value="success">Success Green</option>
                        <option value="danger">Warning Red</option>
                        <option value="glass">Glass Morphic</option>
                      </select>
                    </div>
                    <div class="p-2 border-t border-white/5 mt-2">
                        <p class="text-[9px] text-slate-500">TODO: Dynamic Property Iterator for custom properties.</p>
                    </div>
                  </div>
                </section>

                <!-- Responsive Layout -->
                <section class="space-y-4">
                  <label class="text-[10px] uppercase tracking-[0.2em] font-black text-purple-500/60 block">Grid Distribution</label>
                  <div class="grid grid-cols-2 gap-3">
                    <div class="space-y-1">
                      <span class="text-[8px] font-bold text-slate-500 uppercase tracking-tighter">Desktop Span (1-12)</span>
                      <input type="number" [(ngModel)]="selectedWidget.layout.desktop.colSpan" min="1" max="12" class="w-full bg-slate-900/50 border border-white/10 rounded-lg px-3 py-2 text-xs text-white focus:border-blue-500 outline-none">
                    </div>
                    <div class="space-y-1">
                      <span class="text-[8px] font-bold text-slate-500 uppercase tracking-tighter">Height Units</span>
                      <input type="number" [(ngModel)]="selectedWidget.layout.desktop.rowSpan" min="1" max="10" class="w-full bg-slate-900/50 border border-white/10 rounded-lg px-3 py-2 text-xs text-white focus:border-blue-500 outline-none">
                    </div>
                  </div>
                </section>

                <!-- Data Binding -->
                <section class="space-y-4">
                  <label class="text-[10px] uppercase tracking-[0.2em] font-black text-emerald-500/60 block">Data Propagation</label>
                  <div class="space-y-4 bg-emerald-500/5 p-4 rounded-2xl border border-emerald-500/10">
                    <div class="grid grid-cols-2 gap-2">
                       <button (click)="selectedWidget.bindings.provider = 'Entity'" 
                               [class.bg-emerald-600]="selectedWidget.bindings.provider === 'Entity'"
                               class="py-1.5 rounded bg-black/40 text-[8px] font-black uppercase transition-all">Raw Entity</button>
                       <button (click)="selectedWidget.bindings.provider = 'API'" 
                               [class.bg-emerald-600]="selectedWidget.bindings.provider === 'API'"
                               class="py-1.5 rounded bg-black/40 text-[8px] font-black uppercase transition-all">API / Query</button>
                    </div>

                    <div class="space-y-1">
                      <span class="text-[8px] font-bold text-emerald-900/60 uppercase tracking-tighter">Source Identity</span>
                      <input type="text" [(ngModel)]="selectedWidget.bindings.source" class="w-full bg-slate-900/50 border border-white/10 rounded-lg px-3 py-2 text-xs text-slate-400 outline-none" placeholder="EntityName or URL">
                    </div>

                    <div class="space-y-1" *ngIf="['StatCard', 'Chart'].includes(selectedWidget.type)">
                       <span class="text-[8px] font-bold text-emerald-900/60 uppercase tracking-tighter">Aggregate function</span>
                       <div class="flex space-x-1">
                          <button *ngFor="let agg of ['count', 'sum', 'avg', 'list']" 
                                  (click)="selectedWidget.bindings.params.aggregate = agg"
                                  [class.bg-emerald-600]="selectedWidget.bindings.params.aggregate === agg"
                                  class="flex-1 py-1.5 rounded bg-black/40 text-[8px] font-black uppercase transition-all">{{agg}}</button>
                       </div>
                    </div>
                  </div>
                </section>
            </div>
          </div>
        </aside>
      </div>
    </div>
  `,
  styles: [`
    .grid-active {
        background-image: 
        radial-gradient(circle, rgba(255,255,255,0.05) 1px, transparent 1px);
        background-size: 32px 32px;
    }
    .text-glow {
      filter: drop-shadow(0 0 8px rgba(59, 130, 246, 0.4));
    }
    .glow-blue {
        text-shadow: 0 0 20px rgba(59, 130, 246, 0.5);
    }
    input::-webkit-outer-spin-button, input::-webkit-inner-spin-button { -webkit-appearance: none; margin: 0; }
  `]
})
export class PageDesigner implements OnInit {
  projectId: string | null = null;
  widgets: WidgetMetadata[] = [];
  selectedWidget: WidgetMetadata | null = null;
  isDragging = false;

  pageSettings = {
    route: 'home',
    title: 'Clinic Landing Page',
    accessLevel: 'Public',
    showInMenu: true,
    menuLabel: 'Home'
  };

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
        { type: 'List', icon: 'view_list', label: 'Action Feed' },
      ]
    },
    {
      name: 'Content',
      widgets: [
        { type: 'Hero', icon: 'view_quilt', label: 'Hero Section' },
        { type: 'RichText', icon: 'subject', label: 'Rich Text' },
        { type: 'Image', icon: 'image', label: 'Image Box' },
      ]
    },
    {
      name: 'Interactive',
      widgets: [
        { type: 'ContactForm', icon: 'contact_mail', label: 'Contact Us' },
        { type: 'Map', icon: 'map', label: 'Location Map' },
      ]
    }
  ];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: ApiService
  ) {
    this.projectId = this.route.snapshot.paramMap.get('projectId');
  }

  ngOnInit() {
    // Load existing page if available, otherwise start with a blank canvas
    this.loadSample();
  }

  loadSample() {
    this.widgets = [
      {
        id: 'w1',
        type: 'Hero',
        properties: { title: 'Health Horizon Clinic', icon: 'medical_services', theme: 'primary', subTitle: 'Excellence in Precision Medicine' },
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
        properties: { title: 'Specialists', icon: 'groups', theme: 'glass', subTitle: 'Available Today' },
        layout: {
          desktop: { colStart: 0, colSpan: 4, rowStart: 3, rowSpan: 1 },
          tablet: { colStart: 0, colSpan: 4, rowStart: 3, rowSpan: 1 },
          mobile: { colStart: 0, colSpan: 12, rowStart: 2, rowSpan: 1 }
        },
        bindings: { provider: 'Entity', source: 'Doctor', params: { aggregate: 'count' } }
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
        subTitle: type === 'Hero' ? 'Welcome to our platform' : ''
      },
      layout: {
        desktop: { colStart: 0, colSpan: type === 'Hero' ? 12 : 4, rowStart: 0, rowSpan: 2 },
        tablet: { colStart: 0, colSpan: 6, rowStart: 0, rowSpan: 2 },
        mobile: { colStart: 0, colSpan: 12, rowStart: 0, rowSpan: 2 }
      },
      bindings: {
        provider: 'Entity',
        source: 'Appointment',
        params: { aggregate: 'count', pagination: { enabled: type === 'DataGrid', pageSize: 25 } }
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
      case 'ContactForm': return 'contact_mail';
      case 'Calendar': return 'calendar_today';
      case 'Map': return 'map';
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
      [`col-start-${widget.layout.desktop.colStart + 1}`]: true,
    };
  }

  getThemeClass(theme: string) {
    switch (theme) {
      case 'primary': return 'bg-blue-600/20 text-blue-400';
      case 'success': return 'bg-emerald-600/20 text-emerald-400';
      case 'danger': return 'bg-red-600/20 text-red-400';
      case 'glass': return 'bg-white/10 text-white backdrop-blur-md';
      default: return 'bg-slate-600/20 text-slate-400';
    }
  }

  savePage() {
    alert('Page configuration synchronized with metadata repository.');
  }
}
