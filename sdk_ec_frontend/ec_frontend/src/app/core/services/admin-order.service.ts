import { Injectable, signal } from '@angular/core';
import { Observable, tap, map, switchMap, forkJoin, of, catchError } from 'rxjs';
import { ApiService } from './api.service';
import { ProductService } from './product.service';
import { 
    AdminOrderDto,
    AdminOrderWithProducts,
    OrderItemWithProduct,
    UpdateOrderStatusRequest
} from '../models/order.model';

@Injectable({
    providedIn: 'root'
})
export class AdminOrderService {
    private readonly basePath = '/admin/order';
    
    // Reactive state management
    private orders = signal<AdminOrderWithProducts[]>([]);
    
    // Computed signals
    readonly allOrders = this.orders.asReadonly();

    constructor(
        private apiService: ApiService,
        private productService: ProductService
    ) { }

    /**
     * GET /api/admin/order - Gets all orders from all users (Admin only)
     * Also fetches product details for each order item
     */
    getAllOrders(): Observable<AdminOrderWithProducts[]> {
        return this.apiService.get<AdminOrderDto[]>(this.basePath).pipe(
            switchMap(orders => {
                if (orders.length === 0) {
                    this.orders.set([]);
                    return of([] as AdminOrderWithProducts[]);
                }

                // For each order, enrich items with product details
                const ordersWithProducts$ = orders.map(order =>
                    this.enrichOrderWithProducts(order)
                );

                return forkJoin(ordersWithProducts$);
            }),
            tap(ordersWithProducts => this.orders.set(ordersWithProducts))
        );
    }

    /**
     * GET /api/admin/order/{id} - Gets a specific order by ID (Admin only)
     * Also fetches product details for order items
     */
    getOrderById(id: number): Observable<AdminOrderWithProducts> {
        return this.apiService.get<AdminOrderDto>(`${this.basePath}/${id}`).pipe(
            switchMap(order => this.enrichOrderWithProducts(order))
        );
    }

    /**
     * PATCH /api/admin/order/{id}/status - Updates the status of an order (Admin only)
     */
    updateOrderStatus(id: number, status: string): Observable<AdminOrderDto> {
        const request: UpdateOrderStatusRequest = { status };
        return this.apiService.patch<AdminOrderDto>(`${this.basePath}/${id}/status`, request).pipe(
            tap(() => this.refreshOrders())
        );
    }

    /**
     * Enriches an order with product details for each order item
     * Uses getByIdAny to include inactive products so admins can see complete order history
     */
    private enrichOrderWithProducts(order: AdminOrderDto): Observable<AdminOrderWithProducts> {
        if (order.items.length === 0) {
            return of({ ...order, items: [] } as AdminOrderWithProducts);
        }

        const itemsWithProducts$ = order.items.map(item =>
            this.productService.getByIdAny(item.productId).pipe(
                map(product => ({
                    ...item,
                    product: {
                        id: product.id,
                        name: product.name,
                        description: product.description,
                        imageUrl: product.imageUrl,
                        isActive: product.isActive
                    }
                } as OrderItemWithProduct)),
                catchError(error => {
                    // If product is completely deleted, create a placeholder
                    console.warn(`Product ${item.productId} not found for admin order item:`, error);
                    return of({
                        ...item,
                        product: {
                            id: item.productId,
                            name: 'Product Unavailable',
                            description: 'This product has been removed from the catalog',
                            imageUrl: undefined,
                            isActive: false
                        }
                    } as OrderItemWithProduct);
                })
            )
        );

        return forkJoin(itemsWithProducts$).pipe(
            map(itemsWithProducts => ({
                ...order,
                items: itemsWithProducts
            } as AdminOrderWithProducts))
        );
    }

    /**
     * Refreshes the orders from the server
     */
    refreshOrders(): void {
        this.getAllOrders().subscribe();
    }

    /**
     * Clears the local orders state
     */
    clearLocalOrders(): void {
        this.orders.set([]);
    }

    /**
     * Gets orders filtered by status
     */
    getOrdersByStatus(status: string): AdminOrderWithProducts[] {
        return this.orders().filter(order => 
            order.status.toLowerCase() === status.toLowerCase()
        );
    }

    /**
     * Valid order statuses
     */
    readonly validStatuses = [
        'pending',
        'confirmed',
        'payment_pending',
        'payment_received',
        'delivered',
        'canceled'
    ] as const;
}
