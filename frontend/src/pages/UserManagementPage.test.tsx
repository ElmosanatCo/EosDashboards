import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  administrationApi,
  type ManagedUser,
} from "../features/administration/administrationApi";
import { UserManagementPage } from "./UserManagementPage";

vi.mock("../features/administration/administrationApi", async () => {
  const actual = await vi.importActual<
    typeof import("../features/administration/administrationApi")
  >("../features/administration/administrationApi");
  return {
    ...actual,
    administrationApi: {
      ...actual.administrationApi,
      users: vi.fn(),
      setUserActive: vi.fn(),
    },
  };
});

afterEach(cleanup);

const managedUser: ManagedUser = {
  id: 1,
  personnelCode: "12345",
  firstName: "مریم",
  lastName: "احمدی",
  username: "maryam123",
  maskedMobile: "*******6789",
  departmentId: 1,
  departmentName: "نرم افزار",
  isActive: true,
  mustChangePassword: false,
  roleIds: [1],
  rowVersion: "version-1",
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <UserManagementPage />
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  vi.mocked(administrationApi.users).mockResolvedValue({
    items: [managedUser],
    pageNumber: 1,
    pageSize: 25,
    totalCount: 1,
  });
});

describe("UserManagementPage", () => {
  it("keeps the shared table rhythm and limits numeric font treatment to identifiers", async () => {
    renderPage();

    const table = await screen.findByRole("table", { name: "فهرست کاربران" });

    expect(table).toHaveClass("eos-management-table");
    expect(screen.getByText("مریم احمدی")).not.toHaveClass(
      "eos-persian-number",
    );
    expect(screen.getByText("نرم افزار")).not.toHaveClass("eos-persian-number");
    expect(screen.getByText("12345")).toHaveClass("eos-persian-number");
    expect(screen.getByText("maryam123")).toHaveClass("eos-persian-number");
    expect(screen.getByText("*******6789")).toHaveClass("eos-persian-number");
  });
});
