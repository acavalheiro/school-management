// Mirrors Application.Common.SupportedLanguages on the API. Adding a language means adding it
// here, there, and shipping a complete catalogue in ./locales.
export const SUPPORTED_LANGUAGES = ['en-GB', 'pt-PT'] as const;

export type Language = (typeof SUPPORTED_LANGUAGES)[number];

// Final fallback when neither the account, a cached choice nor the browser picks a language.
// Mirrors SupportedLanguages.Default on the API.
export const DEFAULT_LANGUAGE: Language = 'pt-PT';

export const LANGUAGE_STORAGE_KEY = 'atl_lang';

// Each language is labelled in its own language, so a user can find theirs whatever is active.
export const LANGUAGE_NAMES: Record<Language, string> = {
  'en-GB': 'English (UK)',
  'pt-PT': 'Português (Portugal)',
};

// Shorter labels for narrow places (the 14rem sidebar), where the full names get cut off.
export const LANGUAGE_SHORT_NAMES: Record<Language, string> = {
  'en-GB': 'English (UK)',
  'pt-PT': 'Português',
};

export function isSupportedLanguage(value: unknown): value is Language {
  return typeof value === 'string' && (SUPPORTED_LANGUAGES as readonly string[]).includes(value);
}

/** Maps a browser language tag onto a supported language: any `pt*` → pt-PT, any `en*` → en-GB. */
export function matchBrowserLanguage(tag: string): Language | null {
  const primary = tag.toLowerCase().split('-')[0];
  if (primary === 'pt') return 'pt-PT';
  if (primary === 'en') return 'en-GB';
  return null;
}

function readStoredLanguage(): Language | null {
  try {
    const stored = localStorage.getItem(LANGUAGE_STORAGE_KEY);
    return isSupportedLanguage(stored) ? stored : null;
  } catch {
    return null;
  }
}

/**
 * The language to start with before the server is consulted:
 * the locally cached choice, then the browser's preferences, then en-GB.
 * The signed-in user's saved preference overrides this once it is fetched.
 */
export function detectInitialLanguage(
  browserLanguages: readonly string[] = typeof navigator === 'undefined' ? [] : navigator.languages,
): Language {
  const stored = readStoredLanguage();
  if (stored) return stored;

  for (const tag of browserLanguages) {
    const match = matchBrowserLanguage(tag);
    if (match) return match;
  }

  return DEFAULT_LANGUAGE;
}
