import { Component, EventEmitter, Output } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-toolbar',
  standalone: false,
  templateUrl: './toolbar.component.html',
  styleUrl: './toolbar.component.css'
})
export class ToolbarComponent {
  activeSection: string = localStorage.getItem('activeSection') || 'elements';

  // inline SVG data-URI fallback to guarantee a visible logo despite asset 404s
  logoSrc = 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64"><rect width="100%" height="100%" fill="%23013220" rx="8"/><circle cx="32" cy="24" r="12" fill="%23fff"/><text x="32" y="54" font-size="12" text-anchor="middle" fill="%23fff" font-family="Segoe UI,Arial">MARK</text></svg>';
  logoFailed = false;
  logoProbe: string | null = null;
  logoProbePng: string | null = null;

  @Output() elementDropped = new EventEmitter<any>();

  constructor(private router: Router) {
    setTimeout(() => this.probeLogos(), 0);
  }

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

  goHome() {
    // navigate to dashboard
    try { this.router.navigate(['/dashboard']); } catch (e) { /* noop */ }
  }

  onImgError() {
    console.warn('Toolbar: failed to load', this.logoSrc);
    if (!this.logoFailed && this.logoSrc !== '/assets/logo.png') {
      this.logoSrc = '/assets/logo.png';
      this.logoFailed = true;
      return;
    }
    this.logoFailed = true;
  }

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



