/**
 * Hand-written types for the durable Host's `/api/v2/campaigns` surface (Development, loopback,
 * same-origin only). Source of truth: `src/Cna.OrleansHost/Api/CampaignDurableOutwardApi.cs`.
 * There is no OpenAPI document for v2 yet, so `codec.ts` validates every response at runtime
 * instead of trusting these shapes.
 */
export type Audience = 'axis' | 'commonwealth';

export type JsonValue =
  null | boolean | number | string | JsonValue[] | { [key: string]: JsonValue };

/** Redeemed seat credential. `secret` is a bearer secret: never log, store or render it. */
export interface SeatCredential {
  readonly campaignId: string;
  readonly audience: Audience;
  readonly epoch: number;
  readonly secret: string;
  readonly expiresUnixMs: number;
}

/** Operator-issued, one-use grant redeemed for a `SeatCredential`. Same secrecy as above. */
export interface SeatGrant {
  readonly campaignId: string;
  readonly audience: Audience;
  readonly epoch: number;
  readonly secret: string;
  readonly expiresUnixMs: number;
}

/** One decoded owner opening: the seat's own legal choices at one rules moment. */
export interface OpeningView {
  readonly campaignId: string;
  readonly audience: Audience;
  readonly stage: string;
  readonly actingElementId: string;
  /** Fence value echoed back on submission; a stale value is rejected as `unavailable`. */
  readonly revision: number;
  /** The mechanic's own canonical visible cut, decoded from `visibleCutBase64`. Shape varies by stage. */
  readonly visibleCut: JsonValue;
  /** Opaque references, one per legal choice, echoed back unchanged on submission. */
  readonly publicActionRefs: readonly string[];
}

export type ActionOutcome =
  | {
      readonly kind: 'accepted';
      readonly openingRevision: number;
      readonly publicActionRef: string;
    }
  | {
      readonly kind: 'rejected';
      /** `unavailable` (stale or no such opening) or `rule-not-implemented` (source-legal, unbuilt). */
      readonly code: string;
      readonly requestId: string | undefined;
    };

export interface OpeningKindsView {
  /** `CampaignOwnerOpeningKind` enum names as reported by the Host. */
  readonly kinds: readonly string[];
}

export interface DiscoveredOpening {
  readonly openingStage: string;
  readonly actionStage: string;
  readonly actingElementIds: readonly string[];
  readonly ruleNotImplemented: boolean;
}

/** Raised for transport/contract failures. The message is audience-safe: it never echoes request bodies. */
export class CampaignV2Error extends Error {
  constructor(
    message: string,
    readonly status: number | undefined = undefined,
    readonly code: string | undefined = undefined,
    readonly requestId: string | undefined = undefined,
  ) {
    super(message);
    this.name = 'CampaignV2Error';
  }
}
