import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import StudentsPage from './StudentsPage';
import { studentsApi } from '../api/studentsApi';
import { useAuth } from '../auth/AuthContext';

vi.mock('../api/studentsApi');
vi.mock('../api/tenantsApi');
vi.mock('../auth/AuthContext');

const mockedList = vi.mocked(studentsApi.list);
const mockedCreate = vi.mocked(studentsApi.create);
const mockedUseAuth = vi.mocked(useAuth);

// A fixed "today" so the calendar's default month (and the day-button under test) is
// deterministic instead of depending on whenever the suite happens to run. The target
// date must be in the same month (so it's visible without navigating the calendar) and
// safely before "today" — the calendar disables any date after "today" (a date of
// birth can't be in the future).
const FIXED_NOW = new Date(2024, 6, 20); // July 20, 2024
const TARGET_DATE = new Date(2024, 6, 5); // July 5, 2024

function renderPage() {
  mockedUseAuth.mockReturnValue({
    user: { email: 'admin@school.test', role: 'Admin', tenantId: 'tenant-1' },
    login: vi.fn(),
    logout: vi.fn(),
  });
  mockedList.mockResolvedValue([]);

  return render(
    <MemoryRouter>
      <Routes>
        <Route path="/" element={<StudentsPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

async function openCreateDialog(user: ReturnType<typeof userEvent.setup>) {
  await screen.findByText('No students yet');
  await user.click(screen.getAllByRole('button', { name: /add student/i })[0]);
  await screen.findByRole('dialog');
}

// The Popover's content portals in after the trigger click resolves React's own
// update, so the day buttons aren't guaranteed to exist synchronously — poll for it.
async function pickDate(user: ReturnType<typeof userEvent.setup>, date: Date) {
  await user.click(screen.getByRole('button', { name: /date of birth/i }));
  const dayButton = await waitFor(() => {
    const el = document.querySelector<HTMLElement>(`[data-day="${date.toLocaleDateString()}"]`);
    expect(el).not.toBeNull();
    return el!;
  });
  await user.click(dayButton);
}

describe('StudentsPage create dialog (Admin)', () => {
  beforeEach(() => {
    vi.setSystemTime(FIXED_NOW);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('submits the form with an ISO date and no tenantId once a date is picked', async () => {
    const user = userEvent.setup();
    mockedCreate.mockResolvedValue('new-student-id');
    renderPage();
    await openCreateDialog(user);

    await user.type(screen.getByLabelText(/first name/i), 'Ana');
    await user.type(screen.getByLabelText(/last name/i), 'Silva');
    await user.type(screen.getByLabelText(/email/i), 'ana@school.test');

    await pickDate(user, TARGET_DATE);

    await user.click(screen.getByRole('button', { name: /^add student$/i }));

    await waitFor(() => {
      expect(mockedCreate).toHaveBeenCalledWith({
        firstName: 'Ana',
        lastName: 'Silva',
        email: 'ana@school.test',
        dateOfBirth: '2024-07-05',
      });
    });
  });

  it('keeps the submit button disabled until a date of birth is chosen', async () => {
    const user = userEvent.setup();
    renderPage();
    await openCreateDialog(user);

    await user.type(screen.getByLabelText(/first name/i), 'Ana');
    await user.type(screen.getByLabelText(/last name/i), 'Silva');
    await user.type(screen.getByLabelText(/email/i), 'ana@school.test');

    expect(screen.getByRole('button', { name: /^add student$/i })).toBeDisabled();

    await pickDate(user, TARGET_DATE);

    expect(screen.getByRole('button', { name: /^add student$/i })).not.toBeDisabled();
  });

  it('resets the form when Cancel is clicked', async () => {
    const user = userEvent.setup();
    renderPage();
    await openCreateDialog(user);

    await user.type(screen.getByLabelText(/first name/i), 'Ana');
    await user.click(screen.getByRole('button', { name: /cancel/i }));

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());

    await user.click(screen.getAllByRole('button', { name: /add student/i })[0]);
    expect(screen.getByLabelText(/first name/i)).toHaveValue('');
  });

  it('does not close the dialog when clicking inside the date popover (onInteractOutside guard)', async () => {
    const user = userEvent.setup();
    renderPage();
    await openCreateDialog(user);

    await user.click(screen.getByRole('button', { name: /date of birth/i }));
    const popover = await waitFor(() => {
      const el = document.querySelector<HTMLElement>('[data-slot="popover-content"]');
      expect(el).not.toBeNull();
      return el!;
    });

    // Click the popover's own content container (not a day button) — the dialog's
    // onInteractOutside guard must not treat this as an outside click, since the
    // popover portals outside the dialog's DOM subtree. (Radix's Popover.Content is
    // itself role="dialog", so query the outer "Add Student" dialog by name.)
    await user.click(popover);

    expect(screen.getByRole('dialog', { name: /add student/i })).toBeInTheDocument();
  });
});
