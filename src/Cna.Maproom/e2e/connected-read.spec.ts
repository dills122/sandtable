import { expect, test } from '@playwright/test';

test('local read lane shows only the returned audience view and never submits an order', async ({
  page,
}) => {
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

  await page.goto('/connected');
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
    await page.goto('/connected');
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
          locationId: 'east',
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

    await page.goto('/connected');
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

    await page.getByRole('button', { name: 'Refresh view' }).click();
    await expect.poll(() => reads).toBe(2);
    await expect(page.locator('.roster-item').filter({ hasText: `${own} truck` })).toHaveAttribute(
      'aria-pressed',
      'true',
    );

    await page.getByRole('button', { name: 'Refresh view' }).click();
    await expect.poll(() => reads).toBe(3);
    await expect(page.locator('.roster-item')).toHaveCount(1);
    await expect(page.locator('.roster-item')).toHaveAttribute('aria-pressed', 'false');
    await expect(page.locator('.connected-inspector')).toContainText('No formation selected');
    await expect(page.getByRole('status')).toContainText('no connected browser continuation');
    expect(posts).toEqual(['/api/v1/campaigns', '/api/v1/campaigns/c-1/seat']);
  });
}
