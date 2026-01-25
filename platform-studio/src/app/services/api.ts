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
}
