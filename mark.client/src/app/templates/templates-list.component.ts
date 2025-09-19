import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { DesignService } from '../Designer/services/design.service';
import { CreateDesignDialogComponent } from './create-design-component/create-design-dialog.component';
import { Router } from '@angular/router';
// ...existing code...
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DesignSummary } from '../models/designer.models';

@Component({
  selector: 'app-templates',
  templateUrl: './templates-list.component.html',
  styleUrls: ['./templates-list.component.css'],
  imports: [CommonModule, DatePipe, FormsModule]
})
export class TemplatesListComponent implements OnInit {
  loading = false;
  error: string | null = null;
  designs: any[] = [];

  showAddModal = false;
  newDesignKey = '';

  openAddModal() {
    this.showAddModal = true;
    this.newDesignKey = '';
  }

  closeAddModal() {
    this.showAddModal = false;
    this.newDesignKey = '';
  }

  addDesign() {
    if (!this.newDesignKey.trim()) return;
    this.api.createDesign({ key: this.newDesignKey.trim() }).subscribe({
      next: (result: any) => {
        this.closeAddModal();
        const designId = result?.id ?? result?.Id ?? result;
        const versionId = result?.version?.id ?? result?.Version?.id ?? null;
        if (designId && versionId) {
          this.api.openDesigner(this.router, String(designId), String(versionId));
        } else if (designId) {
          this.api.openDesigner(this.router, String(designId));
        }
        this.refresh();
      },
      error: (e: any) => {
        this.error = e?.message ?? 'Failed to add design';
      }
    });
  }

  constructor(
    private readonly api: DesignService,
    private readonly dialog: MatDialog,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.refresh();
  }

  refresh() {
    this.loading = true;
    this.error = null;
    this.api.listDesigns()
      .subscribe({
        next: (rows: any) => {
          // API returns a PagedResult<DesignListItem> { items: [], page, pageSize, total }
          // but in some environments it may return a raw array. Handle both.
          // Support different casing from backend: items or Items
          if (Array.isArray(rows)) {
            this.designs = rows;
          } else if (rows && Array.isArray(rows.items)) {
            this.designs = rows.items;
          } else if (rows && Array.isArray(rows.Items)) {
            this.designs = rows.Items;
          } else {
            // Fallback: try to coerce into an array if possible
            this.designs = rows ? [rows] : [];
          }

          // For each design row, try to populate extra UI fields:
          // name (fallback to key), and lastUpdated (from latestVersion)
          for (const raw of this.designs) {
            // Normalize common PascalCase/camelCase variants into a consistent shape
            const d: any = {
              id: raw.id ?? raw.Id,
              key: raw.key ?? raw.Key,
              name: raw.name ?? raw.Name ?? raw.key ?? raw.Key ?? '',
              status: raw.status ?? raw.Status ?? 'Active',
              createdAt: raw.createdAt ?? raw.CreatedAt ?? raw.created_at ?? (raw.created || null),
              createdBy: raw.createdBy ?? raw.CreatedBy ?? raw.created_by ?? '',
              latestVersionId: raw.latestVersionId ?? raw.LatestVersionId ?? raw.latest_version_id ?? raw.latestVersion?.id ?? null,
              // keep original for reference
              __raw: raw
            };

            // replace array item with normalized object
            const idx = this.designs.indexOf(raw);
            if (idx >= 0) this.designs[idx] = d;

            // If the row has a latestVersionId, fetch its header to show last-updated
            const latestId = d.latestVersionId;
            if (latestId) {
                // fire-and-forget; UI will update as responses arrive
                d.loadingLatest = true;
                this.api.getVersion(String(latestId)).subscribe({
                  next: (v: any) => {
                    // map version createdAt variants too
                    d.latestVersion = v;
                    d.lastUpdated = v?.createdAt ?? v?.CreatedAt ?? v?.created_at ?? null;
                    d.loadingLatest = false;
                  },
                  error: () => {
                    // ignore; leave lastUpdated unset
                    d.loadingLatest = false;
                  }
                });
            }

          }

          this.loading = false;
        },
        error: (e: any) => { this.error = e?.message ?? 'Failed to load designs'; this.loading = false; }
      });
  }

  newDesign() {
    const ref = this.dialog.open(CreateDesignDialogComponent, {
      width: '520px',
      disableClose: true,
      data: {}
    });

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      // Dialog handles navigation itself; just refresh the list.
      this.refresh();
    });
  }

  open(design: DesignSummary) {
    // Open latest version for editing
    this.api.openDesigner(this.router, design.id);
  }
}
