import { TestBed } from '@angular/core/testing';
import { AtlasIconButton } from './icon';

describe('AtlasIconButton', () => {
  it('names icon-only action and exposes its pressed state', async () => {
    const fixture = TestBed.createComponent(AtlasIconButton);
    fixture.componentRef.setInput('label', 'Fit map');
    fixture.componentRef.setInput('pressed', true);
    const activated = vi.fn();
    fixture.componentInstance.activated.subscribe(activated);
    await fixture.whenStable();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('aria-label')).toBe('Fit map');
    expect(button.getAttribute('title')).toBe('Fit map');
    expect(button.getAttribute('aria-pressed')).toBe('true');
    expect(button.querySelector('span')?.getAttribute('aria-hidden')).toBe('true');
    button.click();
    expect(activated).toHaveBeenCalledTimes(1);

    fixture.componentRef.setInput('disabled', true);
    await fixture.whenStable();
    button.click();
    expect(activated).toHaveBeenCalledTimes(1);
  });

  it('omits toggle semantics for ordinary actions', async () => {
    const fixture = TestBed.createComponent(AtlasIconButton);
    fixture.componentRef.setInput('label', 'Zoom in');
    await fixture.whenStable();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.hasAttribute('aria-pressed')).toBe(false);
  });
});
