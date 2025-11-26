import { Component, signal } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { ProductService } from './core/services/product.service';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { AuthService } from './core/services/auth.service';
import { CartService } from './core/services/cart.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, CommonModule, CurrencyPipe],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('ec_frontend');
  protected dropdownOpen = signal(false);
  protected cartHover = signal(false);
  
  // Check if current user is admin (you can enhance this with actual role check)
  get isAdmin(): boolean {
    const user = this.authService.currentUser();
    // For now, check if email is admin@gmail.com
    // Later you can add a proper role check via API
    return user?.email === 'admin@gmail.com';
  }

  constructor(
    private productService: ProductService, 
    protected authService: AuthService,
    protected cartService: CartService,
    private router: Router
  ) {
    // Product usage of the ProductService
    this.productService.getAll().subscribe(products => {
      console.log('Fetched products:', products);
    });
  }

  ngOnInit() {
    this.authService.supabase.auth.onAuthStateChange((event, session) => {
      if (event === 'SIGNED_IN') {
        this.authService.currentUser.set({
          email: session?.user.email!,
          username:
            session?.user.identities?.at(0)?.identity_data?.['username'],
        });
        // Load cart when user signs in
        this.loadCart();
      } else if (event === 'SIGNED_OUT') {
        this.authService.currentUser.set(null);
        this.cartService.clearLocalCart();
      }
      console.log('Auth event:', event, 'Session:', session);
    });

    // Check if already logged in and load cart
    this.authService.supabase.auth.getSession().then(({ data: { session } }) => {
      if (session) {
        this.loadCart();
      }
    });
  }

  loadCart() {
    // console.log('Loading cart...');
    
    // Get cart info
    this.cartService.getCart().subscribe({
      next: (cart) => {
        console.log('Cart:', cart);
      },
      error: (error) => {
        console.error('Error loading cart:', error);
      }
    });

    // Get cart items
    this.cartService.getCartItems().subscribe({
      next: (items) => {
        console.log('Cart items:', items);
        console.log('Total items count:', this.cartService.itemCount());
        console.log('Cart is empty:', this.cartService.isEmpty());
      },
      error: (error) => {
        console.error('Error loading cart items:', error);
      }
    });
  }

  showUserDropdown() {
    this.dropdownOpen.set(true);
  }

  hideUserDropdown() {
    this.dropdownOpen.set(false);
  }

  logout() {
    this.authService.logout();
    this.hideUserDropdown();
  }

  openCart() {
    this.router.navigate(['/cart']);
  }

  showCartHover() { this.cartHover.set(true); }
  hideCartHover() { this.cartHover.set(false); }
}
