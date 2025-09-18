import { Component, EventEmitter, Output } from '@angular/core';

@Component({
  selector: 'app-toolbar',
  standalone: false,
  templateUrl: './toolbar.component.html',
  styleUrl: './toolbar.component.css'
})
export class ToolbarComponent {
  activeSection: string = localStorage.getItem('activeSection') || 'elements';

  @Output() elementDropped = new EventEmitter<any>();

  toggleRightPanel(section: string) {
    this.activeSection = section;
    localStorage.setItem('activeSection', section); // 👈 save choice
  }

  addShape(type: string) {
    this.elementDropped.emit({ type: 'shape', shape: type });
  }

  addWidget(type: string) {
    this.elementDropped.emit({ type: 'widget', widget: type });
  }

  onDragStart(event: DragEvent, type: string, name: string) {
    const payload: any = { type };
    if (type === 'shape') payload.shape = name;
    if (type === 'widget') payload.widget = name;

    event.dataTransfer?.setData('application/json', JSON.stringify(payload));
  }

  addDirectly(type: 'shape' | 'widget', name: string) {
    this.elementDropped.emit({
      type,
      shape: type === 'shape' ? name : undefined,
      widget: type === 'widget' ? name : undefined,
      x: 100,
      y: 100
    });
  }
}



