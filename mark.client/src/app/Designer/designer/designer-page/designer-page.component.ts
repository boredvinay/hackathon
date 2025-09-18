import { Component, ViewChild } from '@angular/core';
import Konva from 'konva';
import { CanvasComponent } from '../canvas/canvas.component';

@Component({
  selector: 'app-designer-page',
  standalone: false,
  templateUrl: './designer-page.component.html',
  styleUrl: './designer-page.component.css'
})
export class DesignerPageComponent {
  droppedElement: any;
  selectedElement: Konva.Node | null = null;
    debugDsl: any = null;

  @ViewChild('canvasComp') canvasComp!: CanvasComponent;

  droppedElementHandler(element: any) {
    this.droppedElement = element;
    setTimeout(() => this.droppedElement = null, 0);
  }

  selectedElementHandler(node: Konva.Node | null) {
    this.selectedElement = node;
  }

}
