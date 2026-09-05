import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { AuthenticatedUser } from "../features/auth/authTypes";
import { jobDescriptionsApi } from "../features/jobDescriptions/jobDescriptionsApi";
import { ChiefExecutiveDashboardPage } from "./ChiefExecutiveDashboardPage";

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
      reviewWarnings: vi.fn(),
    },
  };
});

afterEach(cleanup);

const user: AuthenticatedUser = {
  id: 1,
  firstName: "مدیر",
  lastName: "عامل",
  roleIds: [1],
  roleCodes: ["ChiefExecutiveOfficer"],
  mustChangePassword: false,
  department: { id: 1, name: "نرم افزار" },
};

beforeEach(() => {
  vi.mocked(jobDescriptionsApi.reviewWarnings).mockResolvedValue([
    {
      versionId: 4,
      departmentId: 1,
      departmentName: "نرم افزار",
      personName: "علی نمونه",
      taskTitle: "توسعه نرم افزار",
      missingSkillName: "مدیریت پروژه",
    },
  ]);
});

describe("ChiefExecutiveDashboardPage", () => {
  it("shows non-blocking skill review warnings", async () => {
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <ChiefExecutiveDashboardPage />
      </QueryClientProvider>,
    );

    expect(
      await screen.findByRole("heading", { name: "موارد نیازمند بررسی" }),
    ).toBeInTheDocument();
    expect(screen.getByText("علی نمونه")).toBeInTheDocument();
    expect(screen.getByText(/مدیریت پروژه/)).toBeInTheDocument();
  });
});
