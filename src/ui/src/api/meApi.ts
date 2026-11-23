import { apiRequest } from './http';
import type { Language } from '@/i18n/languages';

export interface CurrentUserDto {
  id: string;
  email: string;
  role: string;
  preferredLanguage: string;
}

export const meApi = {
  get: () => apiRequest<CurrentUserDto>('/api/me'),

  updateLanguage: (language: Language) =>
    apiRequest<void>('/api/me/language', { method: 'PUT', body: JSON.stringify({ language }) }),
};
