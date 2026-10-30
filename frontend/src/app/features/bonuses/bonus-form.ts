import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, DestroyRef, ElementRef, ViewChild, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription, finalize } from 'rxjs';
import { Employee } from '../../core/models/bonus';
import { BonusReason, BonusType, Department } from '../../core/models/catalog';
import { BonusApi } from '../../core/services/bonus-api';
import { currentLimaMonth, previewBonus } from './bonus-preview';

@Component({
  selector: 'app-bonus-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './bonus-form.html',
  styleUrl: './bonus-form.css',
})
export class BonusForm implements AfterViewInit {
  readonly departments = input.required<Department[]>();
  readonly types = input.required<BonusType[]>();
  readonly reasons = input.required<BonusReason[]>();
  readonly saved = output<void>();
  readonly closed = output<void>();
  @ViewChild('dialog', { static: true }) private dialog!: ElementRef<HTMLDialogElement>;
  private readonly api = inject(BonusApi);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private employeeRequest?: Subscription;
  protected readonly maxMonth = currentLimaMonth();
  protected readonly employee = signal<Employee | null>(null);
  protected readonly looking = signal(false);
  protected readonly saving = signal(false);
  protected readonly lookup = this.fb.nonNullable.group({
    departmentCode: ['', Validators.required],
    employeeNumber: [null as number | null, [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)]],
  });
  protected readonly form = this.fb.nonNullable.group({
    period: [this.maxMonth, [Validators.required, Validators.pattern(/^\d{4}-(0[1-9]|1[0-2])$/)]],
    bonusTypeId: [1, Validators.required],
    value: ['', [Validators.required, Validators.pattern(/^\d+(\.\d{1,4})?$/)]],
    reasonCode: ['', Validators.required],
  });
  private readonly values = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
  protected readonly percentage = computed(() => Number(this.values().bonusTypeId) === 2);
  protected readonly futurePeriod = computed(() => (this.values().period ?? '') > this.maxMonth);
  protected readonly preview = computed(() => {
    const employee = this.employee();
    const values = this.values();
    return employee ? previewBonus(Number(values.bonusTypeId), values.value ?? '', employee.monthlySalary, employee.taxRate) : null;
  });

  constructor() {
    this.lookup.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.employeeRequest?.unsubscribe();
      this.employee.set(null);
      this.looking.set(false);
    });
  }
  ngAfterViewInit(): void { this.dialog.nativeElement.showModal(); }

  protected findEmployee(): void {
    if (this.lookup.invalid) { this.lookup.markAllAsTouched(); return; }
    this.employeeRequest?.unsubscribe();
    this.employee.set(null);
    this.looking.set(true);
    const key = this.lookup.getRawValue();
    this.employeeRequest = this.api.employee(key.departmentCode, key.employeeNumber!)
      .pipe(finalize(() => this.looking.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: employee => this.employee.set(employee), error: () => {} });
  }
  protected save(): void {
    this.form.markAllAsTouched();
    const employee = this.employee();
    if (!employee || this.form.invalid || !this.preview() || this.futurePeriod() || this.saving()) return;
    this.saving.set(true);
    const value = this.form.getRawValue();
    this.api.create({ departmentCode: employee.departmentCode, employeeNumber: employee.employeeNumber,
      period: value.period, bonusTypeId: Number(value.bonusTypeId), value: Number(value.value), reasonCode: value.reasonCode })
      .pipe(finalize(() => this.saving.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: () => this.saved.emit(), error: () => {} });
  }
  protected close(event?: Event): void {
    event?.preventDefault();
    if (!this.saving()) this.closed.emit();
  }
}
