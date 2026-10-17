import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  AtlasClearIcon,
  AtlasClosePanelIcon,
  AtlasContractIcon,
  AtlasCancelIcon,
  AtlasExpandIcon,
  AtlasFitMapIcon,
  AtlasIconButton,
  AtlasLocateIcon,
  AtlasOpenPanelIcon,
  AtlasIssueIcon,
  AtlasRefreshIcon,
  AtlasReviewIcon,
  AtlasSearchIcon,
  AtlasSortIcon,
  AtlasZoomInIcon,
  AtlasZoomOutIcon,
} from '@sandtable/field-atlas';
import type { Meta, StoryObj } from '@storybook/angular-vite';

@Component({
  selector: 'fa-icons-example',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AtlasIconButton,
    AtlasZoomInIcon,
    AtlasZoomOutIcon,
    AtlasFitMapIcon,
    AtlasLocateIcon,
    AtlasOpenPanelIcon,
    AtlasClosePanelIcon,
    AtlasExpandIcon,
    AtlasContractIcon,
    AtlasSearchIcon,
    AtlasSortIcon,
    AtlasClearIcon,
    AtlasReviewIcon,
    AtlasIssueIcon,
    AtlasRefreshIcon,
    AtlasCancelIcon,
  ],
  template: `<main class="fa-stack">
    <h1>Map controls</h1>
    <p>Each icon-only control has a visible hover title and accessible name.</p>
    <div class="actions">
      <fa-icon-button label="Zoom in"
        ><svg lucideZoomIn width="20" height="20"></svg
      ></fa-icon-button>
      <fa-icon-button label="Zoom out"
        ><svg lucideZoomOut width="20" height="20"></svg
      ></fa-icon-button>
      <fa-icon-button label="Fit map"><svg lucideScan width="20" height="20"></svg></fa-icon-button>
      <fa-icon-button label="Locate selected formation"
        ><svg lucideLocateFixed width="20" height="20"></svg
      ></fa-icon-button>
      <fa-icon-button label="Open roster"
        ><svg lucidePanelLeftOpen width="20" height="20"></svg
      ></fa-icon-button>
      <fa-icon-button label="Close roster"
        ><svg lucidePanelLeftClose width="20" height="20"></svg
      ></fa-icon-button>
      <fa-icon-button label="Expand map"
        ><svg lucideExpand width="20" height="20"></svg
      ></fa-icon-button>
      <fa-icon-button label="Contract map"
        ><svg lucideShrink width="20" height="20"></svg
      ></fa-icon-button>
      <fa-icon-button label="Search formations"
        ><svg lucideSearch width="20" height="20"></svg
      ></fa-icon-button>
      <fa-icon-button label="Sort formations"
        ><svg lucideArrowDownUp width="20" height="20"></svg
      ></fa-icon-button>
      <fa-icon-button label="Clear search"
        ><svg lucideX width="20" height="20"></svg
      ></fa-icon-button>
    </div>
    <h2>Text actions</h2>
    <div class="actions">
      <button type="button">
        <svg lucideClipboardCheck aria-hidden="true" width="18" height="18"></svg>Review order
      </button>
      <button type="button">
        <svg lucideSend aria-hidden="true" width="18" height="18"></svg>Issue order
      </button>
      <button type="button">
        <svg lucideRefreshCw aria-hidden="true" width="18" height="18"></svg>Refresh view
      </button>
      <button type="button">
        <svg lucideBan aria-hidden="true" width="18" height="18"></svg>Cancel draft
      </button>
    </div>
  </main>`,
  styles: `
    main {
      max-width: 48rem;
    }
    h1 {
      margin-bottom: 0;
    }
    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-xs);
    }
    .actions > button {
      display: inline-flex;
      align-items: center;
      gap: var(--space-xs);
      min-height: 44px;
      padding: 0 var(--space-sm);
      color: var(--color-ink);
      background: var(--color-panel);
      border: 1px solid var(--color-rule);
      border-radius: var(--radius-control);
    }
    .actions > button:focus-visible {
      outline: 3px solid var(--color-focus);
      outline-offset: 2px;
    }
  `,
})
class IconsExample {}

export default {
  title: 'Components/Icons',
  component: IconsExample,
  parameters: {
    docs: {
      description: {
        component: 'Semantic Lucide aliases, 20px outline icons, and 44px named map controls.',
      },
    },
  },
} satisfies Meta<IconsExample>;

export const MapControls: StoryObj<IconsExample> = {};
