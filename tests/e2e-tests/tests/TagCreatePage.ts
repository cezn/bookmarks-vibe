import { Page } from "@playwright/test";

// Page Object for the Create Tag Page
export class TagCreatePage {
  constructor(private page: Page) {}

  async fillForm(name: string) {
    await this.page.getByRole("textbox", { name: /name/i }).fill(name);
  }

  async submit() {
    await this.page.getByRole("button", { name: /submit/i }).click();
  }
}
