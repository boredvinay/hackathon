import { Component } from '@angular/core';
import { Router } from '@angular/router';
import Highcharts from 'highcharts';
import { HighchartsChartModule } from 'highcharts-angular';
import { LeftNavComponent } from '../shared/left-nav.component';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
  standalone: true,
  imports: [HighchartsChartModule, LeftNavComponent],
})
export class DashboardComponent {

  Highcharts: typeof Highcharts = Highcharts;

  constructor(private router: Router) {}

  // Job Statistics Chart
  jobChartOptions: Highcharts.Options = {
    chart: {
      type: 'column',
      backgroundColor: 'transparent',
      animation: true
    },
    title: { text: 'Job Statistics', style: { color: '#013220' } },
    xAxis: { 
      categories: ['Pending', 'Running', 'Completed'], 
      labels: { style: { color: '#333' } } 
    },
    yAxis: { 
      title: { text: 'Count', style: { color: '#333' } } 
    },
   series: [
    {
      type: 'column',
      name: 'Jobs',
      color: '#013220', // Dark Green for both columns and the legend dot
      data: [
        { y: 10, color: '#808080' },     // Pending (Gold)
        { y: 15, color: '#B8860B' },     // Running (Blue)
        { y: 30, color: '#013220' }      // Completed (Dark Green)
      ]
    }
  ],
    plotOptions: { 
      column: { borderRadius: 5, animation: { duration: 1500 } } 
    }
  };

  // System Health Pie Chart
  systemHealthOptions: Highcharts.Options = {
    chart: { type: 'pie', backgroundColor: 'transparent', animation: true },
    title: { text: 'System Health', style: { color: '#013220' } },
    series: [
      {
        type: 'pie',
        name: 'Status',
        data: [
          { name: 'Healthy', y: 80, color: '#013220' },
          { name: 'Warning', y: 15, color: '#B8860B' },
          { name: 'Critical', y: 5, color: '#800020' }
        ]
      }
    ],
    plotOptions: {
      pie: {
        animation: { duration: 1200 },
        dataLabels: { enabled: true, style: { color: '#333' } }
      }
    }
  };

  // Template Usage Line Chart
  templateChartOptions: Highcharts.Options = {
    chart: { type: 'line', backgroundColor: 'transparent', animation: true },
    title: { text: 'Template Usage', style: { color: '#013220' } },
    xAxis: { categories: ['Jan', 'Feb', 'Mar'], labels: { style: { color: '#333' } } },
    yAxis: { title: { text: 'Templates', style: { color: '#333' } } },
    series: [
      { type: 'line', name: 'Usage', data: [5, 12, 18], color: '#013220' }
    ],
    plotOptions: { line: { animation: { duration: 1500 } } }
  };

  // Navigation
  goTo(path: string) {
    this.router.navigate([path]);
  }
}
