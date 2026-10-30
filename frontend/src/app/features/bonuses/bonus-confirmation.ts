import { AfterViewInit, Component, ElementRef, ViewChild, input, output } from '@angular/core';
import { Bonus } from '../../core/models/bonus';

@Component({
  selector: 'app-bonus-confirmation',
  template: `
    <dialog #dialog aria-labelledby="confirm-title" (cancel)="close($event)">
      <h2 id="confirm-title">{{ pending().action === 'cancel' ? 'Anular' : 'Eliminar' }} bono</h2>
      <p>{{ pending().bonus.fullName }} · {{ pending().bonus.period }}</p>
      <p>{{ pending().action === 'cancel' ? 'El bono quedará en estado Anulado.' : 'El bono dejará de aparecer en el listado; se conservará su historial.' }}</p>
      <footer><button type="button" class="btn btn-secondary" [disabled]="busy()" (click)="close()" autofocus>Volver</button>
        <button type="button" class="btn btn-danger" [disabled]="busy()" (click)="confirmed.emit()">{{ busy() ? 'Procesando…' : 'Confirmar' }}</button></footer>
    </dialog>
  `,
  styles: `
    dialog { border:0; width:min(440px,calc(100vw - 32px)); border-radius:12px; padding:28px; color:var(--text); box-shadow:0 20px 60px #10243b33; }
    dialog::backdrop { background:#14283c80; }
    h2 { font-size:1.3rem; margin:0; }
    p { line-height:1.6; font-size:.9rem; }
    footer { display:flex; justify-content:flex-end; gap:12px; margin-top:24px; }
  `,
})
export class BonusConfirmation implements AfterViewInit {
  readonly pending = input.required<{ bonus: Bonus; action: 'cancel' | 'delete' }>();
  readonly busy = input(false);
  readonly confirmed = output<void>();
  readonly closed = output<void>();
  @ViewChild('dialog', { static: true }) private dialog!: ElementRef<HTMLDialogElement>;
  ngAfterViewInit(): void { this.dialog.nativeElement.showModal(); }
  protected close(event?: Event): void { event?.preventDefault(); if (!this.busy()) this.closed.emit(); }
}
