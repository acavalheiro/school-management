import { describe, expect, it } from 'vitest';
import { LANGUAGE_STORAGE_KEY, detectInitialLanguage, matchBrowserLanguage } from './languages';

describe('matchBrowserLanguage', () => {
  it.each([
    ['pt-PT', 'pt-PT'],
    ['pt-BR', 'pt-PT'],
    ['pt', 'pt-PT'],
    ['PT-br', 'pt-PT'],
    ['en-US', 'en-GB'],
    ['en', 'en-GB'],
    ['fr-FR', null],
  ])('maps %s to %s', (tag, expected) => {
    expect(matchBrowserLanguage(tag)).toBe(expected);
  });
});

describe('detectInitialLanguage', () => {
  it('prefers the locally cached choice over the browser', () => {
    localStorage.setItem(LANGUAGE_STORAGE_KEY, 'en-GB');

    expect(detectInitialLanguage(['pt-BR'])).toBe('en-GB');
  });

  it('ignores an unsupported cached value', () => {
    localStorage.setItem(LANGUAGE_STORAGE_KEY, 'pt-BR');

    expect(detectInitialLanguage(['pt-PT'])).toBe('pt-PT');
  });

  it('uses the first browser language that matches', () => {
    expect(detectInitialLanguage(['fr-FR', 'pt-BR', 'en-GB'])).toBe('pt-PT');
  });

  it('falls back to en-GB', () => {
    expect(detectInitialLanguage(['fr-FR', 'de'])).toBe('en-GB');
    expect(detectInitialLanguage([])).toBe('en-GB');
  });
});
