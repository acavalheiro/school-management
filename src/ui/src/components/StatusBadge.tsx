import { useTranslation } from 'react-i18next';
import { cn } from '@/lib/utils';

const KNOWN_STATUSES = ['active', 'inactive', 'graduated', 'suspended'] as const;
type KnownStatus = (typeof KNOWN_STATUSES)[number];

function isKnownStatus(value: string): value is KnownStatus {
  return (KNOWN_STATUSES as readonly string[]).includes(value);
}

/** A student's status. Values match the API's StudentStatus enum; anything unrecognised is shown as-is. */
export function StatusBadge({ status, className }: { status: string; className?: string }) {
  const { t } = useTranslation();
  const key = status.toLowerCase();
  const isActive = key === 'active';

  return (
    <span
      className={cn(
        'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
        isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500',
        className,
      )}
    >
      {isKnownStatus(key) ? t(`students.status.${key}`) : status}
    </span>
  );
}
