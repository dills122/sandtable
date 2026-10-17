import { expect, test } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test('discoverable shortcuts select, locate, zoom and fit without changing the order draft', async ({
  page,
}) => {
  await page.goto('/');
  const destination = page.getByRole('combobox', { name: 'Move to' });
  await destination.click();
  await page.getByRole('option', { name: 'East crossing · M-6' }).click();
  const help = page.getByRole('button', { name: 'Keyboard help' });
  await help.click();
  await expect(page.locator('#map-shortcuts')).toBeVisible();
  await page.keyboard.press(']');
  await expect(page.getByRole('heading', { name: 'Harrier group' })).toBeVisible();
  await page.keyboard.press('l');
  await expect(page.getByLabel('Map zoom')).toHaveText('150%');
  await page.keyboard.press('+');
  await expect(page.getByLabel('Map zoom')).toHaveText('175%');
  await page.keyboard.press('0');
  await expect(page.getByLabel('Map zoom')).toHaveText('100%');
  await page.keyboard.press('[');
  await expect(page.getByRole('heading', { name: 'Fox group' })).toBeVisible();
  await expect(destination).toContainText('East crossing · M-6');
  await page.keyboard.press('Escape');
  await expect(page.locator('#map-shortcuts')).toBeHidden();
  await expect(help).toBeFocused();
  await page.keyboard.press('?');
  await expect(page.locator('#map-shortcuts')).toBeVisible();
});

test('shortcut keys stay with an order field and its open options', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Keyboard help' }).click();
  const destination = page.getByRole('combobox', { name: 'Move to' });
  await destination.focus();
  await page.keyboard.press('l');
  await expect(page.getByLabel('Map zoom')).toHaveText('100%');
  await page.keyboard.press(']');
  await expect(page.getByRole('heading', { name: 'Fox group' })).toBeVisible();
  await destination.click();
  await expect(page.getByRole('option', { name: 'East crossing · M-6' })).toBeVisible();
  await page.keyboard.press('?');
  await expect(page.locator('#map-shortcuts')).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('option', { name: 'East crossing · M-6' })).toHaveCount(0);
  await expect(page.locator('#map-shortcuts')).toBeVisible();
});

test('map arrow keys and pointer drag pan within the extent while selection remains stable', async ({
  page,
}) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto('/');
  const map = page.getByRole('region', { name: 'Situation map, arrow keys pan when zoomed' });
  const scene = page.locator('.map-scene');
  await page.getByRole('button', { name: 'Zoom in' }).click();
  await page.getByRole('button', { name: 'Zoom in' }).click();
  const before = await scene.boundingBox();
  expect(before).not.toBeNull();
  await map.focus();
  const guarded = await map.evaluate((element) =>
    [
      { altKey: true },
      { ctrlKey: true },
      { metaKey: true },
      { isComposing: true },
      { handled: true },
    ].map(({ handled, ...options }) => {
      const event = new KeyboardEvent('keydown', {
        key: 'ArrowLeft',
        bubbles: true,
        cancelable: true,
        ...options,
      });
      if (handled) event.preventDefault();
      element.dispatchEvent(event);
      return event.defaultPrevented;
    }),
  );
  expect(guarded).toEqual([false, false, false, false, true]);
  await expect.poll(async () => (await scene.boundingBox())?.x ?? 0).toBeCloseTo(before!.x, 1);
  await page.keyboard.press('ArrowLeft');
  await expect
    .poll(async () => (await scene.boundingBox())?.x ?? 0)
    .toBeGreaterThan(before!.x + 20);
  const afterKey = await scene.boundingBox();
  const box = await map.boundingBox();
  expect(box).not.toBeNull();
  await page.mouse.move(box!.x + box!.width * 0.2, box!.y + box!.height * 0.85);
  await page.mouse.down();
  await page.mouse.move(box!.x + box!.width * 0.2 - 80, box!.y + box!.height * 0.85 - 40, {
    steps: 4,
  });
  await page.mouse.up();
  await expect.poll(async () => (await scene.boundingBox())?.x ?? 0).toBeLessThan(afterKey!.x - 40);
  await expect(page.getByRole('heading', { name: 'Fox group' })).toBeVisible();
  await page.getByRole('button', { name: 'Fit map' }).click();
  await expect
    .poll(async () => {
      const frame = await map.boundingBox();
      const terrain = await scene.boundingBox();
      return frame && terrain ? Math.abs(terrain.x - frame.x) : Number.POSITIVE_INFINITY;
    })
    .toBeLessThan(2);
});

test('panning after locating an edge formation moves in the requested direction and stops at a bound', async ({
  page,
}) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('/');
  await page.getByRole('button', { name: 'Kestrel group, infantry, E-8' }).click();
  await page.getByRole('button', { name: 'Locate Kestrel group on map' }).click();
  const map = page.getByRole('region', { name: 'Situation map, arrow keys pan when zoomed' });
  const scene = page.locator('.map-scene');
  const uncoveredTerrain = async () => {
    const frame = await map.boundingBox();
    const terrain = await scene.boundingBox();
    if (!frame || !terrain) return Number.POSITIVE_INFINITY;
    return Math.max(
      terrain.x - frame.x,
      terrain.y - frame.y,
      frame.x + frame.width - terrain.x - terrain.width,
      frame.y + frame.height - terrain.y - terrain.height,
    );
  };
  await expect.poll(uncoveredTerrain).toBeLessThanOrEqual(1);
  const marker = await page
    .getByRole('button', { name: 'Kestrel group, infantry, E-8' })
    .boundingBox();
  const frame = await map.boundingBox();
  expect(marker && frame).not.toBeNull();
  expect(marker!.x).toBeGreaterThanOrEqual(frame!.x);
  expect(marker!.y).toBeGreaterThanOrEqual(frame!.y);
  expect(marker!.x + marker!.width).toBeLessThanOrEqual(frame!.x + frame!.width);
  expect(marker!.y + marker!.height).toBeLessThanOrEqual(frame!.y + frame!.height);
  const locatedX = (await scene.boundingBox())!.x;
  await map.focus();
  await page.keyboard.press('ArrowRight');
  await expect.poll(async () => (await scene.boundingBox())?.x ?? 0).toBeLessThan(locatedX - 20);
  for (let step = 0; step < 50; step++) await page.keyboard.press('ArrowRight');
  const boundedX = (await scene.boundingBox())!.x;
  await page.keyboard.press('ArrowRight');
  await expect.poll(async () => (await scene.boundingBox())?.x ?? 0).toBeCloseTo(boundedX, 1);
  await expect.poll(uncoveredTerrain).toBeLessThanOrEqual(1);
  await expect(page.getByRole('heading', { name: 'Kestrel group' })).toBeVisible();
});

test('keyboard help and focused map retain WCAG A/AA coverage', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Keyboard help' }).click();
  await page.getByRole('region', { name: 'Situation map, arrow keys pan when zoomed' }).focus();
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
    .analyze();
  expect(results.violations).toEqual([]);
});
