import { Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { ProductDto } from './product.service';

/**
 * Request models for creating/updating products
 */
export interface CreateProductRequest {
    name: string;
    description?: string;
    price: number;
    imageUrl?: string;
}

export interface UpdateProductRequest {
    name: string;
    description?: string;
    price: number;
    imageUrl?: string;
}

@Injectable({
    providedIn: 'root'
})
export class AdminProductService {
    private readonly basePath = '/admin/product';
    
    // Reactive state management
    private products = signal<ProductDto[]>([]);
    
    // Computed signals
    readonly allProducts = this.products.asReadonly();

    constructor(private apiService: ApiService) { }

    /**
     * GET /api/admin/product - Gets all products including inactive ones (Admin only)
     */
    getAllProducts(): Observable<ProductDto[]> {
        return this.apiService.get<ProductDto[]>(this.basePath).pipe(
            tap(products => this.products.set(products))
        );
    }

    /**
     * POST /api/admin/product - Creates a new product (Admin only)
     */
    createProduct(request: CreateProductRequest): Observable<ProductDto> {
        return this.apiService.post<ProductDto>(this.basePath, request).pipe(
            tap(() => this.refreshProducts())
        );
    }

    /**
     * PUT /api/admin/product/{id} - Updates an existing product (Admin only)
     */
    updateProduct(id: number, request: UpdateProductRequest): Observable<ProductDto> {
        return this.apiService.put<ProductDto>(`${this.basePath}/${id}`, request).pipe(
            tap(() => this.refreshProducts())
        );
    }

    /**
     * DELETE /api/admin/product/{id} - Soft-deletes a product by setting is_active to false (Admin only)
     */
    deleteProduct(id: number): Observable<{ message: string; productId: number }> {
        return this.apiService.delete<{ message: string; productId: number }>(`${this.basePath}/${id}`).pipe(
            tap(() => this.refreshProducts())
        );
    }

    /**
     * PATCH /api/admin/product/{id}/activate - Reactivates a product by setting is_active to true (Admin only)
     */
    activateProduct(id: number): Observable<{ message: string; productId: number }> {
        return this.apiService.patch<{ message: string; productId: number }>(`${this.basePath}/${id}/activate`, {}).pipe(
            tap(() => this.refreshProducts())
        );
    }

    /**
     * Refreshes the products from the admin endpoint
     */
    refreshProducts(): void {
        this.getAllProducts().subscribe();
    }

    /**
     * Clears the local products state
     */
    clearLocalProducts(): void {
        this.products.set([]);
    }
}
