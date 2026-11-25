import {
  Audience,
  ActionOutcome,
  CampaignV2Error,
  JsonValue,
  OpeningKindsView,
  OpeningView,
  SeatCredential,
  SeatGrant,
} from './types';

export const CONTRACT_VERSION = 2;
/** Generous bound for one visible cut; the Host caps request bodies at 8 KiB but not responses. */
export const MAX_VISIBLE_CUT_BYTES = 1024 * 1024;
const MAX_REFS = 512;

type Obj = Record<string, unknown>;

const fail = (what: string): never => {
  throw new CampaignV2Error(`Unsupported v2 response: ${what}`);
};

function obj(value: unknown, what: string): Obj {
  if (typeof value !== 'object' || value === null || Array.isArray(value)) return fail(what);
  return value as Obj;
}
function str(o: Obj, key: string): string {
  const v = o[key];
  return typeof v === 'string' && v.length > 0 && v.length <= 4096 ? v : fail(key);
}
function int(o: Obj, key: string): number {
  const v = o[key];
  return typeof v === 'number' && Number.isSafeInteger(v) ? v : fail(key);
}
function audience(o: Obj, key: string): Audience {
  const v = o[key];
  return v === 'axis' || v === 'commonwealth' ? v : fail(key);
}
function version(o: Obj): void {
  if (o['contractVersion'] !== CONTRACT_VERSION) fail('contractVersion');
}

function seatShape(raw: unknown, what: string): SeatCredential {
  const o = obj(raw, what);
  return {
    campaignId: str(o, 'campaignId'),
    audience: audience(o, 'audience'),
    epoch: int(o, 'epoch'),
    secret: str(o, 'secret'),
    expiresUnixMs: int(o, 'expiresUnixMs'),
  };
}

/** Parses a pasted seat credential, the shape the simulator and `/seats` both print. */
export function parseSeatCredentialJson(text: string): SeatCredential {
  let raw: unknown;
  try {
    raw = JSON.parse(text);
  } catch {
    throw new CampaignV2Error('That is not valid credential JSON.');
  }
  const o =
    typeof raw === 'object' && raw !== null && 'credential' in raw
      ? (raw as Obj)['credential']
      : raw;
  try {
    return seatShape(o, 'credential');
  } catch {
    throw new CampaignV2Error('That is not a seat credential.');
  }
}

export function parseGrantResponse(raw: unknown): SeatGrant {
  const o = obj(raw, 'grant response');
  version(o);
  return seatShape(o['grant'], 'grant');
}

export function parseSeatResponse(raw: unknown): SeatCredential {
  const o = obj(raw, 'seat response');
  version(o);
  return seatShape(o['credential'], 'credential');
}

export function parseOpeningKinds(raw: unknown): OpeningKindsView {
  const o = obj(raw, 'opening-kinds response');
  version(o);
  const kinds = o['kinds'];
  if (!Array.isArray(kinds) || kinds.length > 512 || kinds.some((k) => typeof k !== 'string'))
    return fail('kinds');
  return { kinds: kinds as string[] };
}

/** Decodes the base64 canonical JSON bytes of a visible cut. Strict UTF-8, bounded size. */
export function decodeVisibleCut(base64: string): JsonValue {
  if (base64.length > Math.ceil((MAX_VISIBLE_CUT_BYTES * 4) / 3) + 4)
    return fail('visibleCut size');
  let bytes: Uint8Array;
  try {
    const binary = atob(base64);
    bytes = Uint8Array.from(binary, (c) => c.charCodeAt(0));
  } catch {
    return fail('visibleCutBase64');
  }
  if (bytes.length > MAX_VISIBLE_CUT_BYTES) return fail('visibleCut size');
  try {
    return JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(bytes)) as JsonValue;
  } catch {
    return fail('visibleCut JSON');
  }
}

export function parseOpeningResponse(
  raw: unknown,
  requested: { campaignId: string; stage: string },
): OpeningView {
  const o = obj(raw, 'opening response');
  version(o);
  const campaignId = str(o, 'campaignId');
  const stage = str(o, 'stage');
  // A reply for another campaign or stage must never be rendered as the one asked for.
  if (campaignId !== requested.campaignId || stage !== requested.stage) fail('opening identity');
  const refs = o['publicActionRefs'];
  if (
    !Array.isArray(refs) ||
    refs.length > MAX_REFS ||
    refs.some((r) => typeof r !== 'string' || r.length === 0) ||
    new Set(refs).size !== refs.length
  )
    return fail('publicActionRefs');
  const visibleCutBase64 = o['visibleCutBase64'];
  if (typeof visibleCutBase64 !== 'string') return fail('visibleCutBase64');
  return {
    campaignId,
    audience: audience(o, 'audience'),
    stage,
    actingElementId: str(o, 'actingElementId'),
    revision: int(o, 'revision'),
    visibleCut: decodeVisibleCut(visibleCutBase64),
    publicActionRefs: refs as string[],
  };
}

export function parseAcceptedAction(
  raw: unknown,
  sent: { openingRevision: number; publicActionRef: string },
): ActionOutcome {
  const o = obj(raw, 'action response');
  version(o);
  if (o['accepted'] !== true) return fail('accepted');
  if (
    int(o, 'openingRevision') !== sent.openingRevision ||
    str(o, 'publicActionRef') !== sent.publicActionRef
  )
    return fail('receipt does not match the submission');
  return { kind: 'accepted', ...sent };
}

export function parseErrorBody(raw: unknown): {
  code: string | undefined;
  requestId: string | undefined;
} {
  if (typeof raw !== 'object' || raw === null) return { code: undefined, requestId: undefined };
  const o = raw as Obj;
  return {
    code: typeof o['code'] === 'string' ? o['code'] : undefined,
    requestId: typeof o['requestId'] === 'string' ? o['requestId'] : undefined,
  };
}
