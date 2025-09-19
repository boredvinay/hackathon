export interface HealthResponse { ok: boolean; }

export interface PreviewRequest {
  designVersionId: string;
  payload?: Record<string, any>;
  format?: 'pdf';
  dpi?: number;
}

export interface RenderSingleRequest {
  designVersionId: string;
  payload: Record<string, any>;
  format?: 'pdf';
  idempotencyKey?: string;
}

export interface RenderResultResponse {
  renderId: string;
  contentType: string;
  url: string;
}

export interface BatchItem { id: string; payload: Record<string, any>; }

export interface RenderBatchRequest {
  designVersionId: string;
  items: BatchItem[];
  format?: 'pdf';
  bundle?: 'zip';
}

export interface RenderBatchItemStatus {
  id: string;
  status: 'Queued' | 'Running' | 'Completed' | 'Failed';
  error?: string | null;
}

export interface RenderJobStatusResponse {
  jobId: string;
  status: 'Queued' | 'Running' | 'Completed' | 'Failed';
  resultUrl?: string | null;
  items: RenderBatchItemStatus[];
}

export interface MergePdfRequestByIds { fileIds: string[]; outputName?: string; }
export interface MergePdfRequestByBase64 { pdfBase64: string[]; outputName?: string; }

export interface DiffRequest {
  versionA: string;
  versionB: string;
  mode: 'pixel' | 'dsl';
}
export interface DiffResponse {
  mode: 'pixel' | 'dsl';
  score: number;
  diffImagePath?: string | null;
  reportPath: string;
  summary: string;
}
