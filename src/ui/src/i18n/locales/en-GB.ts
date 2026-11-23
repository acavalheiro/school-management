// Source of truth for every UI string. Add new keys here first, then to pt-PT.ts:
// pt-PT is typed against this object, so a missing or extra key fails `tsc`.
export const enGB = {
  app: {
    name: 'ATL School',
    tagline: 'Student Management Portal',
  },
  nav: {
    dashboard: 'Dashboard',
    students: 'Students',
    tenants: 'Tenants',
  },
  theme: {
    light: 'Light mode',
    dark: 'Dark mode',
  },
  auth: {
    signOut: 'Sign out',
    signIn: 'Sign in',
    signingIn: 'Signing in…',
    signInDescription: 'Enter your credentials to access your account',
    email: 'Email',
    emailPlaceholder: 'you@example.com',
    password: 'Password',
    invalidCredentials: 'Invalid email or password.',
  },
  language: {
    label: 'Language',
    saveFailed: "Couldn't save your language to your account. It will apply on this device only.",
  },
} as const;
