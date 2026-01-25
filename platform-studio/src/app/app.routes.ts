import { Routes } from '@angular/router';
import { Dashboard } from './components/dashboard/dashboard';
import { ProjectsList } from './pages/projects-list/projects-list';
import { EntityDesigner } from './pages/entity-designer/entity-designer';
import { SecurityDesigner } from './pages/security-designer/security-designer';

export const routes: Routes = [
    {
        path: '',
        component: Dashboard,
        children: [
            { path: 'projects', component: ProjectsList },
            { path: 'projects/:projectId/designer', component: EntityDesigner },
            { path: 'projects/:projectId/security', component: SecurityDesigner },
            { path: '', redirectTo: 'projects', pathMatch: 'full' }
        ]
    }
];
