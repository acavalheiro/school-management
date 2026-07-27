import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { UserPlus, GraduationCap, Calendar as CalendarIcon } from 'lucide-react';
import { studentsApi, type StudentDto, type CreateStudentRequest } from '../api/studentsApi';
import { tenantsApi, type TenantDto } from '../api/tenantsApi';
import { useAuth } from '../auth/AuthContext';
import { cn } from '@/lib/utils';
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

function StatusBadge({ status }: { status: string }) {
  const isActive = status.toLowerCase() === 'active';
  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
        isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'
      }`}
    >
      {status}
    </span>
  );
}

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
function toISODate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

function parseISODate(s: string): Date | undefined {
  return s ? new Date(`${s}T00:00:00`) : undefined;
}

export default function StudentsPage() {
  const { user } = useAuth();
  // Creating students is an Admin/SuperAdmin action server-side; mirror that in the UI.
  const canManage = user?.role === 'Admin' || user?.role === 'SuperAdmin';
  const isSuperAdmin = user?.role === 'SuperAdmin';

  const [students, setStudents] = useState<StudentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [open, setOpen] = useState(false);
  const [dobOpen, setDobOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<CreateStudentRequest>(emptyForm);
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
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  function handleClose() {
    setOpen(false);
    setForm(emptyForm);
    setSelectedTenant(null);
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
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
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-foreground">Students</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            {loading
              ? 'Loading…'
              : `${students.length} student${students.length !== 1 ? 's' : ''} enrolled`}
          </p>
        </div>
        {canManage && (
          <Button onClick={() => setOpen(true)} size="sm" className="gap-2">
            <UserPlus size={15} />
            Add Student
          </Button>
        )}
      </div>

      {/* Table */}
      <div className="rounded-xl border bg-card overflow-hidden">
        {error ? (
          <div className="flex items-center justify-center py-12 text-sm text-destructive">
            {error}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/40">
                {['Name', 'Email', 'Status', 'Date of Birth'].map((h) => (
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
                      <p className="text-sm font-medium text-foreground">No students yet</p>
                      <p className="text-xs text-muted-foreground mt-1">
                        Add your first student to get started.
                      </p>
                      {canManage && (
                        <Button
                          variant="outline"
                          size="sm"
                          className="mt-4"
                          onClick={() => setOpen(true)}
                        >
                          Add Student
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
                      {new Date(s.dateOfBirth).toLocaleDateString()}
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
            <DialogTitle>Add Student</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleCreate} className="space-y-4 mt-2">
            {isSuperAdmin && (
              <div className="space-y-2">
                <Label htmlFor="tenant">Tenant</Label>
                <Combobox
                  items={tenantOptions}
                  value={selectedTenant}
                  onValueChange={(item: TenantOption | null) => setSelectedTenant(item)}
                >
                  <ComboboxInput id="tenant" placeholder="Search tenants…" />
                  <ComboboxContent container={portalContainer}>
                    <ComboboxEmpty>No tenants found.</ComboboxEmpty>
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
                <Label htmlFor="firstName">First name</Label>
                <Input
                  id="firstName"
                  value={form.firstName}
                  onChange={(e) => setForm({ ...form, firstName: e.target.value })}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lastName">Last name</Label>
                <Input
                  id="lastName"
                  value={form.lastName}
                  onChange={(e) => setForm({ ...form, lastName: e.target.value })}
                  required
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="dob">Date of birth</Label>
              <Popover open={dobOpen} onOpenChange={setDobOpen}>
                <PopoverTrigger asChild>
                  <Button
                    id="dob"
                    type="button"
                    variant="outline"
                    className={cn(
                      'w-full justify-start text-left font-normal',
                      !form.dateOfBirth && 'text-muted-foreground',
                    )}
                  >
                    <CalendarIcon className="mr-2 size-4" />
                    {form.dateOfBirth
                      ? parseISODate(form.dateOfBirth)!.toLocaleDateString(undefined, {
                          year: 'numeric',
                          month: 'long',
                          day: 'numeric',
                        })
                      : 'Pick a date'}
                  </Button>
                </PopoverTrigger>
                <PopoverContent className="w-auto p-0" align="start" container={portalContainer}>
                  <Calendar
                    mode="single"
                    selected={parseISODate(form.dateOfBirth)}
                    onSelect={(d) => {
                      if (d) {
                        setForm({ ...form, dateOfBirth: toISODate(d) });
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
            </div>
            <div className="flex justify-end gap-3 pt-2">
              <Button type="button" variant="outline" onClick={handleClose}>
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={saving || !form.dateOfBirth || (isSuperAdmin && !selectedTenant)}
              >
                {saving ? 'Saving…' : 'Add Student'}
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
