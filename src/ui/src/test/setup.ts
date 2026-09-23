import '@testing-library/jest-dom/vitest';
import { afterEach } from 'vitest';
import { cleanup } from '@testing-library/react';

afterEach(() => {
  cleanup();
});

// Node's own built-in `localStorage` global (stable since Node 22, backed by
// --localstorage-file) shadows jsdom's Storage implementation entirely in this
// process — window === globalThis in Vitest's jsdom pool, and window.localStorage
// turned out to be the *same* broken, file-less stub (no setItem/getItem/clear at
// all), not a working jsdom fallback. Replace it with a real in-memory Storage so
// app code using the bare `localStorage` global (tokenStore in authApi.ts) works.
class MemoryStorage implements Storage {
  private store = new Map<string, string>();
  get length() {
    return this.store.size;
  }
  clear() {
    this.store.clear();
  }
  getItem(key: string) {
    return this.store.has(key) ? this.store.get(key)! : null;
  }
  key(index: number) {
    return Array.from(this.store.keys())[index] ?? null;
  }
  removeItem(key: string) {
    this.store.delete(key);
  }
  setItem(key: string, value: string) {
    this.store.set(key, String(value));
  }
}

Object.defineProperty(globalThis, 'localStorage', {
  configurable: true,
  value: new MemoryStorage(),
});

afterEach(() => {
  localStorage.clear();
});

// theme-provider.tsx calls window.matchMedia unconditionally; jsdom doesn't implement it.
window.matchMedia ??= (query: string) =>
  ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }) as unknown as MediaQueryList;

// Radix (Popover/Dialog) and Base UI (Combobox) measure elements for positioning;
// jsdom has no layout engine, so ResizeObserver doesn't exist at all.
class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}
globalThis.ResizeObserver ??= ResizeObserverStub as unknown as typeof ResizeObserver;

// Radix's pointer-based interactions (Dialog, Popover) call these; jsdom doesn't implement them.
if (!Element.prototype.hasPointerCapture) {
  Element.prototype.hasPointerCapture = () => false;
}
if (!Element.prototype.releasePointerCapture) {
  Element.prototype.releasePointerCapture = () => {};
}
if (!Element.prototype.scrollIntoView) {
  Element.prototype.scrollIntoView = () => {};
}
