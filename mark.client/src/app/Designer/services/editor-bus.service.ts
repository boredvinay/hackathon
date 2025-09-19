import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

@Injectable()
export class EditorBus {
  designId$  = new BehaviorSubject<string|null>(null);
  versionId$ = new BehaviorSubject<string|null>(null);

  dslChanged$   = new Subject<any>();
  saveClick$    = new Subject<void>();
  previewClick$ = new Subject<Record<string, any> | null>();
  printClick$   = new Subject<Record<string, any>>();

  setContext(designId: string, versionId: string) {
    this.designId$.next(designId);
    this.versionId$.next(versionId);
  }
}
