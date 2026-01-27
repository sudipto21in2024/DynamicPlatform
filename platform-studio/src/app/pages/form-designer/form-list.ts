import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ApiService } from '../../services/api';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-form-list',
    standalone: true,
    imports: [CommonModule, RouterModule, FormsModule],
    template: `
    <div class="p-6 bg-gray-50 min-h-screen">
      <div class="flex justify-between items-center mb-6">
        <div>
           <h1 class="text-2xl font-bold text-gray-900">Form Designer</h1>
           <p class="text-gray-500">Design UI forms for your entities.</p>
        </div>
        <button (click)="openNewModal()" class="bg-indigo-600 text-white px-4 py-2 rounded shadow hover:bg-indigo-700">
          + New Form
        </button>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div *ngFor="let form of forms" class="bg-white p-6 rounded-lg shadow hover:shadow-md transition cursor-pointer border border-gray-200" (click)="editForm(form.id)">
          <div class="flex items-center justify-between mb-2">
            <h3 class="text-lg font-semibold text-gray-800">{{ form.name }}</h3>
            <span class="px-2 py-1 text-xs font-semibold rounded bg-blue-100 text-blue-800">Form</span>
          </div>
          <p class="text-sm text-gray-500">Last modified: {{ form.lastModified | date }}</p>
        </div>
      </div>

      <!-- New Form Modal -->
      <div *ngIf="showModal" class="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full flex items-center justify-center">
        <div class="bg-white p-5 border w-96 shadow-lg rounded-md">
          <div class="mt-3 text-center">
            <h3 class="text-lg leading-6 font-medium text-gray-900">Create New Form</h3>
            <div class="mt-2 px-7 py-3">
              <input [(ngModel)]="newFormName" placeholder="Form Name (e.g. PatientForm)" class="mb-3 px-3 py-2 border rounded w-full"/>
              <select [(ngModel)]="targetEntity" class="mb-3 px-3 py-2 border rounded w-full">
                 <option value="" disabled selected>Select Entity</option>
                 <option *ngFor="let entity of entities" [value]="entity.name">{{ entity.name }}</option>
              </select>
            </div>
            <div class="items-center px-4 py-3">
              <button (click)="createForm()" class="px-4 py-2 bg-indigo-500 text-white text-base font-medium rounded-md w-full shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-300">
                Create
              </button>
              <button (click)="showModal = false" class="mt-2 px-4 py-2 bg-gray-100 text-gray-700 text-base font-medium rounded-md w-full shadow-sm hover:bg-gray-200 focus:outline-none">
                Cancel
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class FormListComponent implements OnInit {
    forms: any[] = [];
    entities: any[] = [];
    projectId: string = '';
    showModal = false;
    newFormName = '';
    targetEntity = '';

    constructor(
        private readonly api: ApiService,
        private readonly route: ActivatedRoute,
        private readonly router: Router
    ) { }

    ngOnInit() {
        this.route.parent?.paramMap.subscribe(params => {
            this.projectId = params.get('id') || '';
            if (this.projectId) {
                this.loadForms();
                this.loadEntities();
            }
        });
    }

    loadForms() {
        this.api.getForms(this.projectId).subscribe(data => this.forms = data);
    }

    loadEntities() {
        this.api.getEntities(this.projectId).subscribe(data => this.entities = data);
    }

    openNewModal() {
        this.showModal = true;
        this.newFormName = '';
    }

    createForm() {
        if (!this.newFormName || !this.targetEntity) return;

        const metadata = {
            Name: this.newFormName,
            EntityTarget: this.targetEntity,
            Layout: 'Vertical',
            Sections: [],
            Fields: []
        };

        this.api.createForm(this.projectId, metadata).subscribe(res => {
            this.showModal = false;
            this.loadForms();
        });
    }

    editForm(id: string) {
        this.router.navigate([id], { relativeTo: this.route });
    }
}
