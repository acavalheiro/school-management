import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import { enGB } from './locales/en-GB';
import { ptPT } from './locales/pt-PT';
import {
  DEFAULT_LANGUAGE,
  LANGUAGE_STORAGE_KEY,
  SUPPORTED_LANGUAGES,
  detectInitialLanguage,
} from './languages';

// Every change of language, whatever triggered it, is reflected in <html lang> (screen readers,
// hyphenation) and cached locally so the next visit, including the login page, starts in it.
i18n.on('languageChanged', (language) => {
  document.documentElement.lang = language;
  try {
    localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
  } catch {
    // Storage can be unavailable (private mode, blocked site data); the language still applies.
  }
});

void i18n.use(initReactI18next).init({
  resources: {
    'en-GB': { translation: enGB },
    'pt-PT': { translation: ptPT },
  },
  lng: detectInitialLanguage(),
  fallbackLng: DEFAULT_LANGUAGE,
  supportedLngs: [...SUPPORTED_LANGUAGES],
  nonExplicitSupportedLngs: false,
  load: 'currentOnly',
  initAsync: false,
  interpolation: { escapeValue: false }, // React already escapes
});

export default i18n;
