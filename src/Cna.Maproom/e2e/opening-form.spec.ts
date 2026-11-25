import { expect, test } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

const story = (id: string) => `http://127.0.0.1:6006/iframe.html?id=${id}&viewMode=story`;

for (const id of [
  'patterns-opening-form--string-choices',
  'patterns-opening-form--object-offers',
  'patterns-opening-form--no-choices',
  'patterns-opening-form--rule-not-implemented',
]) {
  test(`generic opening form has no WCAG A/AA violations: ${id}`, async ({ page }) => {
    await page.goto(story(id));
    await expect(page.getByRole('group', { name: 'Legal choices' })).toBeVisible();
    const results = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
      .analyze();
    expect(results.violations).toEqual([]);
  });
}

test('opening form is keyboard operable and never exposes action refs', async ({ page }) => {
  await page.goto(story('patterns-opening-form--string-choices'));
  const submit = page.getByRole('button', { name: 'Submit choice' });
  await expect(submit).toBeDisabled();
  await page.getByRole('radio', { name: 'act-first' }).focus();
  await page.keyboard.press('ArrowDown');
  await expect(page.getByRole('radio', { name: 'act-last' })).toBeChecked();
  await expect(submit).toBeEnabled();
  expect(await page.locator('body').innerText()).not.toMatch(/\br[12]\b/);
});

test('rule-not-implemented is announced in a live region', async ({ page }) => {
  await page.goto(story('patterns-opening-form--rule-not-implemented'));
  await expect(page.getByRole('status')).toContainText('not built yet');
});
