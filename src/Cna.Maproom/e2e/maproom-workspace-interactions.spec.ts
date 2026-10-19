import { expect, test } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test('drag keeps the selected zoom while panning without uncovering blank space', async ({
  page,
}) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/');
  const map = page.locator('.map-canvas');
  await map.scrollIntoViewIfNeeded();
  const scene = page.locator('.map-scene');
  const frame = await map.boundingBox();
  expect(frame).not.toBeNull();
  const from = { x: frame!.x + frame!.width * 0.15, y: frame!.y + frame!.height * 0.5 };
  await page.mouse.click(from.x, from.y);
  await expect(page.getByLabel('Map zoom')).toHaveText('100%');
  await page.getByRole('button', { name: 'Zoom in' }).click();
  await page.getByRole('button', { name: 'Zoom in' }).click();
  await expect(page.getByLabel('Map zoom')).toHaveText('150%');
  await page.mouse.move(from.x, from.y);
  await page.mouse.down();
  await page.mouse.move(from.x + 100, from.y - 60, { steps: 6 });
  await page.mouse.up();
  await expect(page.getByLabel('Map zoom')).toHaveText('150%');
  await expect
    .poll(async () => (await scene.boundingBox())?.x ?? 0)
    .toBeGreaterThan(frame!.x - frame!.width * 0.25 + 20);
  const terrain = await scene.boundingBox();
  expect(terrain).not.toBeNull();
  expect(terrain!.x).toBeLessThanOrEqual(frame!.x + 1);
  expect(terrain!.y).toBeLessThanOrEqual(frame!.y + 1);
  expect(terrain!.x + terrain!.width).toBeGreaterThanOrEqual(frame!.x + frame!.width - 1);
  expect(terrain!.y + terrain!.height).toBeGreaterThanOrEqual(frame!.y + frame!.height - 1);
  await expect(page.getByRole('heading', { name: 'Fox group' })).toBeVisible();
  await page.getByRole('button', { name: 'Harrier group, armour, G-5' }).click();
  await expect(page.getByRole('heading', { name: 'Harrier group' })).toBeVisible();
  await page.getByRole('button', { name: 'Fit map' }).click();
  await expect(page.getByLabel('Map zoom')).toHaveText('100%');
  await page.mouse.move(from.x, from.y);
  await page.mouse.down();
  await page.mouse.move(from.x + 100, from.y - 60, { steps: 6 });
  await page.mouse.up();
  await expect(page.getByLabel('Map zoom')).toHaveText('100%');
  await expect(page.getByRole('heading', { name: 'Harrier group' })).toBeVisible();
});

test('expanded map traps focus, keeps controls and selection, and returns to orders', async ({
  page,
}, testInfo) => {
  await page.goto('/');
  await page.getByRole('combobox', { name: 'Move to' }).click();
  await page.getByRole('option', { name: 'East crossing · M-6' }).click();
  const expand = page.getByRole('button', { name: 'Expand map' });
  await expand.click();
  const dialog = page.getByRole('dialog', { name: 'Situation map' });
  await expect(dialog).toBeVisible();
  await expect(page.locator('html')).toHaveCSS('overflow', 'hidden');
  await expect(page.getByRole('button', { name: 'Close expanded map' })).toBeFocused();
  await page.screenshot({ path: testInfo.outputPath('expanded-map-desktop.png') });
  await page.keyboard.press('Shift+Tab');
  await expect(dialog.getByRole('button', { name: 'Open orders for Fox group' })).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('button', { name: 'Close expanded map' })).toBeFocused();
  await dialog.getByRole('button', { name: 'Zoom in' }).click();
  await expect(page.getByLabel('Map zoom')).toHaveText('125%');
  await dialog.getByRole('button', { name: 'Harrier group, armour, G-5' }).click();
  await expect(dialog.getByText('Selected: Harrier group · G-5')).toBeVisible();
  await dialog.getByRole('button', { name: 'Open orders for Harrier group' }).click();
  await expect(dialog).toHaveCount(0);
  await expect(page.locator('html')).not.toHaveCSS('overflow', 'hidden');
  await expect(page.getByRole('heading', { name: 'Harrier group' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Review draft' })).toBeFocused();
  await page.getByRole('button', { name: 'Fox group, infantry, K-7' }).click();
  await expect(page.getByRole('combobox', { name: 'Move to' })).toContainText(
    'East crossing · M-6',
  );
  await expect(page.getByLabel('Map zoom')).toHaveText('125%');
  await expand.click();
  await expect(dialog).toBeVisible();
  await expect(page.getByLabel('Map zoom')).toHaveText('125%');
  await page.keyboard.press('Escape');
  await expect(dialog).toHaveCount(0);
  await expect(expand).toBeFocused();
  await expect(page.locator('html')).not.toHaveCSS('overflow', 'hidden');
  await expect(page.getByLabel('Map zoom')).toHaveText('125%');
});

test('hex overlay keeps equal axes in normal and expanded map views', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/');
  const grid = page.locator('.hex-grid');
  const axisScales = () =>
    grid.evaluate((element) => {
      const transform = (element as SVGSVGElement).getScreenCTM();
      return transform ? { x: transform.a, y: transform.d } : null;
    });
  const normal = await axisScales();
  expect(normal).not.toBeNull();
  expect(Math.abs(normal!.x - normal!.y)).toBeLessThan(0.01);

  await page.getByRole('button', { name: 'Expand map' }).click();
  const expanded = await axisScales();
  expect(expanded).not.toBeNull();
  expect(Math.abs(expanded!.x - expanded!.y)).toBeLessThan(0.01);
});

test('War Diary and fixture result open themed Field Atlas menus with keyboard support', async ({
  page,
}, testInfo) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/');
  const diary = page.getByRole('combobox', { name: 'Show entries for' });
  await page.getByText('Show entries for', { exact: true }).click();
  await expect(diary).toBeFocused();
  const menu = page.locator('.fa-choice-panel');
  await expect(menu).toBeVisible();
  const usesPanelToken = await menu.evaluate((element) => {
    const sample = document.createElement('div');
    sample.style.background = 'var(--color-panel)';
    document.body.append(sample);
    const matches =
      getComputedStyle(element).backgroundColor === getComputedStyle(sample).backgroundColor;
    sample.remove();
    return matches;
  });
  expect(usesPanelToken).toBe(true);
  await expect(menu.getByRole('option', { name: 'All visible formations' })).toBeVisible();
  expect(
    await menu
      .getByRole('option', { name: 'All visible formations' })
      .evaluate((element) => parseFloat(getComputedStyle(element).minHeight)),
  ).toBeGreaterThanOrEqual(44);
  await menu.evaluate(async (element) => {
    await Promise.all(
      element.getAnimations({ subtree: true }).map((animation) => animation.finished),
    );
  });
  await page.screenshot({ path: testInfo.outputPath('war-diary-open-menu.png'), fullPage: true });
  await page.keyboard.press('Escape');
  await expect(diary).toBeFocused();
  const diaryLabel = page.getByRole('button', { name: 'Show entries for' });
  await diaryLabel.focus();
  await page.keyboard.press('Enter');
  await expect(menu).toBeVisible();
  await expect(diary).toBeFocused();
  await page.keyboard.press('Escape');
  await page.keyboard.press('Enter');
  await page.keyboard.press('End');
  await page.keyboard.press('Enter');
  await expect(diary).toContainText('All visible formations');
  await page.getByRole('combobox', { name: 'Move to' }).click();
  await page.getByRole('option', { name: 'East crossing · M-6' }).click();
  await page.getByRole('button', { name: 'Review draft' }).click();
  const outcome = page.getByRole('combobox', { name: 'Fixture result' });
  await page.getByText('Fixture result', { exact: true }).click();
  await expect(outcome).toBeFocused();
  await expect(menu.getByRole('option', { name: 'Lost reply' })).toBeVisible();
  await menu.getByRole('option', { name: 'Rejected' }).click();
  await expect(outcome).toContainText('Rejected');
});

test('choice menu stays in a narrow viewport and preserves forced-color contrast', async ({
  page,
}) => {
  await page.setViewportSize({ width: 320, height: 568 });
  await page.goto('/');
  const diary = page.getByRole('combobox', { name: 'Show entries for' });
  await diary.click();
  const menu = page.locator('.fa-choice-panel');
  const bounds = await menu.boundingBox();
  expect(bounds).not.toBeNull();
  expect(bounds!.x).toBeGreaterThanOrEqual(0);
  expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(320);
  await menu.getByRole('option', { name: 'All visible formations' }).click();
  await expect(diary).toContainText('All visible formations');
  await expect(menu).toBeHidden();
  await page.emulateMedia({ forcedColors: 'active' });
  await diary.click();
  await expect(menu).toBeVisible();
  await expect(menu).toHaveCSS('box-shadow', 'none');
  await expect(diary).toHaveAttribute('aria-expanded', 'true');
  await expect(menu.getByRole('option', { name: 'All visible formations' })).toHaveAttribute(
    'aria-selected',
    'true',
  );
});

test('touch opens and selects the War Diary choice list', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 700 });
  await page.goto('/');
  const client = await page.context().newCDPSession(page);
  const tap = async (x: number, y: number) => {
    await client.send('Input.dispatchTouchEvent', {
      type: 'touchStart',
      touchPoints: [{ x, y }],
    });
    await client.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  };
  const diary = page.getByRole('combobox', { name: 'Show entries for' });
  await diary.scrollIntoViewIfNeeded();
  const trigger = await diary.boundingBox();
  expect(trigger).not.toBeNull();
  await tap(trigger!.x + trigger!.width / 2, trigger!.y + trigger!.height / 2);
  const all = page.locator('.fa-choice-panel').getByRole('option', {
    name: 'All visible formations',
  });
  await expect(all).toBeVisible();
  const option = await all.boundingBox();
  expect(option).not.toBeNull();
  await tap(option!.x + option!.width / 2, option!.y + option!.height / 2);
  await expect(diary).toContainText('All visible formations');
});

test('disabled Field Atlas choice label is not a keyboard stop or an open action', async ({
  page,
}) => {
  await page.goto(
    'http://127.0.0.1:6006/iframe.html?id=foundations-choice-select--states&viewMode=story',
  );
  const disabled = page.locator('fa-choice-select').filter({ hasText: 'Unavailable scope' });
  const label = disabled.getByRole('button', { name: 'Unavailable scope' });
  await expect(label).toHaveAttribute('tabindex', '-1');
  await expect(label).toHaveAttribute('aria-disabled', 'true');
  await expect(disabled.getByRole('combobox')).toHaveAttribute('aria-disabled', 'true');
  await label.dispatchEvent('click');
  await expect(page.locator('.fa-choice-panel')).toHaveCount(0);
  await page.getByRole('combobox', { name: 'Narrow container' }).focus();
  await page.keyboard.press('Tab');
  await expect(label).not.toBeFocused();
  await expect(disabled.getByRole('combobox')).not.toBeFocused();
});

test('Open orders restores focus in inspector when Combat fixture has no order action', async ({
  page,
}) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Show unsupported Combat phase' }).click();
  await page.getByRole('button', { name: 'Hide orders' }).click();
  await page.getByRole('button', { name: 'Expand map' }).click();
  await page
    .getByRole('dialog', { name: 'Situation map' })
    .getByRole('button', { name: 'Open orders for Fox group' })
    .click();
  await expect(page.getByRole('dialog', { name: 'Situation map' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Show orders' })).toHaveCount(0);
  await expect(page.getByText('Combat entry stops in this rules-lab fixture.')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Locate Fox group on map' })).toBeFocused();
});

test('touch drag pans map without scrolling page while outside-map touch still scrolls', async ({
  page,
}) => {
  await page.setViewportSize({ width: 375, height: 700 });
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('/');
  const map = page.locator('.map-canvas');
  await map.scrollIntoViewIfNeeded();
  await page.getByRole('button', { name: 'Zoom in' }).click();
  await page.getByRole('button', { name: 'Zoom in' }).click();
  await expect(page.getByLabel('Map zoom')).toHaveText('150%');
  const box = await map.boundingBox();
  expect(box).not.toBeNull();
  const client = await page.context().newCDPSession(page);
  const x = Math.round(box!.x + box!.width * 0.1);
  const y = Math.round(box!.y + box!.height * 0.5);
  const initialScroll = await page.evaluate(() => scrollY);
  await client.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x, y }] });
  for (let step = 1; step <= 5; step++) {
    await client.send('Input.dispatchTouchEvent', {
      type: 'touchMove',
      touchPoints: [{ x: x + step * 12, y: y - step * 12 }],
    });
  }
  await client.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  await expect(page.getByLabel('Map zoom')).toHaveText('150%');
  expect(await page.evaluate(() => scrollY)).toBe(initialScroll);
  await page.evaluate(() => scrollTo(0, 0));
  await client.send('Input.dispatchTouchEvent', {
    type: 'touchStart',
    touchPoints: [{ x: 180, y: 150 }],
  });
  await client.send('Input.dispatchTouchEvent', {
    type: 'touchMove',
    touchPoints: [{ x: 180, y: 40 }],
  });
  await client.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  await expect.poll(async () => page.evaluate(() => scrollY)).toBeGreaterThan(0);
});

test('expanded map fills a narrow viewport and remains keyboard and screen-reader accessible', async ({
  page,
}, testInfo) => {
  await page.setViewportSize({ width: 375, height: 700 });
  await page.goto('/');
  await page.getByRole('button', { name: 'Expand map' }).click();
  const dialog = page.getByRole('dialog', { name: 'Situation map' });
  const rect = await dialog.boundingBox();
  expect(rect).not.toBeNull();
  expect(rect!.width).toBe(375);
  expect(rect!.height).toBe(700);
  await expect(dialog.locator('.map-canvas')).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
    .analyze();
  expect(results.violations).toEqual([]);
  await page.screenshot({ path: testInfo.outputPath('expanded-map-mobile.png') });
  await page.keyboard.press('Escape');
  await expect(dialog).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Expand map' })).toBeFocused();
});

test('sidebar controls stay in their headers and order work remains reachable on narrow screens', async ({
  page,
}) => {
  await page.setViewportSize({ width: 375, height: 700 });
  await page.goto('/');
  const roster = page.locator('#formations-pane');
  const inspector = page.locator('.inspector');
  const hideRoster = roster.getByRole('button', { name: 'Hide formations' });
  await expect(roster.locator('.roster-heading')).toContainText('Formations');
  await hideRoster.click();
  await expect(roster.getByRole('button', { name: 'Show formations' })).toHaveAttribute(
    'aria-expanded',
    'false',
  );
  await expect(roster.locator('.roster-list')).toBeHidden();
  await roster.getByRole('button', { name: 'Show formations' }).click();
  await expect(roster.locator('.roster-list')).toBeVisible();
  await inspector.getByRole('button', { name: 'Hide orders' }).click();
  await expect(inspector.getByRole('button', { name: 'Show orders' })).toHaveAttribute(
    'aria-expanded',
    'false',
  );
  await expect(inspector.locator('#inspector-body')).toBeHidden();
  await inspector.getByRole('button', { name: 'Show orders' }).click();
  await expect(inspector.getByRole('button', { name: 'Review draft' })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});
