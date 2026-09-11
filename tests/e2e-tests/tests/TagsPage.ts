import { Page } from "@playwright/test";

// Page Object for the Tags List Page
export class TagsPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto("http://localhost:5005/tags");
  }

  async clickCreateNew() {
    await this.page.getByRole("button", { name: /create new/i }).click();
  }

  async getTagName(name: string) {
    return this.page.getByRole("cell", { name });
  }

  async clickEdit(name: string) {
    await this.page
      .getByRole("row", { name: new RegExp(name) })
      .getByRole("button", { name: /edit/i })
      .click();
  }

  async clickDelete(name: string) {
    await this.page
      .getByRole("row", { name: new RegExp(name) })
      .getByRole("button", { name: /delete/i })
      .click();
  }
}
