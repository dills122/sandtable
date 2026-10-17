import { expect, test } from '@playwright/test';

test('skip link reaches operations and roster, map, and ledger share selection', async ({
  page,
}) => {
  await page.goto('/');
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Skip to operations' })).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page.locator('#operations')).toBeFocused();

  const roster = page.locator('.roster-item').filter({ hasText: 'Kestrel group' });
  await roster.focus();
  await page.keyboard.press('Space');
  await expect(roster).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByRole('heading', { name: 'Kestrel group' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Kestrel group, infantry, E-8' })).toHaveAttribute(
    'aria-pressed',
    'true',
  );
  await expect(page.locator('.ledger-row.is-selected')).toContainText('Kestrel group');

  const ledger = page.getByRole('table', { name: 'Formation ledger' });
  const swift = ledger.getByRole('button', { name: 'Swift group' });
  await swift.focus();
  await page.keyboard.press('Enter');
  await expect(swift).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByRole('heading', { name: 'Swift group' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Swift group, recon, J-3' })).toHaveAttribute(
    'aria-pressed',
    'true',
  );
  await expect(page.locator('.map-caption')).toContainText('Swift group · J-3');
});

test('zoom bounds, keyboard camera controls, and fit preserve selected formation', async ({
  page,
}) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Heron group, armour, L-4' }).click();
  const zoomIn = page.getByRole('button', { name: 'Zoom in' });
  const zoomOut = page.getByRole('button', { name: 'Zoom out' });
  await expect(zoomOut).toBeDisabled();
  for (const level of ['125%', '150%', '175%', '200%']) {
    await zoomIn.focus();
    await page.keyboard.press('Enter');
    await expect(page.getByLabel('Map zoom')).toHaveText(level);
  }
  await expect(zoomIn).toBeDisabled();
  await page.getByRole('button', { name: 'Locate Heron group on map' }).click();
  await expect(page.getByLabel('Map zoom')).toHaveText('200%');
  await page.getByRole('button', { name: 'Fit map' }).focus();
  await page.keyboard.press('Space');
  await expect(page.getByLabel('Map zoom')).toHaveText('100%');
  await expect(zoomOut).toBeDisabled();
  await expect(page.getByRole('button', { name: 'Heron group, armour, L-4' })).toHaveAttribute(
    'aria-pressed',
    'true',
  );
});

test('reviewed destination changes invalidate issue until reviewed again', async ({ page }) => {
  await page.goto('/');
  const destination = page.getByRole('combobox', { name: 'Move to' });
  await destination.click();
  await page.getByRole('option', { name: 'East crossing · M-6' }).click();
  await page.getByRole('button', { name: 'Review draft' }).click();
  await expect(page.getByRole('group', { name: 'Reviewed order' })).toContainText('move to M-6');
  await destination.click();
  await page.getByRole('option', { name: 'Inland ridge · H-8' }).click();
  await expect(page.getByRole('button', { name: 'Issue order' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Fox group, infantry, K-7' })).toBeVisible();
  await page.getByRole('button', { name: 'Review draft' }).click();
  await expect(page.getByRole('group', { name: 'Reviewed order' })).toContainText('move to H-8');
  await page.getByRole('button', { name: 'Issue order' }).click();
  await expect(page.getByRole('button', { name: 'Fox group, infantry, H-8' })).toBeVisible();
});

test('narrow map retains touch-size counter targets and selectable map alternative', async ({
  page,
}) => {
  await page.setViewportSize({ width: 320, height: 568 });
  await page.goto('/');
  const map = page.locator('.map-canvas');
  await expect(map).toBeVisible();
  const counters = map.getByRole('button');
  await expect(counters).toHaveCount(8);
  const sizes = await counters.evaluateAll((buttons) =>
    buttons.map((button) => {
      const rect = button.getBoundingClientRect();
      return { width: rect.width, height: rect.height };
    }),
  );
  for (const size of sizes) {
    expect(size.width).toBeGreaterThanOrEqual(44);
    expect(size.height).toBeGreaterThanOrEqual(44);
  }
  const tern = page.getByRole('button', { name: 'Tern group, infantry, O-6' });
  await tern.click();
  await expect(tern).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByRole('heading', { name: 'Tern group' })).toBeVisible();
  await expect(page.locator('.ledger-row.is-selected')).toContainText('Tern group');
});

test('short mobile screen can reach order controls after selecting through ledger', async ({
  page,
}) => {
  await page.setViewportSize({ width: 375, height: 480 });
  await page.goto('/');
  await page
    .getByRole('table', { name: 'Formation ledger' })
    .getByRole('button', { name: 'Merlin group' })
    .click();
  await expect(page.getByRole('heading', { name: 'Merlin group' })).toBeVisible();
  const review = page.getByRole('button', { name: 'Review draft' });
  await review.scrollIntoViewIfNeeded();
  await expect(review).toBeInViewport();
  await expect(page.getByRole('combobox', { name: 'Move to' })).toBeEnabled();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});
