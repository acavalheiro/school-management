import 'i18next';
import type { enGB } from './locales/en-GB';

// Types every t() key against the en-GB catalogue: an unknown key is a compile error.
declare module 'i18next' {
  interface CustomTypeOptions {
    defaultNS: 'translation';
    resources: { translation: typeof enGB };
  }
}
