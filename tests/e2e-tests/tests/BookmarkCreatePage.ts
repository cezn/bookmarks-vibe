import { Page } from "@playwright/test";

// Page Object for the Create Bookmark Page
export class BookmarkCreatePage {
  constructor(private page: Page) {}

  async fillForm(title: string, url: string, tags: string[] = []) {
    await this.page.getByRole("textbox", { name: /title/i }).fill(title);
    await this.page.getByRole("textbox", { name: /url/i }).fill(url);
    if (tags.length) {
      const tagsInput = this.page.getByRole("combobox", { name: /tags/i });
      for (const tag of tags) {
        await tagsInput.fill(tag);
        await tagsInput.press("Enter");
      }
    }
  }

  async submit() {
    await this.page.getByRole("button", { name: /submit/i }).click();
  }

  async getUrlValidationMessage(): Promise<string> {
    return await this.page.evaluate(() => {
      const urlInput = document.querySelector(
        'input[type="url"]'
      ) as HTMLInputElement;
      return urlInput?.validationMessage || "";
    });
  }
}
