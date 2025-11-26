import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminOrderService } from '../../../core/services/admin-order.service';
import { AdminOrderWithProducts } from '../../../core/models/order.model';

@Component({
  selector: 'app-admin-orders',
  imports: [CommonModule, FormsModule],
  templateUrl: './orders.html',
  styleUrl: './orders.css'
})
export class Orders implements OnInit {
  constructor(public adminOrderService: AdminOrderService) {}

  // Access the reactive state from the service
  get orders() { return this.adminOrderService.allOrders; }
  
  // Status options for dropdown
  get statusOptions() { return this.adminOrderService.validStatuses; }
  
  // Track which order is being edited
  editingOrderId: number | null = null;
  selectedStatus: string = '';

  // Filter and sort controls
  filterStatus = signal<string>('all');
  filterSearch = signal<string>('');
  sortBy = signal<'date' | 'total' | 'status'>('date');
  sortDesc = signal<boolean>(true);

  // Expanded order items
  expandedOrderId = signal<number | null>(null);

  // Computed filtered and sorted orders
  readonly filteredOrders = computed(() => {
    let filtered = this.orders();

    // Filter by status
    if (this.filterStatus() !== 'all') {
      filtered = filtered.filter(order => 
        order.status.toLowerCase() === this.filterStatus().toLowerCase()
      );
    }

    // Filter by search (customer name, email, or order ID)
    const searchTerm = this.filterSearch().toLowerCase();
    if (searchTerm) {
      filtered = filtered.filter(order => 
        order.id.toString().includes(searchTerm) ||
        order.username?.toLowerCase().includes(searchTerm) ||
        order.userEmail?.toLowerCase().includes(searchTerm) ||
        order.userId?.toLowerCase().includes(searchTerm)
      );
    }

    // Sort orders
    const sorted = [...filtered].sort((a, b) => {
      let comparison = 0;
      
      switch(this.sortBy()) {
        case 'date':
          comparison = new Date(a.orderDate).getTime() - new Date(b.orderDate).getTime();
          break;
        case 'total':
          comparison = a.totalAmount - b.totalAmount;
          break;
        case 'status':
          comparison = a.status.localeCompare(b.status);
          break;
      }

      return this.sortDesc() ? -comparison : comparison;
    });

    return sorted;
  });

  ngOnInit(): void {
    // Load all orders when component initializes
    this.adminOrderService.refreshOrders();
  }

  /**
   * Formats order date for display
   */
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      day: '2-digit', 
      month: 'short', 
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Gets status badge class
   */
  getStatusClass(status: string): string {
    const normalizedStatus = status.toLowerCase().replace(/ /g, '_');
    switch(normalizedStatus) {
      case 'pending': return 'status-pending';
      case 'confirmed': return 'status-confirmed';
      case 'payment_pending': return 'status-payment-pending';
      case 'payment_received': return 'status-payment-received';
      case 'delivered': return 'status-delivered';
      case 'canceled': return 'status-canceled';
      default: return 'status-default';
    }
  }

  /**
   * Formats status text for display
   */
  formatStatus(status: string): string {
    // Replace underscores with spaces and capitalize
    return status.replace(/_/g, ' ').split(' ').map(word => 
      word.charAt(0).toUpperCase() + word.slice(1)
    ).join(' ');
  }

  /**
   * Start editing order status
   */
  startEdit(order: AdminOrderWithProducts): void {
    this.editingOrderId = order.id;
    this.selectedStatus = order.status;
  }

  /**
   * Cancel editing
   */
  cancelEdit(): void {
    this.editingOrderId = null;
    this.selectedStatus = '';
  }

  /**
   * Save status change
   */
  saveStatus(orderId: number): void {
    if (!this.selectedStatus) return;

    this.adminOrderService.updateOrderStatus(orderId, this.selectedStatus).subscribe({
      next: () => {
        this.cancelEdit();
      },
      error: (error) => {
        console.error('Failed to update order status:', error);
        alert('Failed to update order status. Please try again.');
      }
    });
  }

  /**
   * Tracks orders by their ID for better performance
   */
  trackByOrderId(index: number, order: AdminOrderWithProducts): number {
    return order.id;
  }

  /**
   * Get display name for user (username or email or userId)
   */
  getUserDisplay(order: AdminOrderWithProducts): string {
    if (order.username) return order.username;
    if (order.userEmail) return order.userEmail;
    if (order.userId) return `User ${order.userId.substring(0, 8)}...`;
    return 'Unknown User';
  }

  /**
   * Toggle expanded order items
   */
  toggleExpand(orderId: number): void {
    this.expandedOrderId.set(this.expandedOrderId() === orderId ? null : orderId);
  }

  /**
   * Check if order is expanded
   */
  isExpanded(orderId: number): boolean {
    return this.expandedOrderId() === orderId;
  }

  /**
   * Get unique statuses from orders for filter dropdown
   */
  get availableStatuses(): string[] {
    const statuses = new Set(this.orders().map(o => o.status));
    return Array.from(statuses).sort();
  }
}
