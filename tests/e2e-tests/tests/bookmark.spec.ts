import { test as base, expect, Locator, Page } from "@playwright/test";
import { BookmarksPage } from "./BookmarksPage";
import { BookmarkCreatePage } from "./BookmarkCreatePage";
import { BookmarkEditPage } from "./BookmarkEditPage";

type MyFixtures = {
  authenticatedPage: Page;
};

const test = base.extend<MyFixtures>({
  authenticatedPage: async ({ page }, use) => {
    const uniqueId = Date.now();
    const testEmail = `test-${uniqueId}@example.com`;
    const testPassword = "TestPass123!";

    // Register new user
    await page.goto("http://localhost:5005");
    await page.getByRole("link", { name: "Register as a new user" }).click();
    await page.getByRole("textbox", { name: "Email" }).fill(testEmail);
    await page
      .getByRole("textbox", { name: "Password", exact: true })
      .fill(testPassword);
    await page
      .getByRole("textbox", { name: "Confirm Password" })
      .fill(testPassword);
    await page.getByRole("button", { name: "Register" }).click();
    // Confirm email
    await page
      .getByRole("link", { name: "Click here to confirm your account" })
      .click();

    // Login
    await page.goto("http://localhost:5005");
    await page.getByRole("textbox", { name: "Email" }).fill(testEmail);
    await page.getByRole("textbox", { name: "Password" }).fill(testPassword);
    await page.getByRole("button", { name: "Log in", exact: true }).click();

    await use(page);
  },
});

test.describe("Bookmarks CRUD", () => {
  test("should add, edit, and remove a bookmark", async ({
    authenticatedPage: page,
  }) => {
    // Generate a unique bookmark name and URLs
    const uniqueId = Date.now();
    const bookmarkTitle = `Test Bookmark ${uniqueId}`;
    const updatedTitle = `Updated Bookmark ${uniqueId}`;
    const bookmarkUrl = `https://example.com/${uniqueId}`;
    const updatedUrl = `https://updated.com/${uniqueId}`;
    const bookmarkTags = [`tag${uniqueId}`, `another${uniqueId}`];
    const updatedTags = [`updated${uniqueId}`];

    const listPage = new BookmarksPage(page);
    const createPage = new BookmarkCreatePage(page);
    const editPage = new BookmarkEditPage(page);

    // Go to bookmarks page
    await listPage.goto();

    // Add a new bookmark
    await listPage.clickCreateNew();
    await createPage.fillForm(bookmarkTitle, bookmarkUrl, bookmarkTags);
    await createPage.submit();

    // Should see the new bookmark in the list
    await expect(await listPage.getBookmarkTitle(bookmarkTitle)).toBeVisible();
    await expect(await listPage.getBookmarkUrl(bookmarkUrl)).toBeVisible();
    // Check tags
    // tags column is truncated, so check only first 30 chars
    const tagsText = await extractTagsText(
      await listPage.getBookmarkTags(bookmarkTitle)
    );
    await expect(tagsText.slice(0, 30)).toBe(
      bookmarkTags.join(", ").slice(0, 30)
    );

    // Edit the bookmark
    await listPage.clickEdit(bookmarkTitle);
    await editPage.waitForFormVisible(bookmarkTitle);
    await editPage.fillForm(updatedTitle, updatedUrl, updatedTags);
    await editPage.save();

    // Should see the updated bookmark
    await expect(await listPage.getBookmarkTitle(updatedTitle)).toBeVisible();
    await expect(await listPage.getBookmarkUrl(updatedUrl)).toBeVisible();
    // Check updated tags
    const updatedTagsText = await extractTagsText(
      await listPage.getBookmarkTags(updatedTitle)
    );
    await expect(updatedTagsText.slice(0, 30)).toBe(
      updatedTags.join(", ").slice(0, 30)
    );

    // Delete the bookmark
    await listPage.clickDelete(updatedTitle);

    // Should not see the bookmark anymore
    await expect(
      await listPage.getBookmarkTitle(updatedTitle)
    ).not.toBeVisible();
  });

  test("should handle duplicate bookmark titles", async ({
    authenticatedPage: page,
  }) => {
    const uniqueId = Date.now();
    const bookmarkTitle = `Duplicate Test ${uniqueId}`;
    const bookmarkUrl1 = `https://example1.com/${uniqueId}`;
    const bookmarkUrl2 = `https://example2.com/${uniqueId}`;
    const bookmarkTags = [`tag${uniqueId}`];

    const listPage = new BookmarksPage(page);
    const createPage = new BookmarkCreatePage(page);

    await listPage.goto();

    // Add first bookmark
    await listPage.clickCreateNew();
    await createPage.fillForm(bookmarkTitle, bookmarkUrl1, bookmarkTags);
    await createPage.submit();

    // Add second bookmark with same title but different URL
    await listPage.clickCreateNew();
    await createPage.fillForm(bookmarkTitle, bookmarkUrl2, bookmarkTags);
    await createPage.submit();

    // Both should be visible (assuming duplicates are allowed)
    await expect(await listPage.getBookmarkTitle(bookmarkTitle)).toHaveCount(2);

    // Cleanup
    await listPage.clickDelete(bookmarkTitle);
    await listPage.clickDelete(bookmarkTitle);
  });

  test("should validate bookmark creation with invalid URL", async ({
    authenticatedPage: page,
  }) => {
    const uniqueId = Date.now();
    const bookmarkTitle = `Invalid URL Test ${uniqueId}`;
    const invalidUrl = `invalid-url-${uniqueId}`;
    const bookmarkTags = [`tag${uniqueId}`];

    const listPage = new BookmarksPage(page);
    const createPage = new BookmarkCreatePage(page);

    await listPage.goto();
    await listPage.clickCreateNew();
    await createPage.fillForm(bookmarkTitle, invalidUrl, bookmarkTags);
    await createPage.submit();

    // Check for native browser validation message on the URL input
    const validationMessage = await createPage.getUrlValidationMessage();
    expect(validationMessage).toMatch(/Please enter a URL/i);
  });
});

// --- Search functionality test ---
test.describe("Bookmarks Search", () => {
  test("should filter bookmarks by title and url", async ({
    authenticatedPage: page,
  }) => {
    const uniqueId = Date.now();
    const title1 = `Alpha ${uniqueId}`;
    const url1 = `https://alpha.com/${uniqueId}`;
    const tags1 = [`alpha${uniqueId}`];
    const title2 = `Beta ${uniqueId}`;
    const url2 = `https://beta.com/${uniqueId}`;
    const tags2 = [`beta${uniqueId}`];

    const listPage = new BookmarksPage(page);
    const createPage = new BookmarkCreatePage(page);

    // Go to bookmarks page and add two bookmarks
    await listPage.goto();
    await listPage.clickCreateNew();
    await createPage.fillForm(title1, url1, tags1);
    await createPage.submit();

    await listPage.clickCreateNew();
    await createPage.fillForm(title2, url2, tags2);
    await createPage.submit();

    // Wait for both bookmarks to be visible
    await listPage.goto();
    await expect(await listPage.getBookmarkTitle(title1)).toBeVisible();
    await expect(await listPage.getBookmarkTitle(title2)).toBeVisible();

    // Check tags are visible
    const updatedTagsText = await extractTagsText(
      await listPage.getBookmarkTags(title1)
    );
    await expect(updatedTagsText.slice(0, 30)).toBe(
      tags1.join(", ").slice(0, 30)
    );

    const updatedTagsText2 = await extractTagsText(
      await listPage.getBookmarkTags(title2)
    );
    await expect(updatedTagsText2.slice(0, 30)).toBe(
      tags2.join(", ").slice(0, 30)
    );

    // Search by title1
    const searchBox = page.getByRole("textbox", { name: /search bookmarks/i });
    await searchBox.fill(title1);
    await searchBox.press("Enter");
    await expect(await listPage.getBookmarkTitle(title1)).toBeVisible();
    await expect(await listPage.getBookmarkTitle(title2)).not.toBeVisible();

    // Search by url2
    await searchBox.fill(url2);
    await searchBox.press("Enter");
    await expect(await listPage.getBookmarkTitle(title2)).toBeVisible();
    await expect(await listPage.getBookmarkTitle(title1)).not.toBeVisible();

    // Search by tag
    await searchBox.fill(tags1[0]);
    await searchBox.press("Enter");
    await expect(await listPage.getBookmarkTitle(title1)).toBeVisible();
    await expect(await listPage.getBookmarkTitle(title2)).not.toBeVisible();

    // Clear search and show all
    await searchBox.fill("");
    await searchBox.press("Enter");
    await expect(await listPage.getBookmarkTitle(title1)).toBeVisible();
    await expect(await listPage.getBookmarkTitle(title2)).toBeVisible();

    // Cleanup: delete both bookmarks
    await listPage.clickDelete(title1);
    await listPage.clickDelete(title2);
  });

  test("should handle search with no results", async ({
    authenticatedPage: page,
  }) => {
    const uniqueId = Date.now();
    const title1 = `Search Test ${uniqueId}`;
    const url1 = `https://search.com/${uniqueId}`;
    const tags1 = [`search${uniqueId}`];

    const listPage = new BookmarksPage(page);
    const createPage = new BookmarkCreatePage(page);

    await listPage.goto();
    await listPage.clickCreateNew();
    await createPage.fillForm(title1, url1, tags1);
    await createPage.submit();

    const searchBox = page.getByRole("textbox", { name: /search bookmarks/i });
    await searchBox.fill("nonexistent");
    await searchBox.press("Enter");

    // No bookmarks should be visible
    await expect(await listPage.getBookmarkTitle(title1)).not.toBeVisible();
    await expect(page.getByText(/no bookmarks found/i)).toBeVisible();

    // Cleanup
    await searchBox.fill("");
    await searchBox.press("Enter");
    await listPage.clickDelete(title1);
  });
});

async function extractTagsText(tagsLocator: Locator) {
  return (await tagsLocator.textContent())?.replace(/\s+/g, " ").trim() ?? "";
}
