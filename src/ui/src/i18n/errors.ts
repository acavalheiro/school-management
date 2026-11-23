import type { TFunction } from 'i18next';
import { ApiError } from '@/api/http';

/**
 * A translated, user-safe message for any failure from an API call. Never surfaces the raw
 * response body: server messages are English-only and may carry internal detail.
 */
export function errorMessage(error: unknown, t: TFunction): string {
  if (error instanceof ApiError) {
    switch (error.status) {
      case 400:
        return t('errors.badRequest');
      case 401:
        return t('errors.unauthorized');
      case 403:
        return t('errors.forbidden');
      case 404:
        return t('errors.notFound');
      case 409:
        return t('errors.conflict');
      case 429:
        return t('errors.tooManyRequests');
      default:
        return error.status >= 500 ? t('errors.server') : t('errors.generic');
    }
  }
  // fetch() rejects with a TypeError when the request never reached the server.
  if (error instanceof TypeError) return t('errors.network');
  return t('errors.generic');
}
