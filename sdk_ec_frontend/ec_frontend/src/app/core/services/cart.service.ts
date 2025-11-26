import { Injectable, signal, computed } from '@angular/core';
import { Observable, tap, forkJoin, map, of, switchMap, catchError } from 'rxjs';
import { ApiService } from './api.service';
import { ProductService } from './product.service';
import { 
    CartDto, 
    CartItemDto, 
    AddCartItemRequest, 
    UpdateCartItemRequest,
    CartItemWithProduct
} from '../models/cart.model';

@Injectable({
    providedIn: 'root'
})
export class CartService {
    private readonly basePath = '/cart';
    
    // Reactive state management
    private cartItems = signal<CartItemWithProduct[]>([]);
    
    // Computed signals
    readonly items = this.cartItems.asReadonly();
    readonly itemCount = computed(() => 
        this.cartItems()
            .filter(item => item.product?.isActive !== false)
            .reduce((sum, item) => sum + item.quantity, 0)
    );
    readonly isEmpty = computed(() => this.cartItems().length === 0);
    readonly totalPrice = computed(() => 
        this.cartItems().reduce((sum, item) => 
            sum + (item.product?.price || 0) * item.quantity, 0
        )
    );

    constructor(
        private apiService: ApiService,
        private productService: ProductService
    ) { }

    /**
     * GET /api/cart - Gets the current user's cart
     */
    getCart(): Observable<CartDto> {
        return this.apiService.get<CartDto>(this.basePath);
    }

    /**
     * GET /api/cart/items - Gets all items in the current user's cart
     * Also fetches product details for each item and updates local state
     * Uses getByIdAny to include inactive products so users can see what's in their cart
     */
    getCartItems(): Observable<CartItemWithProduct[]> {
        return this.apiService.get<CartItemDto[]>(`${this.basePath}/items`).pipe(
            switchMap(items => {
                if (items.length === 0) {
                    this.cartItems.set([]);
                    return of([] as CartItemWithProduct[]);
                }

                const itemsWithProducts$ = items.map(item =>
                    this.productService.getByIdAny(item.productId).pipe(
                        map(product => ({ ...item, product } as CartItemWithProduct)),
                        catchError(error => {
                            // If product is completely deleted from database, create a placeholder
                            console.warn(`Product ${item.productId} not found in database:`, error);
                            return of({
                                ...item,
                                product: {
                                    id: item.productId,
                                    name: 'Product Unavailable',
                                    description: 'This product has been removed from the catalog',
                                    price: 0,
                                    imageUrl: null,
                                    isActive: false,
                                    createdAt: new Date(),
                                    updatedAt: new Date()
                                }
                            } as CartItemWithProduct);
                        })
                    )
                );

                return forkJoin(itemsWithProducts$);
            }),
            tap(itemsWithProducts => this.cartItems.set(itemsWithProducts))
        );
    }

    /**
     * POST /api/cart/items - Adds an item to the cart or updates quantity if exists
     */
    addCartItem(request: AddCartItemRequest): Observable<CartItemDto> {
        return this.apiService.post<CartItemDto>(`${this.basePath}/items`, request).pipe(
            tap(() => this.refreshCartItems())
        );
    }

    /**
     * Convenience method to add a single product with quantity
     */
    addProduct(productId: number, quantity: number = 1): Observable<CartItemDto> {
        return this.addCartItem({ productId, quantity });
    }

    /**
     * PUT /api/cart/items/{id} - Updates the quantity of a cart item
     */
    updateCartItem(id: number, request: UpdateCartItemRequest): Observable<CartItemDto> {
        return this.apiService.put<CartItemDto>(`${this.basePath}/items/${id}`, request).pipe(
            tap(() => this.refreshCartItems())
        );
    }

    /**
     * Convenience method to update quantity by item id
     */
    updateQuantity(itemId: number, quantity: number): Observable<CartItemDto> {
        return this.updateCartItem(itemId, { quantity });
    }

    /**
     * DELETE /api/cart/items/{id} - Removes an item from the cart
     */
    deleteCartItem(id: number): Observable<void> {
        return this.apiService.delete<void>(`${this.basePath}/items/${id}`).pipe(
            tap(() => this.refreshCartItems())
        );
    }

    /**
     * Removes an item from the cart (alias for deleteCartItem)
     */
    removeItem(itemId: number): Observable<void> {
        return this.deleteCartItem(itemId);
    }

    /**
     * Increments the quantity of a cart item by 1
     */
    incrementQuantity(item: CartItemWithProduct): Observable<CartItemDto> {
        return this.updateQuantity(item.id, item.quantity + 1);
    }

    /**
     * Decrements the quantity of a cart item by 1
     * If quantity becomes 0, removes the item
     */
    decrementQuantity(item: CartItemWithProduct): Observable<CartItemDto | void> {
        if (item.quantity <= 1) {
            return this.removeItem(item.id);
        }
        return this.updateQuantity(item.id, item.quantity - 1);
    }

    /**
     * Refreshes the cart items from the server
     */
    private refreshCartItems(): void {
        this.getCartItems().subscribe();
    }

    /**
     * Clears the local cart state (useful after logout)
     */
    clearLocalCart(): void {
        this.cartItems.set([]);
    }

    /**
     * Gets a cart item by product ID
     */
    getItemByProductId(productId: number): CartItemWithProduct | undefined {
        return this.cartItems().find(item => item.productId === productId);
    }

    /**
     * Checks if a product is already in the cart
     */
    hasProduct(productId: number): boolean {
        return this.cartItems().some(item => item.productId === productId);
    }
}
