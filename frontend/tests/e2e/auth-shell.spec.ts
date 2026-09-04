import { expect, test } from "@playwright/test";

test("local credential OTP opens the authenticated shell and logout returns to sign-in", async ({
  page,
}) => {
  let rejectPreferenceUpdate = false;
  await page.route("**/api/v1/**", async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith("/auth/refresh")) {
      await route.fulfill({
        status: 401,
        contentType: "application/json",
        body: JSON.stringify({ code: "refresh_denied" }),
      });
    } else if (path.endsWith("/auth/sign-in/challenges")) {
      await route.fulfill({
        json: {
          challengeToken: "synthetic-challenge",
          maskedMobile: "*******6789",
          expiresAt: new Date(Date.now() + 300_000).toISOString(),
          resendAvailableAt: new Date(Date.now() + 60_000).toISOString(),
        },
      });
    } else if (path.includes("/verify")) {
      await route.fulfill({
        json: {
          accessToken: "synthetic-access",
          accessTokenExpiresAt: new Date(Date.now() + 600_000).toISOString(),
          sessionExpiresAt: new Date(Date.now() + 28_800_000).toISOString(),
          user: {
            id: 1,
            firstName: "مدیر",
            lastName: "سامانه",
            roleIds: [1],
            roleCodes: ["SystemAdministrator", "DepartmentManager"],
            department: { id: 1, name: "نرم افزار" },
          },
        },
      });
    } else if (path.endsWith("/preferences/")) {
      if (route.request().method() === "PUT" && rejectPreferenceUpdate) {
        await route.fulfill({
          status: 503,
          contentType: "application/json",
          body: JSON.stringify({ code: "preference_service_unavailable" }),
        });
        return;
      }
      await route.fulfill({
        json: {
          appearanceMode: "system",
          palette: "forestGreen",
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
  await expect(page).toHaveTitle("داشبوردهای علم و صنعت");
  await expect(page.locator("link[rel='icon']")).toHaveAttribute(
    "href",
    "/generated-assets/brand/eos.svg",
  );
  await page.getByLabel("نام کاربری").fill("synthetic-admin");
  await page
    .getByRole("textbox", { name: "رمز عبور" })
    .fill("synthetic password");
  await page.getByRole("button", { name: "ورود و دریافت کد تأیید" }).click();
  await page.getByLabel("کد شش‌رقمی").fill("۱۲۳۴۵۶");
  await page.getByRole("button", { name: "تأیید کد" }).click();
  await expect(page.getByText("داده‌ای برای نمایش وجود ندارد.")).toBeVisible();
  await expect(page.locator("header").getByAltText("EOS")).toBeVisible();
  await expect(page.locator("header").getByText("علم و صنعت")).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "فضای کاری مدیریت" }),
  ).toHaveCSS("text-align", "start");
  await page.keyboard.press("Control+k");
  const searchResults = page.getByRole("list", {
    name: "نتیجه‌های جست‌وجوی سراسری",
  });
  await expect(
    searchResults.getByRole("button", { name: "داشبورد بخش" }),
  ).toBeVisible();
  await expect(
    searchResults.getByRole("button", { name: "داشبورد مدیرعامل" }),
  ).toHaveCount(0);
  await searchResults.getByRole("button", { name: "داشبورد بخش" }).click();
  await expect(
    page.getByRole("heading", { name: "داشبورد بخش" }),
  ).toBeVisible();
  await page.getByRole("tab", { name: "خانه" }).click();
  const mainBounds = await page.locator("main").evaluate((element) => {
    const bounds = element.getBoundingClientRect();
    return { width: bounds.width };
  });
  const homeCardBounds = await page
    .getByRole("heading", { name: "فضای کاری مدیریت" })
    .locator("xpath=../..")
    .evaluate((element) => {
      const bounds = element.getBoundingClientRect();
      return { width: bounds.width };
    });
  expect(Math.round(homeCardBounds.width)).toBe(
    Math.round(mainBounds.width - 32),
  );
  await expect(
    page.getByRole("tab", { name: "خانه" }).locator("img"),
  ).toHaveCount(0);
  const navigationEdge = await page
    .getByRole("navigation", { name: "منوی اصلی" })
    .evaluate((element) => {
      const bounds = element.getBoundingClientRect();
      return Math.round(bounds.x + bounds.width);
    });
  expect(navigationEdge).toBe(1280);
  const statusBar = page.locator("footer");
  await expect(statusBar).toBeVisible();
  await expect(statusBar).toHaveJSProperty("offsetWidth", 1280);
  const desktopDrawerLayout = await page.evaluate(() => {
    const header = document.querySelector("header");
    const drawer = document.querySelector(".MuiDrawer-docked .MuiDrawer-paper");
    const statusBar = document.querySelector("footer");
    if (!header || !drawer || !statusBar) {
      throw new Error("Expected desktop shell elements are missing.");
    }
    return {
      drawer: drawer.getBoundingClientRect().toJSON(),
      header: header.getBoundingClientRect().toJSON(),
      statusBar: statusBar.getBoundingClientRect().toJSON(),
    };
  });
  expect(Math.round(desktopDrawerLayout.drawer.top)).toBe(
    Math.round(desktopDrawerLayout.header.bottom),
  );
  expect(Math.round(desktopDrawerLayout.drawer.bottom)).toBe(
    Math.round(desktopDrawerLayout.statusBar.top),
  );
  rejectPreferenceUpdate = true;
  await page.getByRole("button", { name: "تغییر حالت نمایش" }).click();
  await expect(page.locator("body")).toHaveCSS(
    "background-color",
    "rgb(13, 17, 19)",
  );
  await page.waitForTimeout(500);
  await expect(
    page.getByText(
      "تنظیمات روی این دستگاه اعمال شد؛ همگام‌سازی با سرور انجام نشد.",
    ),
  ).not.toBeVisible();
  await page
    .getByRole("navigation", { name: "منوی اصلی" })
    .getByRole("button", { name: "خانه" })
    .click();
  await expect(
    page.getByRole("navigation", { name: "منوی اصلی" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "باز و بسته کردن منو" }).click();
  await expect(
    page.getByRole("navigation", { name: "منوی اصلی" }),
  ).toBeVisible();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole("button", { name: "باز و بسته کردن منو" }).click();
  await expect(
    page.getByRole("navigation", { name: "منوی اصلی" }),
  ).toBeVisible();
  await expect(page.locator("header").getByAltText("EOS")).toHaveCount(0);
  await expect(
    page.locator(".MuiModal-root .MuiDrawer-paper").getByAltText("EOS"),
  ).toBeVisible();
  await expect(
    page.locator(".MuiModal-root .MuiDrawer-paper").getByText("علم و صنعت"),
  ).toBeVisible();
  const mobileDrawerLayout = await page.evaluate(() => {
    const header = document.querySelector("header");
    const modal = document.querySelector(".MuiModal-root");
    const drawer = modal?.querySelector(".MuiDrawer-paper");
    const statusBar = document.querySelector("footer");
    if (!header || !drawer || !modal || !statusBar) {
      throw new Error("Expected mobile shell elements are missing.");
    }
    return {
      drawer: drawer.getBoundingClientRect().toJSON(),
      header: header.getBoundingClientRect().toJSON(),
      modal: modal.getBoundingClientRect().toJSON(),
      statusBar: statusBar.getBoundingClientRect().toJSON(),
    };
  });
  expect(Math.round(mobileDrawerLayout.drawer.top)).toBe(
    Math.round(mobileDrawerLayout.header.bottom),
  );
  expect(Math.round(mobileDrawerLayout.drawer.bottom)).toBe(
    Math.round(mobileDrawerLayout.statusBar.top),
  );
  expect(Math.round(mobileDrawerLayout.modal.top)).toBe(
    Math.round(mobileDrawerLayout.header.bottom),
  );
  expect(Math.round(mobileDrawerLayout.modal.bottom)).toBe(
    Math.round(mobileDrawerLayout.statusBar.top),
  );
  const drawerBounds = await page
    .getByRole("navigation", { name: "منوی اصلی" })
    .evaluate((element) => {
      const bounds = element.getBoundingClientRect();
      return {
        bottom: Math.round(bounds.bottom),
        viewportHeight: window.innerHeight,
      };
    });
  expect(drawerBounds.bottom).toBeLessThan(drawerBounds.viewportHeight);
  const mobileLayout = await page.locator("main").evaluate((element) => ({
    width: Math.round(element.getBoundingClientRect().width),
    scrollWidth: element.scrollWidth,
    clientWidth: element.clientWidth,
  }));
  expect(mobileLayout.width).toBe(390);
  expect(mobileLayout.scrollWidth).toBe(mobileLayout.clientWidth);
  await page.getByRole("button", { name: "بستن منو" }).click();
  await page.getByRole("button", { name: "منوی کاربر" }).click();
  await expect(
    page.getByRole("button", { name: /انتخاب رنگ‌بندی/ }),
  ).toHaveCount(5);
  await page.getByRole("button", { name: "انتخاب رنگ‌بندی فیروزه‌ای" }).click();
  await expect(page.locator("body")).toHaveCSS(
    "background-color",
    "rgb(13, 17, 19)",
  );
  await page.getByRole("menuitem", { name: "خروج" }).click();
  await expect(
    page.getByRole("button", { name: "ورود و دریافت کد تأیید" }),
  ).toBeVisible();
});

test("sign-in respects a saved dark appearance", async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem("eos.appearance.last-used", "dark");
    localStorage.setItem("eos.palette.last-used", "turquoise");
  });
  await page.route("**/api/v1/auth/refresh", async (route) => {
    await route.fulfill({
      status: 401,
      contentType: "application/json",
      body: JSON.stringify({ code: "refresh_denied" }),
    });
  });

  await page.goto("/");

  await expect(page.locator("main > div").last()).toHaveCSS(
    "background-color",
    "rgb(13, 17, 19)",
  );
  await expect(page.locator("label").first()).toHaveCSS(
    "color",
    "rgb(56, 184, 170)",
  );
  await expect(page.getByAltText("EOS").locator("xpath=..")).toHaveCSS(
    "background-color",
    "rgba(0, 0, 0, 0)",
  );
  await expect(page.locator("aside")).toHaveCSS(
    "background-image",
    /auth-background\.jpg/,
  );
  await expect(
    page.getByRole("heading", { name: "داشبوردهای علم و صنعت" }),
  ).toHaveCSS("color", "rgb(255, 255, 255)");
  const recoveryButton = page.getByRole("button", { name: "فراموشی رمز" });
  await expect(recoveryButton).toHaveCSS("padding-left", "10px");
  await expect(recoveryButton).toHaveCSS("padding-right", "10px");
  await page.getByLabel("نام کاربری").fill("test-user");
  await page.getByRole("textbox", { name: "رمز عبور" }).fill("test-password");
  const signInButton = page.getByRole("button", {
    name: "ورود و دریافت کد تأیید",
  });
  await signInButton.hover();
  await expect(signInButton).toHaveCSS("color", "rgb(7, 19, 18)");
});

test("a strict-mode bootstrap restores a session with one refresh request", async ({
  page,
}) => {
  let refreshRequests = 0;
  await page.route("**/api/v1/**", async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith("/auth/refresh")) {
      refreshRequests++;
      await route.fulfill({
        json: {
          accessToken: "synthetic-access",
          accessTokenExpiresAt: new Date(Date.now() + 600_000).toISOString(),
          sessionExpiresAt: new Date(Date.now() + 28_800_000).toISOString(),
          user: {
            id: 1,
            firstName: "مدیر",
            lastName: "سامانه",
            roleIds: [1],
            roleCodes: ["SystemAdministrator", "DepartmentManager"],
            department: { id: 1, name: "نرم افزار" },
          },
        },
      });
    } else if (path.endsWith("/preferences/")) {
      await route.fulfill({
        json: {
          appearanceMode: "system",
          palette: "forestGreen",
          sidebarCollapsed: false,
        },
      });
    } else {
      await route.fulfill({ status: 404 });
    }
  });

  await page.goto("/");

  await expect(page.getByText("داده‌ای برای نمایش وجود ندارد.")).toBeVisible();
  expect(refreshRequests).toBe(1);
});

test("a System Administrator can open the operational dashboard", async ({
  page,
}) => {
  await page.route("**/api/v1/**", async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith("/auth/refresh")) {
      await route.fulfill({
        json: {
          accessToken: "synthetic-access",
          accessTokenExpiresAt: new Date(Date.now() + 600_000).toISOString(),
          sessionExpiresAt: new Date(Date.now() + 28_800_000).toISOString(),
          user: {
            id: 1,
            firstName: "مدیر",
            lastName: "سامانه",
            roleIds: [1],
            roleCodes: ["SystemAdministrator"],
            mustChangePassword: false,
            department: { id: 1, name: "نرم افزار" },
          },
        },
      });
    } else if (path.endsWith("/preferences/")) {
      await route.fulfill({
        json: {
          appearanceMode: "dark",
          palette: "teal",
          sidebarCollapsed: false,
        },
      });
    } else if (path.endsWith("/administration/dashboard")) {
      await route.fulfill({
        json: {
          activeUsers: 12,
          inactiveUsers: 2,
          successfulSignIns: 18,
          failedSecurityAttempts: 3,
          usersWithActiveSessions: 7,
          latestAuditLogs: [
            {
              id: 1,
              occurredAt: "2026-09-03T10:30:00",
              eventCode: "UserCreated",
              succeeded: true,
              actorUserId: 1,
              actorDisplayName: "مدیر سامانه",
              subjectUserId: 2,
              subjectDisplayName: "کاربر آزمایشی",
            },
          ],
        },
      });
    } else if (path.endsWith("/administration/users")) {
      await route.fulfill({
        json: {
          items: [
            {
              id: 7,
              personnelCode: "P-7",
              firstName: "مدیر",
              lastName: "سامانه",
              username: "admin",
              maskedMobile: "09******00",
              departmentId: 1,
              departmentName: "نرم افزار",
              isActive: true,
              mustChangePassword: false,
              roleIds: [1],
              rowVersion: "row",
            },
          ],
          pageNumber: 1,
          pageSize: 100,
          totalCount: 1,
        },
      });
    } else if (path.endsWith("/administration/audit-logs")) {
      await route.fulfill({
        json: { items: [], pageNumber: 1, pageSize: 50, totalCount: 0 },
      });
    } else if (path.endsWith("/auth/providers")) {
      await route.fulfill({ json: { google: false } });
    } else {
      await route.fulfill({ status: 404 });
    }
  });

  await page.goto("/");
  await expect(page.getByText("فضای کاری مدیریت")).toBeVisible();
  await page.keyboard.press("Control+k");
  await page
    .getByRole("list", { name: "نتیجه‌های جست‌وجوی سراسری" })
    .getByRole("button", { name: "داشبورد مدیر سامانه" })
    .click();
  await expect(
    page.getByRole("heading", { name: "داشبورد مدیر سامانه" }),
  ).toBeVisible();
  await expect(page.getByText("۱۲", { exact: true })).toBeVisible();
  await expect(page.getByText("کاربران دارای نشست فعال")).toBeVisible();
  await expect(page.getByText("کاربر ایجاد شد")).toBeVisible();
  await expect(page.getByLabel("تاریخ سیستم")).toHaveCount(1);
  await expect(page.getByLabel("ساعت سیستم")).toHaveCount(1);
  const dashboardMainWidth = await page
    .locator("main")
    .evaluate((element) => Math.round(element.getBoundingClientRect().width));
  const dashboardCardWidth = await page
    .getByText("کاربران فعال")
    .locator("xpath=../..")
    .evaluate((element) => Math.round(element.getBoundingClientRect().width));
  expect(dashboardCardWidth).toBeLessThanOrEqual(dashboardMainWidth - 32);
  const auditCardWidth = await page
    .getByRole("heading", { name: "آخرین رویدادهای ممیزی" })
    .locator("xpath=../../..")
    .evaluate((element) => Math.round(element.getBoundingClientRect().width));
  expect(auditCardWidth).toBe(dashboardMainWidth - 32);
  await page.screenshot({
    path: "test-results/system-administration-dashboard.png",
  });
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.locator("main")).toHaveJSProperty("scrollWidth", 390);
  await page.screenshot({
    path: "test-results/system-administration-dashboard-mobile.png",
  });
  await page.setViewportSize({ width: 1280, height: 720 });
  await page.keyboard.press("Control+k");
  await page
    .getByRole("list", { name: "نتیجه‌های جست‌وجوی سراسری" })
    .getByRole("button", { name: "ممیزی سامانه" })
    .click();
  await expect(page.getByRole("combobox", { name: "کد رویداد" })).toBeVisible();
  await page.getByRole("combobox", { name: "کد رویداد" }).click();
  await expect(
    page.getByRole("option", { name: "کاربر ایجاد شد" }),
  ).toBeVisible();
  await page.keyboard.press("Escape");
  await page.getByRole("combobox", { name: "انجام‌دهنده" }).click();
  await expect(page.getByRole("option", { name: /مدیر سامانه/ })).toBeVisible();
  await page.keyboard.press("Escape");
  const auditLayout = await page.locator("table").evaluate((table) => {
    const main = table.closest("main");
    const tablePanel = table.closest(".MuiPaper-root");
    if (!main || !tablePanel) throw new Error("Audit layout is incomplete.");
    return {
      mainScrollHeight: main.scrollHeight,
      mainClientHeight: main.clientHeight,
      tablePanelOverflowY: getComputedStyle(tablePanel).overflowY,
    };
  });
  expect(auditLayout.mainScrollHeight).toBe(auditLayout.mainClientHeight);
  expect(auditLayout.tablePanelOverflowY).toBe("auto");
  await expect(page.getByRole("button", { name: "تازه‌سازی" })).toBeVisible();
});

test("linked Google sign-in is initiated through the API endpoint", async ({
  page,
}) => {
  await page.route("**/api/v1/**", async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith("/auth/refresh")) {
      await route.fulfill({ status: 401, body: JSON.stringify({}) });
    } else if (path.endsWith("/auth/providers")) {
      await route.fulfill({ json: { google: true } });
    } else {
      await route.fulfill({ status: 404 });
    }
  });

  await page.goto("/");
  const startRequest = page.waitForRequest(/\/api\/v1\/auth\/google\/start$/);
  await page.getByRole("button", { name: "ورود با Google" }).click();

  expect((await startRequest).method()).toBe("GET");
});

test("a Google callback failure keeps local credential sign-in available", async ({
  page,
}) => {
  await page.route("**/api/v1/**", async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith("/auth/refresh")) {
      await route.fulfill({ status: 401, body: JSON.stringify({}) });
    } else if (path.endsWith("/auth/providers")) {
      await route.fulfill({ json: { google: false } });
    } else {
      await route.fulfill({ status: 404 });
    }
  });

  await page.goto("/?authError=google");

  await expect(
    page.getByText(
      "ورود با Google انجام نشد. دوباره تلاش کنید یا با رمز عبور وارد شوید.",
    ),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "ورود و دریافت کد تأیید" }),
  ).toBeVisible();
  await expect(page).toHaveURL(/\/$/);
});

test("a temporarily unavailable Google service keeps local credential sign-in available", async ({
  page,
}) => {
  await page.route("**/api/v1/**", async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith("/auth/refresh")) {
      await route.fulfill({ status: 401, body: JSON.stringify({}) });
    } else if (path.endsWith("/auth/providers")) {
      await route.fulfill({ json: { google: false } });
    } else {
      await route.fulfill({ status: 404 });
    }
  });

  await page.goto("/?authError=google-unavailable");

  await expect(
    page.getByText(
      "ورود با Google در حال حاضر در دسترس نیست. لطفاً بعداً تلاش کنید یا با رمز عبور وارد شوید.",
    ),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "ورود و دریافت کد تأیید" }),
  ).toBeVisible();
  await expect(page).toHaveURL(/\/$/);
});

test("OTP offers a one-click resend after its 60-second cooldown", async ({
  page,
}) => {
  let resendRequests = 0;
  await page.route("**/api/v1/**", async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith("/auth/refresh")) {
      await route.fulfill({ status: 401, body: JSON.stringify({}) });
    } else if (path.endsWith("/resend")) {
      resendRequests++;
      await route.fulfill({
        json: {
          challengeToken: "replacement-challenge",
          maskedMobile: "*******6789",
          expiresAt: new Date(Date.now() + 300_000).toISOString(),
          resendAvailableAt: new Date(Date.now() + 60_000).toISOString(),
        },
      });
    } else if (path.endsWith("/auth/sign-in/challenges")) {
      await route.fulfill({
        json: {
          challengeToken: "initial-challenge",
          maskedMobile: "*******6789",
          expiresAt: new Date(Date.now() + 300_000).toISOString(),
          resendAvailableAt: new Date(Date.now() - 1_000).toISOString(),
        },
      });
    } else {
      await route.fulfill({ status: 404 });
    }
  });

  await page.goto("/");
  await page.getByLabel("نام کاربری").fill("synthetic-admin");
  await page
    .getByRole("textbox", { name: "رمز عبور" })
    .fill("synthetic-password");
  await page.getByRole("button", { name: "ورود و دریافت کد تأیید" }).click();
  await page.getByRole("button", { name: "ارسال مجدد کد" }).click();

  expect(resendRequests).toBe(1);
  await expect(page.getByText("کد جدید ارسال شد.")).toBeVisible();
  await expect(
    page.getByRole("button", { name: /ارسال مجدد کد تا [۵۶][۰-۹] ثانیه دیگر/ }),
  ).toBeDisabled();
});

test("development server forwards authentication calls to the local API", async ({
  request,
}) => {
  const response = await request.post("/api/v1/auth/refresh");
  expect(response.status()).toBe(403);
  await expect(response.json()).resolves.toMatchObject({
    code: "refresh_rejected",
  });
});
