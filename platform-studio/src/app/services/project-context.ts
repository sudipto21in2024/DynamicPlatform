import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ProjectContextService {
  private readonly projectIdSource = new BehaviorSubject<string | null>(null);
  projectId$ = this.projectIdSource.asObservable();

  setProjectId(id: string | null) {
    this.projectIdSource.next(id);
  }

  getProjectId(): string | null {
    return this.projectIdSource.getValue();
  }
}
