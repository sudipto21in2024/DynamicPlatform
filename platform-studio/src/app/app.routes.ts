import { Routes } from '@angular/router';
import { Dashboard } from './components/dashboard/dashboard';
import { ProjectsList } from './pages/projects-list/projects-list';
import { EntityDesigner } from './pages/entity-designer/entity-designer';
import { SecurityDesigner } from './pages/security-designer/security-designer';
import { WorkflowDesigner } from './pages/workflow-designer/workflow-designer';
import { PageDesigner } from './pages/page-designer/page-designer';
import { EnumDesigner } from './pages/enum-designer/enum-designer';
import { ConnectorStudioComponent } from './pages/connector-studio/connector-studio';
import { AiProvidersComponent } from './pages/settings/ai-providers/ai-providers';

export const routes: Routes = [
    {
        path: '',
        component: Dashboard,
        children: [
            // ── Core ─────────────────────────────────────────────────────
            { path: 'projects', component: ProjectsList },

            // ── Project Designers ─────────────────────────────────────────
            { path: 'projects/:projectId/designer',   component: EntityDesigner },
            { path: 'projects/:projectId/security',   component: SecurityDesigner },
            { path: 'projects/:projectId/pages',      component: PageDesigner },
            { path: 'projects/:projectId/enums',      component: EnumDesigner },
            { path: 'projects/:projectId/workflows',  loadComponent: () => import('./pages/workflow-designer/workflow-dashboard').then(m => m.WorkflowDashboard) },
            { path: 'projects/:projectId/workflows/:workflowId', component: WorkflowDesigner },
            { path: 'projects/:projectId/connectors', component: ConnectorStudioComponent },

            // ── Lazy-loaded Designers ─────────────────────────────────────
            {
                path: 'projects/:id/forms',
                loadComponent: () => import('./pages/form-designer/form-list').then(m => m.FormListComponent)
            },
            {
                path: 'projects/:id/forms/:formId',
                loadComponent: () => import('./pages/form-designer/form-designer').then(m => m.FormDesignerComponent)
            },
            {
                path: 'projects/:projectId/widgets',
                loadComponent: () => import('./pages/widget-designer/widget-designer').then(m => m.WidgetDesignerComponent)
            },

            // ── Settings ──────────────────────────────────────────────────
            { path: 'settings/ai-providers', component: AiProvidersComponent },

            // ── Default ───────────────────────────────────────────────────
            { path: '', redirectTo: 'projects', pathMatch: 'full' }
        ]
    }
];
