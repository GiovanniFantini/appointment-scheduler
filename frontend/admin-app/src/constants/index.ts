// =============================================================================
// APPOINTMENT SCHEDULER - FRONTEND CONSTANTS
// =============================================================================
// Costanti centralizzate per evitare valori hardcodati nel codice

// =============================================================================
// USER ROLES
// =============================================================================
export enum UserRole {
  Admin = 0,
  User = 1,
  Merchant = 2,
}

export const USER_ROLE_NAMES: Record<UserRole, string> = {
  [UserRole.Admin]: 'Admin',
  [UserRole.User]: 'User',
  [UserRole.Merchant]: 'Merchant',
};

// =============================================================================
// LOCAL STORAGE KEYS
// =============================================================================
export const STORAGE_KEYS = {
  TOKEN: 'token',
  USER: 'user',
} as const;

// =============================================================================
// SERVICE TYPES
// =============================================================================
export enum ServiceType {
  Restaurant = 1,
  Sport = 2,
  Health = 3,
  Beauty = 4,
  Other = 5,
}

export const SERVICE_TYPE_NAMES: Record<ServiceType, string> = {
  [ServiceType.Restaurant]: 'Ristorante',
  [ServiceType.Sport]: 'Sport',
  [ServiceType.Health]: 'Salute',
  [ServiceType.Beauty]: 'Bellezza',
  [ServiceType.Other]: 'Altro',
};

export const SERVICE_TYPE_ICONS: Record<ServiceType, string> = {
  [ServiceType.Restaurant]: '🍽️',
  [ServiceType.Sport]: '⚽',
  [ServiceType.Health]: '🏥',
  [ServiceType.Beauty]: '💄',
  [ServiceType.Other]: '📋',
};

// =============================================================================
// BOOKING STATUS
// =============================================================================
export enum BookingStatus {
  Pending = 0,
  Confirmed = 1,
  Cancelled = 2,
  Completed = 3,
}

export const BOOKING_STATUS_NAMES: Record<BookingStatus, string> = {
  [BookingStatus.Pending]: 'In attesa',
  [BookingStatus.Confirmed]: 'Confermata',
  [BookingStatus.Cancelled]: 'Cancellata',
  [BookingStatus.Completed]: 'Completata',
};

// =============================================================================
// BOOKING MODES
// =============================================================================
export enum BookingMode {
  TimeSlot = 1,
  WholeDay = 2,
}

export const BOOKING_MODE_NAMES: Record<BookingMode, string> = {
  [BookingMode.TimeSlot]: 'Fascia oraria',
  [BookingMode.WholeDay]: 'Giornata intera',
};

// =============================================================================
// API CONFIGURATION
// =============================================================================
export const API_CONFIG = {
  BASE_URL: import.meta.env.VITE_API_URL || 'https://appointment-scheduler-api.azurewebsites.net',
  BASE_PATH: import.meta.env.VITE_API_BASE_PATH || '/api',
  TIMEOUT: parseInt(import.meta.env.VITE_API_TIMEOUT || '5000', 10),
  WITH_CREDENTIALS: import.meta.env.VITE_WITH_CREDENTIALS === 'true',
} as const;

// =============================================================================
// VALIDATION RULES
// =============================================================================
export const VALIDATION = {
  MIN_PASSWORD_LENGTH: 6,
  MAX_EMAIL_LENGTH: 256,
  MAX_NAME_LENGTH: 100,
  MAX_BUSINESS_NAME_LENGTH: 200,
  MAX_SERVICE_NAME_LENGTH: 200,
} as const;

// =============================================================================
// UI CONFIGURATION
// =============================================================================
export const UI_CONFIG = {
  MIN_PEOPLE_PER_BOOKING: parseInt(import.meta.env.VITE_MIN_PEOPLE_PER_BOOKING || '1', 10),
  MAX_PEOPLE_PER_BOOKING: parseInt(import.meta.env.VITE_MAX_PEOPLE_PER_BOOKING || '100', 10),
  DEFAULT_SERVICE_DURATION: parseInt(import.meta.env.VITE_DEFAULT_SERVICE_DURATION || '60', 10),
  MIN_SERVICE_CAPACITY: parseInt(import.meta.env.VITE_MIN_SERVICE_CAPACITY || '1', 10),
  MAX_SERVICE_CAPACITY: parseInt(import.meta.env.VITE_MAX_SERVICE_CAPACITY || '1000', 10),
  PRICE_STEP: parseFloat(import.meta.env.VITE_PRICE_STEP || '0.01'),
  LOCALE: import.meta.env.VITE_LOCALE || 'it-IT',
} as const;

// =============================================================================
// ROUTES
// =============================================================================
export const ROUTES = {
  LOGIN: '/login',
  REGISTER: '/register',
  HOME: '/',
  PROFILE: '/profile',
  BOOKINGS: '/bookings',
  SERVICES: '/services',
  USERS: '/users',
  MERCHANTS: '/merchants',
  ANALYTICS: '/analytics',
} as const;

// =============================================================================
// HTTP HEADERS
// =============================================================================
export const HTTP_HEADERS = {
  CONTENT_TYPE: 'Content-Type',
  AUTHORIZATION: 'Authorization',
  ACCEPT: 'Accept',
} as const;

// =============================================================================
// FEATURE FLAGS
// =============================================================================
export const FEATURES = {
  ENABLE_API_DEBUG: import.meta.env.VITE_ENABLE_API_DEBUG === 'true',
  ENABLE_DEBUG_LOGS: import.meta.env.VITE_ENABLE_DEBUG_LOGS === 'true',
} as const;
