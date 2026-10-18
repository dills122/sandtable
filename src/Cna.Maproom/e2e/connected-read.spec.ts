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
          locations: [{ locationId: 'loc-1', label: 'North road', x: 35, y: 45 }],
          ownFormations: [
            {
              formationId: 'own-1',
              label: 'First group',
              kindLabel: 'Infantry',
              locationId: 'loc-1',
              orderStatusLabel: 'Awaiting orders',
            },
          ],
          apparentContacts: [],
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
  await expect(page.getByRole('button', { name: 'First group' }).first()).toBeVisible();
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
