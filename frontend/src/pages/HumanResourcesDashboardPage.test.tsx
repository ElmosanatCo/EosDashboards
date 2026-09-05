import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jobDescriptionsApi } from "../features/jobDescriptions/jobDescriptionsApi";
import { HumanResourcesDashboardPage } from "./HumanResourcesDashboardPage";

vi.mock("../features/jobDescriptions/jobDescriptionsApi", async () => {
  const actual = await vi.importActual<
    typeof import("../features/jobDescriptions/jobDescriptionsApi")
  >("../features/jobDescriptions/jobDescriptionsApi");
  return {
    ...actual,
    jobDescriptionsApi: {
      ...actual.jobDescriptionsApi,
      humanResourcesDepartments: vi.fn(),
      humanResourcesDashboard: vi.fn(),
    },
  };
});

afterEach(cleanup);

beforeEach(() => {
  vi.mocked(jobDescriptionsApi.humanResourcesDepartments).mockResolvedValue([
    { id: 1, name: "نرم افزار" },
    { id: 2, name: "فناوری اطلاعات" },
  ]);
  vi.mocked(jobDescriptionsApi.humanResourcesDashboard).mockResolvedValue({
    metrics: {
      personnelCount: 12,
      activePersonnelCount: 10,
      archivedPersonnelCount: 2,
      healthyDescriptionCount: 8,
      incompleteDescriptionCount: 2,
      needsReviewCount: 0,
      pendingDataCompletionCount: 1,
      pendingDepartmentApprovalCount: 1,
      underHumanResourcesReviewCount: 2,
      approvedDescriptionCount: 6,
      rejectedDescriptionCount: 1,
      activeProjectCount: 3,
      peopleWorkingOnActiveProjectsCount: 5,
    },
    changeSummaries: [
      {
        departmentId: 1,
        departmentName: "نرم افزار",
        changeCount: 2,
        latestChangedAt: "2026-09-05T08:00:00",
      },
    ],
    changes: [
      {
        versionId: 9,
        departmentId: 1,
        departmentName: "نرم افزار",
        personName: "مریم احمدی",
        changeType: "نسخه جدید",
        changedAt: "2026-09-05T08:00:00",
        actorUserId: null,
      },
    ],
    totalChangeCount: 2,
    page: 1,
    pageSize: 20,
  });
});

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  render(
    <QueryClientProvider client={queryClient}>
      <HumanResourcesDashboardPage />
    </QueryClientProvider>,
  );
}

describe("HumanResourcesDashboardPage", () => {
  it("shows metrics, history and the department selector", async () => {
    renderPage();
    expect(
      await screen.findByRole("heading", { name: "داشبورد منابع انسانی" }),
    ).toBeInTheDocument();
    expect(screen.getByText("آمار تغییرات هر بخش")).toBeInTheDocument();
    expect(screen.getByText("تاریخچه تغییرات")).toBeInTheDocument();
    expect(screen.getByText("۱۲")).toBeInTheDocument();
    expect(screen.getByText("مریم احمدی")).toBeInTheDocument();
  });

  it("reloads the dashboard for a selected department", async () => {
    const actions = userEvent.setup();
    renderPage();
    const selector = await screen.findByRole("combobox", {
      name: "محدوده نمایش",
    });
    await actions.click(selector);
    await actions.click(screen.getByRole("option", { name: "فناوری اطلاعات" }));
    await waitFor(() =>
      expect(
        jobDescriptionsApi.humanResourcesDashboard,
      ).toHaveBeenLastCalledWith(2),
    );
  });
});
