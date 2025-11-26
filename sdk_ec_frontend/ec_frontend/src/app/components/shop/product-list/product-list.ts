import { Component, OnInit, OnDestroy, effect } from '@angular/core';
import { ProductDto, ProductService } from '../../../core/services/product.service';
import { AuthService } from '../../../core/services/auth.service';
import { CartService } from '../../../core/services/cart.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

interface ProductWithQuantity extends ProductDto {
  quantity: number;
}

@Component({
  selector: 'app-product-list',
  imports: [CommonModule, FormsModule],
  templateUrl: './product-list.html',
  styleUrl: './product-list.css'
})
export class ProductList implements OnInit, OnDestroy {
  products: ProductWithQuantity[] = [];
  loading = false;
  error: string | null = null;
  isAuthenticated = false;
  addingToCart = new Set<number>(); // Track which products are being added
  // Per-product status messages (e.g., "Added" or "Error") and timers
  private productStatus = new Map<number, { type: 'success' | 'error'; text: string }>();
  private statusTimeouts = new Map<number, any>();

  constructor(
    private productService: ProductService,
    private authService: AuthService,
    private cartService: CartService,
    private router: Router
  ) {
    // React to auth state changes using effect
    effect(() => {
      const user = this.authService.currentUser();
      this.isAuthenticated = user !== null;
      
      if (this.isAuthenticated && this.products.length === 0) {
        this.loadProducts();
      } else if (!this.isAuthenticated) {
        this.products = [];
      }
    });
  }

  ngOnInit(): void {
    this.checkAuthentication();
  }

  ngOnDestroy(): void {
    // Clear all pending status hide timers
    this.statusTimeouts.forEach((timeoutId) => clearTimeout(timeoutId));
    this.statusTimeouts.clear();
  }

  checkAuthentication(): void {
    // Check current session state
    this.authService.supabase.auth.getSession().then(({ data: { session } }) => {
      this.isAuthenticated = session !== null;
      
      if (this.isAuthenticated && session?.user) {
        // Update currentUser signal if not already set
        if (!this.authService.currentUser()) {
          this.authService.currentUser.set({
            email: session.user.email!,
            username: session.user.identities?.at(0)?.identity_data?.['username'] || session.user.email!
          });
        }
        this.loadProducts();
      }
    });
  }

  loadProducts(): void {
    this.loading = true;
    this.error = null;

    this.productService.getAll().subscribe({
      next: (products) => {
        this.products = products.map(p => ({
          ...p,
          quantity: 1
        }));
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load products';
        this.loading = false;
        console.error('Error loading products:', err);
      }
    });
  }

  addToCart(product: ProductWithQuantity): void {
    this.addingToCart.add(product.id);

    this.cartService.addProduct(product.id, product.quantity).subscribe({
      next: (cartItem) => {
        console.log('Product added to cart:', {
          cartItemId: cartItem.id,
          productId: product.id,
          productName: product.name,
          price: product.price,
          quantity: product.quantity,
          total: product.price * product.quantity
        });
        
        // Reset quantity to 1 after adding
        product.quantity = 1;
        this.addingToCart.delete(product.id);
        // Show inline success message for 2 seconds
        this.setStatus(product.id, 'success', 'Added');
      },
      error: (err) => {
        console.error('Error adding product to cart:', err);
        this.addingToCart.delete(product.id);
        // Show inline error message for 2 seconds
        this.setStatus(product.id, 'error', 'Error');
      }
    });
  }

  isAddingToCart(productId: number): boolean {
    return this.addingToCart.has(productId);
  }

  // Returns the status object for a given product id (used in template)
  getStatus(productId: number) {
    return this.productStatus.get(productId);
  }

  // Sets a temporary status for a product and auto-hides after duration
  private setStatus(productId: number, type: 'success' | 'error', text: string, duration = 2000) {
    // Clear any existing timer for this product
    const existing = this.statusTimeouts.get(productId);
    if (existing) {
      clearTimeout(existing);
    }
    this.productStatus.set(productId, { type, text });
    const timeoutId = setTimeout(() => {
      this.productStatus.delete(productId);
      this.statusTimeouts.delete(productId);
    }, duration);
    this.statusTimeouts.set(productId, timeoutId);
  }

  navigateToLogin(): void {
    this.router.navigate(['/login']);
  }

  navigateToRegister(): void {
    this.router.navigate(['/register']);
  }
}
