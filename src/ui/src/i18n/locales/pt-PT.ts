import type { enGB } from './en-GB';
import type { Messages } from './types';

// European Portuguese. Use pt-PT vocabulary (ecrã, palavra-passe, utilizador, Terminar sessão),
// never Brazilian forms. Tenant → Escola, Student → Aluno.
export const ptPT: Messages<typeof enGB> = {
  app: {
    name: 'ATL School',
    tagline: 'Portal de Gestão de Alunos',
  },
  nav: {
    dashboard: 'Painel',
    students: 'Alunos',
    tenants: 'Escolas',
  },
  theme: {
    light: 'Modo claro',
    dark: 'Modo escuro',
  },
  auth: {
    signOut: 'Terminar sessão',
    signIn: 'Iniciar sessão',
    signingIn: 'A iniciar sessão…',
    signInDescription: 'Introduza as suas credenciais para aceder à sua conta',
    email: 'Email',
    emailPlaceholder: 'nome@exemplo.pt',
    password: 'Palavra-passe',
    invalidCredentials: 'Email ou palavra-passe inválidos.',
  },
  language: {
    label: 'Idioma',
    saveFailed: 'Não foi possível guardar o idioma na sua conta. Será aplicado apenas neste dispositivo.',
  },
};
