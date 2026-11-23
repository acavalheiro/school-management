import { useTranslation } from 'react-i18next';
import { cn } from '@/lib/utils';

type KnownStatus = 'active' | 'inactive' | 'graduated' | 'suspended';

// Mirrors Domain.Enums.StudentStatus, which the API serialises as its numeric value.
const STATUS_BY_VALUE: Record<number, KnownStatus> = {
  1: 'active',
  2: 'inactive',
  3: 'graduated',
  4: 'suspended',
};

const KNOWN_NAMES = Object.values(STATUS_BY_VALUE) as readonly string[];

function toKnownStatus(status: number | string): KnownStatus | null {
  if (typeof status === 'number') return STATUS_BY_VALUE[status] ?? null;
  const name = status.toLowerCase();
  return KNOWN_NAMES.includes(name) ? (name as KnownStatus) : null;
}

/** A student's status, by numeric value or name. Anything unrecognised is shown as-is. */
export function StatusBadge({ status, className }: { status: number | string; className?: string }) {
  const { t } = useTranslation();
  const known = toKnownStatus(status);

  return (
    <span
      className={cn(
        'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
        known === 'active' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500',
        className,
      )}
    >
      {known ? t(`students.status.${known}`) : String(status)}
    </span>
  );
}
