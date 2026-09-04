import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { administrationApi } from "../features/administration/administrationApi";
import { DepartmentManagementPage } from "./DepartmentManagementPage";

vi.mock("../features/administration/administrationApi", async () => {
  const actual = await vi.importActual<
    typeof import("../features/administration/administrationApi")
  >("../features/administration/administrationApi");
  return {
    ...actual,
    administrationApi: {
      ...actual.administrationApi,
      departments: vi.fn(),
      deleteDepartment: vi.fn(),
    },
  };
});

afterEach(cleanup);

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <DepartmentManagementPage />
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  vi.mocked(administrationApi.departments).mockResolvedValue([
    {
      id: 1,
      name: "نرم افزار",
      parentDepartmentId: null,
      rowVersion: "version-1",
    },
  ]);
});

describe("DepartmentManagementPage", () => {
  it("uses the shared management-list text treatment for unit names", async () => {
    renderPage();

    const departmentName = await screen.findByText("نرم افزار");

    expect(departmentName).toHaveClass("MuiTypography-body2");
    expect(departmentName).toHaveClass("eos-management-list-text");
    expect(departmentName).not.toHaveClass("eos-persian-number");
  });
});
