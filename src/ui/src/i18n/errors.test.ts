import { describe, expect, it } from 'vitest';
import { ApiError } from '@/api/http';
import i18n from './index';
import { errorMessage } from './errors';

const t = i18n.t.bind(i18n);

describe('errorMessage', () => {
  it.each([
    [400, 'Some of the details are not valid'],
    [401, 'Your session has expired'],
    [403, "You don't have permission"],
    [404, "We couldn't find"],
    [409, 'That already exists'],
    [429, 'Too many attempts'],
    [500, 'The server had a problem'],
    [503, 'The server had a problem'],
    [418, 'Something went wrong'],
  ])('maps HTTP %i to a friendly message', (status, expected) => {
    expect(errorMessage(new ApiError(status, 'body'), t)).toContain(expected);
  });

  it('never includes the raw response body', () => {
    const message = errorMessage(new ApiError(400, 'FirstName: First name is required.'), t);
    expect(message).not.toContain('First name is required');
  });

  it('maps a network failure', () => {
    expect(errorMessage(new TypeError('Failed to fetch'), t)).toContain("Couldn't reach the server");
  });

  it('falls back to a generic message for anything else', () => {
    expect(errorMessage(new Error('boom'), t)).toBe('Something went wrong. Please try again.');
    expect(errorMessage('weird', t)).toBe('Something went wrong. Please try again.');
  });

  it('translates into the active language', async () => {
    await i18n.changeLanguage('pt-PT');
    expect(errorMessage(new ApiError(404, ''), t)).toBe('Não foi possível encontrar o que procurava.');
  });
});
