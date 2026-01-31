import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly apiUrl = 'http://localhost:5018/api'; // Switched to HTTP

  constructor(private readonly http: HttpClient) { }

  getProjects(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/projects`);
  }

  createProject(project: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects`, project);
  }

  getEntities(projectId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/projects/${projectId}/entities`);
  }

  createEntity(projectId: string, metadata: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects/${projectId}/entities`, metadata);
  }

  buildProject(projectId: string): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/projects/${projectId}/build`, {}, { responseType: 'blob' });
  }

  publishProject(projectId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/projects/${projectId}/publish`, {});
  }

  getSecurityConfig(projectId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/projects/${projectId}/security`);
  }

  saveSecurityConfig(projectId: string, config: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects/${projectId}/security`, config);
  }

  getUsersConfig(projectId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/projects/${projectId}/users`);
  }

  saveUsersConfig(projectId: string, config: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects/${projectId}/users`, config);
  }

  getWorkflows(projectId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/projects/${projectId}/workflows`);
  }

  createWorkflow(projectId: string, metadata: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects/${projectId}/workflows`, metadata);
  }

  getForms(projectId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/projects/${projectId}/forms`);
  }

  createForm(projectId: string, metadata: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects/${projectId}/forms`, metadata);
  }

  updateForm(projectId: string, formId: string, metadata: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/projects/${projectId}/forms/${formId}`, metadata);
  }

  getWidgets(projectId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/projects/${projectId}/widgets`);
  }

  createWidget(projectId: string, definition: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects/${projectId}/widgets`, definition);
  }

  updateWidget(projectId: string, widgetId: string, definition: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/projects/${projectId}/widgets/${widgetId}`, definition);
  }

  request(method: string, path: string, body?: any): Observable<any> {
    return this.http.request(method, `${this.apiUrl}/${path}`, { body });
  }

  getJobs(userId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/data/jobs/user/${userId}`);
  }

  getJobStatus(jobId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/data/jobs/${jobId}/status`);
  }

  executeDataOperation(request: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/data/execute`, request);
  }
}
