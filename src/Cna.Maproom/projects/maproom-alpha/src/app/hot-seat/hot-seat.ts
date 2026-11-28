import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  effect,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { AtlasPanel, AtlasStatus } from '@sandtable/field-atlas';
import { CampaignV2Client } from '../v2/campaign-v2.client';
import { parseSeatCredentialJson } from '../v2/codec';
import { OpeningChoice, OpeningForm } from '../v2/opening-form';
import { Audience, CampaignV2Error } from '../v2/types';
import { FoundOpening, HotSeatSession, seatLabel } from './hot-seat.session';

/**
 * Hot-seat shell: two players, one browser. A neutral hand-off screen separates the seats, and
 * the prior seat's decisions, outcome, errors and live-region text are destroyed (not hidden)
 * before it renders. Setup is operator-driven (D10): paste the two seat credentials, or let the
 * operator secret mint and redeem them. Secrets are held in memory only.
 */
@Component({
  selector: 'mr-hot-seat',
  imports: [AtlasPanel, AtlasStatus, OpeningForm],
  providers: [HotSeatSession],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './hot-seat.html',
  styleUrl: './hot-seat.scss',
})
export class HotSeat {
  protected readonly session = inject(HotSeatSession);
  private readonly client = inject(CampaignV2Client);
  private readonly injector = inject(Injector);
  private readonly heading = viewChild<ElementRef<HTMLElement>>('heading');
  protected readonly label = seatLabel;
  protected readonly setupError = signal('');
  protected readonly setupBusy = signal(false);

  constructor() {
    const originalTitle = document.title;
    effect(() => {
      const phase = this.session.phase();
      const seat = this.session.seat();
      document.title =
        phase === 'seat' && seat
          ? `Maproom · ${seatLabel(seat)} seat`
          : phase === 'handoff'
            ? 'Maproom · hand-off'
            : 'Maproom · hot-seat setup';
      // Move focus to the new screen's heading so keyboard and screen-reader users land on it.
      untracked(() =>
        afterNextRender(() => this.heading()?.nativeElement.focus(), { injector: this.injector }),
      );
    });
    inject(DestroyRef).onDestroy(() => {
      this.session.reset();
      document.title = originalTitle;
    });
  }

  protected setupFromCredentials(
    axisText: string,
    commonwealthText: string,
    form: HTMLFormElement,
  ): void {
    this.setupError.set('');
    try {
      this.session.configure(
        parseSeatCredentialJson(axisText),
        parseSeatCredentialJson(commonwealthText),
      );
      form.reset();
    } catch (cause) {
      this.setupError.set(
        cause instanceof CampaignV2Error ? cause.message : 'Credentials not accepted.',
      );
    }
  }

  protected async setupFromOperator(
    campaignId: string,
    operatorSecret: string,
    form: HTMLFormElement,
  ): Promise<void> {
    this.setupError.set('');
    const id = campaignId.trim();
    if (!id || !operatorSecret) {
      this.setupError.set('Enter the campaign id and the operator secret.');
      return;
    }
    this.setupBusy.set(true);
    try {
      const axis = await this.client.redeemSeat(
        await this.client.issueGrant(operatorSecret, id, 'axis'),
      );
      const commonwealth = await this.client.redeemSeat(
        await this.client.issueGrant(operatorSecret, id, 'commonwealth'),
      );
      this.session.configure(axis, commonwealth);
      form.reset();
    } catch (cause) {
      this.setupError.set(
        cause instanceof CampaignV2Error ? cause.message : 'Could not reach the host.',
      );
    } finally {
      this.setupBusy.set(false);
    }
  }

  protected handoff(seat: Audience | null): void {
    if (seat) this.session.handoffTo(seat);
  }

  protected choose(found: FoundOpening, choice: OpeningChoice): void {
    void this.session.choose(found, choice.publicActionRef);
  }

  protected outcomeFor(found: FoundOpening) {
    const outcome = this.session.outcome();
    return outcome && outcome.opening === found.opening ? outcome.result : null;
  }
}
