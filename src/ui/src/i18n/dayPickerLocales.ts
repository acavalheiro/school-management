import { enGB, pt } from 'react-day-picker/locale';
import type { Language } from './languages';

// react-day-picker's own locales: date-fns formats plus translated aria labels.
// Its `pt` is European Portuguese (`pt-BR` is separate).
export const DAY_PICKER_LOCALES = {
  'en-GB': enGB,
  'pt-PT': pt,
} satisfies Record<Language, unknown>;
