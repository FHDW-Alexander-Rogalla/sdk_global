import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminProductService, CreateProductRequest, UpdateProductRequest } from '../../../core/services/admin-product.service';
import { ProductService, ProductDto } from '../../../core/services/product.service';

@Component({
  selector: 'app-admin-products',
  imports: [CommonModule, FormsModule],
  templateUrl: './products.html',
  styleUrl: './products.css'
})
export class Products implements OnInit {
  // Access products from admin service
  products = signal<ProductDto[]>([]);
  
  // Form state
  isEditing = false;
  editingProductId: number | null = null;
  showInactive = false;
  
  // Confirmation state
  confirmingDeactivateId: number | null = null;
  confirmingActivateId: number | null = null;
  
  // Form model
  productForm: CreateProductRequest | UpdateProductRequest = {
    name: '',
    description: '',
    price: 0,
    imageUrl: ''
  };

  // Validation state
  formErrors: { [key: string]: string } = {};

  get activeProducts(): ProductDto[] {
    return this.products().filter(p => p.isActive !== false);
  }

  get inactiveProducts(): ProductDto[] {
    return this.products().filter(p => p.isActive === false);
  }

  get hasInactiveProducts(): boolean {
    return this.inactiveProducts.length > 0;
  }

  constructor(
    private adminProductService: AdminProductService,
    private productService: ProductService
  ) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  /**
   * Load all products (including inactive)
   */
  loadProducts(): void {
    this.adminProductService.getAllProducts().subscribe({
      next: (products) => {
        this.products.set(products);
      },
      error: (error) => {
        console.error('Failed to load products:', error);
        alert('Failed to load products. Please try again.');
      }
    });
  }

  toggleInactiveSection(): void {
    this.showInactive = !this.showInactive;
  }

  /**
   * Open create form
   */
  openCreateForm(): void {
    this.isEditing = true;
    this.editingProductId = null;
    this.productForm = {
      name: '',
      description: '',
      price: 0,
      imageUrl: ''
    };
    this.formErrors = {};
  }

  /**
   * Open edit form
   */
  openEditForm(product: ProductDto): void {
    this.isEditing = true;
    this.editingProductId = product.id;
    this.productForm = {
      name: product.name,
      description: product.description || '',
      price: product.price,
      imageUrl: product.imageUrl || ''
    };
    this.formErrors = {};
  }

  /**
   * Close form
   */
  closeForm(): void {
    this.isEditing = false;
    this.editingProductId = null;
    this.productForm = {
      name: '',
      description: '',
      price: 0,
      imageUrl: ''
    };
    this.formErrors = {};
  }

  /**
   * Validate form
   */
  validateForm(): boolean {
    this.formErrors = {};
    
    if (!this.productForm.name || this.productForm.name.trim() === '') {
      this.formErrors['name'] = 'Product name is required';
    }
    
    if (this.productForm.price < 0) {
      this.formErrors['price'] = 'Price must be greater than or equal to 0';
    }
    
    return Object.keys(this.formErrors).length === 0;
  }

  /**
   * Save product (create or update)
   */
  saveProduct(): void {
    if (!this.validateForm()) {
      return;
    }

    if (this.editingProductId !== null) {
      // Update existing product
      this.adminProductService.updateProduct(this.editingProductId, this.productForm as UpdateProductRequest).subscribe({
        next: () => {
          this.loadProducts();
          this.closeForm();
        },
        error: (error) => {
          console.error('Failed to update product:', error);
          alert('Failed to update product. Please try again.');
        }
      });
    } else {
      // Create new product
      this.adminProductService.createProduct(this.productForm as CreateProductRequest).subscribe({
        next: () => {
          this.loadProducts();
          this.closeForm();
        },
        error: (error) => {
          console.error('Failed to create product:', error);
          alert('Failed to create product. Please try again.');
        }
      });
    }
  }

  /**
   * Show deactivate confirmation
   */
  showDeactivateConfirmation(productId: number): void {
    this.confirmingDeactivateId = productId;
  }

  /**
   * Cancel deactivate confirmation
   */
  cancelDeactivate(): void {
    this.confirmingDeactivateId = null;
  }

  /**
   * Delete product (soft-delete, sets is_active to false)
   */
  deleteProduct(productId: number): void {
    this.adminProductService.deleteProduct(productId).subscribe({
      next: (response) => {
        console.log(response.message);
        this.confirmingDeactivateId = null;
        this.loadProducts();
      },
      error: (error) => {
        console.error('Failed to deactivate product:', error);
        alert('Failed to deactivate product. Please try again.');
        this.confirmingDeactivateId = null;
      }
    });
  }

  /**
   * Show activate confirmation
   */
  showActivateConfirmation(productId: number): void {
    this.confirmingActivateId = productId;
  }

  /**
   * Cancel activate confirmation
   */
  cancelActivate(): void {
    this.confirmingActivateId = null;
  }

  /**
   * Activate product (sets is_active to true)
   */
  activateProduct(productId: number): void {
    this.adminProductService.activateProduct(productId).subscribe({
      next: (response) => {
        console.log(response.message);
        this.confirmingActivateId = null;
        this.loadProducts();
      },
      error: (error) => {
        console.error('Failed to reactivate product:', error);
        alert('Failed to reactivate product. Please try again.');
        this.confirmingActivateId = null;
      }
    });
  }

  /**
   * Format date for display
   */
  formatDate(date: string | Date): string {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    return dateObj.toLocaleDateString('en-US', { 
      day: '2-digit', 
      month: 'short', 
      year: 'numeric'
    });
  }

  /**
   * Track products by ID
   */
  trackByProductId(index: number, product: ProductDto): number {
    return product.id;
  }
}
