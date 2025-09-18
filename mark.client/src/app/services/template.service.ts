// src/app/services/template.service.ts
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TemplateService {
  private api = '/api/templates';

  constructor(private http: HttpClient) {}

  saveTemplateVersion(payload: any): Observable<any> {
    // POST /api/templates/{templateId}/versions
    return this.http.post(`${this.api}/${payload.templateId}/versions`, payload);
  }

  getTemplateVersion(templateId: string, versionId: string): Observable<any> {
    return this.http.get(`${this.api}/${templateId}/versions/${versionId}`);
  }

  listTemplates(): Observable<any> {
    return this.http.get(`${this.api}`);
  }
}
