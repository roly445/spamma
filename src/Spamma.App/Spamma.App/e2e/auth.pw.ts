import { expect, test } from '@playwright/test';

test('visitor can open the login form from the home page', async ({ page }) => {
  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Welcome to Spamma' })).toBeVisible();
  await page.getByRole('link', { name: 'Sign In' }).click();

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'Welcome to Spamma' })).toBeVisible();
  await expect(page.getByLabel('Email address')).toBeVisible();
});

test('login request gives the same confirmation for an unknown address', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Email address').fill('unknown-ui-smoke@example.test');
  await page.getByRole('button', { name: 'Send Magic Link' }).click();

  await expect(page.getByRole('heading', { name: 'Check your email' })).toBeVisible();
  await expect(page.getByText('unknown-ui-smoke@example.test')).toBeVisible();
});

test('invalid magic link gives a recovery path', async ({ page }) => {
  await page.goto('/logging-in');

  await expect(page.getByText('The authentication link is invalid or has expired.')).toBeVisible();
  await page.getByRole('link', { name: 'Go to Login' }).click();
  await expect(page).toHaveURL(/\/login$/);
});

test('setup login is unavailable after setup completes', async ({ page }) => {
  await page.goto('/setup-login');

  await expect(page).toHaveURL('/');
  await expect(page.getByRole('link', { name: 'Sign In' })).toBeVisible();
});

test('anonymous visitors cannot open the inbox', async ({ page }) => {
  await page.goto('/m/inbox');

  await expect(page).toHaveURL(/\/login(?:\?.*)?$/);
  await expect(page.getByLabel('Email address')).toBeVisible();
});
