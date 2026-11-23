import type { CreateStudentRequest } from '@/api/studentsApi';

// Mirrors CreateStudentValidator on the API, so users see specific, translated problems before
// submitting. The server stays authoritative and still validates everything.

export const STUDENT_LIMITS = { name: 100, email: 254 } as const;

// FluentValidation's EmailAddress(): something@something, with no whitespace.
const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+$/;

export type StudentField = 'firstName' | 'lastName' | 'email' | 'dateOfBirth';

export type StudentFieldError =
  | 'required'
  | 'tooLong'
  | 'emailInvalid'
  | 'dateNotInPast';

export type StudentErrors = Partial<Record<StudentField, StudentFieldError>>;

function todayISO(now: Date): string {
  const m = String(now.getMonth() + 1).padStart(2, '0');
  const d = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${m}-${d}`;
}

export function validateStudent(form: CreateStudentRequest, now = new Date()): StudentErrors {
  const errors: StudentErrors = {};

  for (const field of ['firstName', 'lastName'] as const) {
    const value = form[field].trim();
    if (!value) errors[field] = 'required';
    else if (value.length > STUDENT_LIMITS.name) errors[field] = 'tooLong';
  }

  const email = form.email.trim();
  if (!email) errors.email = 'required';
  else if (email.length > STUDENT_LIMITS.email) errors.email = 'tooLong';
  else if (!EMAIL_PATTERN.test(email)) errors.email = 'emailInvalid';

  if (!form.dateOfBirth) errors.dateOfBirth = 'required';
  // ISO dates compare correctly as strings.
  else if (form.dateOfBirth >= todayISO(now)) errors.dateOfBirth = 'dateNotInPast';

  return errors;
}
