import { expect, test } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
test('selection stays coherent; review and pending leave the last confirmed position', async ({
  page,
}) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Harrier group, armour, G-5', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Harrier group', exact: true })).toBeVisible();
  await expect(page.locator('.ledger-row.is-selected')).toContainText('Harrier group');
  await page.getByRole('button', { name: 'Review draft', exact: true }).click();
  await expect(page.getByText('Choose a destination.')).toBeVisible();
  await page.getByRole('combobox', { name: 'Move to' }).click();
  await page.getByRole('option', { name: 'East crossing · M-6' }).click();
  await page.getByRole('button', { name: 'Review draft', exact: true }).click();
  await expect(page.getByRole('group', { name: 'Reviewed order' })).toContainText(
    'Harrier group: move to M-6',
  );
  await page.getByRole('button', { name: 'Issue order' }).click();
  await expect(page.getByRole('combobox', { name: 'Move to' })).toBeDisabled();
  await expect(
    page.getByRole('button', { name: 'Harrier group, armour, G-5', exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole('button', { name: 'Harrier group, armour, G-5', exact: true }),
  ).toHaveAttribute('aria-pressed', 'true');
  await page.getByRole('button', { name: 'Fox group, infantry, K-7', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Fox group', exact: true })).toBeVisible();
  await expect(
    page.getByRole('button', { name: 'Harrier group, armour, M-6', exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole('button', { name: 'Fox group, infantry, K-7', exact: true }),
  ).toHaveAttribute('aria-pressed', 'true');
});
test('keyboard can select a unit and review a draft', async ({ page }) => {
  await page.goto('/');
  const counter = page.getByRole('button', { name: 'Heron group, armour, L-4', exact: true });
  await counter.focus();
  await page.keyboard.press('Enter');
  await expect(counter).toHaveAttribute('aria-pressed', 'true');
  const select = page.getByRole('combobox', { name: 'Move to' });
  await select.focus();
  await page.keyboard.press('Enter');
  await page.keyboard.press('ArrowDown');
  await page.keyboard.press('Enter');
  await page.getByRole('button', { name: 'Review draft', exact: true }).focus();
  await page.keyboard.press('Enter');
  await expect(page.getByRole('group', { name: 'Reviewed order' })).toContainText('Ready to issue');
});
test('lost reply reconciles once; stale refresh requires a new review', async ({ page }) => {
  await page.goto('/');
  const destination = page.getByRole('combobox', { name: 'Move to' });
  await destination.click();
  await page.getByRole('option', { name: 'East crossing · M-6' }).click();
  await page.getByRole('button', { name: 'Review draft' }).click();
  await page.getByLabel('Fixture result').selectOption('lost-reply');
  await page.getByRole('button', { name: 'Issue order' }).click();
  await expect(page.getByText('Reply lost. Outcome unknown until receipt readback.')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Fox group, infantry, K-7' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Issue order' })).toHaveCount(0);
  await page.getByRole('button', { name: 'Simulate reconnect and reconcile' }).click();
  await expect(page.getByRole('button', { name: 'Fox group, infantry, M-6' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Simulate reconnect and reconcile' })).toHaveCount(
    0,
  );
  await destination.click();
  await page.getByRole('option', { name: 'Coast road · J-5' }).click();
  await page.getByRole('button', { name: 'Review draft' }).click();
  await page.getByLabel('Fixture result').selectOption('stale');
  await page.getByRole('button', { name: 'Issue order' }).click();
  await expect(page.getByText('Stale view. Position unchanged.')).toBeVisible();
  await page.getByRole('button', { name: 'Refresh orders' }).click();
  await expect(page.getByRole('button', { name: 'Issue order' })).toHaveCount(0);
  await page.getByRole('button', { name: 'Review draft' }).click();
  await expect(page.getByRole('button', { name: 'Issue order' })).toBeVisible();
});
test('unresolved order blocks another issue and exposes recovery from a different formation', async ({
  page,
}) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Harrier group, armour, G-5' }).click();
  await page.getByRole('combobox', { name: 'Move to' }).click();
  await page.getByRole('option', { name: 'Inland ridge · H-8' }).click();
  await page.getByRole('button', { name: 'Fox group, infantry, K-7' }).click();
  await page.getByRole('combobox', { name: 'Move to' }).click();
  await page.getByRole('option', { name: 'East crossing · M-6' }).click();
  await page.getByRole('button', { name: 'Review draft' }).click();
  await page.getByLabel('Fixture result').selectOption('lost-reply');
  await page.getByRole('button', { name: 'Issue order' }).click();
  await expect(page.getByRole('combobox', { name: 'Move to' })).toBeDisabled();
  await page.getByRole('button', { name: 'Harrier group, armour, G-5' }).click();
  await page.getByRole('button', { name: 'Review draft' }).click();
  await expect(page.getByRole('button', { name: 'Issue order' })).toBeDisabled();
  await expect(page.getByText('Fox group has an unresolved order.')).toBeVisible();
  await expect(page.getByText('Reply lost. Reconcile the submitted order')).toBeVisible();
  await page.getByRole('button', { name: 'Simulate reconnect and reconcile' }).click();
  await expect(page.getByRole('button', { name: 'Issue order' })).toBeEnabled();
  await expect(page.getByRole('button', { name: 'Fox group, infantry, M-6' })).toBeVisible();
});
test('rejected result preserves position and requires refreshed review', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('combobox', { name: 'Move to' }).click();
  await page.getByRole('option', { name: 'East crossing · M-6' }).click();
  await page.getByRole('button', { name: 'Review draft' }).click();
  await page.getByLabel('Fixture result').selectOption('rejected');
  await page.getByRole('button', { name: 'Issue order' }).click();
  await expect(page.getByText('Rejected fixture result. Position unchanged.')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Fox group, infantry, K-7' })).toBeVisible();
  await page.getByRole('button', { name: 'Refresh orders' }).click();
  await expect(page.getByRole('button', { name: 'Issue order' })).toHaveCount(0);
  await page.getByRole('button', { name: 'Review draft' }).click();
  await expect(page.getByRole('button', { name: 'Issue order' })).toBeVisible();
});
test('switching formations preserves each local draft and cannot redirect a delayed result', async ({
  page,
}) => {
  await page.goto('/');
  await page.getByRole('combobox', { name: 'Move to' }).click();
  await page.getByRole('option', { name: 'East crossing · M-6' }).click();
  await page.getByRole('button', { name: 'Review draft' }).click();
  await page.getByRole('button', { name: 'Issue order' }).click();
  await page.getByRole('button', { name: 'Harrier group, armour, G-5' }).click();
  await page.getByRole('combobox', { name: 'Move to' }).click();
  await page.getByRole('option', { name: 'Inland ridge · H-8' }).click();
  await expect(page.getByRole('button', { name: 'Fox group, infantry, M-6' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Harrier group, armour, G-5' })).toBeVisible();
  await page.getByRole('button', { name: 'Fox group, infantry, M-6' }).click();
  await expect(
    page.getByText('Accepted fixture result. Confirmed position updated.'),
  ).toBeVisible();
  await expect(page.getByRole('combobox', { name: 'Move to' })).toHaveText('East crossing · M-6');
  await page.getByRole('button', { name: 'Harrier group, armour, G-5' }).click();
  await expect(page.getByRole('combobox', { name: 'Move to' })).toHaveText('Inland ridge · H-8');
});
test('accepted result stays coherent across map, roster, inspector and ledger', async ({
  page,
}) => {
  await page.goto('/');
  const counter = page.getByRole('button', { name: 'Fox group, infantry, K-7' });
  await counter.focus();
  await page.keyboard.press('Enter');
  await page.getByRole('combobox', { name: 'Move to' }).focus();
  await page.keyboard.press('Enter');
  await page.keyboard.press('ArrowDown');
  await page.keyboard.press('Enter');
  await page.getByRole('button', { name: 'Review draft' }).focus();
  await page.keyboard.press('Enter');
  await page.getByRole('button', { name: 'Issue order' }).focus();
  await page.keyboard.press('Enter');
  await expect(page.getByRole('button', { name: 'Fox group, infantry, J-5' })).toBeVisible();
  await expect(page.locator('.inspector')).toContainText('J-5');
  await expect(page.locator('.roster-item').filter({ hasText: 'Fox group' })).toContainText('J-5');
  await expect(page.locator('.ledger-row').filter({ hasText: 'Fox group' })).toContainText('J-5');
  await page.locator('.inspector').getByRole('button', { name: 'Locate Fox group on map' }).focus();
  await page.keyboard.press('Enter');
  await expect(page.getByLabel('Map zoom')).toHaveText('150%');
  await expect
    .poll(async () => {
      const marker = await page
        .getByRole('button', { name: 'Fox group, infantry, J-5' })
        .boundingBox();
      const canvas = await page.locator('.map-canvas').boundingBox();
      if (!marker || !canvas) return Number.POSITIVE_INFINITY;
      return Math.max(
        Math.abs(marker.x + marker.width / 2 - canvas.x - canvas.width / 2),
        Math.abs(marker.y + marker.height / 2 - canvas.y - canvas.height / 2),
      );
    })
    .toBeLessThan(32);
});
test('unsupported phase explains stop and prevents issue', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Show unsupported Combat phase' }).click();
  await expect(page.getByText('Combat entry stops in this rules-lab fixture.')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Issue order' })).toHaveCount(0);
  await page.getByRole('button', { name: 'Return to movement fixture' }).click();
  await expect(page.getByRole('button', { name: 'Review draft' })).toBeVisible();
});
test('camera locates selection, fits extent and keeps roster selection available', async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/');
  await page.getByRole('button', { name: 'Osprey group, recon, M-8', exact: true }).click();
  await page.getByRole('button', { name: 'Locate Osprey group on map' }).click();
  await expect(page.getByLabel('Map zoom')).toHaveText('150%');
  await expect
    .poll(async () => {
      const marker = await page
        .getByRole('button', { name: 'Osprey group, recon, M-8', exact: true })
        .boundingBox();
      const canvas = await page.locator('.map-canvas').boundingBox();
      if (!marker || !canvas) return false;
      return (
        marker.x >= canvas.x &&
        marker.y >= canvas.y &&
        marker.x + marker.width <= canvas.x + canvas.width &&
        marker.y + marker.height <= canvas.y + canvas.height
      );
    })
    .toBe(true);
  await page.getByRole('button', { name: 'Zoom in' }).click();
  await expect(page.getByLabel('Map zoom')).toHaveText('175%');
  await page.getByRole('button', { name: 'Fit map' }).click();
  await expect(page.getByLabel('Map zoom')).toHaveText('100%');
  await page.getByRole('button', { name: 'Hide formations' }).click();
  await expect(page.locator('#formations-pane')).toBeHidden();
  await page.getByRole('button', { name: 'Show formations' }).click();
  await expect(
    page.getByRole('button', { name: 'Osprey group, recon, M-8', exact: true }),
  ).toHaveAttribute('aria-pressed', 'true');
});
test('intermediate layout starts with a collapsed roster and can expand it', async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 900 });
  await page.goto('/');
  await expect(page.locator('#formations-pane')).toBeHidden();
  await page.getByRole('button', { name: 'Show formations' }).click();
  await expect(page.locator('#formations-pane')).toBeVisible();
});
test('keyboard focus brings a clipped counter into the zoomed map', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/');
  for (let step = 0; step < 4; step++) await page.getByRole('button', { name: 'Zoom in' }).click();
  const counter = page.getByRole('button', { name: 'Tern group, infantry, O-6' });
  const canvas = page.locator('.map-canvas');
  await expect
    .poll(async () => {
      const before = await counter.boundingBox();
      const frame = await canvas.boundingBox();
      return !!before && !!frame && before.x > frame.x + frame.width;
    })
    .toBe(true);
  await counter.focus();
  await expect(counter).toBeFocused();
  await expect
    .poll(async () => {
      const marker = await counter.boundingBox();
      const map = await canvas.boundingBox();
      if (!marker || !map) return false;
      return (
        marker.x >= map.x &&
        marker.y >= map.y &&
        marker.x + marker.width <= map.x + map.width &&
        marker.y + marker.height <= map.y + map.height
      );
    })
    .toBe(true);
  await expect(page.getByRole('button', { name: 'Fox group, infantry, K-7' })).toHaveAttribute(
    'aria-pressed',
    'true',
  );
});
for (const width of [320, 375, 414, 768, 1440, 2560])
  test(`workspace fits ${width}px`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width, height: 900 });
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'The coastal exercise' })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    const overflow = await page.locator('main button, main mat-form-field').evaluateAll(
      (els) =>
        els.filter((el) => {
          const r = el.getBoundingClientRect();
          return r.left < 0 || r.right > innerWidth + 1;
        }).length,
    );
    expect(overflow).toBe(0);
    await page.screenshot({ path: testInfo.outputPath('workspace.png'), fullPage: true });
  });
test('large text and short viewport preserve controls', async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 600 });
  await page.goto('/');
  await page.addStyleTag({ content: 'html {font-size:200%}' });
  await page.getByRole('button', { name: 'Wide inspector' }).click();
  const map = await page.locator('.map-canvas').boundingBox();
  expect(map && map.width).toBeGreaterThanOrEqual(320);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  expect(
    await page.locator('main button').evaluateAll(
      (els) =>
        els.filter((el) => {
          const r = el.getBoundingClientRect();
          return r.left < 0 || r.right > innerWidth + 1;
        }).length,
    ),
  ).toBe(0);
  await page.getByRole('button', { name: 'Review draft' }).scrollIntoViewIfNeeded();
  await expect(page.getByRole('button', { name: 'Review draft' })).toBeInViewport();
});
test('320px viewport at 200% text keeps every action reachable', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 568 });
  await page.goto('/');
  await page.addStyleTag({ content: 'html {font-size:200%}' });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const clipped = await page.locator('main button').evaluateAll(
    (buttons) =>
      buttons.filter((button) => {
        const rect = button.getBoundingClientRect();
        return rect.left < 0 || rect.right > innerWidth + 1;
      }).length,
  );
  expect(clipped).toBe(0);
  await page.getByRole('button', { name: 'Review draft' }).scrollIntoViewIfNeeded();
  await expect(page.getByRole('button', { name: 'Review draft' })).toBeInViewport();
});
test('workspace has no WCAG A or AA axe violations', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'The coastal exercise' })).toBeVisible();
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
    .analyze();
  expect(results.violations).toEqual([]);
});
test('Storybook renders shared components, theme and disabled Formly pattern', async ({ page }) => {
  await page.goto(
    'http://127.0.0.1:6006/iframe.html?id=components-unit-counter--selected&viewMode=story',
  );
  await expect(page.getByRole('button', { name: 'Fox group, infantry, K-7' })).toHaveAttribute(
    'aria-pressed',
    'true',
  );
  await page.goto(
    'http://127.0.0.1:6006/iframe.html?id=patterns-draft-form--disabled&viewMode=story',
  );
  await expect(page.getByRole('button', { name: 'Review draft' })).toBeDisabled();
  await expect(page.getByRole('combobox')).toHaveAttribute('aria-disabled', 'true');
  await page.goto(
    'http://127.0.0.1:6006/iframe.html?id=material-controls--overview&viewMode=story',
  );
  await expect(page.getByRole('button', { name: 'Review order' })).toBeVisible();
  await page.getByRole('combobox').click();
  await expect(page.getByRole('option', { name: 'All visible formations' })).toBeVisible();
});
