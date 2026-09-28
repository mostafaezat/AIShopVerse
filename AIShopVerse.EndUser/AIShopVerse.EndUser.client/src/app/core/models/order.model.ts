export interface Order {
  id: string;
  orderNumber?: string;
  userId?: string;
  subtotal: number;
  discountAmount: number;
  tax: number;
  shippingCost: number;
  total: number;
  status: OrderStatus;
  shippingAddress?: string;
  billingAddress?: string;
  couponCode?: string;
  items: OrderItem[];
  createdAt?: Date;
}

export interface OrderItem {
  id: string;
  orderId?: string;
  productId?: string;
  variantId?: string;
  variantLabel?: string;
  productName?: string;
  productImageUrl?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export enum OrderStatus {
  Pending = 0,
  Paid = 1,
  Processing = 2,
  Shipped = 3,
  Delivered = 4,
  Cancelled = 5,
  Refunded = 6,
}

export interface CheckoutRequest {
  shippingAddress: string;
  billingAddress?: string;
  couponCode?: string;
  paymentMethod?: string;
}

export interface UpdateOrderStatusRequest {
  orderId: string;
  status: OrderStatus;
}
