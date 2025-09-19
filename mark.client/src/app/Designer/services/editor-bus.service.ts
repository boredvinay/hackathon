import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

@Injectable()
export class EditorBus {
  designId$  = new BehaviorSubject<string|null>(null);
  versionId$ = new BehaviorSubject<string|null>(null);

  /** Emits the full DSL object whenever the canvas changes (used by autosave) */
  dslChanged$   = new Subject<any>();

  /** Emits when the user clicks Save (Canvas should emit current DSL to dslChanged$ first) */
  saveClick$    = new Subject<void>();

  /** Emits a payload (or null) when user clicks Preview */
  previewClick$ = new Subject<Record<string, any> | null>();

  /** Emits a payload when user clicks Print */
  printClick$   = new Subject<Record<string, any>>();

  setContext(designId: string, versionId: string | null) {
    this.designId$.next(designId);
    this.versionId$.next(versionId);
  }
}
