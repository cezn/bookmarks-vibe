import { expect, type APIRequestContext, type Page } from "@playwright/test";
import { execSync } from "node:child_process";

export const APP_URL = "http://localhost:5005";

/**
 * Resolve the MailHog web UI base URL.
 * Priority: MAILHOG_URL env var > docker port discovery > localhost:8025.
 */
export function getMailhogUrl(): string {
  if (process.env.MAILHOG_URL) return process.env.MAILHOG_URL;
  try {
    const ports = execSync("docker ps --format '{{.Ports}}'", {
      encoding: "utf8",
    });
    const match = ports.match(/:([0-9]+)->8025/);
    if (match) return `http://localhost:${match[1]}`;
  } catch {
    // docker not available; fall through to default
  }
  return "http://localhost:8025";
}

/**
 * Decode a quoted-printable string (e.g. `=3D` -> `=`, soft line breaks).
 */
export function decodeQuotedPrintable(input: string): string {
  return input
    .replace(/=\r?\n/g, "")
    .replace(/=([0-9A-Fa-f]{2})/g, (_, hex) =>
      String.fromCharCode(parseInt(hex, 16)),
    );
}

/**
 * Fetch the confirmation link for the given email from MailHog.
 * The app uses a real SMTP sender (MailHog), so the "Click here to confirm
 * your account" link is emailed instead of rendered on the page.
 */
export async function getConfirmationLink(
  request: APIRequestContext,
  email: string,
): Promise<string> {
  const mailhogUrl = getMailhogUrl();
  const deadline = Date.now() + 15_000;
  for (;;) {
    const res = await request.get(`${mailhogUrl}/api/v2/messages`);
    expect(res.ok()).toBeTruthy();
    const data = await res.json();
    const message = (data.items ?? []).find(
      (m: { To: { Mailbox: string; Domain: string }[] }) =>
        m.To.some((t) => `${t.Mailbox}@${t.Domain}` === email),
    );
    if (message) {
      const body: string = decodeQuotedPrintable(message.Content.Body);
      const match = body.match(/href=["']?([^"'\s>]*ConfirmEmail[^"'\s>]*)/);
      if (match) {
        // The link is HTML-encoded in the email body.
        return match[1].replace(/&amp;/g, "&");
      }
    }
    if (Date.now() > deadline) {
      throw new Error(
        `No confirmation email for ${email} found in MailHog at ${mailhogUrl}`,
      );
    }
    await new Promise((r) => setTimeout(r, 1_000));
  }
}

/**
 * Register a new user, confirm the account via the MailHog email, and log in.
 *
 * Note: wait for `networkidle` before filling the Blazor forms — otherwise a
 * re-render can reset the inputs to server state mid-typing.
 */
export async function registerAndLogin(
  page: Page,
  request: APIRequestContext,
): Promise<{ email: string; password: string }> {
  const uniqueId = Date.now();
  const email = `test-${uniqueId}@example.com`;
  const password = "TestPass123!";

  // Register new user
  await page.goto(APP_URL);
  await page.getByRole("link", { name: "Register as a new user" }).click();
  await page.waitForLoadState("networkidle");
  await page.getByRole("textbox", { name: "Email" }).fill(email);
  await page
    .getByRole("textbox", { name: "Password", exact: true })
    .fill(password);
  await page.getByRole("textbox", { name: "Confirm password" }).fill(password);
  const registerButton = page.getByRole("button", { name: "Register" });
  await registerButton.waitFor({ state: "visible" });
  await registerButton.click();

  // Confirm account using the link from the email in MailHog
  const confirmLink = await getConfirmationLink(request, email);
  await page.goto(confirmLink);

  // Login
  await page.goto(APP_URL);
  await page.waitForLoadState("networkidle");
  await page.getByRole("textbox", { name: "Email" }).fill(email);
  await page
    .getByRole("textbox", { name: "Password", exact: true })
    .fill(password);
  await page.getByRole("button", { name: "Log in", exact: true }).click();

  // Should land on the app (no longer on the login page)
  await page.waitForURL(
    (url) => !url.pathname.toLowerCase().includes("login"),
    { timeout: 15_000 },
  );

  return { email, password };
}
