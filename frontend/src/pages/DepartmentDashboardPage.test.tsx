import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { AuthenticatedUser } from "../features/auth/authTypes";
import { jobDescriptionsApi } from "../features/jobDescriptions/jobDescriptionsApi";
import { DepartmentDashboardPage } from "./DepartmentDashboardPage";

vi.mock("../app/providers/AuthProvider", () => ({
  useAuth: () => ({ user }),
}));

vi.mock("../features/jobDescriptions/jobDescriptionsApi", async () => {
  const actual = await vi.importActual<
    typeof import("../features/jobDescriptions/jobDescriptionsApi")
  >("../features/jobDescriptions/jobDescriptionsApi");
  return {
    ...actual,
    jobDescriptionsApi: {
      ...actual.jobDescriptionsApi,
      managedDepartments: vi.fn(),
      dashboard: vi.fn(),
    },
  };
});

afterEach(cleanup);

const user: AuthenticatedUser = {
  id: 1,
  firstName: "مریم",
  lastName: "احمدی",
  roleIds: [1],
  roleCodes: ["DepartmentManager"],
  mustChangePassword: false,
  department: { id: 1, name: "نرم افزار" },
};

beforeEach(() => {
  vi.mocked(jobDescriptionsApi.managedDepartments).mockResolvedValue([
    { id: 1, name: "نرم افزار", isOwnDepartment: true },
    { id: 2, name: "فناوری اطلاعات", isOwnDepartment: false },
  ]);
  vi.mocked(jobDescriptionsApi.dashboard).mockResolvedValue({
    personnelCount: 0,
    activePersonnelCount: 0,
    archivedPersonnelCount: 0,
    healthyDescriptionCount: 0,
    incompleteDescriptionCount: 0,
    needsReviewCount: 0,
    pendingDataCompletionCount: 0,
    approvedDescriptionCount: 0,
    activeProjectCount: 0,
    peopleWorkingOnActiveProjectsCount: 0,
    pendingDepartmentApprovalCount: 0,
    underHumanResourcesReviewCount: 0,
    rejectedDescriptionCount: 0,
  });
});

describe("DepartmentDashboardPage", () => {
  it("shows and applies the all-departments display filter", async () => {
    const userActions = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    render(
      <QueryClientProvider client={queryClient}>
        <DepartmentDashboardPage />
      </QueryClientProvider>,
    );

    expect(
      await screen.findByRole("heading", { name: "داشبورد بخش" }),
    ).toBeInTheDocument();
    const selector = await screen.findByRole("combobox", {
      name: "محدوده نمایش",
    });
    expect(selector).toHaveTextContent("همه بخش‌ها");

    await userActions.click(selector);
    expect(
      await screen.findByRole("option", { name: "همه بخش‌ها" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: "نرم افزار" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: "فناوری اطلاعات" }),
    ).toBeInTheDocument();

    await userActions.click(screen.getByRole("option", { name: "همه بخش‌ها" }));
    await waitFor(() => {
      expect(jobDescriptionsApi.dashboard).toHaveBeenLastCalledWith(undefined);
    });

    await userActions.click(selector);
    await userActions.click(
      screen.getByRole("option", { name: "فناوری اطلاعات" }),
    );
    await waitFor(() => {
      expect(jobDescriptionsApi.dashboard).toHaveBeenLastCalledWith(2);
    });
  });

  it("shows the count of healthy descriptions needing skill review", async () => {
    vi.mocked(jobDescriptionsApi.dashboard).mockResolvedValue({
      personnelCount: 1,
      activePersonnelCount: 1,
      archivedPersonnelCount: 0,
      healthyDescriptionCount: 1,
      incompleteDescriptionCount: 0,
      needsReviewCount: 2,
      pendingDataCompletionCount: 0,
      approvedDescriptionCount: 0,
      activeProjectCount: 0,
      peopleWorkingOnActiveProjectsCount: 0,
      pendingDepartmentApprovalCount: 1,
      underHumanResourcesReviewCount: 0,
      rejectedDescriptionCount: 0,
    });
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <DepartmentDashboardPage />
      </QueryClientProvider>,
    );

    expect(await screen.findByText("نیازمند بررسی")).toBeInTheDocument();
    expect(screen.getByText("۲")).toBeInTheDocument();
  });
});
