import AxeBuilder from '@axe-core/playwright';
import { expect, Page, test } from '@playwright/test';

/**
 * The CI job has no .NET Host, so the v2 API is a route-level fake. Its response shapes were
 * captured from a live Cna.LocalCampaignSimulator run (Stage 1 synthetic campaign).
 */
const AXIS_SECRET = 'AXIS-SECRET-0123456789';
const CW_SECRET = 'CW-SECRET-9876543210';
const axisCredential = JSON.stringify({
  campaignId: 'campaign-local-simulator',
  audience: 'axis',
  epoch: 1,
  secret: AXIS_SECRET,
  expiresUnixMs: 1795589959493,
});
const cwCredential = JSON.stringify({
  campaignId: 'campaign-local-simulator',
  audience: 'commonwealth',
  epoch: 1,
  secret: CW_SECRET,
  expiresUnixMs: 1795589959517,
});
const AXIS_REF = 'act.a7c953015203e6f8635edcafa906e5683dcc2a94e3671e944df5ed16eacd9397';
const cut = (value: unknown) => Buffer.from(JSON.stringify(value)).toString('base64');

interface Seen {
  stage: string;
  secret: string;
  kind: 'openings' | 'actions';
}

async function fakeHost(page: Page): Promise<Seen[]> {
  const seen: Seen[] = [];
  let axisInitiativeDone = false;
  await page.route('**/api/v2/campaigns/**', async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    const body = request.postDataJSON() as {
      credential: { audience: string; secret: string };
      stage: string;
      openingRevision?: number;
      publicActionRef?: string;
    };
    const kind = path.endsWith('/actions') ? 'actions' : 'openings';
    seen.push({ stage: body.stage, secret: body.credential.secret, kind });
    const unavailable = {
      status: 409,
      json: { contractVersion: 2, code: 'unavailable', requestId: 'rq' },
    };
    if (path.endsWith('/openings/open')) {
      await route.fulfill({
        json: {
          contractVersion: 2,
          campaignId: 'campaign-local-simulator',
          audience: body.credential.audience,
          open:
            body.credential.audience === 'axis' && !axisInitiativeDone
              ? [
                  {
                    openingStage: 'initiative-order',
                    actionStage: 'initiative-order',
                    actingElementIds: ['land.initiative-order'],
                    ruleNotImplemented: false,
                  },
                ]
              : [],
        },
      });
      return;
    }
    if (kind === 'actions') {
      if (body.credential.audience !== 'axis' || body.publicActionRef !== AXIS_REF) {
        await route.fulfill(unavailable);
        return;
      }
      axisInitiativeDone = true;
      await route.fulfill({
        json: { contractVersion: 2, accepted: true, openingRevision: 1, publicActionRef: AXIS_REF },
      });
      return;
    }
    if (
      body.credential.audience === 'axis' &&
      body.stage === 'initiative-order' &&
      !axisInitiativeDone
    ) {
      await route.fulfill({
        json: {
          contractVersion: 2,
          campaignId: 'campaign-local-simulator',
          audience: 'axis',
          stage: 'initiative-order',
          actingElementId: 'land.initiative-order',
          revision: 1,
          visibleCutBase64: cut({
            contractVersion: 1,
            campaignId: 'campaign-local-simulator',
            positionId: 'land.position.operation-1.initiative-declaration',
            choices: ['act-first', 'act-last'],
          }),
          publicActionRefs: [
            AXIS_REF,
            'act.fb537fdd79245af1a826178d78e5fbb5293733a3cf2d5e33af1eb6e36552a24a',
          ],
        },
      });
      return;
    }
    await route.fulfill(unavailable);
  });
  return seen;
}

const axe = (page: Page) =>
  new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze();

async function startSession(page: Page) {
  await page.goto('/hot-seat');
  await page.getByLabel('Axis seat credential (JSON)').fill(axisCredential);
  await page.getByLabel('Commonwealth seat credential (JSON)').fill(cwCredential);
  await page.getByRole('button', { name: 'Start session' }).click();
}

test('setup, hand-off and seat screens have no WCAG A/AA violations', async ({ page }) => {
  await fakeHost(page);
  await page.goto('/hot-seat');
  expect((await axe(page)).violations).toEqual([]);
  await startSession(page);
  await expect(
    page.getByRole('heading', { name: /Pass the device to the Axis player/ }),
  ).toBeFocused();
  expect((await axe(page)).violations).toEqual([]);
  await page.getByRole('button', { name: /I am the Axis player/ }).click();
  await expect(page.getByRole('heading', { name: 'Axis seat' })).toBeFocused();
  await page.getByRole('button', { name: 'Find my decisions' }).click();
  await expect(page.getByRole('radio', { name: 'act-first' })).toBeVisible();
  expect((await axe(page)).violations).toEqual([]);
});

test('hand-off removes the prior seat from DOM, live regions, storage and later requests', async ({
  page,
}) => {
  const seen = await fakeHost(page);
  await startSession(page);
  // Setup inputs are cleared once the credentials are taken.
  await page.getByRole('button', { name: /I am the Axis player/ }).click();
  await page.getByRole('button', { name: 'Find my decisions' }).click();
  await page.getByRole('radio', { name: 'act-first' }).check();
  await page.getByRole('button', { name: 'Submit choice' }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Choice accepted' })).toBeVisible();

  await page.getByRole('button', { name: /hand the device to the Commonwealth player/ }).click();
  await expect(
    page.getByRole('heading', { name: /Pass the device to the Commonwealth player/ }),
  ).toBeFocused();

  // Privacy negatives: nothing of the Axis seat survives in the page.
  const text = await page.locator('body').innerText();
  const html = await page.content();
  for (const leaked of [
    'act-first',
    'act-last',
    'Choice accepted',
    'initiative-declaration',
    'Axis seat',
  ]) {
    expect(text).not.toContain(leaked);
  }
  for (const leaked of [AXIS_SECRET, CW_SECRET, AXIS_REF, 'act-first'])
    expect(html).not.toContain(leaked);
  await expect(page.locator('mr-opening-form')).toHaveCount(0);
  for (const region of await page.getByRole('status').all()) {
    expect(await region.innerText()).toMatch(
      /^Screen cleared\. Waiting for the Commonwealth player\.$/,
    );
  }
  expect(await page.title()).toBe('Maproom · hand-off');
  expect(page.url()).not.toMatch(/SECRET|act\./);
  const storage = await page.evaluate(() => ({
    local: Object.keys(localStorage),
    session: Object.keys(sessionStorage),
    cookie: document.cookie,
  }));
  expect(storage).toEqual({ local: [], session: [], cookie: '' });

  // The Commonwealth seat sees only its own (empty) decisions and never reuses the Axis secret.
  const before = seen.length;
  await page.getByRole('button', { name: /I am the Commonwealth player/ }).click();
  // Before any lookup: no stale Axis decision, outcome or option may be on the Commonwealth screen.
  await expect(page.getByRole('heading', { name: 'Commonwealth seat' })).toBeVisible();
  await expect(page.locator('mr-opening-form')).toHaveCount(0);
  expect(await page.locator('body').innerText()).not.toMatch(/act-first|Choice accepted/);
  await page.getByRole('button', { name: 'Find my decisions' }).click();
  await expect(
    page.getByText('No decision is open for the Commonwealth seat right now.'),
  ).toBeVisible();
  const after = seen.slice(before);
  expect(after.length).toBeGreaterThan(0);
  expect(after.every((r) => r.secret === CW_SECRET)).toBe(true);
  expect(await page.locator('body').innerText()).not.toContain('act-first');
  expect(
    await page.evaluate(() => [Object.keys(localStorage), Object.keys(sessionStorage)]),
  ).toEqual([[], []]);
});

test('whole hand-off is operable from the keyboard', async ({ page }) => {
  await fakeHost(page);
  await page.goto('/hot-seat');
  await page.getByLabel('Axis seat credential (JSON)').fill(axisCredential);
  await page.getByLabel('Commonwealth seat credential (JSON)').fill(cwCredential);
  await page.keyboard.press('Enter');
  const proceed = page.getByRole('button', { name: /I am the Axis player/ });
  await proceed.focus();
  await page.keyboard.press('Enter');
  await page.getByRole('button', { name: 'Find my decisions' }).focus();
  await page.keyboard.press('Enter');
  await page.getByRole('radio', { name: 'act-first' }).focus();
  await page.keyboard.press('Space');
  await expect(page.getByRole('radio', { name: 'act-first' })).toBeChecked();
  await expect(page.getByRole('button', { name: 'Submit choice' })).toBeEnabled();
  await page.getByRole('button', { name: 'Submit choice' }).focus();
  await page.keyboard.press('Enter');
  await expect(page.getByText('Choice accepted.')).toBeVisible();
});

test('a malformed or mismatched credential is refused without starting a session', async ({
  page,
}) => {
  await fakeHost(page);
  await page.goto('/hot-seat');
  await page.getByLabel('Axis seat credential (JSON)').fill('nope');
  await page.getByLabel('Commonwealth seat credential (JSON)').fill(cwCredential);
  await page.getByRole('button', { name: 'Start session' }).click();
  await expect(page.getByRole('alert')).toContainText('not valid credential JSON');
  await page.getByLabel('Axis seat credential (JSON)').fill(cwCredential);
  await page.getByRole('button', { name: 'Start session' }).click();
  await expect(page.getByRole('alert')).toContainText('one Axis and one Commonwealth');
  await expect(page.getByRole('heading', { name: 'Start a hot-seat session' })).toBeVisible();
});

test('ending the session forgets the credentials', async ({ page }) => {
  const seen = await fakeHost(page);
  await startSession(page);
  await page.getByRole('button', { name: 'End session' }).click();
  await expect(page.getByRole('heading', { name: 'Start a hot-seat session' })).toBeVisible();
  expect(await page.content()).not.toContain(AXIS_SECRET);
  expect(seen).toEqual([]);
});
