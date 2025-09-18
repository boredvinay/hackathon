import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DesignerPageComponent } from './designer-page/designer-page.component';
import { CanvasComponent } from './canvas/canvas.component';

import { PropertyPanelComponent } from './property-panel/property-panel.component';
import { ToolbarComponent } from './toolbar/toolbar.component';
import { FormsModule } from '@angular/forms';



@NgModule({
  declarations: [
    DesignerPageComponent,
    CanvasComponent,
    
    ToolbarComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    PropertyPanelComponent,
  ],
  exports: [
    DesignerPageComponent   // 👈 export so AppModule can use it
  ]
})
export class DesignerModule { }
