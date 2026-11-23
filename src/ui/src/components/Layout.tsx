import { NavLink, Outlet } from 'react-router-dom';
import { LayoutDashboard, Users, Building2, LogOut, Sun, Moon, type LucideIcon } from 'lucide-react';
import { useAuth } from '../auth/AuthContext';
import { useTheme } from './theme-provider';
import { Button } from '@/components/ui/button';
import { useTranslation } from 'react-i18next';
import { LanguageSwitcher } from './LanguageSwitcher';
import type { enGB } from '@/i18n/locales/en-GB';

interface NavItem {
  to: string;
  labelKey: `nav.${keyof typeof enGB.nav}`;
  icon: LucideIcon;
  roles: string[];
}

const NAV_ITEMS: NavItem[] = [
  { to: '/dashboard', labelKey: 'nav.dashboard', icon: LayoutDashboard, roles: ['Admin', 'User', 'SuperAdmin'] },
  { to: '/students', labelKey: 'nav.students', icon: Users, roles: ['Admin', 'User', 'SuperAdmin'] },
  { to: '/tenants', labelKey: 'nav.tenants', icon: Building2, roles: ['SuperAdmin'] },
];

export default function Layout() {
  const { t } = useTranslation();
  const { user, logout } = useAuth();
  const { theme, setTheme } = useTheme();
  const isDark = theme === 'dark';

  const visibleItems = NAV_ITEMS.filter(
    (item) => user && item.roles.includes(user.role),
  );

  return (
    <div className="flex min-h-screen bg-background">
      {/* Sidebar */}
      <aside className="w-56 border-r flex flex-col">
        <div className="px-5 py-4 border-b">
          <span className="font-semibold text-foreground text-lg">{t('app.name')}</span>
        </div>

        <nav className="flex-1 py-4 px-3 flex flex-col gap-1">
          {visibleItems.map(({ to, labelKey, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                [
                  'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-primary text-primary-foreground'
                    : 'text-muted-foreground hover:bg-muted hover:text-foreground',
                ].join(' ')
              }
            >
              <Icon size={16} />
              {t(labelKey)}
            </NavLink>
          ))}
        </nav>

        <div className="px-3 py-4 border-t space-y-1">
          <div className="px-3 py-1 text-xs text-muted-foreground truncate">
            {user?.email}
          </div>
          <LanguageSwitcher className="px-3 py-1" />
          <Button
            variant="ghost"
            size="sm"
            className="w-full justify-start gap-2"
            onClick={() => setTheme(isDark ? 'light' : 'dark')}
          >
            {isDark ? <Sun size={16} /> : <Moon size={16} />}
            {isDark ? t('theme.light') : t('theme.dark')}
          </Button>
          <Button variant="ghost" size="sm" className="w-full justify-start gap-2" onClick={logout}>
            <LogOut size={16} />
            {t('auth.signOut')}
          </Button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 p-6 overflow-auto">
        <Outlet />
      </main>
    </div>
  );
}
