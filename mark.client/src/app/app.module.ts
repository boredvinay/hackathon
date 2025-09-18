import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { CommonModule } from '@angular/common';
import { DesignerModule } from './Designer/designer/designer.module';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';

import { BaseChartDirective, provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { DashboardComponent } from './dashboard/dashboard.component';
import { HighchartsChartModule } from 'highcharts-angular'; 

@NgModule({
  declarations: [
    AppComponent,
     
  ],
  imports: [
    BrowserModule, HttpClientModule,
    AppRoutingModule,
    FormsModule,
    DesignerModule,
    CommonModule,
    HighchartsChartModule,
    FontAwesomeModule,
     DashboardComponent,
  ],
  providers: [
    provideCharts(withDefaultRegisterables())  // register all chart types
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
