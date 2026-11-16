import { Component, inject, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { OrderService } from '../../services/order.service';
import { OrderTicket } from '../../models/order.model';
import { Router } from '@angular/router';
import { TranslatePipe } from '../../../../i18n/translate.pipe';
@Component({
  selector: 'app-new-order',
  imports: [TranslatePipe, ReactiveFormsModule],
  templateUrl: './new-order.html',
  styleUrl: './new-order.css',
})
export class NewOrder {
  private fb = inject(FormBuilder);
  private orderService = inject(OrderService);
  router = inject(Router);

  created = output<OrderTicket>();

  isSending = signal(false);
  errorMessage = signal('');

  form = this.fb.nonNullable.group({
    table: ['', Validators.required],
    dishes: this.fb.array([this.createDishGroup()]),
  });

  createDishGroup() {
    return this.fb.nonNullable.group({
      name: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
    });
  }

  get dishes() {
    return this.form.controls.dishes;
  }

  addDish() {
    this.dishes.push(this.createDishGroup());
  }

  removeDish(index: number) {
    if (this.dishes.length === 1) {
      return;
    }
    this.dishes.removeAt(index);
  }

  send() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.form.getRawValue();

    this.isSending.set(true);
    this.errorMessage.set('');

    this.orderService.createOrder(request).subscribe({
      next: (order) => {
        this.isSending.set(false);
        this.created.emit(order);
        this.resetForm();
        this.onDashboard();
      },

      error: (err) => {
        this.isSending.set(false);
        this.errorMessage.set(err.error?.message ?? 'Something went wrong. Please try again.');
      },
    });
  }

  private resetForm() {
    this.dishes.clear();
    this.dishes.push(this.createDishGroup());
    this.form.reset();
  }
  onDashboard() {
    this.router.navigateByUrl('/layout/dashboard');
  }
}
