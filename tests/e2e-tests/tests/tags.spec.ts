import { test as base, expect, Page } from "@playwright/test";
import { BookmarksPage } from "./BookmarksPage";
import { BookmarkCreatePage } from "./BookmarkCreatePage";
import { TagsPage } from "./TagsPage";
import { registerAndLogin } from "./helpers/auth";
import { uniqueId } from "./helpers/unique";

type MyFixtures = {
  authenticatedPage: Page;
};

const test = base.extend<MyFixtures>({
  authenticatedPage: async ({ page, request }, use) => {
    // Register a new user, confirm the account via the MailHog email,
    // and log in.
    await registerAndLogin(page, request);

    await use(page);
  },
});

test.describe("Tag statistics", () => {
  test("should show tags of a new bookmark on the tags page", async ({
    authenticatedPage: page,
  }) => {
    const uid = uniqueId();
    const bookmarkTitle = `Tag Stats Bookmark ${uid}`;
    const bookmarkUrl = `https://tagstats.com/${uid}`;
    const bookmarkTags = [`tagstats${uid}`, `othertag${uid}`];

    const listPage = new BookmarksPage(page);
    const createPage = new BookmarkCreatePage(page);
    const tagsPage = new TagsPage(page);

    // Create a new bookmark with tags
    await listPage.goto();
    await listPage.clickCreateNew();
    await createPage.fillForm(bookmarkTitle, bookmarkUrl, bookmarkTags);
    await createPage.submit();
    await expect(await listPage.getBookmarkTitle(bookmarkTitle)).toBeVisible();

    // Tag statistics are updated asynchronously (Kafka consumer),
    // so give the subscriber time to process the event. Playwright's
    // expect retries until the timeout, so no fixed sleep is needed.
    await tagsPage.goto();
    for (const tag of bookmarkTags) {
      await expect(tagsPage.getTagName(tag)).toBeVisible({ timeout: 30_000 });
    }

    // Cleanup: delete the bookmark
    await listPage.goto();
    await listPage.clickDelete(bookmarkTitle);
    await expect(
      await listPage.getBookmarkTitle(bookmarkTitle),
    ).not.toBeVisible();
  });
});
