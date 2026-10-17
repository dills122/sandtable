import { expect, test } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test('ledger filters, empties, sorts, and preserves map selection', async ({ page }) => {
  await page.goto('/');
  const ledger = page.getByRole('table', { name: 'Formation ledger' });
  const search = page.getByRole('searchbox', { name: 'Find a formation' });
  await search.fill('recon');
  await expect(ledger.getByRole('row')).toHaveCount(3);
  await ledger.getByRole('button', { name: 'Osprey group' }).click();
  await expect(page.getByRole('heading', { name: 'Osprey group' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Osprey group, recon, M-8' })).toHaveAttribute(
    'aria-pressed',
    'true',
  );
  await search.fill('no such unit');
  await expect(ledger).toContainText('No visible formations match this search.');
  await search.fill('');
  await ledger.getByRole('button', { name: 'Position' }).click();
  await expect(ledger.getByRole('columnheader', { name: 'Position' })).toHaveAttribute(
    'aria-sort',
    'ascending',
  );
  await expect(ledger.locator('tbody tr').first()).toContainText('E-8');
  await ledger.getByRole('button', { name: 'Position' }).click();
  await expect(ledger.locator('tbody tr').first()).toContainText('O-6');
});

test('ledger keyboard and expanded records keep labeled fields', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 700 });
  await page.goto('/');
  const ledger = page.getByRole('table', { name: 'Formation ledger' });
  await page.getByRole('button', { name: 'Expand records' }).click();
  await expect(page.locator('.records-grid')).toHaveClass(/records-expanded/);
  const name = ledger.getByRole('button', { name: 'Tern group' });
  await name.focus();
  await page.keyboard.press('Enter');
  await expect(name).toHaveAttribute('aria-pressed', 'true');
  await expect(page.getByRole('heading', { name: 'Tern group' })).toBeVisible();
  await expect(ledger.getByRole('row', { name: /Tern group/ }).getByRole('cell')).toHaveCount(3);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});

test('accepted fixture updates diary, ledger, map and locate; rejected result adds no entry', async ({
  page,
}) => {
  await page.goto('/');
  await page.getByRole('combobox', { name: 'Move to' }).click();
  await page.getByRole('option', { name: 'East crossing · M-6' }).click();
  await page.getByRole('button', { name: 'Review draft' }).click();
  await page.getByRole('button', { name: 'Issue order' }).click();
  await expect(page.getByRole('button', { name: 'Fox group, infantry, M-6' })).toBeVisible();
  await expect(page.getByRole('row', { name: /Fox group/ })).toContainText('accepted');
  await expect(page.locator('.diary-entry')).toContainText('Fox group moved to M-6');
  await page.locator('.roster-item').filter({ hasText: 'Harrier group' }).click();
  await expect(page.locator('.diary-entry')).toHaveCount(0);
  await page.getByRole('combobox', { name: 'Show entries for' }).selectOption('all');
  await expect(page.locator('.diary-entry')).toHaveCount(1);
  await page.getByRole('button', { name: 'Locate formation on map' }).click();
  await expect(page.getByRole('heading', { name: 'Fox group' })).toBeVisible();
  await expect(page.getByLabel('Map zoom')).toHaveText('150%');
  await page.locator('.roster-item').filter({ hasText: 'Harrier group' }).click();
  await page.getByRole('combobox', { name: 'Move to' }).click();
  await page.getByRole('option', { name: 'Inland ridge · H-8' }).click();
  await page.getByRole('button', { name: 'Review draft' }).click();
  await page.getByLabel('Fixture result').selectOption('rejected');
  await page.getByRole('button', { name: 'Issue order' }).click();
  await expect(page.getByRole('row', { name: /Harrier group/ })).toContainText('rejected');
  await expect(page.locator('.diary-entry')).toHaveCount(1);
});

test('narrow records remain readable at 200% and pass WCAG A/AA axe', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 568 });
  await page.goto('/');
  await page.addStyleTag({ content: 'html {font-size:200%}' });
  const ledger = page.getByRole('table', { name: 'Formation ledger' });
  await expect(ledger.getByRole('columnheader')).toHaveCount(4);
  await expect(ledger.getByRole('row', { name: /Fox group/ }).getByRole('cell')).toHaveCount(3);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
    .analyze();
  expect(results.violations).toEqual([]);
});
