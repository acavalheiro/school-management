import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import StudentsPage from './StudentsPage';
import { studentsApi, type StudentDto } from '../api/studentsApi';
import { ApiError } from '../api/http';
import { useAuth } from '../auth/AuthContext';
import i18n from '../i18n';

vi.mock('../api/studentsApi');
vi.mock('../api/tenantsApi');
vi.mock('../auth/AuthContext');

const mockedList = vi.mocked(studentsApi.list);
const mockedCreate = vi.mocked(studentsApi.create);
const mockedUseAuth = vi.mocked(useAuth);

const FIXED_NOW = new Date(2024, 6, 20);
const TARGET_DATE = new Date(2024, 6, 5);

function student(overrides: Partial<StudentDto> = {}): StudentDto {
  return {
    id: 's1',
    firstName: 'Ana',
    lastName: 'Silva',
    email: 'ana@school.test',
    dateOfBirth: '2015-03-01',
    status: 'Active',
    createdAt: '2024-01-01T00:00:00Z',
    ...overrides,
  };
}

function renderPage(students: StudentDto[] = []) {
  mockedUseAuth.mockReturnValue({
    user: { email: 'admin@school.test', role: 'Admin', tenantId: 'tenant-1' },
    login: vi.fn(),
    logout: vi.fn(),
  });
  mockedList.mockResolvedValue(students);

  return render(
    <MemoryRouter>
      <Routes>
        <Route path="/" element={<StudentsPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

async function openDialog(user: ReturnType<typeof userEvent.setup>, addLabel: RegExp) {
  await user.click((await screen.findAllByRole('button', { name: addLabel }))[0]);
  return screen.findByRole('dialog');
}

async function pickDate(user: ReturnType<typeof userEvent.setup>, dobLabel: RegExp) {
  await user.click(screen.getByRole('button', { name: dobLabel }));
  const day = await waitFor(() => {
    const el = document.querySelector<HTMLElement>(`[data-day="${TARGET_DATE.toLocaleDateString()}"]`);
    expect(el).not.toBeNull();
    return el!;
  });
  await user.click(day);
}

describe('StudentsPage translations', () => {
  it('renders headings, plural count, status and dates in en-GB', async () => {
    renderPage([student(), student({ id: 's2', status: 'Graduated' })]);

    expect(await screen.findByText('2 students enrolled')).toBeInTheDocument();
    expect(screen.getByText('Date of Birth')).toBeInTheDocument();
    expect(screen.getByText('Graduated')).toBeInTheDocument();
    expect(screen.getAllByText('1 Mar 2015')).toHaveLength(2);
  });

  it('renders the same page in pt-PT, with pt-PT date formatting', async () => {
    await i18n.changeLanguage('pt-PT');
    renderPage([student({ status: 'Suspended' })]);

    expect(await screen.findByText('1 aluno matriculado')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Alunos' })).toBeInTheDocument();
    expect(screen.getByText('Data de nascimento')).toBeInTheDocument();
    expect(screen.getByText('Suspenso')).toBeInTheDocument();
    expect(
      screen.getByText(new Intl.DateTimeFormat('pt-PT', { dateStyle: 'medium' }).format(new Date(2015, 2, 1))),
    ).toBeInTheDocument();
    expect(screen.queryByText('1 Mar 2015')).not.toBeInTheDocument();
  });

  it('uses the plural form for zero in pt-PT', async () => {
    await i18n.changeLanguage('pt-PT');
    renderPage([]);

    expect(await screen.findByText('0 alunos matriculados')).toBeInTheDocument();
  });

  it('shows an unrecognised status as-is rather than a missing key', async () => {
    renderPage([student({ status: 'Transferred' })]);

    expect(await screen.findByText('Transferred')).toBeInTheDocument();
  });
});

describe('StudentsPage create-student validation', () => {
  beforeEach(() => {
    vi.setSystemTime(FIXED_NOW);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('shows field-specific messages and does not call the API', async () => {
    const user = userEvent.setup();
    renderPage();
    const dialog = await openDialog(user, /add student/i);

    await user.type(within(dialog).getByLabelText(/email/i), 'not-an-email');
    await pickDate(user, /date of birth/i);
    await user.click(within(dialog).getByRole('button', { name: /^add student$/i }));

    expect(within(dialog).getAllByText('This field is required.')).toHaveLength(2);
    expect(within(dialog).getByText('Enter a valid email address.')).toBeInTheDocument();
    expect(within(dialog).getByLabelText(/first name/i)).toHaveAttribute('aria-invalid', 'true');
    expect(within(dialog).getByLabelText(/first name/i)).toHaveAccessibleDescription('This field is required.');
    expect(mockedCreate).not.toHaveBeenCalled();
  });

  it('shows the same messages in pt-PT', async () => {
    const user = userEvent.setup();
    await i18n.changeLanguage('pt-PT');
    renderPage();
    const dialog = await openDialog(user, /adicionar aluno/i);

    await user.type(within(dialog).getByLabelText(/email/i), 'not-an-email');
    await pickDate(user, /data de nascimento/i);
    await user.click(within(dialog).getByRole('button', { name: /^adicionar aluno$/i }));

    expect(within(dialog).getAllByText('Este campo é obrigatório.')).toHaveLength(2);
    expect(within(dialog).getByText('Introduza um endereço de email válido.')).toBeInTheDocument();
    expect(mockedCreate).not.toHaveBeenCalled();
  });

  it('clears a field error as soon as the field is edited', async () => {
    const user = userEvent.setup();
    renderPage();
    const dialog = await openDialog(user, /add student/i);

    await pickDate(user, /date of birth/i);
    await user.click(within(dialog).getByRole('button', { name: /^add student$/i }));
    expect(within(dialog).getAllByText('This field is required.')).toHaveLength(3);

    await user.type(within(dialog).getByLabelText(/first name/i), 'A');

    expect(within(dialog).getAllByText('This field is required.')).toHaveLength(2);
  });

  it('does not offer today as a date of birth, since it must be strictly in the past', async () => {
    const user = userEvent.setup();
    renderPage();
    const dialog = await openDialog(user, /add student/i);

    await user.click(within(dialog).getByRole('button', { name: /date of birth/i }));
    const dayButton = (d: Date) =>
      waitFor(() => {
        const el = document.querySelector<HTMLButtonElement>(`[data-day="${d.toLocaleDateString()}"]`);
        expect(el).not.toBeNull();
        return el!;
      });

    expect(await dayButton(FIXED_NOW)).toBeDisabled();
    expect(await dayButton(new Date(2024, 6, 19))).not.toBeDisabled();
  });

  it('shows a translated error and keeps the dialog open when the API rejects', async () => {
    const user = userEvent.setup();
    mockedCreate.mockRejectedValue(new ApiError(400, 'DateOfBirth: Date of birth must be in the past.'));
    renderPage();
    const dialog = await openDialog(user, /add student/i);

    await user.type(within(dialog).getByLabelText(/first name/i), 'Ana');
    await user.type(within(dialog).getByLabelText(/last name/i), 'Silva');
    await user.type(within(dialog).getByLabelText(/email/i), 'ana@school.test');
    await pickDate(user, /date of birth/i);
    await user.click(within(dialog).getByRole('button', { name: /^add student$/i }));

    expect(await within(dialog).findByRole('alert')).toHaveTextContent(/some of the details are not valid/i);
    expect(screen.queryByText(/must be in the past/)).not.toBeInTheDocument();
    expect(screen.getByRole('dialog', { name: /add student/i })).toBeInTheDocument();
  });
});
