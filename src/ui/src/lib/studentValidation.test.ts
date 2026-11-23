import { describe, expect, it } from 'vitest';
import { validateStudent } from './studentValidation';

const NOW = new Date(2024, 6, 20);
const valid = { firstName: 'Ana', lastName: 'Silva', email: 'ana@school.test', dateOfBirth: '2015-03-01' };

describe('validateStudent', () => {
  it('accepts a valid student', () => {
    expect(validateStudent(valid, NOW)).toEqual({});
  });

  it('requires every field, ignoring surrounding whitespace', () => {
    expect(validateStudent({ firstName: ' ', lastName: '', email: '  ', dateOfBirth: '' }, NOW)).toEqual({
      firstName: 'required',
      lastName: 'required',
      email: 'required',
      dateOfBirth: 'required',
    });
  });

  it('enforces the server length limits', () => {
    expect(validateStudent({ ...valid, firstName: 'a'.repeat(101) }, NOW).firstName).toBe('tooLong');
    expect(validateStudent({ ...valid, firstName: 'a'.repeat(100) }, NOW).firstName).toBeUndefined();
    expect(validateStudent({ ...valid, email: `${'a'.repeat(250)}@b.pt` }, NOW).email).toBe('tooLong');
  });

  it.each(['ana', 'ana@', '@school.test', 'ana @school.test', 'a@b@c'])('rejects the email %s', (email) => {
    expect(validateStudent({ ...valid, email }, NOW).email).toBe('emailInvalid');
  });

  it('requires the date of birth to be strictly in the past', () => {
    expect(validateStudent({ ...valid, dateOfBirth: '2024-07-20' }, NOW).dateOfBirth).toBe('dateNotInPast');
    expect(validateStudent({ ...valid, dateOfBirth: '2024-08-01' }, NOW).dateOfBirth).toBe('dateNotInPast');
    expect(validateStudent({ ...valid, dateOfBirth: '2024-07-19' }, NOW).dateOfBirth).toBeUndefined();
  });
});
