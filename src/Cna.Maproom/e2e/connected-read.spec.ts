import { expect, test } from '@playwright/test';

test('local read lane shows only the returned audience view and never submits an order', async ({
  page,
}, testInfo) => {
  const requests: string[] = [];
  await page.route('**/api/v1/**', async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    requests.push(`${request.method()} ${path}`);
    if (path === '/api/v1/bootstrap') {
      await route.fulfill({ json: { contractVersion: 1, csrfToken: 'csrf' } });
    } else if (path === '/api/v1/fixtures') {
      await route.fulfill({
        json: [
          {
            fixtureId: 'rules9',
            displayName: 'Breakdown truck lab',
            scenarioId: 'lab-1',
            rulesetHash: 'r',
            setupHash: 's',
            contentHash: 'c',
          },
        ],
      });
    } else if (path === '/api/v1/campaigns') {
      expect(request.headers()['x-sandtable-csrf']).toBe('csrf');
      await route.fulfill({
        status: 201,
        json: {
          contractVersion: 1,
          campaign: { campaignId: 'c-1', displayName: 'Lab' },
          seatGrants: { axis: 'a-grant', commonwealth: 'private-opposing-grant' },
        },
      });
    } else if (path === '/api/v1/campaigns/c-1/seat') {
      expect(request.postDataJSON()).toEqual({ contractVersion: 1, grantId: 'a-grant' });
      await route.fulfill({
        json: {
          contractVersion: 1,
          campaignId: 'c-1',
          seat: 'axis',
          sessionGeneration: 'g-1',
          lifetime: 'temporary-memory',
        },
      });
    } else if (path === '/api/v1/campaigns/c-1/view') {
      await route.fulfill({
        json: {
          contractVersion: 1,
          campaignId: 'c-1',
          seat: 'axis',
          sessionGeneration: 'g-1',
          audienceRevision: '1',
          rulesetHash: 'r',
          position: {
            positionId: 'p-1',
            turn: '1',
            phaseId: 'movement',
            stageId: 'initial',
            decision: 'human',
          },
          locations: [
            { locationId: 'loc-1', label: 'North road', x: -1, y: -1 },
            { locationId: 'loc-2', label: 'North', x: 0, y: -1 },
            { locationId: 'loc-3', label: 'North east', x: 1, y: -1 },
            { locationId: 'loc-4', label: 'West', x: -1, y: 0 },
            { locationId: 'loc-5', label: 'Center', x: 0, y: 0 },
            { locationId: 'loc-6', label: 'East', x: 1, y: 0 },
            { locationId: 'loc-7', label: 'South west', x: -1, y: 1 },
            { locationId: 'loc-8', label: 'South', x: 0, y: 1 },
            { locationId: 'loc-9', label: 'South road', x: 1, y: 1 },
          ],
          ownFormations: [
            {
              formationId: 'own-1',
              label: 'First group',
              kindLabel: 'Infantry',
              locationId: 'loc-1',
              orderStatusLabel: 'Awaiting orders',
            },
          ],
          apparentContacts: [
            { contactId: 'contact-1', label: 'Unidentified presence', locationId: 'loc-9' },
          ],
          legalActions: [],
          formationLedger: [
            {
              formationId: 'own-1',
              label: 'First group',
              kindLabel: 'Infantry',
              locationLabel: 'North road',
              orderStatusLabel: 'Awaiting orders',
            },
          ],
          warDiary: [
            {
              entryId: 'd-1',
              audienceCursor: '1',
              title: 'Situation opened',
              body: 'Own side briefing.',
            },
          ],
          nextDiaryCursor: '1',
          actionSetToken: 'opaque',
        },
      });
    } else {
      throw new Error(`Unexpected API call: ${path}`);
    }
  });

  await page.goto('/?mode=campaign');
  await expect(page.getByRole('main')).toHaveCount(1);
  await expect(page.getByRole('banner')).toHaveCount(1);
  await expect(page.getByRole('link', { name: 'Local campaign read mode' })).toHaveAttribute(
    'aria-current',
    'page',
  );
  await page.getByRole('button', { name: 'Check local fixtures' }).click();
  await page.getByRole('button', { name: 'Create and enter Axis' }).click();
  await expect(page.getByRole('heading', { name: 'Situation map' })).toBeVisible();
  await expect(page.locator('.connected-unit').filter({ hasText: 'First group' })).toBeVisible();
  await expect(page.locator('.connected-map-point')).toHaveCount(9);
  const map = await page.getByRole('region', { name: 'Audience-safe campaign map' }).boundingBox();
  const northwest = await page.locator('[data-location-id="loc-1"]').boundingBox();
  const center = await page.locator('[data-location-id="loc-5"]').boundingBox();
  const southeast = await page.locator('[data-location-id="loc-9"]').boundingBox();
  expect(map && northwest && center && southeast).toBeTruthy();
  expect(northwest!.x).toBeGreaterThan(map!.x);
  expect(northwest!.y).toBeGreaterThan(map!.y);
  expect(northwest!.x).toBeLessThan(center!.x);
  expect(northwest!.y).toBeLessThan(center!.y);
  expect(center!.x).toBeLessThan(southeast!.x);
  expect(center!.y).toBeLessThan(southeast!.y);
  expect(southeast!.x + southeast!.width).toBeLessThan(map!.x + map!.width);
  expect(southeast!.y + southeast!.height).toBeLessThan(map!.y + map!.height);
  const hexAxes = await page.locator('.hex-grid').evaluate((element) => {
    const matrix = (element as SVGSVGElement).getScreenCTM();
    return matrix ? { x: matrix.a, y: matrix.d } : null;
  });
  expect(hexAxes).not.toBeNull();
  expect(Math.abs(hexAxes!.x - hexAxes!.y)).toBeLessThan(0.01);
  await page.getByRole('button', { name: 'Zoom in' }).click();
  await page.getByRole('button', { name: 'Zoom in' }).click();
  await expect(page.getByLabel('Map zoom')).toHaveText('150%');
  const beforeDrag = await page.locator('.map-scene').getAttribute('style');
  const canvasFrame = await page.locator('.map-canvas').boundingBox();
  expect(canvasFrame).not.toBeNull();
  const dragFrom = {
    x: canvasFrame!.x + canvasFrame!.width * 0.45,
    y: canvasFrame!.y + canvasFrame!.height * 0.45,
  };
  await page.mouse.move(dragFrom.x, dragFrom.y);
  await page.mouse.down();
  await page.mouse.move(dragFrom.x + 90, dragFrom.y - 50, { steps: 6 });
  await page.mouse.up();
  await expect.poll(() => page.locator('.map-scene').getAttribute('style')).not.toBe(beforeDrag);
  await expect(page.getByLabel('Map zoom')).toHaveText('150%');
  await page.getByRole('button', { name: 'Fit map' }).click();
  await expect(page.getByLabel('Map zoom')).toHaveText('100%');
  for (const [width, height, largeText] of [
    [502, 500, false],
    [320, 568, false],
    [320, 568, true],
  ] as const) {
    await page.setViewportSize({ width, height });
    if (largeText) await page.addStyleTag({ content: 'html { font-size: 200%; }' });
    const frame = await page
      .getByRole('region', { name: 'Audience-safe campaign map' })
      .boundingBox();
    const canvas = page.getByRole('region', {
      name: 'Situation map, arrow keys pan when zoomed',
    });
    const points = await page.locator('.connected-map-point').all();
    expect(frame).not.toBeNull();
    for (const point of points) {
      const card = await point.boundingBox();
      expect(card).not.toBeNull();
      expect(card!.x).toBeGreaterThanOrEqual(frame!.x);
      expect(card!.x + card!.width).toBeLessThanOrEqual(frame!.x + frame!.width);
      expect(card!.y).toBeGreaterThanOrEqual(frame!.y);
      expect(card!.y + card!.height).toBeLessThanOrEqual(frame!.y + frame!.height);
    }
    const scroll = page.getByRole('region', { name: 'Scrollable situation map' });
    const viewport = await scroll.boundingBox();
    expect(viewport).not.toBeNull();
    expect(viewport!.x).toBeGreaterThanOrEqual(-1);
    expect(viewport!.x + viewport!.width).toBeLessThanOrEqual(width + 1);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    expect(await scroll.evaluate((element) => element.scrollWidth)).toBeGreaterThan(
      await scroll.evaluate((element) => element.clientWidth),
    );
    await expect(page.getByLabel('Map zoom')).toHaveText('100%');
    await page.getByRole('button', { name: 'Zoom in' }).click();
    await expect(page.getByLabel('Map zoom')).toHaveText('125%');
    const beforePan = await page.locator('.map-scene').getAttribute('style');
    await canvas.focus();
    await page.keyboard.press('ArrowRight');
    await expect.poll(() => page.locator('.map-scene').getAttribute('style')).not.toBe(beforePan);
    await expect(page.getByLabel('Map zoom')).toHaveText('125%');
    await page.getByRole('button', { name: 'Fit map' }).click();
    await expect(page.getByLabel('Map zoom')).toHaveText('100%');
    await scroll.evaluate((element) => (element.scrollLeft = element.scrollWidth));
    const east = await page.locator('[data-location-id="loc-9"]').boundingBox();
    expect(east!.x + east!.width).toBeLessThanOrEqual(viewport!.x + viewport!.width + 1);
    expect(east!.x).toBeGreaterThanOrEqual(viewport!.x - 1);
    if (width === 502)
      await page.screenshot({ path: testInfo.outputPath('connected-502.png'), fullPage: true });
    await expect(page.getByRole('button', { name: 'Refresh view' })).toBeVisible();
    await expect(page.locator('.roster-item')).toHaveCount(1);
  }
  const scroll = page.getByRole('region', { name: 'Scrollable situation map' });
  await scroll.evaluate((element) => (element.scrollLeft = 0));
  await page.locator('.map-canvas').scrollIntoViewIfNeeded();
  const touchCanvas = await page.locator('.map-canvas').boundingBox();
  expect(touchCanvas).not.toBeNull();
  expect(
    await page.locator('.map-canvas').evaluate((element) => getComputedStyle(element).touchAction),
  ).toBe('pan-x');
  const touch = await page.context().newCDPSession(page);
  const touchY = Math.round(touchCanvas!.y + touchCanvas!.height * 0.5);
  const touchX = Math.round(Math.min(touchCanvas!.x + touchCanvas!.width * 0.8, 270));
  await touch.send('Input.dispatchTouchEvent', {
    type: 'touchStart',
    touchPoints: [{ x: touchX, y: touchY }],
  });
  for (let step = 1; step <= 8; step++) {
    await touch.send('Input.dispatchTouchEvent', {
      type: 'touchMove',
      touchPoints: [{ x: touchX - step * 18, y: touchY }],
    });
  }
  await touch.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  await expect.poll(() => scroll.evaluate((element) => element.scrollLeft)).toBeGreaterThan(0);
  await expect(page.getByLabel('Map zoom')).toHaveText('100%');

  await page.setViewportSize({ width: 375, height: 700 });
  await page.getByRole('button', { name: 'Keyboard help' }).click();
  await expect(page.getByText('swipe a narrow map sideways', { exact: false })).toBeVisible();
  await expect(page.getByText('choose a formation', { exact: false })).toHaveCount(0);
  await page.getByRole('button', { name: 'Expand map' }).click();
  const dialog = page.getByRole('dialog', { name: 'Situation map' });
  const expanded = await dialog.boundingBox();
  expect(expanded).not.toBeNull();
  expect(expanded!.x).toBe(0);
  expect(expanded!.y).toBe(0);
  expect(expanded!.width).toBe(375);
  expect(expanded!.height).toBe(700);
  const expandedCanvas = await dialog.locator('.map-canvas').boundingBox();
  const expandedCaption = await dialog.locator('.map-caption').boundingBox();
  expect(expandedCanvas).not.toBeNull();
  expect(expandedCaption).not.toBeNull();
  expect(expandedCanvas!.height).toBeGreaterThan(150);
  expect(expandedCaption!.y + expandedCaption!.height).toBeLessThanOrEqual(700);
  await expect(page.locator('.roster')).toHaveAttribute('inert', '');
  await expect(page.locator('.records-grid')).toHaveAttribute('inert', '');
  await expect(page.locator('.skip-link')).toHaveAttribute('inert', '');
  await expect(page.locator('.command-bar')).toHaveAttribute('inert', '');
  await expect(dialog.getByRole('button', { name: 'Close expanded map' })).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(dialog.getByRole('button', { name: 'Keyboard help' })).toBeFocused();
  await page.keyboard.press('Escape');
  await expect(dialog).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Expand map' })).toBeFocused();
  await expect(page.locator('.roster')).not.toHaveAttribute('inert', '');
  await expect(page.locator('[data-location-id="loc-1"]')).toContainText('First group');
  await expect(page.locator('[data-location-id="loc-9"]')).toContainText('Unidentified presence');
  await expect(page.getByRole('heading', { name: 'War Diary' })).toBeVisible();
  await expect(page.getByText('Own side briefing.')).toBeVisible();
  await expect(page.getByText('private-opposing-grant')).toHaveCount(0);
  await expect(page.getByText('Orders unavailable in connected read mode.')).toBeVisible();
  expect(requests).toEqual([
    'GET /api/v1/bootstrap',
    'GET /api/v1/fixtures',
    'POST /api/v1/campaigns',
    'POST /api/v1/campaigns/c-1/seat',
    'GET /api/v1/campaigns/c-1/view',
  ]);
  await page.getByRole('link', { name: 'Fixture study' }).click();
  await expect(page.getByRole('heading', { name: 'The coastal exercise' })).toBeVisible();
  await expect(page.getByText('First group')).toHaveCount(0);
  await expect(page.getByText('Unidentified presence')).toHaveCount(0);
});

test('main Maproom switches between fixture and local campaign without losing the route mode', async ({
  page,
}) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'The coastal exercise' })).toBeVisible();
  await page.getByRole('link', { name: 'Local campaign read mode' }).click();
  await expect(page).toHaveURL(/\?mode=campaign$/);
  await expect(page.getByRole('heading', { name: 'Connect to local host' })).toBeVisible();
  await expect(page.getByRole('main')).toHaveCount(1);
  await expect(page.getByRole('banner')).toHaveCount(1);
  await expect(page.getByRole('heading', { name: 'The coastal exercise' })).toHaveCount(0);
  await page.keyboard.press('?');
  await page.keyboard.press(']');
  await page.getByRole('link', { name: 'Fixture study' }).click();
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole('heading', { name: 'The coastal exercise' })).toBeVisible();
  await expect(page.getByText('Move around the exercise')).toBeHidden();
  await expect(page.getByRole('button', { name: 'Fox group, infantry, K-7' })).toHaveAttribute(
    'aria-pressed',
    'true',
  );
  await page.goBack();
  await expect(page).toHaveURL(/\?mode=campaign$/);
  await expect(page.getByRole('heading', { name: 'Connect to local host' })).toBeVisible();
  await page.goForward();
  await expect(page.getByRole('heading', { name: 'The coastal exercise' })).toBeVisible();
});

test('browser history leaves fixture fullscreen before entering campaign mode', async ({
  page,
}) => {
  await page.goto('/?mode=campaign');
  await page.getByRole('link', { name: 'Fixture study' }).click();
  await page.getByRole('button', { name: 'Expand map' }).click();
  await expect(page.getByRole('dialog', { name: 'Situation map' })).toBeVisible();
  await expect(page.locator('html')).toHaveCSS('overflow', 'hidden');

  await page.goBack();
  await expect(page).toHaveURL(/\?mode=campaign$/);
  await expect(page.getByRole('heading', { name: 'Connect to local host' })).toBeVisible();
  await expect(page.getByRole('dialog', { name: 'Situation map' })).toHaveCount(0);
  await expect(page.locator('html')).not.toHaveCSS('overflow', 'hidden');
  await expect(page.locator('.command-bar')).not.toHaveAttribute('inert', '');
  await expect(page.getByRole('link', { name: 'Fixture study' })).toBeVisible();

  await page.goForward();
  await expect(page.getByRole('heading', { name: 'The coastal exercise' })).toBeVisible();
  await expect(page.getByRole('dialog', { name: 'Situation map' })).toHaveCount(0);
  await expect(page.locator('html')).not.toHaveCSS('overflow', 'hidden');
});

test('unavailable initial view stops without showing fixture orders', async ({ page }) => {
  await page.route('**/api/v1/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path === '/api/v1/bootstrap') {
      await route.fulfill({ json: { contractVersion: 1, csrfToken: 'csrf' } });
    } else if (path === '/api/v1/fixtures') {
      await route.fulfill({ json: [{ fixtureId: 'rules9', displayName: 'Lab' }] });
    } else if (path === '/api/v1/campaigns') {
      await route.fulfill({
        status: 201,
        json: {
          contractVersion: 1,
          campaign: { campaignId: 'c-1' },
          seatGrants: { axis: 'grant-a', commonwealth: 'grant-b' },
        },
      });
    } else if (path === '/api/v1/campaigns/c-1/seat') {
      await route.fulfill({
        json: { contractVersion: 1, campaignId: 'c-1', seat: 'axis', sessionGeneration: 'g-1' },
      });
    } else if (path === '/api/v1/campaigns/c-1/view') {
      await route.fulfill({ status: 503, json: { contractVersion: 1, code: 'unavailable' } });
    } else throw new Error(`Unexpected API call: ${path}`);
  });
  await page.goto('/connected');
  await page.getByRole('button', { name: 'Check local fixtures' }).click();
  await page.getByRole('button', { name: 'Create and enter Axis' }).click();
  await expect(page.getByRole('alert')).toContainText('view is unavailable');
  await expect(page.getByRole('heading', { name: 'Situation map' })).toHaveCount(0);
  await expect(page.getByText('Prepare an order')).toHaveCount(0);
});

for (const deniedStatus of [401, 403]) {
  test(`refresh ${deniedStatus} removes revoked audience view and selection`, async ({ page }) => {
    let reads = 0;
    await page.route('**/api/v1/**', async (route) => {
      const path = new URL(route.request().url()).pathname;
      if (path === '/api/v1/bootstrap')
        await route.fulfill({ json: { contractVersion: 1, csrfToken: 'csrf' } });
      else if (path === '/api/v1/fixtures')
        await route.fulfill({ json: [{ fixtureId: 'rules9', displayName: 'Lab' }] });
      else if (path === '/api/v1/campaigns')
        await route.fulfill({
          status: 201,
          json: {
            contractVersion: 1,
            campaign: { campaignId: 'c-1' },
            seatGrants: { axis: 'grant-a', commonwealth: 'grant-b' },
          },
        });
      else if (path === '/api/v1/campaigns/c-1/seat')
        await route.fulfill({
          json: { contractVersion: 1, campaignId: 'c-1', seat: 'axis', sessionGeneration: 'g-1' },
        });
      else if (path === '/api/v1/campaigns/c-1/view') {
        reads++;
        await route.fulfill(
          reads === 1
            ? {
                json: {
                  contractVersion: 1,
                  campaignId: 'c-1',
                  seat: 'axis',
                  sessionGeneration: 'g-1',
                  position: {
                    turn: '1',
                    phaseId: 'movement',
                    stageId: 'initial',
                    decision: 'human',
                  },
                  locations: [{ locationId: 'loc-1', label: 'North road', x: 35, y: 45 }],
                  ownFormations: [
                    {
                      formationId: 'own-1',
                      label: 'Private own unit',
                      kindLabel: 'Infantry',
                      locationId: 'loc-1',
                      orderStatusLabel: 'Ready',
                    },
                  ],
                  apparentContacts: [],
                  legalActions: [],
                  formationLedger: [],
                  warDiary: [],
                },
              }
            : { status: deniedStatus, json: { contractVersion: 1, code: 'forbidden' } },
        );
      } else throw new Error(`Unexpected API call: ${path}`);
    });
    await page.goto(deniedStatus === 401 ? '/?mode=campaign' : '/connected');
    await page.getByRole('button', { name: 'Check local fixtures' }).click();
    await page.getByRole('button', { name: 'Create and enter Axis' }).click();
    await expect(page.getByText('Private own unit').first()).toBeVisible();
    await page.getByRole('button', { name: 'Refresh view' }).click();
    await expect(page.getByRole('alert')).toContainText('previous view was removed');
    await expect(page.getByText('Private own unit')).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Situation map' })).toHaveCount(0);
  });
}

test('mismatched refresh generation removes prior audience view', async ({ page }) => {
  let reads = 0;
  await page.route('**/api/v1/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path === '/api/v1/bootstrap')
      await route.fulfill({ json: { contractVersion: 1, csrfToken: 'csrf' } });
    else if (path === '/api/v1/fixtures')
      await route.fulfill({ json: [{ fixtureId: 'rules9', displayName: 'Lab' }] });
    else if (path === '/api/v1/campaigns')
      await route.fulfill({
        status: 201,
        json: {
          contractVersion: 1,
          campaign: { campaignId: 'c-1' },
          seatGrants: { axis: 'grant-a', commonwealth: 'grant-b' },
        },
      });
    else if (path === '/api/v1/campaigns/c-1/seat')
      await route.fulfill({
        json: { contractVersion: 1, campaignId: 'c-1', seat: 'axis', sessionGeneration: 'g-1' },
      });
    else if (path === '/api/v1/campaigns/c-1/session')
      await route.fulfill({
        json: {
          contractVersion: 1,
          campaignId: 'c-1',
          seat: 'commonwealth',
          sessionGeneration: 'other-generation',
        },
      });
    else if (path === '/api/v1/campaigns/c-1/view') {
      reads++;
      await route.fulfill({
        json: {
          contractVersion: 1,
          campaignId: 'c-1',
          seat: 'axis',
          sessionGeneration: reads === 1 ? 'g-1' : 'other-generation',
          position: { turn: '1', phaseId: 'movement', stageId: 'initial', decision: 'human' },
          locations: [],
          ownFormations: [
            {
              formationId: 'own-1',
              label: 'Private own unit',
              kindLabel: 'Infantry',
              locationId: 'loc-1',
              orderStatusLabel: 'Ready',
            },
          ],
          apparentContacts: [],
          legalActions: [],
          formationLedger: [],
          warDiary: [],
        },
      });
    } else throw new Error(`Unexpected API call: ${path}`);
  });
  await page.goto('/connected');
  await page.getByRole('button', { name: 'Check local fixtures' }).click();
  await page.getByRole('button', { name: 'Create and enter Axis' }).click();
  await expect(page.getByText('Private own unit').first()).toBeVisible();
  await page.getByRole('button', { name: 'Refresh view' }).click();
  await expect(page.getByRole('alert')).toContainText('Seat identity changed');
  await expect(page.getByText('Private own unit')).toHaveCount(0);
});

test('refresh recovers a rotated generation only after rechecking the same seat', async ({
  page,
}) => {
  let reads = 0;
  await page.route('**/api/v1/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path === '/api/v1/bootstrap')
      await route.fulfill({ json: { contractVersion: 1, csrfToken: 'csrf' } });
    else if (path === '/api/v1/fixtures')
      await route.fulfill({ json: [{ fixtureId: 'rules9', displayName: 'Lab' }] });
    else if (path === '/api/v1/campaigns')
      await route.fulfill({
        status: 201,
        json: {
          contractVersion: 1,
          campaign: { campaignId: 'c-1' },
          seatGrants: { axis: 'grant-a', commonwealth: 'grant-b' },
        },
      });
    else if (path === '/api/v1/campaigns/c-1/seat')
      await route.fulfill({
        json: { contractVersion: 1, campaignId: 'c-1', seat: 'axis', sessionGeneration: 'g-1' },
      });
    else if (path === '/api/v1/campaigns/c-1/session')
      await route.fulfill({
        json: { contractVersion: 1, campaignId: 'c-1', seat: 'axis', sessionGeneration: 'g-2' },
      });
    else if (path === '/api/v1/campaigns/c-1/view') {
      reads++;
      await route.fulfill({
        json: {
          contractVersion: 1,
          campaignId: 'c-1',
          seat: 'axis',
          sessionGeneration: reads === 1 ? 'g-1' : 'g-2',
          position: { turn: '1', phaseId: 'movement', stageId: 'initial', decision: 'human' },
          locations: [],
          ownFormations: [],
          apparentContacts: [],
          legalActions: [],
          formationLedger: [],
          warDiary: [],
        },
      });
    } else throw new Error(`Unexpected API call: ${path}`);
  });
  await page.goto('/connected');
  await page.getByRole('button', { name: 'Check local fixtures' }).click();
  await page.getByRole('button', { name: 'Create and enter Axis' }).click();
  await expect(page.getByRole('heading', { name: 'Situation map' })).toBeVisible();
  await page.getByRole('button', { name: 'Refresh view' }).click();
  await expect.poll(() => reads).toBe(3);
  await expect(page.getByRole('heading', { name: 'Situation map' })).toBeVisible();
  await expect(page.getByRole('alert')).toHaveCount(0);
});

test('conflicting process-local creation explains restart boundary', async ({ page }) => {
  await page.route('**/api/v1/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path === '/api/v1/bootstrap')
      await route.fulfill({ json: { contractVersion: 1, csrfToken: 'csrf' } });
    else if (path === '/api/v1/fixtures')
      await route.fulfill({ json: [{ fixtureId: 'rules9', displayName: 'Lab' }] });
    else if (path === '/api/v1/campaigns')
      await route.fulfill({ status: 409, json: { contractVersion: 1, code: 'conflict' } });
    else throw new Error(`Unexpected API call: ${path}`);
  });
  await page.goto('/connected');
  await page.getByRole('button', { name: 'Check local fixtures' }).click();
  await page.getByRole('button', { name: 'Create and enter Axis' }).click();
  await expect(page.getByRole('alert')).toContainText('restart the local host');
  await expect(page.getByRole('heading', { name: 'Situation map' })).toHaveCount(0);
});

// Authorized reads use a fresh mocked session for each seat.
for (const seat of ['axis', 'commonwealth'] as const) {
  test(`${seat} operational read retains then clears outward selection`, async ({ page }) => {
    const own = seat === 'axis' ? 'Axis' : 'Commonwealth';
    const opposing = seat === 'axis' ? 'Commonwealth' : 'Axis';
    let reads = 0;
    const posts: string[] = [];
    await page.route('**/api/v1/**', async (route) => {
      const request = route.request();
      const path = new URL(request.url()).pathname;
      if (request.method() === 'POST') posts.push(path);
      if (path === '/api/v1/bootstrap')
        await route.fulfill({ json: { contractVersion: 1, csrfToken: 'csrf' } });
      else if (path === '/api/v1/fixtures')
        await route.fulfill({ json: [{ fixtureId: 'rules9', displayName: 'Truck lab' }] });
      else if (path === '/api/v1/campaigns')
        await route.fulfill({
          status: 201,
          json: {
            contractVersion: 1,
            campaign: { campaignId: 'c-1' },
            seatGrants: { axis: 'grant-axis', commonwealth: 'grant-commonwealth' },
          },
        });
      else if (path === '/api/v1/campaigns/c-1/seat') {
        expect(request.postDataJSON()).toEqual({ contractVersion: 1, grantId: `grant-${seat}` });
        await route.fulfill({
          json: {
            contractVersion: 1,
            campaignId: 'c-1',
            seat,
            sessionGeneration: 'g-1',
          },
        });
      } else if (path === '/api/v1/campaigns/c-1/view') {
        reads++;
        const infantry = {
          formationId: 'outward-infantry',
          label: `${own} infantry`,
          kindLabel: 'Infantry',
          locationId: 'west',
          orderStatusLabel: 'No reserve designation',
        };
        const truck = {
          formationId: 'outward-truck',
          label: `${own} truck`,
          kindLabel: 'Truck convoy',
          locationId: reads === 2 ? 'missing' : 'east',
          orderStatusLabel: 'Reserve I',
        };
        const ownFormations = reads === 3 ? [infantry] : [infantry, truck];
        await route.fulfill({
          json: {
            contractVersion: 1,
            campaignId: 'c-1',
            seat,
            sessionGeneration: 'g-1',
            audienceRevision: String(reads),
            rulesetHash: 'r',
            position: {
              positionId: 'p-1',
              turn: '1',
              phaseId: 'movement',
              stageId: 'initial',
              decision: reads === 3 ? 'unsupported-phase' : 'human',
            },
            locations: [
              { locationId: 'west', label: 'West', x: -1, y: 0 },
              { locationId: 'east', label: 'East', x: 1, y: 0 },
            ],
            ownFormations,
            apparentContacts: [
              { contactId: 'contact', label: 'Unidentified presence', locationId: 'east' },
            ],
            legalActions:
              reads === 3
                ? []
                : [
                    {
                      actionId: 'move',
                      actionContractVersion: 1,
                      kind: 'move',
                      label: 'Move truck',
                      formationId: 'outward-truck',
                    },
                  ],
            formationLedger: ownFormations.map((formation) => ({
              formationId: formation.formationId,
              label: formation.label,
              kindLabel: formation.kindLabel,
              locationLabel: formation.locationId === 'west' ? 'West' : 'East',
              orderStatusLabel: formation.orderStatusLabel,
            })),
            warDiary: [],
            nextDiaryCursor: '0',
            actionSetToken: 'opaque',
          },
        });
      } else throw new Error(`Unexpected API call: ${path}`);
    });

    await page.goto('/?mode=campaign');
    await page.getByRole('button', { name: 'Check local fixtures' }).click();
    await page.getByRole('button', { name: `Create and enter ${own}` }).click();
    await expect(page.getByRole('heading', { name: 'Situation map' })).toBeVisible();
    await expect(page.locator('.connected-unit[data-kind="truck"]')).toContainText(`${own} truck`);
    await expect(page.locator('.roster-item')).toHaveCount(2);
    await expect(page.getByRole('table', { name: 'Formation ledger' })).toContainText(
      'Truck convoy',
    );
    await expect(page.getByText('No diary entries in this view.')).toBeVisible();
    await expect(page.getByText(`${opposing} infantry`)).toHaveCount(0);
    await page
      .locator('.roster-item')
      .filter({ hasText: `${own} truck` })
      .click();
    await expect(page.locator('.connected-inspector')).toContainText('Move truck');
    await expect(page.getByRole('button', { name: 'Move truck' })).toHaveCount(0);
    const scene = page.locator('.connected-map .map-scene');
    const fitted = await scene.getAttribute('style');
    const locate = page.getByRole('button', { name: 'Locate on map' });
    await page.setViewportSize({ width: 320, height: 568 });
    await locate.focus();
    await page.keyboard.press('Enter');
    await expect(page.getByLabel('Map zoom')).toHaveText('150%');
    await expect(scene).not.toHaveAttribute('style', fitted!);
    await expect(page.locator('.connected-map .map-canvas')).toBeFocused();
    const scroll = page.getByRole('region', { name: 'Scrollable situation map' });
    const marker = page.locator('.connected-unit[data-kind="truck"]');
    await expect
      .poll(async () => {
        const [viewport, selectedMarker] = await Promise.all([
          scroll.boundingBox(),
          marker.boundingBox(),
        ]);
        if (!viewport || !selectedMarker) return false;
        return (
          selectedMarker.x >= viewport.x - 1 &&
          selectedMarker.x + selectedMarker.width <= viewport.x + viewport.width + 1 &&
          selectedMarker.y >= viewport.y - 1 &&
          selectedMarker.y + selectedMarker.height <= viewport.y + viewport.height + 1 &&
          selectedMarker.x >= -1 &&
          selectedMarker.x + selectedMarker.width <= 321 &&
          selectedMarker.y >= -1 &&
          selectedMarker.y + selectedMarker.height <= 569
        );
      })
      .toBe(true);
    await expect(page.locator('.roster-item').filter({ hasText: `${own} truck` })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    await expect(page.getByText(`${opposing} infantry`)).toHaveCount(0);
    await expect(page.getByRole('button', { name: /prepare an order|issue order/i })).toHaveCount(
      0,
    );
    await page.getByRole('button', { name: 'Fit map' }).click();
    await expect(page.getByLabel('Map zoom')).toHaveText('100%');
    await expect(scene).toHaveAttribute('style', fitted!);

    await page.getByRole('button', { name: 'Refresh view' }).click();
    await expect.poll(() => reads).toBe(2);
    await expect(page.locator('.roster-item').filter({ hasText: `${own} truck` })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    await expect(locate).toBeDisabled();

    await page.getByRole('button', { name: 'Refresh view' }).click();
    await expect.poll(() => reads).toBe(3);
    await expect(page.locator('.roster-item')).toHaveCount(1);
    await expect(page.locator('.roster-item')).toHaveAttribute('aria-pressed', 'false');
    await expect(page.locator('.connected-inspector')).toContainText('No formation selected');
    await expect(page.getByRole('status')).toContainText('no connected browser continuation');
    expect(posts).toEqual(['/api/v1/campaigns', '/api/v1/campaigns/c-1/seat']);
  });
}
