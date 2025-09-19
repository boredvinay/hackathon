import { Component, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import Konva from 'konva';
import { CanvasComponent } from '../canvas/canvas.component';
import { ActivatedRoute } from '@angular/router';
import { EditorBus } from '../../services/editor-bus.service';
import { Subject, takeUntil } from 'rxjs';
import { RenderFacade } from '../../services/render.facade';
import { DesignFacade } from '../../services/design.facade';

@Component({
  selector: 'app-designer-page',
  standalone: false,
  templateUrl: './designer-page.component.html',
  styleUrl: './designer-page.component.css',
  providers: [EditorBus, RenderFacade, DesignFacade]
})
export class DesignerPageComponent implements AfterViewInit, OnDestroy {
  droppedElement: any;
  selectedElement: Konva.Node | null = null;

  @ViewChild('canvasComp') canvasComp!: CanvasComponent;

  private designId: string | null = null;
  private versionId: string | null = null;
  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private bus: EditorBus,
    private render: RenderFacade,
    private design: DesignFacade
  ) {}

  ngAfterViewInit(): void {
    const sl = this.route.snapshot.paramMap;
    const qp = this.route.snapshot.queryParamMap;

    this.designId = sl.get('designId') ?? qp.get('designId') ?? '11111111-1111-1111-1111-111111111111';
    const vid = sl.get('versionId') ?? qp.get('versionId');
    this.versionId = vid && vid !== 'latest' ? vid : null;

    // wire context for services and canvas
    this.bus.setContext(this.designId, this.versionId);
    this.canvasComp.setContext(this.designId, this.versionId);

    // subscribe to render outputs to open results in a new tab
    this.render.preview$
      .pipe(takeUntil(this.destroy$))
      .subscribe(r => window.open(r.url, '_blank'));

    this.render.print$
      .pipe(takeUntil(this.destroy$))
      .subscribe(r => window.open(r.url, '_blank'));

    // kick off autosave stream (subscription drives the effect)
    this.design.autosave$
      .pipe(takeUntil(this.destroy$))
      .subscribe();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  droppedElementHandler(element: any) {
    this.droppedElement = element;
    setTimeout(() => (this.droppedElement = null), 0);
  }
  selectedElementHandler(node: Konva.Node | null) {
    this.selectedElement = node;
  }

  // toolbar actions
  onSave()   { this.canvasComp.save(); }    // canvas should push current DSL into bus.saveClick$
  onPreview(){ this.canvasComp.preview({ orderNumber: 'ORD-42', sku: 'SKU-42' }); }
  onPrint()  { this.canvasComp.print({ orderNumber: 'ORD-42', sku: 'SKU-42' }); }
}
