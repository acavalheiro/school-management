import { useEffect, useState, type FormEvent } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, Plus, Users, ShieldCheck, Copy, Check } from 'lucide-react';
import {
  tenantsApi,
  type TenantDto,
  type TenantUserDto,
  type CreatedUserResponse,
} from '../api/tenantsApi';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogClose,
} from '@/components/ui/dialog';

const ROLES = ['Admin', 'User'] as const;

export default function TenantDetailPage() {
  const { id = '' } = useParams();

  const [tenant, setTenant] = useState<TenantDto | null>(null);
  const [users, setUsers] = useState<TenantUserDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<string>('Admin');
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [created, setCreated] = useState<CreatedUserResponse | null>(null);

  function loadUsers() {
    setLoading(true);
    setError(null);
    Promise.all([tenantsApi.get(id), tenantsApi.listUsers(id)])
      .then(([t, u]) => {
        setTenant(t);
        setUsers(u);
      })
      .catch((e: unknown) => setError(e instanceof Error ? e.message : 'Failed to load tenant'))
      .finally(() => setLoading(false));
  }

  useEffect(loadUsers, [id]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setCreateError(null);
    setCreating(true);
    try {
      const result = await tenantsApi.createUser(id, { email: email.trim(), role });
      setCreated(result);
      const updated = await tenantsApi.listUsers(id);
      setUsers(updated);
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : 'Failed to create user');
    } finally {
      setCreating(false);
    }
  }

  function handleOpenChange(open: boolean) {
    setDialogOpen(open);
    if (!open) {
      // Reset everything, including the one-time password panel — it is never shown again.
      setEmail('');
      setRole('Admin');
      setCreateError(null);
      setCreated(null);
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="space-y-3">
        <Link
          to="/tenants"
          className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft size={15} />
          Back to tenants
        </Link>
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold text-foreground">
              {tenant?.name ?? 'Tenant'}
            </h1>
            <p className="text-sm text-muted-foreground mt-1">
              Manage the admins and users of this school
            </p>
          </div>
          <Button onClick={() => setDialogOpen(true)} className="gap-2">
            <Plus size={16} />
            Add User
          </Button>
        </div>
      </div>

      {/* Content */}
      {loading ? (
        <UsersSkeleton />
      ) : error ? (
        <div className="rounded-lg border border-destructive/40 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      ) : users.length === 0 ? (
        <EmptyState onNew={() => setDialogOpen(true)} />
      ) : (
        <UsersTable users={users} />
      )}

      {/* Add user dialog */}
      <Dialog open={dialogOpen} onOpenChange={handleOpenChange}>
        <DialogContent>
          {created ? (
            <CreatedPanel created={created} onDone={() => handleOpenChange(false)} />
          ) : (
            <>
              <DialogHeader>
                <DialogTitle>Add user</DialogTitle>
                <DialogDescription>
                  Create an admin or user for this tenant. A temporary password is
                  generated and shown once.
                </DialogDescription>
              </DialogHeader>

              <form onSubmit={handleCreate} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="user-email">Email</Label>
                  <Input
                    id="user-email"
                    type="email"
                    placeholder="e.g. staff@greenfield.edu"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    autoFocus
                    required
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="user-role">Role</Label>
                  <select
                    id="user-role"
                    value={role}
                    onChange={(e) => setRole(e.target.value)}
                    className="flex h-9 w-full rounded-md border border-input bg-background text-foreground px-3 py-1 text-sm shadow-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                  >
                    {ROLES.map((r) => (
                      <option key={r} value={r} className="bg-background text-foreground">
                        {r}
                      </option>
                    ))}
                  </select>
                </div>

                {createError && <p className="text-sm text-destructive">{createError}</p>}

                <div className="flex justify-end gap-2 pt-1">
                  <DialogClose asChild>
                    <Button type="button" variant="outline" disabled={creating}>
                      Cancel
                    </Button>
                  </DialogClose>
                  <Button type="submit" disabled={creating || !email.trim()}>
                    {creating ? 'Creating…' : 'Create user'}
                  </Button>
                </div>
              </form>
            </>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}

function CreatedPanel({
  created,
  onDone,
}: {
  created: CreatedUserResponse;
  onDone: () => void;
}) {
  const [copied, setCopied] = useState(false);

  async function copy() {
    try {
      await navigator.clipboard.writeText(created.temporaryPassword);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // Clipboard may be unavailable; the password is visible for manual copying.
    }
  }

  return (
    <div className="space-y-4">
      <DialogHeader>
        <DialogTitle>User created</DialogTitle>
        <DialogDescription>
          Share these credentials with {created.email}. The temporary password is shown
          only now and cannot be retrieved again.
        </DialogDescription>
      </DialogHeader>

      <div className="space-y-3 rounded-lg border bg-muted/30 p-4">
        <div>
          <p className="text-xs font-medium text-muted-foreground">Email</p>
          <p className="text-sm text-foreground">{created.email}</p>
        </div>
        <div>
          <p className="text-xs font-medium text-muted-foreground">Role</p>
          <p className="text-sm text-foreground">{created.role}</p>
        </div>
        <div>
          <p className="text-xs font-medium text-muted-foreground">Temporary password</p>
          <div className="mt-1 flex items-center gap-2">
            <code className="flex-1 rounded bg-background border px-2 py-1.5 font-mono text-sm text-foreground break-all">
              {created.temporaryPassword}
            </code>
            <Button type="button" variant="outline" size="icon" onClick={copy}>
              {copied ? <Check size={15} /> : <Copy size={15} />}
            </Button>
          </div>
        </div>
      </div>

      <div className="flex justify-end">
        <Button type="button" onClick={onDone}>
          Done
        </Button>
      </div>
    </div>
  );
}

function UsersTable({ users }: { users: TenantUserDto[] }) {
  return (
    <div className="rounded-lg border overflow-hidden">
      <table className="w-full text-sm">
        <thead>
          <tr className="bg-muted/60 text-muted-foreground border-b">
            <th className="text-left px-4 py-3 font-medium">Email</th>
            <th className="text-left px-4 py-3 font-medium">Role</th>
          </tr>
        </thead>
        <tbody>
          {users.map((u, i) => (
            <tr
              key={u.id}
              className={[
                'border-b last:border-0 hover:bg-muted/40 transition-colors',
                i % 2 === 1 ? 'bg-muted/20' : '',
              ].join(' ')}
            >
              <td className="px-4 py-3">
                <div className="flex items-center gap-3">
                  <div className="flex-shrink-0 w-8 h-8 rounded-md bg-primary/10 flex items-center justify-center">
                    <ShieldCheck size={14} className="text-primary" />
                  </div>
                  <span className="font-medium text-foreground">{u.email}</span>
                </div>
              </td>
              <td className="px-4 py-3 text-muted-foreground">{u.role}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="px-4 py-2 border-t bg-muted/30 text-xs text-muted-foreground">
        {users.length} user{users.length !== 1 ? 's' : ''}
      </div>
    </div>
  );
}

function EmptyState({ onNew }: { onNew: () => void }) {
  return (
    <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-16 text-center">
      <div className="w-12 h-12 rounded-full bg-muted flex items-center justify-center mb-4">
        <Users size={22} className="text-muted-foreground" />
      </div>
      <p className="font-medium text-foreground">No users yet</p>
      <p className="text-sm text-muted-foreground mt-1 mb-4">
        Add the first admin for this school.
      </p>
      <Button size="sm" onClick={onNew} className="gap-2">
        <Plus size={14} />
        Add User
      </Button>
    </div>
  );
}

function UsersSkeleton() {
  return (
    <div className="rounded-lg border overflow-hidden animate-pulse">
      <div className="bg-muted/60 border-b px-4 py-3 flex gap-8">
        <div className="h-3 w-32 bg-muted-foreground/20 rounded" />
        <div className="h-3 w-16 bg-muted-foreground/20 rounded" />
      </div>
      {[...Array(3)].map((_, i) => (
        <div key={i} className="border-b last:border-0 px-4 py-3 flex items-center gap-3">
          <div className="w-8 h-8 rounded-md bg-muted" />
          <div className="h-3 w-48 bg-muted rounded" />
        </div>
      ))}
    </div>
  );
}
