import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { administrationApi } from "../features/administration/administrationApi";
import { SystemAuditPage } from "./SystemAuditPage";

vi.mock("../features/administration/administrationApi", () => ({
  administrationApi: {
    users: vi.fn(),
    auditLogs: vi.fn(),
  },
}));

describe("SystemAuditPage filters", () => {
  beforeEach(() => {
    vi.mocked(administrationApi.users).mockResolvedValue({
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
    });
    vi.mocked(administrationApi.auditLogs).mockResolvedValue({
      items: [],
      pageNumber: 1,
      pageSize: 50,
      totalCount: 0,
    });
  });

  it("offers Persian event labels and user choices for actor and subject", async () => {
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    render(
      <QueryClientProvider client={queryClient}>
        <SystemAuditPage />
      </QueryClientProvider>,
    );

    expect(screen.getByTestId("audit-filters")).toBeInTheDocument();
    await user.click(
      await screen.findByRole("combobox", { name: "کد رویداد" }),
    );
    expect(
      screen.getByRole("option", { name: "کاربر ایجاد شد" }),
    ).toBeInTheDocument();
    await user.keyboard("{Escape}");
    await user.click(screen.getByRole("combobox", { name: "انجام‌دهنده" }));
    await user.click(screen.getByRole("option", { name: /مدیر سامانه/ }));
    await waitFor(() => {
      const latestQuery = vi
        .mocked(administrationApi.auditLogs)
        .mock.calls.at(-1)?.[0];
      expect(latestQuery?.get("actorUserId")).toBe("7");
    });
    await user.click(screen.getByRole("combobox", { name: "کاربر هدف" }));
    expect(
      screen.getByRole("option", { name: /مدیر سامانه/ }),
    ).toBeInTheDocument();
  });
});
