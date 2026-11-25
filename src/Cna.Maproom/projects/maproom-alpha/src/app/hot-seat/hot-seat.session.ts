import { Injectable, computed, inject, signal } from '@angular/core';
import { CampaignV2Client } from '../v2/campaign-v2.client';
import { StageEntry, STAGE_CATALOG, elementFor } from '../v2/stage-catalog';
import { ActionOutcome, Audience, CampaignV2Error, OpeningView, SeatCredential } from '../v2/types';

export type HotSeatPhase = 'setup' | 'handoff' | 'seat';

export interface FoundOpening {
  readonly entry: StageEntry;
  readonly opening: OpeningView;
}

const OTHER: Record<Audience, Audience> = { axis: 'commonwealth', commonwealth: 'axis' };
export const seatLabel = (seat: Audience): string => (seat === 'axis' ? 'Axis' : 'Commonwealth');

/**
 * Hot-seat state for two players sharing one browser. The invariant: at most one seat's decision
 * data exists in memory or in the DOM at a time, and it is destroyed before the hand-off screen
 * renders. Nothing here is written to storage, cookies or the URL.
 *
 * - Both credentials live in a true private field (`#seats`), never exposed to templates.
 * - Every asynchronous result is tagged with a generation; a reply that lands after a hand-off
 *   (or reset) belongs to the previous seat and is discarded unseen.
 * - Opening queries are not read-only on the Host (they upsert opening refs and may commit a
 *   timeout), so decisions are only fetched on an explicit player action, never on a timer.
 */
@Injectable()
export class HotSeatSession {
  private readonly client = inject(CampaignV2Client);
  #seats: Partial<Record<Audience, SeatCredential>> = {};
  #generation = 0;

  readonly phase = signal<HotSeatPhase>('setup');
  /** The seat the device is handed to (handoff phase) or the seat in front of the screen (seat phase). */
  readonly seat = signal<Audience | null>(null);
  readonly openings = signal<readonly FoundOpening[]>([]);
  readonly outcome = signal<{ stage: string; result: ActionOutcome } | null>(null);
  readonly error = signal('');
  readonly busy = signal(false);
  /** Acting unit id typed by the active seat. Seat-private: cleared on hand-off and reset. */
  readonly unitId = signal('');
  /** True once the active seat has had its decisions looked up at least once. */
  readonly searched = signal(false);
  readonly otherSeat = computed(() => {
    const seat = this.seat();
    return seat ? OTHER[seat] : null;
  });
  readonly configured = computed(() => this.phase() !== 'setup');

  /** Replaces any prior session. Both credentials must match one campaign and their seats. */
  configure(axis: SeatCredential, commonwealth: SeatCredential): void {
    if (
      axis.audience !== 'axis' ||
      commonwealth.audience !== 'commonwealth' ||
      axis.campaignId !== commonwealth.campaignId
    )
      throw new CampaignV2Error(
        'Seat credentials must be one Axis and one Commonwealth seat for the same campaign.',
      );
    this.wipe();
    this.#seats = { axis, commonwealth };
    this.handoffTo('axis');
  }

  campaignId(): string | null {
    return this.#seats.axis?.campaignId ?? null;
  }

  /** Destroys everything the active seat could see, then shows the neutral hand-off screen. */
  handoffTo(seat: Audience): void {
    this.#generation++;
    this.openings.set([]);
    this.outcome.set(null);
    this.error.set('');
    this.busy.set(false);
    this.searched.set(false);
    this.unitId.set('');
    this.seat.set(seat);
    this.phase.set('handoff');
  }

  /** The seat holder confirms they are at the screen. Shows nothing until they ask for decisions. */
  reveal(): void {
    if (this.phase() !== 'handoff') return;
    this.phase.set('seat');
  }

  /** Ends the session and forgets both credentials. */
  reset(): void {
    this.wipe();
    this.#seats = {};
    this.phase.set('setup');
  }

  private wipe(): void {
    this.#generation++;
    this.openings.set([]);
    this.outcome.set(null);
    this.error.set('');
    this.busy.set(false);
    this.searched.set(false);
    this.unitId.set('');
    this.seat.set(null);
  }

  private credential(): SeatCredential | null {
    const seat = this.seat();
    return this.phase() === 'seat' && seat ? (this.#seats[seat] ?? null) : null;
  }

  /**
   * Looks up the active seat's open decisions. There is no "what is open for me" endpoint, so this
   * probes the seat-level gameplay stages, plus unit stages when an acting unit id is supplied.
   * A 409 `unavailable` means "nothing open here" and is the normal answer for most stages.
   */
  async findDecisions(unitId?: string): Promise<void> {
    const credential = this.credential();
    if (!credential || this.busy()) return;
    const generation = this.#generation;
    this.busy.set(true);
    this.error.set('');
    this.outcome.set(null);
    const found: FoundOpening[] = [];
    try {
      const unit = unitId?.trim();
      for (const entry of STAGE_CATALOG) {
        if (entry.category !== 'gameplay') continue;
        if (entry.element.kind === 'unit' && !unit) continue;
        try {
          const opening = await this.client.queryOpening(
            credential,
            entry.openingStage,
            elementFor(entry, unit),
          );
          if (generation !== this.#generation) return;
          found.push({ entry, opening });
        } catch (cause) {
          if (generation !== this.#generation) return;
          if (cause instanceof CampaignV2Error && cause.status === 409) continue;
          throw cause;
        }
      }
      this.openings.set(found);
      this.searched.set(true);
    } catch (cause) {
      if (generation !== this.#generation) return;
      this.error.set(
        cause instanceof CampaignV2Error ? cause.message : 'Could not reach the host.',
      );
    } finally {
      if (generation === this.#generation) this.busy.set(false);
    }
  }

  /** Submits one choice for the active seat. Reply for a prior seat is discarded. */
  async choose(found: FoundOpening, publicActionRef: string): Promise<void> {
    const credential = this.credential();
    if (!credential || this.busy()) return;
    const generation = this.#generation;
    this.busy.set(true);
    this.error.set('');
    try {
      const result = await this.client.submitAction(
        credential,
        found.opening,
        publicActionRef,
        found.entry.actionStage,
      );
      if (generation !== this.#generation) return;
      this.outcome.set({ stage: found.opening.stage, result });
    } catch (cause) {
      if (generation !== this.#generation) return;
      this.error.set(
        cause instanceof CampaignV2Error ? cause.message : 'Could not reach the host.',
      );
    } finally {
      if (generation === this.#generation) this.busy.set(false);
    }
  }
}
