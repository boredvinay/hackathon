import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import Konva from 'konva';

@Component({
  selector: 'app-property-panel',
  imports: [CommonModule, FormsModule],

  templateUrl: './property-panel.component.html',
  styleUrl: './property-panel.component.css'
})
export class PropertyPanelComponent {
  @Input() selectedElement: Konva.Node | null = null;
  @Output() updateProps = new EventEmitter<any>();

  // --- Helpers for element type ---
  get isTextElement(): boolean {
    return this.selectedElement?.getClassName?.() === 'Text';
  }

  get isLineElement(): boolean {
    return this.selectedElement?.getClassName?.() === 'Line';
  }

  get isShapeElement(): boolean {
    const name = this.selectedElement?.getClassName?.();
    return name != null && name !== 'Line' && name !== 'Text';
  }

  get isCodeElement(): boolean {
    const type = this.selectedElement?.getAttr?.('type') ?? this.selectedElement?.getAttr?.('widget');
    return type != null && ['barcode', 'QRCode', 'DataMatrix', 'Aztec', 'MaxiCode'].includes(type);
  }

  // --- Barcode ---
  get barcodeValue(): string {
    return this.selectedElement?.getAttr?.('value') || '';
  }
  set barcodeValue(val: string) {
    if (!this.selectedElement?.getLayer) return;
    this.selectedElement.setAttr?.('value', val);
    this.selectedElement.getLayer()?.batchDraw();
    this.updateProps.emit({ value: val });
  }

  get barcodeType(): string {
    return this.selectedElement?.getAttr?.('barcodeType') || 'Code128';
  }
  set barcodeType(val: string) {
    if (!this.selectedElement?.getLayer) return;
    this.selectedElement.setAttr?.('barcodeType', val);
    this.selectedElement.getLayer()?.batchDraw();
    this.updateProps.emit({ barcodeType: val });
  }

  // --- Text properties ---
  get textValue(): string {
    return this.isTextElement ? (this.selectedElement as Konva.Text).text() : '';
  }
  set textValue(val: string) {
    if (!this.isTextElement) return;
    (this.selectedElement as Konva.Text).text(val);
    this.selectedElement?.getLayer()?.batchDraw();
    this.updateProps.emit({ text: val });
  }

  get fontSizeValue(): number {
    return this.isTextElement ? (this.selectedElement as Konva.Text).fontSize() : 12;
  }
  set fontSizeValue(val: number) {
    if (!this.isTextElement) return;
    (this.selectedElement as Konva.Text).fontSize(Number(val));
    this.selectedElement?.getLayer()?.batchDraw();
    this.updateProps.emit({ fontSize: val });
  }

  get fontFamilyValue(): string {
    return this.isTextElement ? (this.selectedElement as Konva.Text).fontFamily() : 'Arial';
  }
  set fontFamilyValue(val: string) {
    if (!this.isTextElement) return;
    (this.selectedElement as Konva.Text).fontFamily(val);
    this.selectedElement?.getLayer()?.batchDraw();
    this.updateProps.emit({ fontFamily: val });
  }

  get fillValue(): string {
    if (!this.selectedElement) return '#808080';
    return 'fill' in this.selectedElement && typeof (this.selectedElement as any).fill === 'function'
      ? (this.selectedElement as any).fill()
      : '#808080';
  }

  // --- Text styles ---
  toggleBold(val: boolean) {
    if (!this.isTextElement) return;
    let style = (this.selectedElement as Konva.Text).fontStyle() || '';
    style = val ? `${style} bold`.trim() : style.replace('bold', '').trim();
    (this.selectedElement as Konva.Text).fontStyle(style || 'normal');
    this.selectedElement?.getLayer()?.batchDraw();
    this.updateProps.emit({ fontStyle: style });
  }

  toggleItalic(val: boolean) {
    if (!this.isTextElement) return;
    let style = (this.selectedElement as Konva.Text).fontStyle() || '';
    style = val ? `${style} italic`.trim() : style.replace('italic', '').trim();
    (this.selectedElement as Konva.Text).fontStyle(style || 'normal');
    this.selectedElement?.getLayer()?.batchDraw();
    this.updateProps.emit({ fontStyle: style });
  }

  toggleUnderline(val: boolean) {
    if (!this.isTextElement) return;
    (this.selectedElement as Konva.Text).textDecoration(val ? 'underline' : '');
    this.selectedElement?.getLayer()?.batchDraw();
    this.updateProps.emit({ textDecoration: val ? 'underline' : 'none' });
  }

  get isBold(): boolean {
    return this.isTextElement && (this.selectedElement as Konva.Text).fontStyle()?.includes('bold');
  }

  get isItalic(): boolean {
    return this.isTextElement && (this.selectedElement as Konva.Text).fontStyle()?.includes('italic');
  }

  get isUnderline(): boolean {
    return this.isTextElement && (this.selectedElement as Konva.Text).textDecoration() === 'underline';
  }

  // --- Generic updates for shapes / lines ---
  update(prop: string, value: any) {
    if (!this.selectedElement) return;

    const node = this.selectedElement;
    switch (prop) {
      case 'color':
        node.setAttr?.('fill', value);
        break;
      case 'stroke':
        node.setAttr?.('stroke', value);
        break;
      case 'strokeWidth':
        node.setAttr?.('strokeWidth', Number(value));
        break;
      case 'lineStyle':
        if (node.getClassName?.() === 'Line') {
          if (value === 'solid') node.setAttr?.('dash', []);
          if (value === 'dashed') node.setAttr?.('dash', [10, 5]);
          if (value === 'dotted') node.setAttr?.('dash', [2, 4]);
        }
        break;
      default:
        const parsed = Number(value);
        node.setAttr?.(prop, !isNaN(parsed) ? parsed : value);
    }

    node.getLayer?.()?.batchDraw?.();
    this.updateProps.emit({ [prop]: value });
  }
  // Line controls
  get strokeColor(): string {
    return this.selectedElement?.getAttr('stroke') || '#000';
  }

  get strokeWidth(): number {
    return this.selectedElement?.getAttr('strokeWidth') ?? 1;
  }

}
