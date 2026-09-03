import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { describe, expect, beforeEach, it, vi } from "vitest";
import { UserFormPage } from "./UserFormPage";
import { administrationApi } from "../features/administration/administrationApi";
import { TabWorkspaceProvider } from "../navigation/TabWorkspaceProvider";

vi.mock("../features/administration/administrationApi", () => ({
  administrationApi: {
    roles: vi.fn(),
    departments: vi.fn(),
    user: vi.fn(),
    users: vi.fn(),
    createUser: vi.fn(),
    updateUser: vi.fn(),
    resetPassword: vi.fn(),
  },
}));

function renderCreateForm() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <TabWorkspaceProvider>
        <UserFormPage />
      </TabWorkspaceProvider>
    </QueryClientProvider>,
  );
}

describe("UserFormPage", () => {
  beforeEach(() => {
    vi.mocked(administrationApi.roles).mockResolvedValue([
      { id: 1, code: "SystemAdministrator", displayName: "مدیر سامانه" },
    ]);
    vi.mocked(administrationApi.departments).mockResolvedValue([
      { id: 1, name: "نرم افزار", parentDepartmentId: null, rowVersion: "v1" },
    ]);
  });

  it("renders the create fields after lookup data loads", async () => {
    renderCreateForm();

    expect(
      await screen.findByRole("heading", { name: "تعریف کاربر" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("textbox", { name: /کد پرسنلی/ }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("progressbar", { name: "در حال دریافت فرم کاربر" }),
    ).not.toBeInTheDocument();
  });
});
