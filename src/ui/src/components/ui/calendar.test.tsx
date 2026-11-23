import { describe, expect, it } from 'vitest';
import { render } from '@testing-library/react';
import { Calendar } from './calendar';
import i18n from '../../i18n';

function firstWeekday(container: HTMLElement) {
  // DayPicker hides the weekday row from the a11y tree (aria-hidden thead) and puts the
  // full day name in each header cell's aria-label.
  return container.querySelector('.rdp-weekday')?.getAttribute('aria-label');
}

// Both the UK and Portugal start the week on Monday (ISO 8601).
describe('Calendar week start', () => {
  it('starts the week on Monday in en-GB', () => {
    const { container } = render(<Calendar mode="single" defaultMonth={new Date(2026, 10)} />);
    expect(firstWeekday(container)).toMatch(/monday/i);
  });

  it('starts the week on Monday in pt-PT', async () => {
    await i18n.changeLanguage('pt-PT');
    const { container } = render(<Calendar mode="single" defaultMonth={new Date(2026, 10)} />);
    expect(firstWeekday(container)).toMatch(/segunda-feira/i);
  });
});
