export interface Review {
  id: string;
  userId?: string;
  userName?: string;
  productId: string;
  rating: number;
  comment?: string;
  isApproved: boolean;
  createdAt?: Date;
}

export interface CreateReviewRequest {
  productId: string;
  rating: number;
  comment?: string;
}
