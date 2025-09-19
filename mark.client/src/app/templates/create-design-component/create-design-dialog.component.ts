import { Component, Inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { DesignService } from '../../Designer/services/design.service';

import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-create-design-dialog',
  templateUrl: './create-design-dialog.component.html',
  styleUrls: ['./create-design-dialog.component.css'],
  imports: [
    MatFormFieldModule,
    MatInputModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    CommonModule,
    ReactiveFormsModule
  ]
})
export class CreateDesignDialogComponent implements OnInit {
  constructor(
    @Inject(MAT_DIALOG_DATA) public data: any,
    private readonly dialogRef: MatDialogRef<CreateDesignDialogComponent>,
    private readonly fb: FormBuilder,
    private readonly api: DesignService,
    private readonly router: Router
  ) {}

  creating = false;
  error: string | null = null;
  keyManuallyEdited = false;

  form: any;

  ngOnInit() {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
  key: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/)]]
    });
  }

  onNameInput() {
    if (this.keyManuallyEdited) return;
    const name = this.form.controls.name.value ?? '';
    const slug = this.slugify(name);
    this.form.controls.key.setValue(slug);
  }

  onKeyInput() {
    this.keyManuallyEdited = true;
  }

  slugify(s: string): string {
    return (s || '')
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/(^-+)|(-+$)/g, '')
      .substring(0, 64);
  }

  cancel() { this.dialogRef.close(null); }

  create() {
    this.error = null;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
  const key  = this.form.value.key!.trim();

    this.creating = true;

    // Create the design (server now creates an initial Draft version and returns it)
    this.api.createDesign({ key }).subscribe({
      next: (design: any) => {
        this.creating = false;
        const designId = design?.id ?? design?.Id ?? design;
        const versionId = design?.version?.id ?? design?.Version?.id ?? null;
        // Navigate directly to the designer so the user starts editing immediately.
        if (designId) {
          this.api.openDesigner(this.router, String(designId), versionId ? String(versionId) : undefined);
        }
        this.dialogRef.close({ created: true, designId: String(designId), versionId: versionId ? String(versionId) : undefined });
      },
      error: (e) => {
        this.creating = false;
        this.error = e?.message ?? 'Failed to create design';
      }
    });
  }
}
