import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import {
  ApproveRequest, CreateDesignRequest, CreateDesignResponse,
  CreateDesignVersionRequest, CreateDesignVersionResponse,
  DesignDto, DesignListItem, DesignVersionDto, DesignVersionListItem,
  ListDesignsQuery, PagedResult, PreviewResponse,
  SaveDslRequest, SaveSchemaRequest
} from '../../models/designer.models';

@Injectable({ providedIn: 'root' })
export class DesignService {
  private readonly base = "http://localhost:5009";

  constructor(private readonly http: HttpClient) {}

  health() { return this.http.get<{ ok: boolean }>(`${this.base}/api/designs/health`); }

  createDesign(req: CreateDesignRequest) {
    return this.http.post<CreateDesignResponse>(`${this.base}/api/designs`, req);
  }
  getDesign(designId: string) {
    return this.http.get<DesignDto>(`${this.base}/api/designs/${encodeURIComponent(designId)}`);
  }
  listDesigns(q: ListDesignsQuery = {}) {
    let params = new HttpParams()
      .set('page', String(q.page ?? 1))
      .set('pageSize', String(q.pageSize ?? 20));
    if (q.q) params = params.set('q', q.q);
    if (q.status) params = params.set('status', q.status);
    return this.http.get<PagedResult<DesignListItem>>(`${this.base}/api/designs`, { params });
  }

  createVersion(designId: string, req: CreateDesignVersionRequest) {
    return this.http.post<CreateDesignVersionResponse>(
      `${this.base}/api/designs/${encodeURIComponent(designId)}/versions`, req);
  }
  getVersion(versionId: string) {
    return this.http.get<DesignVersionDto>(`${this.base}/api/designs/versions/${encodeURIComponent(versionId)}`);
  }
  listPublished(designId?: string) {
    const params = designId ? new HttpParams().set('designId', designId) : undefined;
    return this.http.get<DesignVersionListItem[]>(`${this.base}/api/designs/versions/published`, { params });
  }

  // DSL is text
  getDsl(versionId: string) {
    return this.http.get(`${this.base}/api/designs/versions/${encodeURIComponent(versionId)}/dsl`, { responseType: 'text' });
  }
  putDsl(versionId: string, body: SaveDslRequest, etag?: string) {
    const headers = etag ? new HttpHeaders({ 'If-Match': etag }) : undefined;
    return this.http.put(`${this.base}/api/designs/versions/${encodeURIComponent(versionId)}/dsl`, body, { headers });
  }

  // Schema (optional in editor)
  getSchema(versionId: string) {
    return this.http.get(`${this.base}/api/designs/versions/${encodeURIComponent(versionId)}/schema`, { responseType: 'text' });
  }
  putSchema(versionId: string, body: SaveSchemaRequest, etag?: string) {
    const headers = etag ? new HttpHeaders({ 'If-Match': etag }) : undefined;
    return this.http.put(`${this.base}/api/designs/versions/${encodeURIComponent(versionId)}/schema`, body, { headers });
  }

  submit(versionId: string) {
    return this.http.post(`${this.base}/api/designs/versions/${encodeURIComponent(versionId)}/submit`, {});
  }
  approve(versionId: string, req: ApproveRequest) {
    return this.http.post(`${this.base}/api/designs/versions/${encodeURIComponent(versionId)}/approve`, req);
  }
  generatePreview(versionId: string, payload?: any) {
    return this.http.post<PreviewResponse>(`${this.base}/api/designs/versions/${encodeURIComponent(versionId)}/preview`, payload ?? {});
  }
}
