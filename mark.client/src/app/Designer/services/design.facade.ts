import { Injectable } from '@angular/core';
import { DesignService } from './design.service';
import { EditorBus } from './editor-bus.service';
import { BehaviorSubject, Observable, defer, merge, timer } from 'rxjs';
import { debounceTime, distinctUntilChanged, filter, map, shareReplay, switchMap, tap, withLatestFrom } from 'rxjs/operators';

const stable = (obj:any) => {
  const seen = new WeakSet();
  const s = (o:any):any => {
    if (o && typeof o === 'object') {
      if (seen.has(o)) return null;
      seen.add(o);
      if (Array.isArray(o)) return o.map(s);
      return Object.keys(o).sort().reduce((a:any,k)=> (a[k]=s(o[k]),a),{});
    }
    return o;
  };
  return JSON.stringify(s(obj));
};

@Injectable()
export class DesignFacade {
  private etag$ = new BehaviorSubject<string|null>(null);

  /** Stream to load DSL if needed elsewhere */
  dsl$!: Observable<any>;

  /** Drives save/autosave effects */
  autosave$!: Observable<any>;

  constructor(
    private readonly api: DesignService,
    private readonly bus: EditorBus
  ) {
    // If you need initial DSL load in this facade, wire it here
    this.dsl$ = this.bus.versionId$.pipe(
      filter((v): v is string => !!v),
      switchMap(vId => this.api.getDsl(vId).pipe(
        map(txt => JSON.parse(txt)),
        tap(() => this.etag$.next(null)) // replace with real ETag if you choose to surface it
      )),
      shareReplay(1)
    );

    // Autosave:
    // - On any DSL change (debounced 500ms)
    // - On explicit Save click (fires immediately)
    this.autosave$ = merge(
      this.bus.dslChanged$.pipe(debounceTime(500)),
      this.bus.saveClick$.pipe(switchMap(() => timer(0)))
    ).pipe(
      withLatestFrom(this.bus.versionId$),
      filter(([, v]) => !!v),
      map(([dsl, v]) => ({ dsl, versionId: v as string })),
      distinctUntilChanged((a,b)=> stable(a.dsl) === stable(b.dsl)),
      switchMap(({dsl, versionId}) =>
        defer(() => this.api.putDsl(versionId, { dslJson: JSON.stringify(dsl) }, this.etag$.value || undefined))
      ),
      shareReplay(1)
    );
  }

  setContext(designId: string, versionId: string | null) {
    this.bus.setContext(designId, versionId);
  }
}
