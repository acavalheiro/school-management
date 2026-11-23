import { useId, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Languages } from 'lucide-react';
import {
  LANGUAGE_NAMES,
  LANGUAGE_SHORT_NAMES,
  SUPPORTED_LANGUAGES,
  isSupportedLanguage,
} from '@/i18n/languages';
import { useLanguage } from '@/i18n/useLanguage';

/** `compact` uses the short language names, for narrow containers such as the sidebar. */
export function LanguageSwitcher({ className, compact = false }: { className?: string; compact?: boolean }) {
  const { t } = useTranslation();
  const names = compact ? LANGUAGE_SHORT_NAMES : LANGUAGE_NAMES;
  const { language, setLanguage } = useLanguage();
  const id = useId();
  const [saveFailed, setSaveFailed] = useState(false);

  return (
    <div className={className}>
      <div className="flex items-center gap-2">
        <label htmlFor={id} className="text-muted-foreground">
          <Languages size={16} aria-hidden="true" />
          <span className="sr-only">{t('language.label')}</span>
        </label>
        <select
          id={id}
          value={language}
          onChange={(e) => {
            if (!isSupportedLanguage(e.target.value)) return;
            setSaveFailed(false);
            void setLanguage(e.target.value).then((saved) => setSaveFailed(!saved));
          }}
          className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
        >
          {SUPPORTED_LANGUAGES.map((lang) => (
            <option key={lang} value={lang} lang={lang}>
              {names[lang]}
            </option>
          ))}
        </select>
      </div>
      {saveFailed && (
        <p role="alert" className="mt-1 text-xs text-destructive">
          {t('language.saveFailed')}
        </p>
      )}
    </div>
  );
}
