import { TestBed } from '@angular/core/testing';
import { AtlasUnitCounter } from './unit-counter';
describe('AtlasUnitCounter', () => {
  it('exposes identity and selection, emits once, and suppresses disabled clicks', async () => {
    const fixture = TestBed.createComponent(AtlasUnitCounter);
    fixture.componentRef.setInput('code', 'FOX');
    fixture.componentRef.setInput('label', 'Fox group, infantry, K-7');
    fixture.componentRef.setInput('selected', true);
    const selected = vi.fn();
    fixture.componentInstance.chosen.subscribe(selected);
    await fixture.whenStable();
    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('aria-label')).toBe('Fox group, infantry, K-7');
    expect(button.getAttribute('aria-pressed')).toBe('true');
    button.click();
    expect(selected).toHaveBeenCalledTimes(1);
    fixture.componentRef.setInput('disabled', true);
    await fixture.whenStable();
    button.click();
    expect(selected).toHaveBeenCalledTimes(1);
  });
});
