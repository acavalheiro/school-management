import { describe, expect, it } from 'vitest';
import { enGB } from './locales/en-GB';
import { ptPT } from './locales/pt-PT';

type Tree = { [key: string]: string | Tree };

function leaves(tree: Tree, prefix = ''): [string, string][] {
  return Object.entries(tree).flatMap(([key, value]) =>
    typeof value === 'string' ? [[`${prefix}${key}`, value]] : leaves(value, `${prefix}${key}.`),
  );
}

// Key parity is enforced by the type checker; these catch what types can't.
describe('translation catalogues', () => {
  it('has no empty pt-PT strings', () => {
    const empty = leaves(ptPT as Tree).filter(([, value]) => value.trim() === '');
    expect(empty).toEqual([]);
  });

  it('keeps the same interpolation placeholders in both languages', () => {
    const placeholders = (s: string) => (s.match(/{{\s*\w+\s*}}/g) ?? []).sort();
    const pt = Object.fromEntries(leaves(ptPT as Tree));
    const mismatched = leaves(enGB as Tree).filter(
      ([key, value]) => placeholders(value).join() !== placeholders(pt[key] ?? '').join(),
    );
    expect(mismatched).toEqual([]);
  });
});
