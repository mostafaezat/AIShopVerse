export interface Coupon {
  id: string;
  code?: string;
  description?: string;
  discountType: DiscountType;
  discountValue: number;
  minOrderValue?: number;
  maxUses?: number;
  usedCount: number;
  validFrom?: Date;
  validTo?: Date;
  isActive: boolean;
}

export enum DiscountType {
  Percentage = 0,
  Fixed = 1,
}
