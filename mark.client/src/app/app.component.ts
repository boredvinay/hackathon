import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: false,
  styleUrl: './app.component.css',
})
export class AppComponent implements OnInit {
  public forecasts: WeatherForecast[] = [];
  showLeftNav = true;

  constructor(private http: HttpClient, private router: Router) {
    // Hide left nav on any designer route
    this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe((ev: any) => {
      const url: string = ev?.urlAfterRedirects ?? ev?.url ?? '';
      this.showLeftNav = !url.startsWith('/designer');
    });
  }

  ngOnInit() {}

  title = 'mark.client';
}
