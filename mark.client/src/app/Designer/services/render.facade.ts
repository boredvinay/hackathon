import { Injectable } from '@angular/core';
import { RenderService } from './render.service';
import { EditorBus } from './editor-bus.service';
import { Observable } from 'rxjs';
import { filter, shareReplay, switchMap, withLatestFrom } from 'rxjs/operators';

@Injectable()
export class RenderFacade {
  preview$!: Observable<{ renderId: string; url: string; contentType: string }>;
  print$!: Observable<{ renderId: string; url: string; contentType: string }>;

  constructor(
    private readonly api: RenderService,
    private readonly bus: EditorBus
  ) {
    this.preview$ = this.bus.previewClick$.pipe(
      withLatestFrom(this.bus.versionId$),
      filter(([, vid]) => !!vid),
      switchMap(([payload, vid]) =>
        this.api.preview({ designVersionId: vid as string, payload: payload ?? undefined })
      ),
      shareReplay(1)
    );

    this.print$ = this.bus.printClick$.pipe(
      withLatestFrom(this.bus.versionId$),
      filter(([, vid]) => !!vid),
      switchMap(([payload, vid]) =>
        this.api.single({
          designVersionId: vid as string,
          payload,
          idempotencyKey: `ui-${Date.now()}`
        })
      ),
      shareReplay(1)
    );
  }
}
