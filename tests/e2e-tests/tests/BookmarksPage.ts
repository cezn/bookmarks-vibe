import { Page } from "@playwright/test";

// Page Object for the Bookmarks List Page
export class BookmarksPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto("http://localhost:5005");
  }

  async clickCreateNew() {
    await this.page.getByRole("button", { name: /create new/i }).click();
  }

  async getBookmarkTitle(title: string) {
    return this.page.getByRole("cell", { name: title });
  }

  async getBookmarkUrl(url: string) {
    return this.page.getByRole("link", { name: url });
  }

  async getBookmarkTags(title: string) {
    // Assumes tags are rendered in the same row as the title, as text or badges
    return this.page
      .getByRole("row", { name: new RegExp(title) })
      .locator('[data-testid="bookmark-tags"]');
  }

  async clickEdit(title: string) {
    await this.page
      .getByRole("row", { name: new RegExp(title) })
      .first()
      .getByRole("button", { name: /edit/i })
      .click();
  }

  async clickDelete(title: string) {
    await this.page
      .getByRole("row", { name: new RegExp(title) })
      .first()
      .getByRole("button", { name: /delete/i })
      .click();
  }
}
