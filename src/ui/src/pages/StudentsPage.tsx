import { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { UserPlus, GraduationCap } from 'lucide-react';
import { studentsApi, type StudentDto, type CreateStudentRequest } from '../api/studentsApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
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

export default function StudentsPage() {
  const [students, setStudents] = useState<StudentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<CreateStudentRequest>(emptyForm);
  const [searchParams, setSearchParams] = useSearchParams();

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
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setSaving(true);
    try {
      await studentsApi.create(form);
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
        <Button onClick={() => setOpen(true)} size="sm" className="gap-2">
          <UserPlus size={15} />
          Add Student
        </Button>
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
                      <Button
                        variant="outline"
                        size="sm"
                        className="mt-4"
                        onClick={() => setOpen(true)}
                      >
                        Add Student
                      </Button>
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
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Student</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleCreate} className="space-y-4 mt-2">
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
              <Input
                id="dob"
                type="date"
                value={form.dateOfBirth}
                onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })}
                required
              />
            </div>
            <div className="flex justify-end gap-3 pt-2">
              <Button type="button" variant="outline" onClick={handleClose}>
                Cancel
              </Button>
              <Button type="submit" disabled={saving}>
                {saving ? 'Saving…' : 'Add Student'}
              </Button>
            </div>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
