import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AiProvider {
  id: string;
  name: string;
  baseUrl: string;
  apiKeyPreview: string;
  defaultModel: string;
  isDefault: boolean;
  isEnabled: boolean;
  lastTestStatus: string;
  lastTestedAt: string | null;
}

export interface AiSkillSummary {
  skillName: string;
  description: string;
  category: string;
  defaultTemperature: number;
  maxTokens: number;
}

export interface TestResult {
  status: string;
  response?: string;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly apiUrl = 'http://localhost:5018/api';

  constructor(private readonly http: HttpClient) { }

  // ── Projects ──────────────────────────────────────────────────────────
  getProjects(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/projects`);
  }
  createProject(project: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects`, project);
  }

  // ── Entities ──────────────────────────────────────────────────────────
  getEntities(projectId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/projects/${projectId}/entities`);
  }
  createEntity(projectId: string, metadata: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects/${projectId}/entities`, metadata);
  }
  exportEntities(projectId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/projects/${projectId}/entities/export`, { responseType: 'blob' });
  }
  importEntities(projectId: string, request: { entities: any[], confirmedOverwrites: string[] }): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects/${projectId}/entities/import`, request);
  }

  // ── Build & Deploy ─────────────────────────────────────────────────────
  buildProject(projectId: string, options: any = {}): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/projects/${projectId}/build`, options, { responseType: 'blob' });
  }
  publishProject(projectId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/projects/${projectId}/publish`, {});
  }

  // ── Security & Users ───────────────────────────────────────────────────
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

  // ── Workflows ──────────────────────────────────────────────────────────
  getWorkflows(projectId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/projects/${projectId}/workflows`);
  }
  createWorkflow(projectId: string, metadata: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects/${projectId}/workflows`, metadata);
  }
  updateWorkflow(projectId: string, id: string, metadata: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/projects/${projectId}/workflows/${id}`, metadata);
  }
  deleteWorkflow(projectId: string, id: string): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/projects/${projectId}/workflows/${id}`);
  }
  publishWorkflow(projectId: string, id: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects/${projectId}/workflows/${id}/publish`, {});
  }

  // ── Forms ──────────────────────────────────────────────────────────────
  getForms(projectId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/projects/${projectId}/forms`);
  }
  createForm(projectId: string, metadata: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects/${projectId}/forms`, metadata);
  }
  updateForm(projectId: string, formId: string, metadata: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/projects/${projectId}/forms/${formId}`, metadata);
  }

  // ── Widgets ────────────────────────────────────────────────────────────
  getWidgets(projectId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/projects/${projectId}/widgets`);
  }
  createWidget(projectId: string, definition: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/projects/${projectId}/widgets`, definition);
  }
  updateWidget(projectId: string, widgetId: string, definition: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/projects/${projectId}/widgets/${widgetId}`, definition);
  }

  // ── Jobs & Data ────────────────────────────────────────────────────────
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

  // ── AI: Generation ─────────────────────────────────────────────────────
  aiGenerateSchema(prompt: string, projectId?: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/ai/generate-schema`, { prompt, projectId });
  }
  aiDesignEntity(prompt: string, currentEntityJson: string, projectId?: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/ai/design-entity`, { prompt, projectId, currentEntityJson });
  }
  aiGenerateConnector(prompt: string, projectId: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/ai/generate-connector`, { prompt, projectId });
  }
  aiGenerateRule(prompt: string, projectId: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/ai/generate-rule`, { prompt, projectId });
  }
  aiExplainLogic(codeSnippet: string, projectId: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/ai/explain-logic`, { prompt: codeSnippet, projectId });
  }
  aiGetSkills(): Observable<AiSkillSummary[]> {
    return this.http.get<AiSkillSummary[]>(`${this.apiUrl}/ai/skills`);
  }

  // ── AI: BYOK Provider Management ──────────────────────────────────────
  getAiProviders(): Observable<AiProvider[]> {
    return this.http.get<AiProvider[]>(`${this.apiUrl}/ai/providers`);
  }
  addAiProvider(provider: any): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/ai/providers`, provider);
  }
  deleteAiProvider(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/ai/providers/${id}`);
  }
  testAiProvider(baseUrl: string, apiKey: string, defaultModel: string): Observable<TestResult> {
    return this.http.post<TestResult>(`${this.apiUrl}/ai/providers/test`, { baseUrl, apiKey, defaultModel });
  }
}
