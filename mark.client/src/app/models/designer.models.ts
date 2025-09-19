export type DesignStatus = 'Active' | 'Archived';
export type VersionState = 'Draft' | 'InReview' | 'Published' | 'Archived';

export interface DesignDto {
  id: string;
  key: string;
  status: DesignStatus;
  createdAt: string;
  createdBy: string;
  latestVersionId?: string | null;
}

export interface DesignListItem {
  id: string;
  key: string;
  status: DesignStatus;
  createdAt: string;
  createdBy: string;
  latestVersionId?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface DesignVersionDto {
  id: string;
  designId: string;
  semVer: string;
  state: VersionState;
  sha256: string;
  previewPath?: string | null;
  createdAt: string;
  createdBy: string;
}

export interface DesignVersionListItem {
  id: string;
  designId: string;
  semVer: string;
  state: VersionState;
  createdAt: string;
  createdBy: string;
}

export interface CreateDesignRequest { key: string; createdBy?: string; }
export interface CreateDesignResponse { id: string; }

export interface CreateDesignVersionRequest {
  semVer: string;
  jsonDsl?: string;
  jsonSchema?: string;
  createdBy?: string;
}
export interface CreateDesignVersionResponse { id: string; }

export interface SaveDslRequest { dslJson: string; }
export interface SaveSchemaRequest { schemaJson: string; }

export interface ApproveRequest { reviewer: string; signatureHash: string; }
export interface PreviewResponse { versionId: string; previewPath: string; }

export interface ListDesignsQuery {
  q?: string;
  status?: DesignStatus | '';
  page?: number;
  pageSize?: number;
}
