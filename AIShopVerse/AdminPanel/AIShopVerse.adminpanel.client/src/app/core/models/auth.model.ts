export interface UserDto {
  id: string;
  fullName?: string;
  email?: string;
  phoneNumber?: string;
  roles: string[];
}

export interface AuthResponseDto {
  success: boolean;
  message?: string;
  token?: string;
  refreshToken?: string;
  expiresAt?: Date;
  user?: UserDto;
}

export interface RefreshTokenDto {
  token: string;
  refreshToken: string;
}
