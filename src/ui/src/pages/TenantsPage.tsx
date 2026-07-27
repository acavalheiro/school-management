import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Building2, Plus, ChevronRight } from 'lucide-react';
import { tenantsApi, type TenantDto } from '../api/tenantsApi';
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

export default function TenantsPage() {
  const navigate = useNavigate();
  const [tenants, setTenants] = useState<TenantDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [newName, setNewName] = useState('');
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  function loadTenants() {
    setLoading(true);
    setError(null);
    tenantsApi
      .list()
      .then(setTenants)
      .catch((e: unknown) => setError(e instanceof Error ? e.message : 'Failed to load tenants'))
      .finally(() => setLoading(false));
  }

  useEffect(loadTenants, []);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setCreateError(null);
    setCreating(true);
    try {
      await tenantsApi.create(newName.trim());
      setDialogOpen(false);
      setNewName('');
      loadTenants();
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : 'Failed to create tenant');
    } finally {
      setCreating(false);
    }
  }

  function handleOpenChange(open: boolean) {
    setDialogOpen(open);
    if (!open) {
      setNewName('');
      setCreateError(null);
    }
  }

  return (
    <div className="space-y-6">
      {/* Page header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-foreground">Tenants</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Manage all schools registered in the platform
          </p>
        </div>
        <Button onClick={() => setDialogOpen(true)} className="gap-2">
          <Plus size={16} />
          New Tenant
        </Button>
      </div>

      {/* Content */}
      {loading ? (
        <TenantsSkeleton />
      ) : error ? (
        <div className="rounded-lg border border-destructive/40 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      ) : tenants.length === 0 ? (
        <EmptyState onNew={() => setDialogOpen(true)} />
      ) : (
        <TenantsTable tenants={tenants} onOpen={(id) => navigate(`/tenants/${id}`)} />
      )}

      {/* Create dialog */}
      <Dialog open={dialogOpen} onOpenChange={handleOpenChange}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>New Tenant</DialogTitle>
            <DialogDescription>
              Create a standalone school tenant. You can assign users to it afterwards.
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleCreate} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="tenant-name">School name</Label>
              <Input
                id="tenant-name"
                placeholder="e.g. Greenfield Academy"
                value={newName}
                onChange={(e) => setNewName(e.target.value)}
                autoFocus
                required
              />
            </div>

            {createError && (
              <p className="text-sm text-destructive">{createError}</p>
            )}

            <div className="flex justify-end gap-2 pt-1">
              <DialogClose asChild>
                <Button type="button" variant="outline" disabled={creating}>
                  Cancel
                </Button>
              </DialogClose>
              <Button type="submit" disabled={creating || !newName.trim()}>
                {creating ? 'Creating…' : 'Create tenant'}
              </Button>
            </div>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function TenantsTable({
  tenants,
  onOpen,
}: {
  tenants: TenantDto[];
  onOpen: (id: string) => void;
}) {
  return (
    <div className="rounded-lg border overflow-hidden">
      <table className="w-full text-sm">
        <thead>
          <tr className="bg-muted/60 text-muted-foreground border-b">
            <th className="text-left px-4 py-3 font-medium">Name</th>
            <th className="text-left px-4 py-3 font-medium hidden sm:table-cell">Tenant ID</th>
            <th className="text-left px-4 py-3 font-medium">Created</th>
            <th className="px-4 py-3" aria-hidden />
          </tr>
        </thead>
        <tbody>
          {tenants.map((t, i) => (
            <tr
              key={t.id}
              onClick={() => onOpen(t.id)}
              className={[
                'border-b last:border-0 hover:bg-muted/40 transition-colors cursor-pointer',
                i % 2 === 1 ? 'bg-muted/20' : '',
              ].join(' ')}
            >
              <td className="px-4 py-3">
                <div className="flex items-center gap-3">
                  <div className="flex-shrink-0 w-8 h-8 rounded-md bg-primary/10 flex items-center justify-center">
                    <Building2 size={14} className="text-primary" />
                  </div>
                  <span className="font-medium text-foreground">{t.name}</span>
                </div>
              </td>
              <td className="px-4 py-3 hidden sm:table-cell">
                <span className="font-mono text-xs text-muted-foreground bg-muted px-2 py-0.5 rounded">
                  {t.id}
                </span>
              </td>
              <td className="px-4 py-3 text-muted-foreground">
                {new Date(t.createdAt).toLocaleDateString(undefined, {
                  year: 'numeric',
                  month: 'short',
                  day: 'numeric',
                })}
              </td>
              <td className="px-4 py-3 text-right text-muted-foreground">
                <ChevronRight size={16} className="inline-block" />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="px-4 py-2 border-t bg-muted/30 text-xs text-muted-foreground">
        {tenants.length} tenant{tenants.length !== 1 ? 's' : ''}
      </div>
    </div>
  );
}

function EmptyState({ onNew }: { onNew: () => void }) {
  return (
    <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-16 text-center">
      <div className="w-12 h-12 rounded-full bg-muted flex items-center justify-center mb-4">
        <Building2 size={22} className="text-muted-foreground" />
      </div>
      <p className="font-medium text-foreground">No tenants yet</p>
      <p className="text-sm text-muted-foreground mt-1 mb-4">
        Get started by creating the first school tenant.
      </p>
      <Button size="sm" onClick={onNew} className="gap-2">
        <Plus size={14} />
        New Tenant
      </Button>
    </div>
  );
}

function TenantsSkeleton() {
  return (
    <div className="rounded-lg border overflow-hidden animate-pulse">
      <div className="bg-muted/60 border-b px-4 py-3 flex gap-8">
        <div className="h-3 w-16 bg-muted-foreground/20 rounded" />
        <div className="h-3 w-32 bg-muted-foreground/20 rounded hidden sm:block" />
        <div className="h-3 w-20 bg-muted-foreground/20 rounded" />
      </div>
      {[...Array(4)].map((_, i) => (
        <div key={i} className="border-b last:border-0 px-4 py-3 flex items-center gap-3">
          <div className="w-8 h-8 rounded-md bg-muted" />
          <div className="h-3 w-40 bg-muted rounded" />
        </div>
      ))}
    </div>
  );
}
