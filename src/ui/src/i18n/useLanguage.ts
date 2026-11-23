import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { tokenStore } from '@/api/tokenStore';
import { meApi } from '@/api/meApi';
import { DEFAULT_LANGUAGE, isSupportedLanguage, type Language } from './languages';

/** The active language, a setter, and locale-aware date formatting. */
export function useLanguage() {
  const { i18n } = useTranslation();
  const language: Language = isSupportedLanguage(i18n.resolvedLanguage)
    ? i18n.resolvedLanguage
    : DEFAULT_LANGUAGE;

  /**
   * Applies the language immediately, then saves it to the account when signed in.
   * Resolves to false if that save failed; the language stays applied locally either way.
   */
  const setLanguage = useCallback(
    async (next: Language): Promise<boolean> => {
      await i18n.changeLanguage(next);
      if (!tokenStore.get()) return true;
      try {
        await meApi.updateLanguage(next);
        return true;
      } catch {
        return false;
      }
    },
    [i18n],
  );

  const formatDate = useCallback(
    (value: Date | string, options?: Intl.DateTimeFormatOptions) =>
      new Intl.DateTimeFormat(language, options).format(typeof value === 'string' ? new Date(value) : value),
    [language],
  );

  return { language, setLanguage, formatDate };
}
