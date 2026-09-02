import { expect, test } from "@playwright/test";

test("organizational OTP opens the authenticated shell and logout returns to sign-in", async ({
  page,
}) => {
  await page.route("**/api/v1/**", async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith("/auth/refresh")) {
      await route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({ code: "refresh_denied" }),
      });
    } else if (path.endsWith("/auth/challenges")) {
      await route.fulfill({
        json: {
          challengeToken: "synthetic-challenge",
          maskedMobile: "*******6789",
          expiresAtUtc: new Date(Date.now() + 300_000).toISOString(),
          resendAvailableAtUtc: new Date(Date.now() + 60_000).toISOString(),
        },
      });
    } else if (path.includes("/verify")) {
      await route.fulfill({
        json: {
          accessToken: "synthetic-access",
          accessTokenExpiresAtUtc: new Date(Date.now() + 600_000).toISOString(),
          sessionExpiresAtUtc: new Date(Date.now() + 28_800_000).toISOString(),
          user: {
            id: 1,
            accountName: "TEST\\admin",
            firstName: "مدیر",
            lastName: "سامانه",
            roleIds: [1],
          },
        },
      });
    } else if (path.endsWith("/preferences/")) {
      await route.fulfill({
        json: {
          appearanceMode: "system",
          palette: "navyTeal",
          sidebarCollapsed: false,
        },
      });
    } else if (path.endsWith("/auth/logout")) {
      await route.fulfill({ status: 204 });
    } else {
      await route.fulfill({ status: 404 });
    }
  });

  await page.goto("/");
  await page.getByRole("button", { name: "ورود با حساب سازمانی" }).click();
  await page.getByLabel("کد شش‌رقمی").fill("۱۲۳۴۵۶");
  await page.getByRole("button", { name: "تأیید کد" }).click();
  await expect(page.getByText("داشبوردها به‌زودی اضافه می‌شوند")).toBeVisible();
  await expect(page.getByText(/نسخه/)).toBeVisible();
  await page.getByRole("button", { name: "خروج" }).click();
  await expect(
    page.getByRole("button", { name: "ورود با حساب سازمانی" }),
  ).toBeVisible();
});
