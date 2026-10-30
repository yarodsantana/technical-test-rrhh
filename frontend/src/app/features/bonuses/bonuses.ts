import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { EMPTY, Subject, catchError, finalize, forkJoin, switchMap } from 'rxjs';
import { Bonus, BonusFilter } from '../../core/models/bonus';
import { BonusReason, BonusType, Department, State, MasterStateCodes, BonusStates } from '../../core/models/catalog';
import { PagedResponse } from '../../core/models/paged-response';
import { BonusApi } from '../../core/services/bonus-api';
import { CatalogApi } from '../../core/services/catalog-api';
import { Notification } from '../../core/services/notification';
import { BonusForm } from './bonus-form';
import { BonusConfirmation } from './bonus-confirmation';

const emptyFilter = (): BonusFilter => ({ departmentCode: '', employeeNumber: null, period: '', stateCode: '' });

@Component({
  selector: 'app-bonuses',
  imports: [CommonModule, ReactiveFormsModule, BonusForm, BonusConfirmation],
  templateUrl: './bonuses.html',
  styleUrl: './bonuses.css',
})
export class Bonuses implements OnInit {
  private readonly api = inject(BonusApi);
  private readonly catalog = inject(CatalogApi);
  private readonly notification = inject(Notification);
  private readonly destroyRef = inject(DestroyRef);
  private readonly requests = new Subject<number>();
  private appliedFilter = emptyFilter();
  protected readonly registered = BonusStates.Registered;
  protected readonly departments = signal<Department[]>([]);
  protected readonly bonusStates = signal<State[]>([]);
  protected readonly bonusTypes = signal<BonusType[]>([]);
  protected readonly reasons = signal<BonusReason[]>([]);
  protected readonly loading = signal(false);
  protected readonly failed = signal(false);
  protected readonly catalogsReady = signal(false);
  protected readonly showForm = signal(false);
  protected readonly confirmation = signal<{ bonus: Bonus; action: 'cancel' | 'delete' } | null>(null);
  protected readonly acting = signal(false);
  protected readonly page = signal<PagedResponse<Bonus>>({ pageNumber: 1, pageSize: 10, totalPages: 0, totalRecords: 0, data: [] });
  protected readonly filters = inject(FormBuilder).group({
    departmentCode: [''], employeeNumber: [null as number | null, [Validators.min(1), Validators.pattern(/^\d+$/)]],
    period: [''], stateCode: [''],
  });

  ngOnInit(): void {
    this.requests.pipe(
      switchMap(page => {
        this.loading.set(true);
        this.failed.set(false);
        return this.api.list(this.appliedFilter, page).pipe(
          catchError(() => { this.failed.set(true); return EMPTY; }),
          finalize(() => this.loading.set(false)),
        );
      }), takeUntilDestroyed(this.destroyRef),
    ).subscribe(result => {
      // When the last row of a page is removed, move to the preceding existing page.
      if (result.pageNumber > 1 && result.data.length === 0) {
        this.requests.next(Math.max(1, result.totalPages));
      } else this.page.set(result);
    });
    this.loadCatalogs();
    this.requests.next(1);
  }

  protected loadCatalogs(): void {
    forkJoin({ departments: this.catalog.getDepartments(), states: this.catalog.getStates(MasterStateCodes.Bonus),
      types: this.catalog.getBonusTypes(), reasons: this.catalog.getBonusReasons() })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: data => {
        this.departments.set(data.departments); this.bonusStates.set(data.states);
        this.bonusTypes.set(data.types); this.reasons.set(data.reasons); this.catalogsReady.set(true);
      }, error: () => {} });
  }

  protected search(): void {
    if (this.filters.invalid) { this.filters.markAllAsTouched(); return; }
    const value = this.filters.getRawValue();
    this.appliedFilter = { departmentCode: value.departmentCode ?? '', employeeNumber: value.employeeNumber,
      period: value.period ?? '', stateCode: value.stateCode ?? '' };
    this.requests.next(1);
  }
  protected clear(): void { this.filters.reset(emptyFilter()); this.search(); }
  protected goTo(page: number): void { this.requests.next(page); }
  protected saved(): void {
    this.showForm.set(false);
    this.notification.show('success', 'Bono registrado correctamente.');
    this.requests.next(1);
  }
  protected confirm(): void {
    const confirmation = this.confirmation();
    if (!confirmation || this.acting()) return;
    this.acting.set(true);
    const operation = confirmation.action === 'cancel' ? this.api.cancel(confirmation.bonus.bonusId)
      : this.api.delete(confirmation.bonus.bonusId);
    operation.pipe(finalize(() => this.acting.set(false)), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.confirmation.set(null);
        this.notification.show('success', confirmation.action === 'cancel' ? 'Bono anulado.' : 'Bono eliminado.');
        this.requests.next(this.page().pageNumber);
      }, error: () => {},
    });
  }
}

