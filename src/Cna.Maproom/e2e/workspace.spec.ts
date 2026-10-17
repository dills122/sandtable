import { expect, test } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
test('selection stays coherent across map, inspector and ledger; draft review never moves a unit', async ({
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
  await expect(page.getByRole('status')).toContainText('Harrier group → M-6');
  await expect(
    page.getByRole('button', { name: 'Harrier group, armour, G-5', exact: true }),
  ).toHaveAttribute('aria-pressed', 'true');
  await page.getByRole('button', { name: 'Fox group, infantry, K-7', exact: true }).click();
  await expect(page.getByRole('status')).toBeEmpty();
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
  await expect(page.getByRole('status')).toContainText('Draft reviewed');
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
      if (!marker || !canvas) return Number.POSITIVE_INFINITY;
      return Math.max(
        Math.abs(marker.x + marker.width / 2 - canvas.x - canvas.width / 2),
        Math.abs(marker.y + marker.height / 2 - canvas.y - canvas.height / 2),
      );
    })
    .toBeLessThan(32);
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
