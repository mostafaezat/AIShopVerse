export interface Product {
  id: string;
  nameAR?: string;
  nameEN?: string;
  sku?: string;
  price: number;
  discountPrice?: number;
  stockQuantity: number;
  isActive: boolean;
  descriptionAR?: string;
  descriptionEN?: string;
  categoryId?: string;
  brandId?: string;
  categoryName?: string;
  brandName?: string;
  images: ProductImage[];
  attributes: ProductAttribute[];
  variants: ProductVariant[];
  averageRating?: number;
  reviewCount?: number;
}

export interface ProductImage {
  id: string;
  imageUrl?: string;
  displayOrder: number;
  isPrimary: boolean;
}

export interface ProductAttribute {
  id: string;
  name?: string;
  value?: string;
}

export interface ProductVariant {
  id: string;
  sku?: string;
  price: number;
  stockQuantity: number;
  attributeValues?: string;
}

export interface Category {
  id: string;
  nameAR?: string;
  nameEN?: string;
  description?: string;
  imageUrl?: string;
  parentId?: string;
  displayOrder: number;
  isActive: boolean;
}

export interface Brand {
  id: string;
  nameAR?: string;
  nameEN?: string;
  logoUrl?: string;
  description?: string;
  isActive: boolean;
}

export interface ProductFilter {
  categoryId?: string;
  brandId?: string;
  minPrice?: number;
  maxPrice?: number;
  minRating?: number;
  inStockOnly?: boolean;
  searchTerm?: string;
  sortBy?: string;
  page: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
