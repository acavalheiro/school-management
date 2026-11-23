import { useEffect, useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Building2, Plus, ChevronRight } from 'lucide-react';
import { tenantsApi, type TenantDto } from '../api/tenantsApi';
import { useTranslation } from 'react-i18next';
import { errorMessage } from '@/i18n/errors';
import { useLanguage } from '@/i18n/useLanguage';
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
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [tenants, setTenants] = useState<TenantDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [newName, setNewName] = useState('');
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<unknown>(null);

  function loadTenants() {
    setLoading(true);
    setError(null);
    tenantsApi
      .list()
      .then(setTenants)
      .catch((e: unknown) => setError(e))
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
      setCreateError(err);
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
          <h1 className="text-2xl font-semibold text-foreground">{t('tenants.title')}</h1>
          <p className="text-sm text-muted-foreground mt-1">
            {t('tenants.subtitle')}
          </p>
        </div>
        <Button onClick={() => setDialogOpen(true)} className="gap-2">
          <Plus size={16} />
          {t('tenants.new')}
        </Button>
      </div>

      {/* Content */}
      {loading ? (
        <TenantsSkeleton />
      ) : error != null ? (
        <div className="rounded-lg border border-destructive/40 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {errorMessage(error, t)}
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
            <DialogTitle>{t('tenants.new')}</DialogTitle>
            <DialogDescription>
              {t('tenants.dialog.description')}
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleCreate} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="tenant-name">{t('tenants.dialog.name')}</Label>
              <Input
                id="tenant-name"
                placeholder={t('tenants.dialog.namePlaceholder')}
                value={newName}
                onChange={(e) => setNewName(e.target.value)}
                autoFocus
                required
              />
            </div>

            {createError != null && (
              <p className="text-sm text-destructive">{errorMessage(createError, t)}</p>
            )}

            <div className="flex justify-end gap-2 pt-1">
              <DialogClose asChild>
                <Button type="button" variant="outline" disabled={creating}>
                  {t('common.cancel')}
                </Button>
              </DialogClose>
              <Button type="submit" disabled={creating || !newName.trim()}>
                {creating ? t('tenants.dialog.creating') : t('tenants.dialog.submit')}
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
  const { t } = useTranslation();
  const { formatDate } = useLanguage();
  return (
    <div className="rounded-lg border overflow-hidden">
      <table className="w-full text-sm">
        <thead>
          <tr className="bg-muted/60 text-muted-foreground border-b">
            <th className="text-left px-4 py-3 font-medium">{t('tenants.columns.name')}</th>
            <th className="text-left px-4 py-3 font-medium hidden sm:table-cell">{t('tenants.columns.id')}</th>
            <th className="text-left px-4 py-3 font-medium">{t('tenants.columns.created')}</th>
            <th className="px-4 py-3" aria-hidden />
          </tr>
        </thead>
        <tbody>
          {tenants.map((tenant, i) => (
            <tr
              key={tenant.id}
              onClick={() => onOpen(tenant.id)}
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
                  <span className="font-medium text-foreground">{tenant.name}</span>
                </div>
              </td>
              <td className="px-4 py-3 hidden sm:table-cell">
                <span className="font-mono text-xs text-muted-foreground bg-muted px-2 py-0.5 rounded">
                  {tenant.id}
                </span>
              </td>
              <td className="px-4 py-3 text-muted-foreground">
                {formatDate(tenant.createdAt, { dateStyle: 'medium' })}
              </td>
              <td className="px-4 py-3 text-right text-muted-foreground">
                <ChevronRight size={16} className="inline-block" />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="px-4 py-2 border-t bg-muted/30 text-xs text-muted-foreground">
        {t('tenants.count', { count: tenants.length })}
      </div>
    </div>
  );
}

function EmptyState({ onNew }: { onNew: () => void }) {
  const { t } = useTranslation();
  return (
    <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-16 text-center">
      <div className="w-12 h-12 rounded-full bg-muted flex items-center justify-center mb-4">
        <Building2 size={22} className="text-muted-foreground" />
      </div>
      <p className="font-medium text-foreground">{t('tenants.empty')}</p>
      <p className="text-sm text-muted-foreground mt-1 mb-4">
        {t('tenants.emptyHint')}
      </p>
      <Button size="sm" onClick={onNew} className="gap-2">
        <Plus size={14} />
        {t('tenants.new')}
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
