import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { UserPlus, GraduationCap, Calendar as CalendarIcon } from 'lucide-react';
import { studentsApi, type StudentDto, type CreateStudentRequest } from '../api/studentsApi';
import { tenantsApi, type TenantDto } from '../api/tenantsApi';
import { useAuth } from '../auth/AuthContext';
import { cn } from '@/lib/utils';
import { useTranslation } from 'react-i18next';
import { errorMessage } from '@/i18n/errors';
import { useLanguage } from '@/i18n/useLanguage';
import { StatusBadge } from '@/components/StatusBadge';
import {
  STUDENT_LIMITS,
  validateStudent,
  type StudentErrors,
  type StudentField,
} from '@/lib/studentValidation';
import { Button } from '@/components/ui/button';
import { Calendar } from '@/components/ui/calendar';
import {
  Combobox,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxInput,
  ComboboxItem,
  ComboboxList,
} from '@/components/ui/combobox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

function TableSkeleton() {
  return (
    <>
      {[70, 55, 80, 65].map((w, i) => (
        <tr key={i} className="border-b last:border-0">
          <td className="px-4 py-3">
            <div className="flex items-center gap-3">
              <div className="w-7 h-7 rounded-full bg-muted animate-pulse shrink-0" />
              <div className="h-4 bg-muted rounded animate-pulse" style={{ width: `${w}px` }} />
            </div>
          </td>
          <td className="px-4 py-3">
            <div className="h-4 bg-muted rounded animate-pulse w-40" />
          </td>
          <td className="px-4 py-3">
            <div className="h-5 w-14 bg-muted rounded-full animate-pulse" />
          </td>
          <td className="px-4 py-3">
            <div className="h-4 bg-muted rounded animate-pulse w-24" />
          </td>
        </tr>
      ))}
    </>
  );
}

const emptyForm: CreateStudentRequest = {
  firstName: '',
  lastName: '',
  email: '',
  dateOfBirth: '',
};

type TenantOption = { value: string; label: string };

// The API expects a plain 'YYYY-MM-DD' date; convert to/from a Date using local
// components so the calendar's selection is not shifted by the timezone.
export function toISODate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

export function parseISODate(s: string): Date | undefined {
  return s ? new Date(`${s}T00:00:00`) : undefined;
}

export default function StudentsPage() {
  const { t } = useTranslation();
  const { formatDate } = useLanguage();
  const { user } = useAuth();
  // Creating students is an Admin/SuperAdmin action server-side; mirror that in the UI.
  const canManage = user?.role === 'Admin' || user?.role === 'SuperAdmin';
  const isSuperAdmin = user?.role === 'SuperAdmin';

  const [students, setStudents] = useState<StudentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [open, setOpen] = useState(false);
  const [dobOpen, setDobOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<CreateStudentRequest>(emptyForm);
  const [fieldErrors, setFieldErrors] = useState<StudentErrors>({});
  const [createError, setCreateError] = useState<unknown>(null);
  // Popups must portal into the dialog: a modal dialog sets body pointer-events:none,
  // so anything portalled to body renders but cannot be clicked.
  const [portalContainer, setPortalContainer] = useState<HTMLElement | null>(null);
  const [searchParams, setSearchParams] = useSearchParams();

  // A SuperAdmin has no tenant of their own, so they must pick the target tenant.
  const [tenants, setTenants] = useState<TenantDto[]>([]);
  const [selectedTenant, setSelectedTenant] = useState<TenantOption | null>(null);
  // Stable references so Base UI can match the selected value against the item list.
  const tenantOptions = useMemo<TenantOption[]>(
    () => tenants.map((t) => ({ value: t.id, label: t.name })),
    [tenants],
  );

  useEffect(() => {
    if (isSuperAdmin && open && tenants.length === 0) {
      tenantsApi.list().then(setTenants).catch(() => {});
    }
  }, [isSuperAdmin, open, tenants.length]);

  useEffect(() => {
    if (searchParams.get('action') === 'add') {
      setOpen(true);
      setSearchParams({}, { replace: true });
    }
  }, []);

  useEffect(() => {
    studentsApi
      .list()
      .then(setStudents)
      .catch((e: unknown) => setError(e))
      .finally(() => setLoading(false));
  }, []);

  function updateField(field: StudentField, value: string) {
    setForm((f) => ({ ...f, [field]: value }));
    // Clear a field's error as soon as the user edits it; it is re-checked on submit.
    setFieldErrors((errors) => {
      const next = { ...errors };
      delete next[field];
      return next;
    });
  }

  function handleClose() {
    setOpen(false);
    setForm(emptyForm);
    setFieldErrors({});
    setCreateError(null);
    setSelectedTenant(null);
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setCreateError(null);
    const errors = validateStudent(form);
    setFieldErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setSaving(true);
    try {
      // Only a SuperAdmin sends tenantId; an Admin's tenant comes from their token,
      // and sending an empty string would fail Guid binding server-side.
      await studentsApi.create(
        isSuperAdmin ? { ...form, tenantId: selectedTenant?.value } : form,
      );
      const updated = await studentsApi.list();
      setStudents(updated);
      handleClose();
    } catch (err) {
      setCreateError(err);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-foreground">{t('students.title')}</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            {loading ? t('common.loading') : t('students.enrolled', { count: students.length })}
          </p>
        </div>
        {canManage && (
          <Button onClick={() => setOpen(true)} size="sm" className="gap-2">
            <UserPlus size={15} />
            {t('students.add')}
          </Button>
        )}
      </div>

      {/* Table */}
      <div className="rounded-xl border bg-card overflow-hidden">
        {error != null ? (
          <div className="flex items-center justify-center py-12 text-sm text-destructive">
            {errorMessage(error, t)}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/40">
                {[
                  t('students.columns.name'),
                  t('students.columns.email'),
                  t('students.columns.status'),
                  t('students.columns.dateOfBirth'),
                ].map((h) => (
                  <th
                    key={h}
                    className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wide"
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <TableSkeleton />
              ) : students.length === 0 ? (
                <tr>
                  <td colSpan={4}>
                    <div className="flex flex-col items-center justify-center py-16 text-center">
                      <GraduationCap size={32} className="text-muted-foreground/40 mb-3" />
                      <p className="text-sm font-medium text-foreground">{t('students.empty')}</p>
                      <p className="text-xs text-muted-foreground mt-1">
                        {t('students.emptyHint')}
                      </p>
                      {canManage && (
                        <Button
                          variant="outline"
                          size="sm"
                          className="mt-4"
                          onClick={() => setOpen(true)}
                        >
                          {t('students.add')}
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
              ) : (
                students.map((s) => (
                  <tr
                    key={s.id}
                    className="border-b last:border-0 hover:bg-muted/30 transition-colors"
                  >
                    <td className="px-4 py-3 font-medium text-foreground">
                      <div className="flex items-center gap-3">
                        <div className="w-7 h-7 rounded-full bg-muted flex items-center justify-center text-xs font-medium text-muted-foreground shrink-0">
                          {s.firstName[0]}
                          {s.lastName[0]}
                        </div>
                        {s.firstName} {s.lastName}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{s.email}</td>
                    <td className="px-4 py-3">
                      <StatusBadge status={s.status} />
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {formatDate(parseISODate(s.dateOfBirth.slice(0, 10))!, { dateStyle: 'medium' })}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        )}
      </div>

      {/* Add Student Dialog */}
      <Dialog open={open} onOpenChange={handleClose}>
        <DialogContent
          onInteractOutside={(e) => {
            // The tenant combobox and the date-picker calendar are portalled outside
            // the dialog; interacting with them must not be treated as an outside-click
            // that closes the dialog.
            const target = e.detail.originalEvent.target as Element | null;
            if (target?.closest('[data-slot="combobox-content"], [data-slot="popover-content"]'))
              e.preventDefault();
          }}
        >
          <DialogHeader>
            <DialogTitle>{t('students.add')}</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleCreate} noValidate className="space-y-4 mt-2">
            {isSuperAdmin && (
              <div className="space-y-2">
                <Label htmlFor="tenant">{t('students.form.tenant')}</Label>
                <Combobox
                  items={tenantOptions}
                  value={selectedTenant}
                  onValueChange={(item: TenantOption | null) => setSelectedTenant(item)}
                >
                  <ComboboxInput id="tenant" placeholder={t('students.form.searchTenants')} />
                  <ComboboxContent container={portalContainer}>
                    <ComboboxEmpty>{t('students.form.noTenants')}</ComboboxEmpty>
                    <ComboboxList>
                      {(item: TenantOption) => (
                        <ComboboxItem key={item.value} value={item}>
                          {item.label}
                        </ComboboxItem>
                      )}
                    </ComboboxList>
                  </ComboboxContent>
                </Combobox>
              </div>
            )}
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="firstName">{t('students.form.firstName')}</Label>
                <Input
                  id="firstName"
                  value={form.firstName}
                  onChange={(e) => updateField('firstName', e.target.value)}
                  maxLength={STUDENT_LIMITS.name}
                  aria-invalid={fieldErrors.firstName ? true : undefined}
                  aria-describedby={fieldErrors.firstName ? 'firstName-error' : undefined}
                  required
                />
                <FieldError id="firstName-error" error={fieldErrors.firstName} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lastName">{t('students.form.lastName')}</Label>
                <Input
                  id="lastName"
                  value={form.lastName}
                  onChange={(e) => updateField('lastName', e.target.value)}
                  maxLength={STUDENT_LIMITS.name}
                  aria-invalid={fieldErrors.lastName ? true : undefined}
                  aria-describedby={fieldErrors.lastName ? 'lastName-error' : undefined}
                  required
                />
                <FieldError id="lastName-error" error={fieldErrors.lastName} />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="email">{t('students.form.email')}</Label>
              <Input
                id="email"
                type="email"
                value={form.email}
                onChange={(e) => updateField('email', e.target.value)}
                maxLength={STUDENT_LIMITS.email}
                aria-invalid={fieldErrors.email ? true : undefined}
                aria-describedby={fieldErrors.email ? 'email-error' : undefined}
                required
              />
              <FieldError id="email-error" error={fieldErrors.email} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="dob">{t('students.form.dateOfBirth')}</Label>
              <Popover open={dobOpen} onOpenChange={setDobOpen}>
                <PopoverTrigger asChild>
                  <Button
                    id="dob"
                    type="button"
                    variant="outline"
                    aria-invalid={fieldErrors.dateOfBirth ? true : undefined}
                    aria-describedby={fieldErrors.dateOfBirth ? 'dob-error' : undefined}
                    className={cn(
                      'w-full justify-start text-left font-normal',
                      !form.dateOfBirth && 'text-muted-foreground',
                    )}
                  >
                    <CalendarIcon className="mr-2 size-4" />
                    {form.dateOfBirth
                      ? formatDate(parseISODate(form.dateOfBirth)!, { dateStyle: 'long' })
                      : t('students.form.pickDate')}
                  </Button>
                </PopoverTrigger>
                <PopoverContent className="w-auto p-0" align="start" container={portalContainer}>
                  <Calendar
                    mode="single"
                    selected={parseISODate(form.dateOfBirth)}
                    onSelect={(d) => {
                      if (d) {
                        updateField('dateOfBirth', toISODate(d));
                        setDobOpen(false);
                      }
                    }}
                    captionLayout="dropdown"
                    startMonth={new Date(1950, 0)}
                    endMonth={new Date()}
                    disabled={{ after: new Date() }}
                    defaultMonth={parseISODate(form.dateOfBirth) ?? new Date()}
                    autoFocus
                  />
                </PopoverContent>
              </Popover>
              <FieldError id="dob-error" error={fieldErrors.dateOfBirth} />
            </div>
            {createError != null && (
              <p role="alert" className="text-sm text-destructive">
                {errorMessage(createError, t)}
              </p>
            )}
            <div className="flex justify-end gap-3 pt-2">
              <Button type="button" variant="outline" onClick={handleClose}>
                {t('common.cancel')}
              </Button>
              <Button
                type="submit"
                disabled={saving || !form.dateOfBirth || (isSuperAdmin && !selectedTenant)}
              >
                {saving ? t('common.saving') : t('students.add')}
              </Button>
            </div>
          </form>
          {/* Portal target inside the dialog for the combobox/date-picker popups. */}
          <div ref={setPortalContainer} />
        </DialogContent>
      </Dialog>
    </div>
  );
}

function FieldError({ id, error }: { id: string; error?: StudentErrors[StudentField] }) {
  const { t } = useTranslation();
  if (!error) return null;
  return (
    <p id={id} className="text-xs text-destructive">
      {t(`validation.${error}`)}
    </p>
  );
}
