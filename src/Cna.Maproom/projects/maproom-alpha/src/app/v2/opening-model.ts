import { JsonValue, OpeningView } from './types';

/**
 * Turns any opening's visible cut into a renderable description without knowing its kind.
 * The Host's cuts differ per stage (`choices: string[]`, `movementOffers: object[]`, ...), but the
 * action refs are positionally parallel to the choice-bearing array, so we find that array by
 * shape instead of by stage name. If nothing aligns, choices fall back to "Option N" and every
 * fact in the cut is still shown, so no opening kind can render empty or silently lose a choice.
 */
export interface Fact {
  readonly label: string;
  /** Scalar value, already formatted. Absent when the fact has children or items. */
  readonly value?: string;
  readonly children?: readonly Fact[];
  readonly items?: readonly string[];
}

export interface ChoiceView {
  readonly ref: string;
  readonly label: string;
  readonly details: readonly Fact[];
}

export interface OpeningDescription {
  readonly facts: readonly Fact[];
  readonly choices: readonly ChoiceView[];
  /** True when each ref was matched to a described entry of the cut. */
  readonly aligned: boolean;
  readonly truncated: boolean;
}

const HIDDEN_KEYS = new Set(['contractVersion', 'campaignId']);
const PREFERRED_CHOICE_KEYS = ['choices', 'options', 'offers'];
const LABEL_KEYS = ['label', 'title', 'name', 'description', 'summary', 'action', 'kind'];
const MAX_DEPTH = 6;
const MAX_NODES = 1500;

export function humanize(key: string): string {
  const spaced = key
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/[_.-]+/g, ' ')
    .trim()
    .toLowerCase();
  return spaced.charAt(0).toUpperCase() + spaced.slice(1);
}

function scalarText(value: JsonValue): string {
  if (value === null) return 'None';
  if (typeof value === 'boolean') return value ? 'Yes' : 'No';
  return String(value);
}

const isScalar = (v: JsonValue): v is null | boolean | number | string =>
  v === null || typeof v !== 'object';
const isRecord = (v: JsonValue): v is { [key: string]: JsonValue } =>
  typeof v === 'object' && v !== null && !Array.isArray(v);

class Budget {
  nodes = 0;
  truncated = false;
}

function factsOf(
  record: { [key: string]: JsonValue },
  depth: number,
  budget: Budget,
  skip?: string,
): Fact[] {
  const entries = Object.entries(record).filter(([k]) => k !== skip && !HIDDEN_KEYS.has(k));
  const keys = new Set(entries.map(([k]) => k));
  const facts: Fact[] = [];
  for (const [key, value] of entries) {
    // Fold FooNumerator/FooDenominator into one readable fraction.
    if (key.endsWith('Denominator') && keys.has(key.slice(0, -'Denominator'.length) + 'Numerator'))
      continue;
    if (key.endsWith('Numerator')) {
      const base = key.slice(0, -'Numerator'.length);
      const den = record[base + 'Denominator'];
      if (den !== undefined && isScalar(value) && isScalar(den)) {
        facts.push({ label: humanize(base), value: `${scalarText(value)}/${scalarText(den)}` });
        continue;
      }
    }
    const fact = factOf(humanize(key), value, depth, budget);
    if (fact) facts.push(fact);
  }
  return facts;
}

function factOf(label: string, value: JsonValue, depth: number, budget: Budget): Fact | undefined {
  if (++budget.nodes > MAX_NODES || depth > MAX_DEPTH) {
    budget.truncated = true;
    return undefined;
  }
  if (isScalar(value)) return { label, value: scalarText(value) };
  if (Array.isArray(value)) {
    if (value.length === 0) return { label, value: 'None' };
    if (value.every(isScalar)) return { label, items: value.map(scalarText) };
    const children = value
      .map((entry, i) => factOf(`${i + 1}`, entry, depth + 1, budget))
      .filter((f): f is Fact => f !== undefined);
    return { label, children };
  }
  return { label, children: factsOf(value, depth + 1, budget) };
}

function findChoiceKey(cut: { [key: string]: JsonValue }, refCount: number): string | undefined {
  const matches = (k: string) => {
    const v = cut[k];
    return Array.isArray(v) && v.length === refCount;
  };
  for (const key of PREFERRED_CHOICE_KEYS) if (matches(key)) return key;
  const suffixed = Object.keys(cut).filter(
    (k) => /(Offers|Choices|Options)$/.test(k) && matches(k),
  );
  if (suffixed.length === 1) return suffixed[0];
  // Last resort: exactly one top-level array of the right length is unambiguous.
  const arrays = Object.keys(cut).filter(matches);
  return arrays.length === 1 ? arrays[0] : undefined;
}

function choiceLabel(entry: JsonValue, details: readonly Fact[], index: number): string {
  if (isScalar(entry)) return scalarText(entry);
  if (isRecord(entry)) {
    for (const key of LABEL_KEYS) {
      const v = entry[key];
      if (typeof v === 'string' && v.length > 0) return v;
    }
    const parts = details
      .filter((f) => f.value !== undefined)
      .slice(0, 4)
      .map((f) => `${f.label} ${f.value}`);
    if (parts.length > 0) return parts.join(' · ');
  }
  return `Option ${index + 1}`;
}

export function describeOpening(opening: OpeningView): OpeningDescription {
  const budget = new Budget();
  const cut = opening.visibleCut;
  const refs = opening.publicActionRefs;
  const record = isRecord(cut) ? cut : undefined;
  const choiceKey = record && refs.length > 0 ? findChoiceKey(record, refs.length) : undefined;
  const entries = choiceKey ? (record![choiceKey] as JsonValue[]) : undefined;

  const choices: ChoiceView[] = refs.map((ref, i) => {
    const entry = entries?.[i];
    if (entry === undefined) return { ref, label: `Option ${i + 1}`, details: [] };
    const details = isRecord(entry) ? factsOf(entry, 1, budget) : [];
    return { ref, label: choiceLabel(entry, details, i), details };
  });

  let facts: Fact[];
  if (record) facts = factsOf(record, 0, budget, choiceKey);
  else {
    const fact = factOf('Visible state', cut, 0, budget);
    facts = fact ? [fact] : [];
  }
  return {
    facts,
    choices,
    aligned: entries !== undefined || refs.length === 0,
    truncated: budget.truncated,
  };
}
