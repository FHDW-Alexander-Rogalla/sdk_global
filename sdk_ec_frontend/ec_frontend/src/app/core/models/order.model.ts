export interface OrderDto {
    id: number;
    userId?: string;
    orderDate: string;
    status: string;
    updatedAt: string;
    items: OrderItemDto[];
}

export interface OrderItemDto {
    id: number;
    orderId: number;
    productId: number;
    quantity: number;
    priceAtPurchase: number;
}

export interface UpdateOrderStatusRequest {
    status: string;
}

// Extended order item with product details (for frontend use)
export interface OrderItemWithProduct extends OrderItemDto {
    product?: {
        id: number;
        name: string;
        description?: string;
        imageUrl?: string;
        isActive?: boolean;
    };
}

// Extended order with product details in items
export interface OrderWithProducts extends Omit<OrderDto, 'items'> {
    items: OrderItemWithProduct[];
}

// Admin-specific order DTO with additional user information
export interface AdminOrderDto {
    id: number;
    userId?: string;
    userEmail?: string;
    username?: string;
    orderDate: string;
    status: string;
    updatedAt: string;
    items: OrderItemDto[];
    totalAmount: number;
}

// Extended admin order with product details
export interface AdminOrderWithProducts extends Omit<AdminOrderDto, 'items'> {
    items: OrderItemWithProduct[];
}
