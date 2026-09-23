import { describe, expect, it } from 'vitest';
import { parseISODate, toISODate } from './StudentsPage';

describe('toISODate', () => {
  it('formats using local date components, not UTC', () => {
    // A local midnight date must not shift to the previous/next day when formatted.
    const d = new Date(2024, 0, 15); // Jan 15, 2024, local time
    expect(toISODate(d)).toBe('2024-01-15');
  });

  it('pads single-digit month and day', () => {
    const d = new Date(2024, 2, 5); // March 5, 2024
    expect(toISODate(d)).toBe('2024-03-05');
  });
});

describe('parseISODate', () => {
  it('round-trips with toISODate for a first-of-month date', () => {
    const iso = '2024-02-01';
    expect(toISODate(parseISODate(iso)!)).toBe(iso);
  });

  it('round-trips with toISODate for a Dec 31 boundary date', () => {
    const iso = '2023-12-31';
    expect(toISODate(parseISODate(iso)!)).toBe(iso);
  });

  it('returns undefined for an empty string', () => {
    expect(parseISODate('')).toBeUndefined();
  });
});
