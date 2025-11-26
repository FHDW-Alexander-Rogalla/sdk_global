import { Injectable, signal, computed } from '@angular/core';
import { Observable, tap, forkJoin, map, of, switchMap, catchError } from 'rxjs';
import { ApiService } from './api.service';
import { ProductService } from './product.service';
import { CartService } from './cart.service';
import { 
    OrderDto, 
    OrderItemDto, 
    UpdateOrderStatusRequest,
    OrderWithProducts,
    OrderItemWithProduct
} from '../models/order.model';

@Injectable({
    providedIn: 'root'
})
export class OrderService {
    private readonly basePath = '/order';
    
    // Reactive state management
    private orders = signal<OrderWithProducts[]>([]);
    
    // Computed signals
    readonly allOrders = this.orders.asReadonly();
    readonly orderCount = computed(() => this.orders().length);
    readonly hasOrders = computed(() => this.orders().length > 0);

    constructor(
        private apiService: ApiService,
        private productService: ProductService,
        private cartService: CartService
    ) { }

    /**
     * POST /api/order/checkout - Creates an order from the user's current cart
     * Converts all cart items to order items and empties the cart
     */
    checkoutCart(): Observable<OrderDto> {
        return this.apiService.post<OrderDto>(`${this.basePath}/checkout`, {}).pipe(
            tap(() => {
                // Refresh orders and clear cart after successful checkout
                this.refreshOrders();
                this.cartService.clearLocalCart();
                // Reload cart items to ensure it's empty
                this.cartService.getCartItems().subscribe();
            })
        );
    }

    /**
     * GET /api/order - Gets all orders for the current user
     * Also fetches product details for each order item
     */
    getUserOrders(): Observable<OrderWithProducts[]> {
        return this.apiService.get<OrderDto[]>(this.basePath).pipe(
            switchMap(orders => {
                if (orders.length === 0) {
                    this.orders.set([]);
                    return of([] as OrderWithProducts[]);
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
     * GET /api/order/{id} - Gets a specific order by ID
     * Also fetches product details for order items
     */
    getOrderById(id: number): Observable<OrderWithProducts> {
        return this.apiService.get<OrderDto>(`${this.basePath}/${id}`).pipe(
            switchMap(order => this.enrichOrderWithProducts(order))
        );
    }

    /**
     * PATCH /api/order/{id}/cancel - Cancels an order (only if not delivered)
     */
    cancelOrder(id: number): Observable<OrderDto> {
        return this.apiService.patch<OrderDto>(`${this.basePath}/${id}/cancel`, {}).pipe(
            tap(() => this.refreshOrders())
        );
    }

    /**
     * PATCH /api/order/{id}/status - Updates the status of an order
     */
    updateOrderStatus(id: number, status: string): Observable<OrderDto> {
        const request: UpdateOrderStatusRequest = { status };
        return this.apiService.patch<OrderDto>(`${this.basePath}/${id}/status`, request).pipe(
            tap(() => this.refreshOrders())
        );
    }

    /**
     * Enriches an order with product details for each order item
     * Uses getByIdAny to include inactive products so users can see their order history
     */
    private enrichOrderWithProducts(order: OrderDto): Observable<OrderWithProducts> {
        if (order.items.length === 0) {
            return of({ ...order, items: [] } as OrderWithProducts);
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
                    console.warn(`Product ${item.productId} not found for order item:`, error);
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
            } as OrderWithProducts))
        );
    }

    /**
     * Refreshes the orders from the server
     */
    refreshOrders(): void {
        this.getUserOrders().subscribe();
    }

    /**
     * Clears the local orders state (useful after logout)
     */
    clearLocalOrders(): void {
        this.orders.set([]);
    }

    /**
     * Gets orders filtered by status
     */
    getOrdersByStatus(status: string): OrderWithProducts[] {
        return this.orders().filter(order => order.status === status);
    }

    /**
     * Gets the most recent order
     */
    getMostRecentOrder(): OrderWithProducts | undefined {
        const orders = this.orders();
        if (orders.length === 0) return undefined;
        
        return orders.reduce((latest, current) => 
            new Date(current.orderDate) > new Date(latest.orderDate) ? current : latest
        );
    }

    /**
     * Calculates total price for an order
     */
    calculateOrderTotal(order: OrderDto | OrderWithProducts): number {
        return order.items.reduce((sum, item) => 
            sum + (item.priceAtPurchase * item.quantity), 0
        );
    }
}