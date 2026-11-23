import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { StatusBadge } from './StatusBadge';
import i18n from '../i18n';

// The API serialises Domain.Enums.StudentStatus as its numeric value
// (Active = 1, Inactive = 2, Graduated = 3, Suspended = 4).
describe('StatusBadge', () => {
  it.each([
    [1, 'Active'],
    [2, 'Inactive'],
    [3, 'Graduated'],
    [4, 'Suspended'],
  ])('renders the numeric status %i the API actually sends as %s', (status, label) => {
    render(<StatusBadge status={status} />);
    expect(screen.getByText(label)).toBeInTheDocument();
  });

  it('still accepts the status by name', () => {
    render(<StatusBadge status="Graduated" />);
    expect(screen.getByText('Graduated')).toBeInTheDocument();
  });

  it('translates numeric statuses into pt-PT', async () => {
    await i18n.changeLanguage('pt-PT');
    render(<StatusBadge status={4} />);
    expect(screen.getByText('Suspenso')).toBeInTheDocument();
  });

  it('shows an unknown value as-is instead of crashing', () => {
    render(<StatusBadge status={9} />);
    expect(screen.getByText('9')).toBeInTheDocument();
  });
});
