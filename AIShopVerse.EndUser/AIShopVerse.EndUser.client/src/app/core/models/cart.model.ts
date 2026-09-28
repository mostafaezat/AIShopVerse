export interface CartItemDto {
  id: string;
  productId: string;
  variantId?: string;
  variantLabel?: string;
  productName?: string;
  productImageUrl?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface CartDto {
  id: string;
  items: CartItemDto[];
  subtotal: number;
  discountAmount?: number;
  tax: number;
  shippingCost: number;
  total: number;
  couponCode?: string;
}

export interface AddToCartRequest {
  productId: string;
  quantity: number;
  variantId?: string;
}

export interface UpdateCartItemRequest {
  productId: string;
  quantity: number;
}
