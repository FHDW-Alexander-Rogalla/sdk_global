import { Component, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OrderService } from '../../../core/services/order.service';
import { OrderWithProducts } from '../../../core/models/order.model';

@Component({
  selector: 'app-orders',
  imports: [CommonModule, RouterLink],
  templateUrl: './orders.html',
  styleUrl: './orders.css'
})
export class Orders implements OnInit {
  constructor(public orderService: OrderService) {}

  // Access the reactive state from the service via getters
  get orders() { return this.orderService.allOrders; }
  get hasOrders() { return this.orderService.hasOrders; }
  
  // History toggle
  showHistory = false;
  
  // Local computed signals
  readonly activeOrders = computed(() => 
    this.orderService.allOrders().filter(order => {
      const status = order.status.toLowerCase();
      return status !== 'delivered' && status !== 'canceled' && status !== 'cancelled';
    })
  );
  
  readonly completedOrders = computed(() => 
    this.orderService.allOrders().filter(order => {
      const status = order.status.toLowerCase();
      return status === 'delivered' || status === 'canceled' || status === 'cancelled';
    })
  );

  ngOnInit(): void {
    // Load orders when component initializes
    this.orderService.refreshOrders();
  }

  /**
   * Calculates total for an order
   */
  getOrderTotal(order: OrderWithProducts): number {
    return this.orderService.calculateOrderTotal(order);
  }

  /**
   * Formats order date for display
   */
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      day: '2-digit', 
      month: '2-digit', 
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  }

  /**
   * Gets status badge class
   */
  getStatusClass(status: string): string {
    switch(status.toLowerCase()) {
      case 'pending': return 'status-pending';
      case 'confirmed': return 'status-confirmed';
      case 'payment pending': return 'status-payment-pending';
      case 'payment received': return 'status-payment-received';
      case 'delivered': return 'status-delivered';
      case 'canceled': return 'status-canceled';
      case 'cancelled': return 'status-canceled'; // Alternative spelling
      default: return 'status-default';
    }
  }

  /**
   * Gets localized status text
   */
  getStatusText(status: string): string {
    switch(status.toLowerCase()) {
      case 'pending': return 'Pending';
      case 'confirmed': return 'Confirmed';
      case 'payment pending': return 'Payment Pending';
      case 'payment received': return 'Payment Received';
      case 'delivered': return 'Delivered';
      case 'canceled': return 'Canceled';
      case 'cancelled': return 'Canceled';
      default: return status;
    }
  }

  /**
   * Checks if an order can be cancelled
   */
  canCancelOrder(order: OrderWithProducts): boolean {
    const status = order.status.toLowerCase();
    return status !== 'delivered' && status !== 'canceled' && status !== 'cancelled';
  }

  /**
   * Cancels an order
   */
  cancelOrder(order: OrderWithProducts): void {
    if (!confirm(`Are you sure you want to cancel order from ${this.formatDate(order.orderDate)}?`)) {
      return;
    }

    this.orderService.cancelOrder(order.id).subscribe({
      next: () => {
        // Orders will be refreshed automatically via the service
      },
      error: (error) => {
        console.error('Failed to cancel order:', error);
        const message = error.error?.message || 'Failed to cancel order. Please try again.';
        alert(message);
      }
    });
  }

  /**
   * Tracks orders by their ID for better performance
   */
  trackByOrderId(index: number, order: OrderWithProducts): number {
    return order.id;
  }

  /**
   * Toggles history visibility
   */
  toggleHistory(): void {
    this.showHistory = !this.showHistory;
  }
}
