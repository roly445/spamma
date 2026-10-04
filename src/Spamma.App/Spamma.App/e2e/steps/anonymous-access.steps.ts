import { expect } from '@playwright/test';
import { createBdd } from 'playwright-bdd';

const { Given, When, Then } = createBdd();

Given('I am visiting Spamma anonymously', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Welcome to Spamma' })).toBeVisible();
});

Given('I am on the login page', async ({ page }) => {
  await page.goto('/login');
  await expect(page.getByLabel('Email address')).toBeVisible();
});

Given('I follow a login link without a token', async ({ page }) => {
  await page.goto('/logging-in');
});

Given('initial setup has completed', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('link', { name: 'Sign In' })).toBeVisible();
});

When('I choose to sign in', async ({ page }) => {
  await page.getByRole('link', { name: 'Sign In' }).click();
});

When('I request a magic link for an unregistered address', async ({ page }) => {
  await page.getByLabel('Email address').fill('unknown-ui-smoke@example.test');
  await page.getByRole('button', { name: 'Send Magic Link' }).click();
});

When('I choose to return to login', async ({ page }) => {
  await page.getByRole('link', { name: 'Go to Login' }).click();
});

When('I try to open the setup login page', async ({ page }) => {
  await page.goto('/setup-login');
});

When('I try to open the inbox', async ({ page }) => {
  await page.goto('/m/inbox');
});

Then('I see the login form', async ({ page }) => {
  await expect(page).toHaveURL(/\/login(?:\?.*)?$/);
  await expect(page.getByLabel('Email address')).toBeVisible();
});

Then('I see a generic check-your-email confirmation', async ({ page }) => {
  await expect(page.getByRole('heading', { name: 'Check your email' })).toBeVisible();
  await expect(page.getByText('unknown-ui-smoke@example.test')).toBeVisible();
});

Then('I am told the link is invalid or expired', async ({ page }) => {
  await expect(page.getByText('The authentication link is invalid or has expired.')).toBeVisible();
});

Then('I am returned to the home page', async ({ page }) => {
  await expect(page).toHaveURL('/');
  await expect(page.getByRole('link', { name: 'Sign In' })).toBeVisible();
});
