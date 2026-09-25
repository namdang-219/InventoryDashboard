export interface UserDto {
  id: string;
  email: string;
  roles: readonly string[];
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  refreshTokenExpiry: string;
  user: UserDto;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}
