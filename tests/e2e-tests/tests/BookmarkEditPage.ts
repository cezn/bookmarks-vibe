import { Page, expect } from "@playwright/test";
import { setTimeout } from "node:timers/promises";

// Page Object for the Edit Bookmark Page
export class BookmarkEditPage {
  constructor(private page: Page) {}

  async waitForFormVisible(title: string) {
    const titleInput = this.page.getByRole("textbox", { name: /title/i });
    await expect(titleInput).toBeVisible();
    await expect(titleInput).toHaveValue(title);
    await setTimeout(100); // for some reason 'title' input can't be updated to quickly.
    // at least on firefox when running react-dev-server. With StaticAssets it works fine.
  }

  async fillForm(title: string, url: string, tags: string[] = []) {
    await this.page.getByRole("textbox", { name: /title/i }).fill(title);
    await this.page.getByRole("textbox", { name: /url/i }).fill(url);
    // Clear existing tags by pressing Backspace until empty
    const tagsInput = this.page.getByRole("combobox", { name: /tags/i });
    for (let i = 0; i < 5; ++i) await tagsInput.press("Backspace");
    if (tags.length) {
      for (const tag of tags) {
        await tagsInput.fill(tag);
        await tagsInput.press("Enter");
      }
    }
  }

  async save() {
    await this.page.getByRole("button", { name: /save/i }).click();
  }
}
