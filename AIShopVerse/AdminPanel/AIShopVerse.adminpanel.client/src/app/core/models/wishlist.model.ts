export interface WishlistItem {
  id: string;
  userId?: string;
  productId: string;
  productName?: string;
  productImageUrl?: string;
  productPrice?: number;
  addedAt?: Date;
}
