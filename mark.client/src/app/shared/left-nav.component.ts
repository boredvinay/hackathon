import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-left-nav',
  templateUrl: './left-nav.component.html',
  styleUrls: ['./left-nav.component.css']
})
export class LeftNavComponent {
  // inline SVG data-URI fallback to guarantee a visible logo despite asset 404s
  // small compact SVG: circle + MARK text
  logoSrc = 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64"><rect width="100%" height="100%" fill="%23013220" rx="8"/><circle cx="32" cy="24" r="12" fill="%23fff"/><text x="32" y="54" font-size="12" text-anchor="middle" fill="%23fff" font-family="Segoe UI,Arial">MARK</text></svg>';
  logoFailed = false;
  logoProbe: string | null = null;
  logoProbePng: string | null = null;

  constructor(private router: Router) {
    setTimeout(() => this.probeLogos(), 0);
  }

  goTo(path: string) {
    this.router.navigate([path]);
  }

  onImgError() {
    console.warn('LeftNav: failed to load', this.logoSrc);
    if (!this.logoFailed && this.logoSrc !== '/assets/logo.png') {
      this.logoSrc = '/assets/logo.png';
      this.logoFailed = true;
      return;
    }
    this.logoFailed = true;
  }

  // probe asset URLs and store simple status strings for debugging in the template
  async probeLogos() {
    try {
      const r = await fetch('/assets/logo.webp', { method: 'HEAD' });
      this.logoProbe = `${r.status} ${r.statusText}`;
    } catch (e) {
      this.logoProbe = `error: ${(e as any)?.message ?? e}`;
    }

    try {
      const r2 = await fetch('/assets/logo.png', { method: 'HEAD' });
      this.logoProbePng = `${r2.status} ${r2.statusText}`;
    } catch (e) {
      this.logoProbePng = `error: ${(e as any)?.message ?? e}`;
    }
  }
}
