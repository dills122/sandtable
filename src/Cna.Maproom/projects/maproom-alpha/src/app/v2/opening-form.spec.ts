import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { OpeningChoice, OpeningForm } from './opening-form';
import { ActionOutcome, OpeningView } from './types';

const base: OpeningView = {
  campaignId: 'c1',
  audience: 'axis',
  stage: 'initiative-order',
  actingElementId: 'initiative-order',
  revision: 1,
  visibleCut: { contractVersion: 2, positionId: 'p1', choices: ['act-first', 'act-last'] },
  publicActionRefs: ['r1', 'r2'],
};

@Component({
  imports: [OpeningForm],
  template: `<mr-opening-form
    [opening]="opening()"
    heading="Initiative order"
    [busy]="busy()"
    [outcome]="outcome()"
    (chosen)="picks.push($event)"
  />`,
})
class Host {
  readonly opening = signal(base);
  readonly busy = signal(false);
  readonly outcome = signal<ActionOutcome | null>(null);
  readonly picks: OpeningChoice[] = [];
}

function mount() {
  const fixture = TestBed.createComponent(Host);
  fixture.detectChanges();
  const el = fixture.nativeElement as HTMLElement;
  return { fixture, host: fixture.componentInstance, el };
}
const radios = (el: HTMLElement) =>
  Array.from(el.querySelectorAll<HTMLInputElement>('input[type=radio]'));
const submit = (el: HTMLElement) => el.querySelector<HTMLButtonElement>('button[type=submit]')!;

describe('OpeningForm', () => {
  it('renders one labelled radio per ref inside a fieldset with a legend', () => {
    const { el } = mount();
    expect(el.querySelector('fieldset legend')?.textContent).toContain('Legal choices');
    const inputs = radios(el);
    expect(inputs).toHaveLength(2);
    expect(
      inputs.map((i) => el.querySelector(`label[for="${i.id}"]`)?.textContent?.trim()),
    ).toEqual(['act-first', 'act-last']);
    expect(el.textContent).toContain('Position id');
    expect(el.textContent).not.toContain('r1');
  });

  it('keeps submit disabled until a choice is made, then emits that ref only', async () => {
    const { fixture, host, el } = mount();
    expect(submit(el).disabled).toBe(true);
    radios(el)[1].click();
    fixture.detectChanges();
    expect(submit(el).disabled).toBe(false);
    submit(el).click();
    expect(host.picks).toHaveLength(1);
    expect(host.picks[0].publicActionRef).toBe('r2');
    expect(host.picks[0].opening).toBe(base);
  });

  it('drops a selection when the opening is refreshed', () => {
    const { fixture, host, el } = mount();
    radios(el)[0].click();
    fixture.detectChanges();
    host.opening.set({ ...base, revision: 2, publicActionRefs: ['n1', 'n2'] });
    fixture.detectChanges();
    expect(radios(el).some((r) => r.checked)).toBe(false);
    expect(submit(el).disabled).toBe(true);
  });

  it('locks the form while busy and after acceptance', () => {
    const { fixture, host, el } = mount();
    radios(el)[0].click();
    host.busy.set(true);
    fixture.detectChanges();
    expect(submit(el).disabled).toBe(true);
    host.busy.set(false);
    host.outcome.set({ kind: 'accepted', openingRevision: 1, publicActionRef: 'r1' });
    fixture.detectChanges();
    expect(el.querySelector('[role=status]')?.textContent).toContain('Choice accepted');
    expect(submit(el).disabled).toBe(true);
    expect(el.querySelector('fieldset')?.disabled).toBe(true);
  });

  it('explains rejections in the live region', () => {
    const { fixture, host, el } = mount();
    host.outcome.set({ kind: 'rejected', code: 'rule-not-implemented', requestId: undefined });
    fixture.detectChanges();
    expect(el.querySelector('[role=status]')?.textContent).toContain('not built yet');
    host.outcome.set({ kind: 'rejected', code: 'unavailable', requestId: undefined });
    fixture.detectChanges();
    expect(el.querySelector('[role=status]')?.textContent).toContain('no longer current');
  });

  it('renders an unfamiliar kind with object offers and no bespoke screen', () => {
    const { fixture, host, el } = mount();
    host.opening.set({
      ...base,
      stage: 'movement',
      visibleCut: {
        movementOffers: [
          {
            elementId: 'u1',
            originLocationId: 'A',
            destinationLocationId: 'B',
            totalCostNumerator: 3,
            totalCostDenominator: 2,
          },
        ],
        weather: { sandstorm: false },
      },
      publicActionRefs: ['m1'],
    });
    fixture.detectChanges();
    expect(radios(el)).toHaveLength(1);
    expect(el.textContent).toContain('Destination location id B');
    expect(el.textContent).toContain('Sandstorm');
  });

  it('shows an explicit empty state with no choices', () => {
    const { fixture, host, el } = mount();
    host.opening.set({ ...base, visibleCut: { choices: [] }, publicActionRefs: [] });
    fixture.detectChanges();
    expect(el.textContent).toContain('No legal choice is open');
    expect(radios(el)).toHaveLength(0);
  });
});
