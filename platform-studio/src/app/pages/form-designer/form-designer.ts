import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../services/api';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-form-designer',
    standalone: true,
    imports: [CommonModule, FormsModule],
    template: `
    <div class="flex h-screen bg-white">
      <!-- 1. Palette: Available Fields -->
      <div class="w-64 border-r bg-gray-50 flex flex-col">
        <div class="p-4 border-b bg-gray-100">
          <h2 class="font-bold text-gray-700">Available Fields</h2>
          <p class="text-xs text-gray-500">From entity: {{ metadata?.entityTarget }}</p>
        </div>
        <div class="p-2 overflow-y-auto flex-1">
          <div *ngFor="let field of availableFields" class="p-2 bg-white border mb-2 rounded shadow-sm cursor-pointer hover:bg-indigo-50 flex justify-between items-center">
            <span class="text-sm font-medium">{{ field.name }}</span>
            <button (click)="addFieldToActiveSection(field)" class="text-xs bg-indigo-100 text-indigo-700 px-2 py-1 rounded hover:bg-indigo-200">+</button>
          </div>
          <div *ngIf="availableFields.length === 0" class="text-sm text-gray-400 p-4 text-center">
             No unused fields available.
          </div>
        </div>
      </div>

      <!-- 2. Canvas: Form Layout -->
      <div class="flex-1 flex flex-col bg-gray-100">
        <div class="h-14 border-b bg-white flex items-center justify-between px-4 shadow-sm">
           <div class="flex items-center gap-4">
              <span class="font-bold text-lg">{{ metadata?.name }}</span>
              <select [(ngModel)]="metadata.layout" class="border rounded px-2 py-1 text-sm bg-gray-50">
                 <option value="Vertical">Vertical</option>
                 <option value="Horizontal">Horizontal</option>
                 <option value="Inline">Inline</option>
              </select>
           </div>
           <button (click)="save()" class="bg-indigo-600 text-white px-4 py-2 rounded hover:bg-indigo-700 font-medium text-sm">Save Form</button>
        </div>

        <div class="flex-1 overflow-y-auto p-8">
           <div class="max-w-3xl mx-auto bg-white min-h-[500px] shadow rounded-lg p-8 border">
              
              <!-- Sections -->
              <div *ngFor="let section of metadata.sections; let sIdx = index" class="mb-8 border border-dashed border-gray-300 rounded p-4 relative hover:border-indigo-300 bg-gray-50/50" (click)="activeSectionIndex = sIdx">
                 <div class="flex justify-between mb-4">
                    <input [(ngModel)]="section.title" class="font-bold text-lg bg-transparent border-b border-transparent hover:border-gray-300 focus:outline-none" />
                    <button (click)="removeSection(sIdx)" class="text-red-500 text-xs hover:underline">Remove Section</button>
                 </div>
                 
                 <!-- Fields in Section -->
                 <div class="space-y-3 min-h-[50px]" [class.border-indigo-500]="activeSectionIndex === sIdx" [class.border]="activeSectionIndex === sIdx">
                    <div *ngFor="let fieldName of section.fieldNames; let fIdx = index" (click)="selectField(fieldName, $event)" 
                         class="p-3 bg-white border rounded flex items-center justify-between cursor-pointer hover:shadow-md transition"
                         [class.ring-2]="selectedField?.name === fieldName" [class.ring-indigo-500]="selectedField?.name === fieldName">
                       
                       <div class="flex items-center gap-3">
                          <span class="text-gray-400 text-xs">::</span>
                          <span class="font-medium text-sm">{{ getFieldLabel(fieldName) }}</span>
                          <span class="text-xs text-gray-400">({{ getFieldType(fieldName) }})</span>
                       </div>
                       <button (click)="removeField(section, fIdx, fieldName, $event)" class="text-gray-400 hover:text-red-500">×</button>
                    </div>
                    
                    <div *ngIf="section.fieldNames.length === 0" class="text-center text-gray-400 text-sm py-4 border border-dashed rounded bg-white">
                       Select this section and add fields from the left palette.
                    </div>
                 </div>
              </div>

              <button (click)="addSection()" class="w-full py-3 border-2 border-dashed border-gray-300 rounded text-gray-500 font-medium hover:border-indigo-500 hover:text-indigo-600 transition">
                 + Add New Section
              </button>

           </div>
        </div>
      </div>

      <!-- 3. Inspector: Field Properties -->
      <div class="w-72 border-l bg-white flex flex-col">
         <div class="p-4 border-b bg-gray-50">
            <h2 class="font-bold text-gray-700">Properties</h2>
         </div>
         <div class="p-4 overflow-y-auto flex-1">
            <div *ngIf="selectedField; else noSelection">
               <div class="mb-4">
                  <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Field Name</label>
                  <input [value]="selectedField.name" disabled class="w-full bg-gray-100 border rounded px-2 py-1 text-sm text-gray-600" />
               </div>

               <div class="mb-4">
                  <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Label</label>
                  <input [(ngModel)]="selectedField.label" class="w-full border rounded px-2 py-1 text-sm" />
               </div>

               <div class="mb-4">
                  <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Placeholder</label>
                  <input [(ngModel)]="selectedField.placeholder" class="w-full border rounded px-2 py-1 text-sm" />
               </div>

               <div class="mb-4 flex items-center gap-2">
                  <input type="checkbox" [(ngModel)]="selectedField.isRequired" id="req" />
                  <label for="req" class="text-sm text-gray-700">Required</label>
               </div>

               <div class="mb-4">
                  <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Validation Regex</label>
                  <input [(ngModel)]="selectedField.validationPattern" class="w-full border rounded px-2 py-1 text-sm font-mono" />
               </div>
               
               <div class="mb-4">
                  <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Type</label>
                   <input [value]="selectedField.type" disabled class="w-full bg-gray-100 border rounded px-2 py-1 text-sm text-gray-600" />
               </div>

            </div>
            <ng-template #noSelection>
               <div class="text-center text-gray-400 mt-10">
                  Select a field on the canvas to edit its properties.
               </div>
            </ng-template>
         </div>
      </div>
    </div>
  `
})
export class FormDesignerComponent implements OnInit {
    projectId = '';
    formId = '';
    metadata: any = { sections: [], fields: [] };
    entityDefinition: any = null;

    availableFields: any[] = [];
    activeSectionIndex = 0;
    selectedField: any = null;

    constructor(private readonly api: ApiService, private readonly route: ActivatedRoute) { }

    ngOnInit() {
        this.route.parent?.paramMap.subscribe(p => this.projectId = p.get('id') || '');
        this.route.paramMap.subscribe(p => {
            this.formId = p.get('id') || '';
            if (this.formId) this.loadData();
        });
    }

    loadData() {
        // 1. Get Project Artifacts to find the form
        this.api.getForms(this.projectId).subscribe(forms => {
            const artifact = forms.find(x => x.id === this.formId);
            if (artifact) {
                // Parse JSON content
                this.metadata = typeof artifact.content === 'string' ? JSON.parse(artifact.content) : artifact.content;
                // Ensure defaults
                if (!this.metadata.sections) this.metadata.sections = [];
                if (!this.metadata.fields) this.metadata.fields = [];

                // 2. Load Entity Definition
                this.loadEntity(this.metadata.entityTarget);
            }
        });
    }

    loadEntity(entityName: string) {
        this.api.getEntities(this.projectId).subscribe(entities => {
            const entityArtifact = entities.find(e => {
                const cont = typeof e.content === 'string' ? JSON.parse(e.content) : e.content;
                return cont.name === entityName || e.name === entityName;
            });

            if (entityArtifact) {
                this.entityDefinition = typeof entityArtifact.content === 'string' ? JSON.parse(entityArtifact.content) : entityArtifact.content;
                this.updateAvailableFields();
            }
        });
    }

    updateAvailableFields() {
        if (!this.entityDefinition) return;

        // Get all fields from entity
        const allEntityFields = this.entityDefinition.fields || [];

        // Filter out fields already present in form metadata
        const usedFieldNames = new Set(this.metadata.fields.map((f: any) => f.name));
        this.availableFields = allEntityFields.filter((f: any) => !usedFieldNames.has(f.name));
    }

    addSection() {
        this.metadata.sections.push({
            title: 'New Section',
            fieldNames: [],
            order: this.metadata.sections.length
        });
        this.activeSectionIndex = this.metadata.sections.length - 1;
    }

    removeSection(index: number) {
        if (confirm('Remove section and return fields to palette?')) {
            const section = this.metadata.sections[index];
            // Remove references from Fields list? No, keep the configuration but they become "unused" or remove them?
            // Guide pattern: Field must be in 'Fields' list to be valid. 
            // If we remove section, we should probably remove the field instance from the FormMetadata.Fields too, to reset its config.

            section.fieldNames.forEach((name: string) => {
                const idx = this.metadata.fields.findIndex((f: any) => f.name === name);
                if (idx > -1) this.metadata.fields.splice(idx, 1);
            });

            this.metadata.sections.splice(index, 1);
            this.updateAvailableFields();
        }
    }

    addFieldToActiveSection(entityField: any) {
        if (this.metadata.sections.length === 0) this.addSection();

        const section = this.metadata.sections[this.activeSectionIndex];

        // Create FormFieldConfig
        const formField = {
            name: entityField.name,
            type: entityField.type,
            label: entityField.name, // Default label
            placeholder: '',
            isRequired: false,
            order: section.fieldNames.length
        };

        this.metadata.fields.push(formField);
        section.fieldNames.push(entityField.name);

        this.updateAvailableFields();
        this.selectedField = formField;
    }

    selectField(fieldName: string, event: Event) {
        event.stopPropagation();
        this.selectedField = this.metadata.fields.find((f: any) => f.name === fieldName);
    }

    removeField(section: any, index: number, fieldName: string, event: Event) {
        event.stopPropagation();
        section.fieldNames.splice(index, 1);

        const idx = this.metadata.fields.findIndex((f: any) => f.name === fieldName);
        if (idx > -1) this.metadata.fields.splice(idx, 1);

        this.selectedField = null;
        this.updateAvailableFields();
    }

    getFieldLabel(fieldName: string) {
        const f = this.metadata.fields.find((x: any) => x.name === fieldName);
        return f ? f.label : fieldName;
    }

    getFieldType(fieldName: string) {
        const f = this.metadata.fields.find((x: any) => x.name === fieldName);
        return f ? f.type : '?';
    }

    save() {
        this.api.updateForm(this.projectId, this.formId, this.metadata).subscribe(() => {
            alert('Form saved successfully!');
        });
    }
}
