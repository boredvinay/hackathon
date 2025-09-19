import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import {
  DiffRequest, DiffResponse, HealthResponse, MergePdfRequestByBase64,
  MergePdfRequestByIds, PreviewRequest, RenderBatchRequest,
  RenderJobStatusResponse, RenderResultResponse, RenderSingleRequest
} from '../../models/render.models';

@Injectable({ providedIn: 'root' })
export class RenderService {
  private readonly base = "http://localhost:5009";
  constructor(private readonly http: HttpClient) {}

  health() { return this.http.get<HealthResponse>(`${this.base}/api/render/health`); }

  preview(req: PreviewRequest) {
    return this.http.post<RenderResultResponse>(`${this.base}/api/render/preview`, req);
  }
  single(req: RenderSingleRequest) {
    const headers = req.idempotencyKey ? new HttpHeaders({ 'Idempotency-Key': req.idempotencyKey }) : undefined;
    return this.http.post<RenderResultResponse>(`${this.base}/api/render/single`, req, { headers });
  }
  batch(req: RenderBatchRequest) {
    return this.http.post<RenderJobStatusResponse>(`${this.base}/api/render/batch`, req);
  }
  batchStatus(jobId: string) {
    return this.http.get<RenderJobStatusResponse>(`${this.base}/api/render/batch/${encodeURIComponent(jobId)}`);
  }
  mergeByIds(req: MergePdfRequestByIds) {
    return this.http.post<RenderResultResponse>(`${this.base}/api/render/merge-pdf`, req);
  }
  mergeByBase64(req: MergePdfRequestByBase64) {
    return this.http.post<RenderResultResponse>(`${this.base}/api/render/merge-pdf`, req);
  }
  diff(req: DiffRequest) {
    return this.http.post<DiffResponse>(`${this.base}/api/render/diff`, req);
  }
  fileUrl(fileId: string) { return `${this.base}/api/render/files/${encodeURIComponent(fileId)}`; }
  openFile(fileId: string) { window.open(this.fileUrl(fileId), '_blank'); }
}
