import { expect, test } from "@playwright/test";

test("trophy home navigates to a competition detail", async ({ page }) => {
  await page.goto("/");

  await expect(page.getByRole("heading", { name: "The Trophy Cabinet" })).toBeVisible();
  await expect(page.getByLabel("Next Arsenal fixture")).toContainText("Arsenal");
  await expect(page.locator(".trophy")).toHaveCount(4);
  await expect(page.locator(".trophy.eliminated")).toHaveCount(2);
  await expect(page.getByRole("button", { name: "Continue with Google" })).toBeDisabled();

  await page.getByLabel("Premier League standings").click();

  await expect(page.getByRole("heading", { name: "Premier League" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Standings" })).toBeVisible();
});
