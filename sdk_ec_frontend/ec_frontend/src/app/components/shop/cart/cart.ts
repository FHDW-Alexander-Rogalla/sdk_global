import { Component, OnInit } from '@angular/core';
import { CartService } from '../../../core/services/cart.service';
import { OrderService } from '../../../core/services/order.service';
import { CartItemWithProduct } from '../../../core/models/cart.model';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-cart',
  imports: [CommonModule, CurrencyPipe, FormsModule],
  templateUrl: './cart.html',
  styleUrl: './cart.css'
})
export class Cart implements OnInit {
  cartItems: CartItemWithProduct[] = [];
  loading = false;
  error: string | null = null;
  submittingOrder = false;
  checkoutError: string | null = null;
  showInactive = false;
  // Track locally edited quantities without immediately persisting
  private editedQuantities: Record<number, number> = {};

  get activeItems(): CartItemWithProduct[] {
    return this.cartItems.filter(item => item.product?.isActive !== false);
  }

  get inactiveItems(): CartItemWithProduct[] {
    return this.cartItems.filter(item => item.product?.isActive === false);
  }

  get hasInactiveItems(): boolean {
    return this.inactiveItems.length > 0;
  }

  constructor(
    private cartService: CartService,
    private orderService: OrderService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCartItems();
  }

  loadCartItems(): void {
    this.loading = true;
    this.error = null;
    this.cartService.getCartItems().subscribe({
      next: items => {
        this.cartItems = items;
        this.loading = false;
      },
      error: err => {
        this.error = 'Error while trying to load the cart.';
        this.loading = false;
      }
    });
  }

  getTotal(): number {
    return this.activeItems.reduce((sum, item) => sum + (item.product?.price || 0) * item.quantity, 0);
  }

  toggleInactiveSection(): void {
    this.showInactive = !this.showInactive;
  }

  getItemTotal(item: CartItemWithProduct): number {
    return (item.product?.price || 0) * item.quantity;
  }

  updateQuantity(item: CartItemWithProduct, quantity: number): void {
    if (quantity < 1) return;
    this.cartService.updateQuantity(item.id, quantity).subscribe({
      next: () => this.loadCartItems()
    });
  }

  // Called on local input change; does not persist yet
  onLocalQuantityChange(item: CartItemWithProduct, value: number) {
    if (value < 1) return;
    this.editedQuantities[item.id] = value;
  }

  isQuantityDirty(item: CartItemWithProduct): boolean {
    return this.editedQuantities[item.id] !== undefined && this.editedQuantities[item.id] !== item.quantity;
  }

  applyQuantityChange(item: CartItemWithProduct) {
    const newQty = this.editedQuantities[item.id];
    if (newQty && newQty > 0 && newQty !== item.quantity) {
      this.updateQuantity(item, newQty);
      delete this.editedQuantities[item.id];
    }
  }

  removeItem(item: CartItemWithProduct): void {
    this.cartService.removeItem(item.id).subscribe({
      next: () => this.loadCartItems()
    });
  }

  clearCart(): void {
    // Remove all items sequentially (simple approach). Could be optimized with backend batch endpoint.
    const items = [...this.cartItems];
    if (items.length === 0) return;
    let remaining = items.length;
    items.forEach(it => {
      this.cartService.removeItem(it.id).subscribe({
        next: () => {
          remaining--;
          if (remaining === 0) {
            this.loadCartItems();
          }
        },
        error: () => {
          remaining--;
          if (remaining === 0) {
            this.loadCartItems();
          }
        }
      });
    });
  }

  removeAllInactive(): void {
    const inactiveItems = [...this.inactiveItems];
    if (inactiveItems.length === 0) return;

    let remaining = inactiveItems.length;
    inactiveItems.forEach(item => {
      this.cartService.removeItem(item.id).subscribe({
        next: () => {
          remaining--;
          if (remaining === 0) {
            this.loadCartItems();
          }
        },
        error: () => {
          remaining--;
          if (remaining === 0) {
            this.loadCartItems();
          }
        }
      });
    });
  }

  submitOrder(): void {
    if (this.activeItems.length === 0) {
      this.checkoutError = 'Your cart has no available items to order.';
      return;
    }

    if (this.hasInactiveItems) {
      this.checkoutError = 'Please remove unavailable items before placing your order.';
      return;
    }

    // Confirm order before submitting
    const confirmMessage = `You are about to place an order for ${this.activeItems.length} item(s) with a total of ${this.getTotal().toFixed(2)} EUR. Continue?`;
    if (!confirm(confirmMessage)) {
      return;
    }

    this.submittingOrder = true;
    this.checkoutError = null;

    this.orderService.checkoutCart().subscribe({
      next: order => {
        this.submittingOrder = false;
        console.log('Order successfully created:', order);
        
        // Show success message
        this.checkoutError = null;
        this.error = null;
        
        // Clear local cart state and reload
        this.loadCartItems();
        
        // Navigate to orders page
        this.router.navigate(['/orders']);
      },
      error: err => {
        this.submittingOrder = false;
        console.error('Error creating order:', err);
        
        // Parse backend error message
        if (err.error?.message) {
          this.checkoutError = err.error.message;
          
          // If inactive products detected, reload cart to update status
          if (err.error.inactiveProductIds) {
            this.loadCartItems();
          }
        } else {
          this.checkoutError = 'Failed to place order. Please try again.';
        }
      }
    });
  }
}
