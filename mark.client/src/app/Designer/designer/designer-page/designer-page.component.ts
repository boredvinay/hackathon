import { Component, ViewChild, AfterViewInit, OnDestroy, Inject } from '@angular/core';
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
  debugDsl: any = null;

  @ViewChild('canvasComp') canvasComp!: CanvasComponent;

  // context (can be provided via route params or hard-coded defaults)
  private designId: string | null = null;
  private versionId: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private bus: EditorBus
  ) {}

  ngAfterViewInit(): void {
    // Read context from route: /designer?designId=...&versionId=...
    const qp = this.route.snapshot.queryParamMap;
    this.designId = qp.get('designId') ?? '11111111-1111-1111-1111-111111111111';
    this.versionId = qp.get('versionId') ?? '33333333-3333-3333-3333-333333333333';

    // Set the context on both: bus (for services) and canvas (your local vars)
    this.canvasComp.setContext(this.designId, this.versionId);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ===== bridge events to canvas =====
  droppedElementHandler(element: any) {
    this.droppedElement = element;
    setTimeout(() => (this.droppedElement = null), 0);
  }

  selectedElementHandler(node: Konva.Node | null) {
    this.selectedElement = node;
  }

  // ===== quick actions (toolbar buttons call these) =====
  onSave()   { this.canvasComp.save(); }
  onPreview(){ this.canvasComp.preview({ orderNumber: 'ORD-42', sku: 'SKU-42' }); }
  onPrint()  { this.canvasComp.print({ orderNumber: 'ORD-42', sku: 'SKU-42' }); }
}
