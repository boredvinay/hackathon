import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
  OnInit,
  OnDestroy,
  NgZone,
} from '@angular/core';
import Konva from 'konva';
import { BarcodeUtils } from '../../../utils/barcode-utils';
import { HttpErrorResponse } from '@angular/common/http';
import { TemplateService } from '../../../services/template.service';

// added: shared editor bus + facades + rxjs
import { EditorBus } from '../../services/editor-bus.service';
import { DesignFacade } from '../../services/design.facade';
import { RenderFacade } from '../../services/render.facade';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-canvas',
  standalone: false,
  templateUrl: './canvas.component.html',
  styleUrl: './canvas.component.css',
})
export class CanvasComponent implements OnInit, OnDestroy {
  @Input() droppedElement: any;
  @Output() elementSelected = new EventEmitter<Konva.Node | null>();
  @ViewChild('stageContainer', { static: true }) canvasContainer!: ElementRef;

  // sizes & stage
  customSizeSelected = false;
  customWidth = 800;
  customHeight = 600;
  stage!: Konva.Stage;
  layer!: Konva.Layer;
  transformer!: Konva.Transformer;
  selectedNode: Konva.Node | null = null;

  paper!: Konva.Rect;

  // history + nodes
  private history: string[] = [];
  private historyStep = -1;
  private widgetNodes: Konva.Node[] = [];

  // default paper
  paperWidth = 400;
  paperHeight = 600;

  // layers & helpers
  private gridLayer!: Konva.Layer;
  private rulerLayer!: Konva.Layer;
  private guideLayer!: Konva.Layer;
  private snapTolerance = 5;

  // backend
  currentTemplateId = '00000000-0000-0000-0000-000000000000'; // replace or set from parent
  currentVersionId: string | null = null;

  // added: destroy notifier for subscriptions
  private destroy$ = new Subject<void>();

  constructor(
    private templateService: TemplateService,
    // added: integration services
    private bus: EditorBus,
    private designFacade: DesignFacade,
    private renderFacade: RenderFacade,
    private zone: NgZone
  ) {}

  ngOnInit() {
    window.addEventListener('resize', () => this.centerPaper());

    this.stage = new Konva.Stage({
      container: this.canvasContainer.nativeElement,
      width: 900,
      height: 700,
    });

    this.layer = new Konva.Layer();
    this.stage.add(this.layer);

    this.paperWidth = 210 * 3.78;
    this.paperHeight = 297 * 3.78;

    this.paper = new Konva.Rect({
      x: (this.stage.width() - this.paperWidth) / 2,
      y: (this.stage.height() - this.paperHeight) / 2,
      width: this.paperWidth,
      height: this.paperHeight,
      fill: '#fff',
      stroke: '#000',
      strokeWidth: 1,
      listening: false,
    });
    this.layer.add(this.paper);

    this.transformer = new Konva.Transformer({
      rotateEnabled: true,
      padding: 5,
      borderDash: [4, 4],
    });
    this.layer.add(this.transformer);

    this.gridLayer = new Konva.Layer();
    this.rulerLayer = new Konva.Layer();
    this.guideLayer = new Konva.Layer();
    this.stage.add(this.gridLayer);
    this.stage.add(this.rulerLayer);
    this.stage.add(this.guideLayer);

    this.drawGrid();
    this.drawRulers();

    this.stage.on('click', (e) => this.onStageClick(e));

    const el = this.canvasContainer.nativeElement;
    el.addEventListener('dragover', (e: DragEvent) => e.preventDefault());
    el.addEventListener('drop', (e: DragEvent) => this.onDrop(e));

    const saved = localStorage.getItem('canvasState');
    if (saved) this.restoreFromJSON(saved);
    this.saveState();

    // --------- minimal integration wiring ----------
    // Load DSL on version change
    this.designFacade.dsl$.pipe(takeUntil(this.destroy$)).subscribe((raw) => {
      try {
        const dsl = typeof raw === 'string' ? JSON.parse(raw) : raw;
        if (dsl && typeof dsl === 'object') this.loadFromJsonDsl(dsl);
      } catch (e) {
        console.warn('DSL parse error:', e, raw);
      }
    });

    // Keep autosave flow hot (debounced + cancel-on-new is in facade)
    this.designFacade.autosave$.pipe(takeUntil(this.destroy$)).subscribe();

    // Open preview/print results
    this.renderFacade.preview$
      .pipe(takeUntil(this.destroy$))
      .subscribe((r) => r && window.open(r.url, '_blank'));

    this.renderFacade.print$
      .pipe(takeUntil(this.destroy$))
      .subscribe((r) => r && window.open(r.url, '_blank'));
    // ------------------------------------------------
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  ngOnChanges() {
    if (this.droppedElement) this.addElement(this.droppedElement);
  }

  // Allow parent page to set backend context (IDs)
  setContext(designId: string, versionId: string | null) {
    this.bus.setContext(designId, versionId);
    this.currentTemplateId = designId;
    this.currentVersionId = versionId;
  }

  /** manual save: triggers immediate autosave */
  save() {
    this.bus.saveClick$.next();
  }

  /** quick preview */
  preview(
    sample: Record<string, any> = { orderNumber: 'ORD-1', sku: 'SKU-1' }
  ) {
    this.bus.previewClick$.next(sample);
  }

  /** one-off print */
  print(sample: Record<string, any> = { orderNumber: 'ORD-1', sku: 'SKU-1' }) {
    this.bus.printClick$.next(sample);
  }

  // ========== Drag helpers used by toolbar ==========
  onDragStart(e: DragEvent, type: string, subtype: string) {
    const payload = { type, subtype }; // example: {type:'widget', subtype:'barcode'}
    e.dataTransfer?.setData('application/json', JSON.stringify(payload));
  }

  // for toolbar double-click -> add directly without dragging
  addDirectly(type: string, subtype: string) {
    const base = { x: this.paper.x() + 20, y: this.paper.y() + 20 };
    if (type === 'shape') {
      this.addElement({ ...base, type: 'shape', shape: subtype });
    } else if (type === 'widget') {
      if (subtype === 'text') {
        this.addElement({
          ...base,
          type: 'widget',
          widget: 'text',
          text: 'Edit me',
          placeholder: '',
        });
      } else if (subtype === 'barcode') {
        this.addElement({
          ...base,
          type: 'widget',
          widget: 'barcode',
          barcodeType: 'Code128',
          value: 'sample123',
        });
      } else if (subtype === 'qrcode') {
        this.addElement({
          ...base,
          type: 'widget',
          widget: 'qrcode',
          barcodeType: 'QRCode',
          value: 'https://example.com',
        });
      } else if (subtype === 'datamatrix') {
        this.addElement({
          ...base,
          type: 'widget',
          widget: 'datamatrix',
          barcodeType: 'DataMatrix',
          value: 'DM123',
        });
      } else if (subtype === 'aztec') {
        this.addElement({
          ...base,
          type: 'widget',
          widget: 'aztec',
          barcodeType: 'Aztec',
          value: 'AZ123',
        });
      } else if (subtype === 'maxicode') {
        this.addElement({
          ...base,
          type: 'widget',
          widget: 'maxicode',
          barcodeType: 'MaxiCode',
          value: 'MX123',
        });
      }
    }
  }

  // ========== Stage interaction ==========
  private onStageClick(e: Konva.KonvaEventObject<MouseEvent>) {
    if (e.target === this.stage || e.target === this.paper)
      return this.clearSelection();

    this.selectedNode = e.target as Konva.Shape;
    this.transformer.nodes([this.selectedNode]);
    this.layer.batchDraw();
    this.elementSelected.emit(this.selectedNode);
  }

  private onDrop(e: DragEvent) {
    e.preventDefault();
    let elem: any;
    try {
      elem = JSON.parse(e.dataTransfer!.getData('application/json'));
    } catch {
      return;
    }

    const rect = this.canvasContainer.nativeElement.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    if (
      x < this.paper.x() ||
      y < this.paper.y() ||
      x > this.paper.x() + this.paper.width() ||
      y > this.paper.y() + this.paper.height()
    )
      return;

    // If toolbar sends {type, subtype}
    if (elem.type && elem.subtype) {
      if (elem.type === 'shape')
        this.addElement({ type: 'shape', shape: elem.subtype, x, y });
      else if (elem.type === 'widget') {
        const w = elem.subtype;
        if (w === 'text')
          this.addElement({
            type: 'widget',
            widget: 'text',
            text: 'Edit me',
            x,
            y,
            placeholder: '',
          });
        else if (w === 'barcode')
          this.addElement({
            type: 'widget',
            widget: 'barcode',
            barcodeType: 'Code128',
            value: 'sample123',
            x,
            y,
          });
        else if (w === 'qrcode')
          this.addElement({
            type: 'widget',
            widget: 'qrcode',
            barcodeType: 'QRCode',
            value: 'https://example.com',
            x,
            y,
          });
        else if (w === 'datamatrix')
          this.addElement({
            type: 'widget',
            widget: 'datamatrix',
            barcodeType: 'DataMatrix',
            value: 'DM123',
            x,
            y,
          });
        else if (w === 'aztec')
          this.addElement({
            type: 'widget',
            widget: 'aztec',
            barcodeType: 'Aztec',
            value: 'AZ123',
            x,
            y,
          });
        else if (w === 'maxicode')
          this.addElement({
            type: 'widget',
            widget: 'maxicode',
            barcodeType: 'MaxiCode',
            value: 'MX123',
            x,
            y,
          });
      }
      return;
    }

    // Or if toolbar serialized a different payload (existing code)
    this.addElement({ ...elem, x, y });
  }

  private getDragBoundFunc(node: Konva.Shape) {
    return (pos: Konva.Vector2d) => {
      const minX = this.paper.x();
      const minY = this.paper.y();
      const maxX = this.paper.x() + this.paper.width() - (node.width?.() ?? 0);
      const maxY =
        this.paper.y() + this.paper.height() - (node.height?.() ?? 0);
      return {
        x: Math.max(minX, Math.min(pos.x, maxX)),
        y: Math.max(minY, Math.min(pos.y, maxY)),
      };
    };
  }

  // ========== Add elements (extended) ==========
  addElement(element: any) {
    let node: Konva.Node | null = null;
    const props: any = {
      x: element.x ?? 50,
      y: element.y ?? 50,
      draggable: true,
    };

    // Shapes
    if (element.type === 'shape') {
      if (element.shape === 'rect')
        node = new Konva.Rect({
          ...props,
          width: element.width ?? 100,
          height: element.height ?? 60,
          fill: element.color ?? '#808080',
        });
      else if (element.shape === 'circle')
        node = new Konva.Circle({
          ...props,
          radius: element.radius ?? 40,
          fill: element.color ?? '#808080',
        });
      else if (element.shape === 'line')
        node = new Konva.Line({
          ...props,
          points: element.points ?? [0, 0, 150, 0],
          stroke: element.color ?? 'black',
          strokeWidth: element.strokeWidth ?? 2,
        });
    }

    // Widgets
    else if (element.type === 'widget') {
      if (element.widget === 'text') {
        const txt = new Konva.Text({
          ...props,
          text: element.text ?? 'Edit me',
          fontSize: element.fontSize ?? 20,
          fill: element.color ?? 'black',
          width: element.width ?? 200,
        });
        if (element.placeholder)
          txt.setAttr('placeholder', element.placeholder);
        node = txt;
      }

      // 1D barcode generic (use JsBarcode)
      else if (element.widget === 'barcode' || element.widget === 'barcode1D') {
        // create temporary placeholder rect while we generate canvas
        const placeholder = new Konva.Rect({
          ...props,
          width: element.width ?? 200,
          height: element.height ?? 60,
          fill: '#eee',
        });
        this.layer.add(placeholder);
        placeholder.dragBoundFunc(this.getDragBoundFunc(placeholder));
        this.attachNodeEvents(placeholder);
        this.widgetNodes.push(placeholder);
        this.layer.batchDraw();

        // generate barcode image
        BarcodeUtils.generateCanvas(
          '1D',
          element.barcodeType ?? 'Code128',
          String(element.value ?? 'sample123'),
          element.width ?? 200,
          element.height ?? 60
        )
          .then((canvas) => {
            const img = new Image();
            img.src = canvas.toDataURL();
            img.onload = () => {
              const kImg = new Konva.Image({
                x: placeholder.x(),
                y: placeholder.y(),
                image: img,
                width: element.width ?? 200,
                height: element.height ?? 60,
                draggable: true,
              });
              kImg.setAttr('barcodeType', element.barcodeType ?? 'Code128');
              kImg.setAttr('value', element.value ?? 'sample123');
              kImg.setAttr('is2D', false);
              kImg.dragBoundFunc(this.getDragBoundFunc(kImg));
              this.layer.add(kImg);
              placeholder.destroy();
              this.widgetNodes.push(kImg);
              this.layer.batchDraw();
              this.saveState();
              // notify bus for autosave debounce
              this.zone.runOutsideAngular(() =>
                this.bus.dslChanged$.next(this.toJsonDsl())
              );
            };
          })
          .catch(() => {
            // keep placeholder if generation fails
          });

        return;
      }

      // QR / DataMatrix / Aztec / MaxiCode
      else if (
        ['qrcode', 'datamatrix', 'aztec', 'maxicode', 'barcode2D'].includes(
          element.widget
        ) ||
        element.barcodeType
      ) {
        const sizeW = element.width ?? element.size ?? 120;
        const sizeH = element.height ?? element.size ?? sizeW;

        // placeholder rect
        const placeholder = new Konva.Rect({
          ...props,
          width: sizeW,
          height: sizeH,
          fill: '#ddd',
        });
        this.layer.add(placeholder);
        placeholder.dragBoundFunc(this.getDragBoundFunc(placeholder));
        this.attachNodeEvents(placeholder);
        this.widgetNodes.push(placeholder);
        this.layer.batchDraw();
        type BarcodeWidget = 'qrcode' | 'datamatrix' | 'aztec' | 'maxicode';
        const typeMap: Record<BarcodeWidget, string> = {
          qrcode: 'QRCode',
          datamatrix: 'DataMatrix',
          aztec: 'Aztec',
          maxicode: 'MaxiCode',
        };

        const sym =
          element.barcodeType ??
          typeMap[element.widget as BarcodeWidget] ??
          'QRCode';
        BarcodeUtils.generateCanvas(
          '2D',
          sym,
          String(element.value ?? 'https://example.com'),
          sizeW,
          sizeH
        )
          .then((canvas) => {
            const img = new Image();
            img.src = canvas.toDataURL();
            img.onload = () => {
              const kImg = new Konva.Image({
                x: placeholder.x(),
                y: placeholder.y(),
                image: img,
                width: sizeW,
                height: sizeH,
                draggable: true,
              });
              kImg.setAttr('barcodeType', sym);
              kImg.setAttr('value', element.value ?? '');
              kImg.setAttr('is2D', true);
              kImg.dragBoundFunc(this.getDragBoundFunc(kImg));
              this.layer.add(kImg);
              placeholder.destroy();
              this.widgetNodes.push(kImg);
              this.layer.batchDraw();
              this.saveState();
              // notify bus for autosave debounce
              this.zone.runOutsideAngular(() =>
                this.bus.dslChanged$.next(this.toJsonDsl())
              );
            };
          })
          .catch(() => {
            // leave placeholder if generation fails
          });

        return;
      }

      // image widget
      else if (element.widget === 'image') {
        const img = new window.Image();
        img.src = element.src || 'https://via.placeholder.com/100';
        img.onload = () => {
          const kImg = new Konva.Image({
            ...props,
            image: img,
            width: element.width ?? 100,
            height: element.height ?? 100,
            draggable: true,
          });
          kImg.dragBoundFunc(this.getDragBoundFunc(kImg));
          this.layer.add(kImg);
          this.widgetNodes.push(kImg);
          this.layer.batchDraw();
          this.saveState();
          this.zone.runOutsideAngular(() =>
            this.bus.dslChanged$.next(this.toJsonDsl())
          );
        };
        return;
      }
    }

    if (node) {
      node.dragBoundFunc(this.getDragBoundFunc(node as Konva.Shape));
      this.addNodeToLayer(this.layer, node);
      this.attachNodeEvents(node);
      this.updateWidgetOpacity();
      this.widgetNodes.push(node);
      this.layer.batchDraw();
      this.saveState();
      this.zone.runOutsideAngular(() =>
        this.bus.dslChanged$.next(this.toJsonDsl())
      );
    }
  }

  addNodeToLayer(layer: Konva.Layer, node: Konva.Node): void {
    if (node instanceof Konva.Shape || node instanceof Konva.Group) {
      layer.add(node);
      layer.batchDraw();
    } else {
      console.warn('Unsupported node type:', node.getClassName());
    }
  }

  // ========== update selected props (property panel) ==========
  updateSelectedProps(props: any) {
    if (!this.selectedNode) return;

    const node = this.selectedNode;

    if (props.width !== undefined) {
      if ((node as any).width) (node as any).width(Number(props.width));
    }
    if (props.height !== undefined) {
      if ((node as any).height) (node as any).height(Number(props.height));
    }
    if (props.fill !== undefined) {
      if ((node as any).fill) (node as any).fill(props.fill);
    }
    if (props.text !== undefined && node instanceof Konva.Text) {
      node.text(props.text);
    }
    if (props.fontSize !== undefined && node instanceof Konva.Text) {
      node.fontSize(Number(props.fontSize));
    }

    // barcode/2D updates: regenerate image if value or barcodeType changed
    if (
      (props.value !== undefined || props.barcodeType !== undefined) &&
      node instanceof Konva.Image
    ) {
      const value = props.value ?? node.getAttr('value') ?? '';
      const type = props.barcodeType ?? node.getAttr('barcodeType') ?? 'QRCode';
      const is2D = node.getAttr('is2D') ?? true;
      const w = node.width();
      const h = node.height();

      const kind = is2D ? '2D' : '1D';
      BarcodeUtils.generateCanvas(kind, type, String(value), w, h)
        .then((canvas) => {
          const img = new Image();
          img.src = canvas.toDataURL();
          img.onload = () => {
            (node as Konva.Image).image(img);
            node.setAttr('value', value);
            node.setAttr('barcodeType', type);
            this.layer.batchDraw();
            this.saveState();
            this.zone.runOutsideAngular(() =>
              this.bus.dslChanged$.next(this.toJsonDsl())
            );
          };
        })
        .catch((err) => console.warn('Barcode regen failed', err));
    } else {
      node.getLayer()?.batchDraw();
      this.saveState();
      this.zone.runOutsideAngular(() =>
        this.bus.dslChanged$.next(this.toJsonDsl())
      );
    }
  }

  // ========== selection helpers ==========
  clearSelection() {
    this.selectedNode = null;
    this.transformer.nodes([]);
    this.layer.batchDraw();
    this.elementSelected.emit(null);
  }

  delete() {
    if (this.selectedNode) {
      this.selectedNode.destroy();
      this.clearSelection();
      this.layer.batchDraw();
      this.saveState();
      this.zone.runOutsideAngular(() =>
        this.bus.dslChanged$.next(this.toJsonDsl())
      );
    }
  }

  copy() {
    if (!this.selectedNode) return;
    const clone = (this.selectedNode as any).clone();
    clone.position({
      x: this.selectedNode.x() + 10,
      y: this.selectedNode.y() + 10,
    });
    clone.dragBoundFunc(this.getDragBoundFunc(clone));
    this.layer.add(clone);
    this.layer.batchDraw();
    this.saveState();
    this.zone.runOutsideAngular(() =>
      this.bus.dslChanged$.next(this.toJsonDsl())
    );
  }

  download() {
    this.gridLayer.visible(false);
    this.rulerLayer.visible(false);
    this.guideLayer.visible(false);
    this.transformer.visible(false);

    this.layer.batchDraw();

    const dataURL = this.layer.toDataURL({ pixelRatio: 2 });

    this.gridLayer.visible(true);
    this.rulerLayer.visible(true);
    this.guideLayer.visible(true);
    this.transformer.visible(true);
    this.layer.batchDraw();

    const a = document.createElement('a');
    a.href = dataURL;
    a.download = 'label.png';
    a.click();
  }

  // ========== history ==========
  private saveState() {
    const widgets = this.layer
      .getChildren()
      .filter(
        (child) => child !== this.paper && !(child instanceof Konva.Transformer)
      );
    const json = JSON.stringify(widgets.map((w) => w.toObject()));

    if (this.history[this.historyStep] !== json) {
      this.history = this.history.slice(0, this.historyStep + 1);
      this.history.push(json);
      this.historyStep = this.history.length - 1;
      localStorage.setItem('canvasState', json);
      // notify bus for debounced autosave
      this.zone.runOutsideAngular(() =>
        this.bus.dslChanged$.next(this.toJsonDsl())
      );
    }
  }

  undo() {
    if (this.historyStep > 0) {
      this.historyStep--;
      this.restoreFromHistory();
    }
  }

  redo() {
    if (this.historyStep < this.history.length - 1) {
      this.historyStep++;
      this.restoreFromHistory();
    }
  }

  private restoreFromHistory() {
    const json = this.history[this.historyStep];
    if (!json) return;

    this.layer.getChildren().forEach((child: Konva.Node) => {
      if (child !== this.paper && !(child instanceof Konva.Transformer))
        child.destroy();
    });

    try {
      const widgets = JSON.parse(json);
      widgets.forEach((obj: any) => {
        const node = Konva.Node.create(obj) as Konva.Shape;
        node.dragBoundFunc(this.getDragBoundFunc(node));
        this.layer.add(node);
        this.attachNodeEvents(node);
        this.widgetNodes.push(node);
      });
    } catch (err) {
      console.error('Error restoring widget history:', err);
    }

    this.layer.batchDraw();
    this.clearSelection();
    this.zone.runOutsideAngular(() =>
      this.bus.dslChanged$.next(this.toJsonDsl())
    );
  }

  // ========== restore & load DSL ==========
  private restoreFromJSON(json: string) {
    try {
      const widgets = JSON.parse(json);
      widgets.forEach((obj: any) => {
        const node = Konva.Node.create(obj) as Konva.Shape;
        node.dragBoundFunc(this.getDragBoundFunc(node));
        node.on('dragmove', () => this.showGuides(node));
        node.on('dragend', () => this.hideGuides());
        this.layer.add(node);
        this.attachNodeEvents(node);
        this.updateWidgetOpacity();
        this.widgetNodes.push(node);
      });
    } catch (err) {
      console.error('Failed to restore canvas:', err);
    }
    this.layer.batchDraw();
  }

  loadFromJsonDsl(dsl: any) {
    // accept multiple shapes of DSL:
    // - { paperSize: {width,height}, widgets: [...] }
    // - { design: { size:{width,height}, dpi? }, widgets: [...] }
    // - fallback to current paper size

    const paperSize =
      dsl && dsl.paperSize
        ? dsl.paperSize
        : dsl && dsl.design && dsl.design.size
        ? dsl.design.size
        : null;

    const width = Number(paperSize?.width ?? this.paperWidth);
    const height = Number(paperSize?.height ?? this.paperHeight);

    // reset canvas but keep stage initialized
    this.resetCanvas();

    // apply safe paper size
    this.paperWidth = isFinite(width) && width > 0 ? width : this.paperWidth;
    this.paperHeight =
      isFinite(height) && height > 0 ? height : this.paperHeight;
    this.centerAndResizePaper();

    // widgets may live under dsl.widgets or dsl.design.widgets (rare); normalize to array
    const widgets = Array.isArray(dsl?.widgets)
      ? dsl.widgets
      : Array.isArray(dsl?.design?.widgets)
      ? dsl.design.widgets
      : [];

    widgets.forEach((widget: any) => this.addWidgetFromDsl(widget));
  }

  private addWidgetFromDsl(widget: any) {
    if (!widget || typeof widget !== 'object') return;
    if (widget.type === 'text' || widget.type === 'Text') {
      this.addElement({
        type: 'widget',
        widget: 'text',
        text: widget.value ?? widget.text ?? '',
        x: widget.position?.x ?? 20,
        y: widget.position?.y ?? 20,
        placeholder: widget.placeholder,
      });
    } else if (
      widget.type === 'barcode' ||
      widget.type === 'barcode1D' ||
      widget.type === 'barcode2D' ||
      widget.type === 'qrcode' ||
      widget.type === 'datamatrix' ||
      widget.type === 'aztec' ||
      widget.type === 'maxicode'
    ) {
      const t =
        widget.barcodeType ??
        (widget.type === 'qrcode' ? 'QRCode' : widget.type);
      const w = widget.size?.width ?? widget.width ?? widget.size ?? 120;
      const h = widget.size?.height ?? widget.height ?? w;
      this.addElement({
        type: 'widget',
        widget:
          widget.type === 'barcode2D' ? 'datamatrix' : widget.type || 'qrcode',
        barcodeType: t,
        value: widget.value,
        x: widget.position?.x ?? 20,
        y: widget.position?.y ?? 20,
        width: w,
        height: h,
      });
    } else if (widget.type === 'shape') {
      this.addElement({
        type: 'shape',
        shape: widget.shape,
        x: widget.position?.x ?? 20,
        y: widget.position?.y ?? 20,
        width: widget.size?.width,
        height: widget.size?.height,
        color: widget.color,
      });
    } else {
      this.addElement({
        type: 'shape',
        shape: 'rect',
        x: widget.position?.x ?? 20,
        y: widget.position?.y ?? 20,
      });
    }
  }

  // ========== paper, rulers, grid, guides ==========
  private centerAndResizePaper() {
    this.paper.width(this.paperWidth);
    this.paper.height(this.paperHeight);
    this.centerPaper();
    if (this.gridLayer) this.gridLayer.destroy();
    if (this.rulerLayer) this.rulerLayer.destroy();
    if (this.guideLayer) this.guideLayer.destroy();
    this.drawGrid();
    this.drawRulers();
    this.layer.batchDraw();
    this.updateWidgetOpacity();
  }

  centerPaper() {
    const x = (this.stage.width() - this.paper.width()) / 2;
    const y = (this.stage.height() - this.paper.height()) / 2;
    this.paper.position({ x, y });
    this.layer.batchDraw();
  }

  private drawGrid() {
    this.gridLayer = new Konva.Layer();
    const gridSize = 20;
    for (let i = 0; i < this.stage.width(); i += gridSize)
      this.gridLayer.add(
        new Konva.Line({
          points: [i, 0, i, this.stage.height()],
          stroke: '#ddd',
          strokeWidth: 1,
        })
      );
    for (let j = 0; j < this.stage.height(); j += gridSize)
      this.gridLayer.add(
        new Konva.Line({
          points: [0, j, this.stage.width(), j],
          stroke: '#ddd',
          strokeWidth: 1,
        })
      );
    this.stage.add(this.gridLayer);
    this.gridLayer.moveToBottom();
  }

  private drawRulers() {
    this.rulerLayer = new Konva.Layer();
    for (let x = 0; x < this.stage.width(); x += 10) {
      this.rulerLayer.add(
        new Konva.Line({
          points: [x, 0, x, x % 50 === 0 ? 15 : 8],
          stroke: '#666',
          strokeWidth: 1,
        })
      );
      if (x % 50 === 0)
        this.rulerLayer.add(
          new Konva.Text({
            x: x + 2,
            y: 2,
            text: String(x),
            fontSize: 10,
            fill: '#333',
          })
        );
    }
    for (let y = 0; y < this.stage.height(); y += 10) {
      this.rulerLayer.add(
        new Konva.Line({
          points: [0, y, y % 50 === 0 ? 15 : 8, y],
          stroke: '#666',
          strokeWidth: 1,
        })
      );
      if (y % 50 === 0)
        this.rulerLayer.add(
          new Konva.Text({
            x: 2,
            y: y + 2,
            text: String(y),
            fontSize: 10,
            fill: '#333',
          })
        );
    }
    this.stage.add(this.rulerLayer);
    this.rulerLayer.moveToBottom();
  }

  private updateWidgetOpacity() {
    this.layer.getChildren().forEach((child) => {
      if (child === this.paper || child instanceof Konva.Transformer) return;

      const box = (child as any).getClientRect();
      const paperBox = this.paper.getClientRect();

      const isOutside =
        box.x < paperBox.x ||
        box.y < paperBox.y ||
        box.x + box.width > paperBox.x + paperBox.width ||
        box.y + box.height > paperBox.y + paperBox.height;

      child.opacity(isOutside ? 0.4 : 1);
    });

    this.layer.batchDraw();
  }

  private showGuides(node: Konva.Shape) {
    this.guideLayer.destroyChildren();

    const nodeBox = node.getClientRect();
    const snapPointsX = [
      this.paper.x(),
      this.paper.x() + this.paper.width() / 2,
      this.paper.x() + this.paper.width(),
    ];
    const snapPointsY = [
      this.paper.y(),
      this.paper.y() + this.paper.height() / 2,
      this.paper.y() + this.paper.height(),
    ];

    for (const x of snapPointsX) {
      if (Math.abs(nodeBox.x - x) < this.snapTolerance) {
        node.x(x);
        this.guideLayer.add(
          new Konva.Line({
            points: [
              x,
              this.paper.y(),
              x,
              this.paper.y() + this.paper.height(),
            ],
            stroke: 'blue',
            strokeWidth: 1,
            dash: [4, 4],
          })
        );
      }
    }

    for (const y of snapPointsY) {
      if (Math.abs(nodeBox.y - y) < this.snapTolerance) {
        node.y(y);
        this.guideLayer.add(
          new Konva.Line({
            points: [this.paper.x(), y, this.paper.x() + this.paper.width(), y],
            stroke: 'blue',
            strokeWidth: 1,
            dash: [4, 4],
          })
        );
      }
    }

    this.guideLayer.batchDraw();
  }

  private hideGuides() {
    this.guideLayer.destroyChildren();
    this.guideLayer.batchDraw();
  }

  private attachNodeEvents(node: Konva.Node) {
    node.on('dragmove transformend dragend', () => {
      this.updateWidgetOpacity();
      this.saveState();
      this.zone.runOutsideAngular(() =>
        this.bus.dslChanged$.next(this.toJsonDsl())
      );
    });
    node.on('click', () => {
      this.selectedNode = node as Konva.Shape;
      this.transformer.nodes([this.selectedNode]);
      this.elementSelected.emit(this.selectedNode);
    });
  }

  // ========== Backend save/load (legacy TemplateService path) ==========
  saveToBackend(useHjson = false) {
    const dsl = this.toJsonDsl();
    const payload = {
      templateId: this.currentTemplateId,
      templateName: dsl.templateName || 'UnnamedTemplate',
      jsonDsl: JSON.stringify(dsl),
      semVer: '1.0.0',
      state: 'Draft',
      createdBy: 'web-user',
    };

    this.templateService.saveTemplateVersion(payload).subscribe({
      next: (res: any) => {
        console.log('Saved version', res);
        if (res?.id) this.currentVersionId = res.id;
      },
      error: (err: HttpErrorResponse) => {
        console.error('Save failed', err.message, err.status);
      },
    });
  }

  loadFromBackend(templateId: string, versionId: string) {
    this.templateService
      .getTemplateVersion(templateId, versionId)
      .subscribe((ver: any) => {
        const dsl = JSON.parse(ver.jsonDsl);
        this.loadFromJsonDsl(dsl);
      });
  }

  private clampAllNodesToPaper() {
    this.layer.getChildren().forEach((child) => {
      if (child === this.paper || child instanceof Konva.Transformer) return;
      const box = child.getClientRect();

      let newX = child.x();
      let newY = child.y();

      if (box.x < this.paper.x()) newX = this.paper.x();
      if (box.y < this.paper.y()) newY = this.paper.y();
      if (box.x + box.width > this.paper.x() + this.paper.width())
        newX = this.paper.x() + this.paper.width() - box.width;
      if (box.y + box.height > this.paper.y() + this.paper.height())
        newY = this.paper.y() + this.paper.height() - box.height;

      child.position({ x: newX, y: newY });
    });

    this.layer.batchDraw();
  }

  /** Paper size */
  setPaperSize(event: Event) {
    const select = event.target as HTMLSelectElement;
    const value = select.value;

    if (value === 'custom') {
      this.customSizeSelected = true;
      return;
    }

    this.customSizeSelected = false;

    const paperSizes: Record<string, { w: number; h: number }> = {
      a4: { w: 210 * 3.78, h: 297 * 3.78 },
      a3: { w: 297 * 3.78, h: 420 * 3.78 },
      '4:3': { w: 800, h: 600 },
      '16:9': { w: 1280, h: 720 },
    };

    const size = paperSizes[value];
    if (!size) return;

    this.paperWidth = size.w;
    this.paperHeight = size.h;
    this.centerAndResizePaper();
  }

  applyCustomSize() {
    this.paperWidth = this.customWidth;
    this.paperHeight = this.customHeight;
    this.centerAndResizePaper();
  }

  // ========== reset ==========
  public resetCanvas() {
    for (const node of this.widgetNodes) node.destroy();
    this.widgetNodes = [];

    const children = this.layer.getChildren();
    for (const child of children) {
      if (child === this.paper || child instanceof Konva.Transformer) continue;
      child.destroy();
    }

    this.clearSelection();
    this.selectedNode = null;
    this.droppedElement = null;
    this.history = [];
    this.historyStep = -1;
    localStorage.removeItem('canvasState');

    this.centerPaper();

    this.layer.batchDraw();
    this.updateWidgetOpacity();
  }

  // ---------- NODE FACTORIES ----------
  createLine(options: any = {}) {
    return this.createNode(
      new Konva.Line({
        points: options.points || [0, 0, 100, 100],
        stroke: options.stroke || 'black',
        strokeWidth: options.strokeWidth || 2,
        lineCap: options.lineCap || 'round',
        lineJoin: options.lineJoin || 'round',
        dash: options.dash || [],
        tension: options.tension || 0,
        closed: options.closed || false,
        draggable: true,
        id: options.id,
        name: options.name,
      })
    );
  }

  createRect(options: any = {}) {
    return this.createNode(
      new Konva.Rect({
        x: options.x || 50,
        y: options.y || 50,
        width: options.width || 100,
        height: options.height || 50,
        stroke: options.stroke || 'black',
        strokeWidth: options.strokeWidth || 2,
        fill: options.fill || 'transparent',
        cornerRadius: options.cornerRadius || 0,
        draggable: true,
        id: options.id,
        name: options.name,
      })
    );
  }

  createCircle(options: any = {}) {
    return this.createNode(
      new Konva.Circle({
        x: options.x || 100,
        y: options.y || 100,
        radius: options.radius || 50,
        stroke: options.stroke || 'black',
        strokeWidth: options.strokeWidth || 2,
        fill: options.fill || 'transparent',
        draggable: true,
        id: options.id,
        name: options.name,
      })
    );
  }

  createText(options: any = {}) {
    return this.createNode(
      new Konva.Text({
        x: options.x || 50,
        y: options.y || 50,
        text: options.text || 'Sample Text',
        fontSize: options.fontSize || 18,
        fontFamily: options.fontFamily || 'Arial',
        fill: options.fill || 'black',
        width: options.width,
        align: options.align || 'left',
        draggable: true,
        id: options.id,
        name: options.name,
        placeholder: options.placeholder,
      })
    );
  }

  createImage(options: any = {}) {
    const imgEl = new Image();
    imgEl.src = options.src || '';
    return this.createNode(
      new Konva.Image({
        x: options.x || 50,
        y: options.y || 50,
        width: options.width || 100,
        height: options.height || 100,
        image: imgEl,
        draggable: true,
        id: options.id,
        name: options.name,
        barcodeType: options.barcodeType,
        is2D: options.is2D,
        value: options.value,
      })
    );
  }

  private createNode(node: Konva.Shape | Konva.Group) {
    node.on('click', (e) => {
      this.selectedNode = node;
      this.elementSelected.emit(node);
      e.cancelBubble = true;
    });
    this.layer.add(node);
    this.widgetNodes.push(node);
    this.layer.draw();
    return node;
  }

  toJsonDsl(): any {
    const dsl: any = {
      templateName: '',
      paperSize: { width: this.stage.width(), height: this.stage.height() },
      widgets: this.widgetNodes
        .map((node) => {
          const type = node.getClassName();

          if (type === 'Text') {
            const t = node as Konva.Text;
            const widget = {
              type: 'text',
              value: t.text(),
              placeholder: (t as any).getAttr('placeholder') || '',
              position: { x: t.x(), y: t.y() },
              size: { width: t.width(), height: t.height() },
              fontSize: t.fontSize(),
              color: t.fill(),
            };
            if (widget.placeholder === 'templateName')
              dsl.templateName = widget.value;
            return widget;
          }

          if (type === 'Image' && (node as any).getAttr('barcodeType')) {
            return {
              type: (node as any).getAttr('is2D') ? 'barcode2D' : 'barcode1D',
              value: (node as any).getAttr('value'),
              barcodeType: (node as any).getAttr('barcodeType'),
              position: { x: node.x(), y: node.y() },
              size: {
                width: (node as any).width(),
                height: (node as any).height(),
              },
            };
          }

          if (type === 'Line') {
            const l = node as Konva.Line;
            return {
              type: 'shape',
              shape: 'line',
              points: l.points(),
              position: { x: l.x(), y: l.y() },
              stroke: l.stroke(),
              strokeWidth: l.strokeWidth(),
              dash: l.dash(),
              draggable: l.draggable(),
            };
          }

          if (type === 'Rect') {
            const r = node as Konva.Rect;
            return {
              type: 'shape',
              shape: 'rect',
              position: { x: r.x(), y: r.y() },
              size: { width: r.width(), height: r.height() },
              fill: r.fill(),
              cornerRadius: r.cornerRadius(),
              draggable: r.draggable(),
            };
          }

          if (type === 'Circle') {
            const c = node as Konva.Circle;
            return {
              type: 'shape',
              shape: 'circle',
              position: { x: c.x(), y: c.y() },
              radius: c.radius(),
              stroke: c.stroke(),
              fill: c.fill(),
              draggable: c.draggable(),
            };
          }

          return null;
        })
        .filter(Boolean),
    };

    return dsl;
  }

  fromJsonDsl(obj: any): Konva.Node {
    let node: Konva.Shape | Konva.Group;

    switch (obj.type) {
      case 'shape':
        switch (obj.shape) {
          case 'line':
            node = new Konva.Line({
              points: obj.points || [0, 0, 100, 100],
              stroke: obj.stroke || 'black',
              strokeWidth: obj.strokeWidth || 2,
              dash: obj.dash || [],
              lineCap: obj.lineCap || 'round',
              lineJoin: obj.lineJoin || 'round',
              tension: obj.tension || 0,
              closed: obj.closed || false,
              draggable: obj.draggable ?? true,
              id: obj.id,
              name: obj.name,
            });
            break;

          case 'rect':
            node = new Konva.Rect({
              x: obj.position?.x || 0,
              y: obj.position?.y || 0,
              width: obj.size?.width || 100,
              height: obj.size?.height || 50,
              fill: obj.fill || obj.color || 'transparent',
              cornerRadius: obj.cornerRadius || 0,
              draggable: obj.draggable ?? true,
              id: obj.id,
              name: obj.name,
            });
            break;

          case 'circle':
            node = new Konva.Circle({
              x: obj.position?.x || 0,
              y: obj.position?.y || 0,
              radius: obj.radius || 50,
              fill: obj.fill || obj.color || 'transparent',
              draggable: obj.draggable ?? true,
              id: obj.id,
              name: obj.name,
            });
            break;

          default:
            throw new Error('Unknown shape type');
        }
        break;

      case 'text':
        node = new Konva.Text({
          x: obj.position?.x || 0,
          y: obj.position?.y || 0,
          text: obj.value || '',
          fontSize: obj.fontSize || 18,
          fill: obj.color || 'black',
          width: obj.size?.width,
          align: obj.align || 'left',
          draggable: obj.draggable ?? true,
          id: obj.id,
          name: obj.name,
          placeholder: obj.placeholder,
        });
        break;

      case 'barcode1D':
      case 'barcode2D': {
        const imgEl = new Image();
        imgEl.src = ''; // load dynamically if needed
        node = new Konva.Image({
          x: obj.position?.x || 0,
          y: obj.position?.y || 0,
          width: obj.size?.width || 100,
          height: obj.size?.height || 50,
          image: imgEl,
          draggable: obj.draggable ?? true,
          id: obj.id,
          name: obj.name,
          barcodeType: obj.barcodeType,
          is2D: obj.type === 'barcode2D',
          value: obj.value,
        });
        break;
      }

      default:
        throw new Error(`Unknown type: ${obj.type}`);
    }

    this.createNode(node);
    return node;
  }
}
