import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DesignerPageComponent } from './Designer/designer/designer-page/designer-page.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { TemplatesListComponent } from './templates/templates-list.component';

const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'designer', component: DesignerPageComponent },
  { path: 'designer/:designId', component: DesignerPageComponent },
  { path: 'designer/:designId/:versionId', component: DesignerPageComponent },
  { path: 'templates', component: TemplatesListComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { 



  
}
