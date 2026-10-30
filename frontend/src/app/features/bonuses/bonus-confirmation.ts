import { AfterViewInit, Component, ElementRef, ViewChild, input, output } from '@angular/core';
import { Bonus } from '../../core/models/bonus';

@Component({
  host: { class: 'modal modal-shell' },
  selector: 'app-bonus-confirmation',
  template: `
    <dialog #dialog class="bonus-dialog bonus-dialog-sm modal-content shadow" aria-labelledby="confirm-title" (cancel)="close($event)">
      <header class="modal-header"><h2 id="confirm-title" class="modal-title fs-5">{{ pending().action === 'cancel' ? 'Anular' : 'Eliminar' }} bono</h2></header>
      <div class="modal-body"><p class="fw-semibold">{{ pending().bonus.fullName }} · {{ pending().bonus.period }}</p>
        <p class="text-body-secondary mb-0">{{ pending().action === 'cancel' ? 'El bono quedará en estado Anulado.' : 'El bono dejará de aparecer en el listado; se conservará su historial.' }}</p></div>
      <footer class="modal-footer"><button type="button" class="btn btn-outline-secondary" [disabled]="busy()" (click)="close()" autofocus>Volver</button>
        <button type="button" class="btn btn-danger" [disabled]="busy()" (click)="confirmed.emit()">{{ busy() ? 'Procesando…' : 'Confirmar' }}</button></footer>
    </dialog>
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
