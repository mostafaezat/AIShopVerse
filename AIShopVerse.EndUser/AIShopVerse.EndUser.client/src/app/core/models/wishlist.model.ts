export interface WishlistItem {
  id: string;
  productId: string;
  productName?: string;
  productImageUrl?: string;
  price?: number;
  addedAt?: Date;
}
