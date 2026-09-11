import { Page, expect } from "@playwright/test";

// Page Object for the Edit Tag Page
export class TagEditPage {
  constructor(private page: Page) {}

  async waitForFormVisible(name: string) {
    const nameInput = this.page.getByRole("textbox", { name: /name/i });
    await expect(nameInput).toBeVisible();
    await expect(nameInput).toHaveValue(name);
  }

  async fillForm(name: string) {
    await this.page.getByRole("textbox", { name: /name/i }).fill(name);
  }

  async save() {
    await this.page.getByRole("button", { name: /save/i }).click();
  }
}
