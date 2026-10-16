import { TestBed } from '@angular/core/testing';
import { DraftForm, readDestination } from './draft-form';
import { provideMaproomForms } from './forms';
describe('Draft form compatibility', () => {
  it('starts disabled and enables through the Formly field configuration', async () => {
    TestBed.configureTestingModule({ providers: [provideMaproomForms()] });
    const fixture = TestBed.createComponent(DraftForm);
    fixture.componentRef.setInput('disabled', true);
    await fixture.whenStable();
    expect(fixture.componentInstance.form.controls.destination.disabled).toBe(true);
    expect(fixture.nativeElement.querySelector('mat-select').getAttribute('aria-disabled')).toBe(
      'true',
    );
    fixture.componentRef.setInput('disabled', false);
    await fixture.whenStable();
    expect(fixture.componentInstance.form.controls.destination.enabled).toBe(true);
  });
  it('validates, reviews, clears review after selection change and blocks disabled submission', async () => {
    TestBed.configureTestingModule({ providers: [provideMaproomForms()] });
    const fixture = TestBed.createComponent(DraftForm);
    await fixture.whenStable();
    const form = fixture.componentInstance;
    form.review();
    expect(form.reviewed()).toBeNull();
    expect(form.form.invalid).toBe(true);
    form.form.get('destination')!.setValue('M-6');
    await fixture.whenStable();
    form.review();
    expect(form.reviewed()).toBe('M-6');
    fixture.componentRef.setInput('formation', 'Harrier group');
    await fixture.whenStable();
    expect(form.reviewed()).toBeNull();
    fixture.componentRef.setInput('disabled', true);
    await fixture.whenStable();
    expect(form.form.disabled).toBe(true);
    form.review();
    expect(form.reviewed()).toBeNull();
  });
  it('rejects unknown and non-string dynamic values at the typed adapter', () => {
    for (const value of ['', null, undefined, {}, 42, 'hidden-position'])
      expect(readDestination(value)).toBeNull();
    expect(readDestination('J-5')).toBe('J-5');
  });
});
