import { Routes } from '@angular/router';
import { Dashboard } from './components/dashboard/dashboard';
import { ProjectsList } from './pages/projects-list/projects-list';
import { EntityDesigner } from './pages/entity-designer/entity-designer';
import { SecurityDesigner } from './pages/security-designer/security-designer';
import { WorkflowDesigner } from './pages/workflow-designer/workflow-designer';
import { PageDesigner } from './pages/page-designer/page-designer';
import { EnumDesigner } from './pages/enum-designer/enum-designer';

export const routes: Routes = [
    {
        path: '',
        component: Dashboard,
        children: [
            { path: 'projects', component: ProjectsList },
            { path: 'projects/:projectId/designer', component: EntityDesigner },
            { path: 'projects/:projectId/security', component: SecurityDesigner },
            { path: 'projects/:projectId/pages', component: PageDesigner },
            { path: 'projects/:projectId/enums', component: EnumDesigner },
            { path: 'projects/:projectId/workflows', component: WorkflowDesigner },
            {
                path: 'projects/:id/forms',
                loadComponent: () => import('./pages/form-designer/form-list').then(m => m.FormListComponent)
            },
            {
                path: 'projects/:id/forms/:formId',
                loadComponent: () => import('./pages/form-designer/form-designer').then(m => m.FormDesignerComponent)
            },
            { path: '', redirectTo: 'projects', pathMatch: 'full' }
        ]
    }
];
