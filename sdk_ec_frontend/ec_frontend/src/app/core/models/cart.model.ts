import { ProductDto } from '../services/product.service';

export interface CartDto {
    id: number;
    userId: string;
    createdAt: string;
    updatedAt: string;
}

export interface CartItemDto {
    id: number;
    cartId: number;
    productId: number;
    quantity: number;
}

export interface AddCartItemRequest {
    productId: number;
    quantity: number;
}

export interface UpdateCartItemRequest {
    quantity: number;
}

// Extended cart item with product details (for frontend use)
export interface CartItemWithProduct extends CartItemDto {
    product?: ProductDto;
}
