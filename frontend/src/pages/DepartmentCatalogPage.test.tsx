import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { AuthenticatedUser } from "../features/auth/authTypes";
import { jobDescriptionsApi } from "../features/jobDescriptions/jobDescriptionsApi";
import { DepartmentCatalogPage } from "./DepartmentCatalogPage";

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
      catalog: vi.fn(),
      deactivateDepartmentSkill: vi.fn(),
      deactivatePublicSkill: vi.fn(),
      deactivateDepartmentTask: vi.fn(),
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

function renderPage(kind: "skills" | "tasks") {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <DepartmentCatalogPage kind={kind} />
    </QueryClientProvider>,
  );
}

beforeEach(() => {
  vi.mocked(jobDescriptionsApi.managedDepartments).mockResolvedValue([
    { id: 1, name: "نرم افزار", isOwnDepartment: true },
    { id: 2, name: "فناوری اطلاعات", isOwnDepartment: false },
  ]);
  vi.mocked(jobDescriptionsApi.catalog).mockResolvedValue({
    skills: [
      {
        id: 100,
        departmentId: null,
        name: "مهارت عمومی",
        ownerDepartmentId: 1,
        usageDepartmentCount: 1,
        canEdit: true,
        canDelete: true,
      },
      ...Array.from({ length: 30 }, (_, index) => ({
        id: index + 1,
        departmentId: index % 2 === 0 ? 1 : 2,
        name: `مهارت ${index + 1}`,
        ownerDepartmentId: null,
        usageDepartmentCount: 0,
        canEdit: true,
        canDelete: true,
      })),
    ],
    tasks: Array.from({ length: 30 }, (_, index) => ({
      id: index + 1,
      departmentId: 1,
      title: `وظیفه ${index + 1}`,
      isProject: index % 2 === 0,
      requiredSkillIds: index === 0 ? [1, 2] : [],
    })),
  });
  vi.mocked(jobDescriptionsApi.deactivateDepartmentTask).mockResolvedValue(
    undefined,
  );
});

describe("DepartmentCatalogPage", () => {
  it("separates public and department skills and labels each department scope", async () => {
    const userActions = userEvent.setup();
    renderPage("skills");

    expect(
      await screen.findByRole("heading", { name: "کاتالوگ مهارت‌ها" }),
    ).toBeInTheDocument();
    expect(
      await screen.findByRole("table", { name: "فهرست مهارت‌ها" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "عمومی" })).toBeInTheDocument();
    expect(screen.getByRole("tab", { name: "اختصاصی" })).toBeInTheDocument();

    await userActions.click(screen.getByRole("tab", { name: "عمومی" }));
    expect(screen.getByText("مهارت عمومی")).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "ثبت مهارت عمومی" }),
    ).toBeInTheDocument();

    await userActions.click(screen.getByRole("tab", { name: "اختصاصی" }));
    await userActions.click(screen.getByRole("combobox", { name: "بخش هدف" }));
    await userActions.click(
      await screen.findByRole("option", { name: "همه بخش‌ها" }),
    );
    expect(await screen.findByText(/صفحه\s+۱\s+از\s+۲/)).toBeInTheDocument();
    expect(screen.getAllByRole("row")).toHaveLength(26);
    expect(screen.getAllByText("اختصاصی - نرم افزار").length).toBeGreaterThan(
      0,
    );
    expect(
      screen.getAllByText("اختصاصی - فناوری اطلاعات").length,
    ).toBeGreaterThan(0);

    await userActions.type(
      screen.getByRole("textbox", { name: "جست‌وجو در مهارت‌ها" }),
      "مهارت 30",
    );

    expect(screen.getByText("مهارت 30")).toBeInTheDocument();
    expect(screen.queryByText("مهارت 1")).not.toBeInTheDocument();
  });

  it("uses a separate task catalog surface and does not annotate department options", async () => {
    const userActions = userEvent.setup();
    renderPage("tasks");

    expect(
      await screen.findByRole("heading", { name: "کاتالوگ وظایف" }),
    ).toBeInTheDocument();
    expect(
      await screen.findByRole("table", { name: "فهرست وظایف" }),
    ).toBeInTheDocument();
    await userActions.click(screen.getByRole("combobox", { name: "بخش هدف" }));
    expect(
      await screen.findByRole("option", { name: "نرم افزار" }),
    ).toBeInTheDocument();
    expect(
      await screen.findByRole("option", { name: "فناوری اطلاعات" }),
    ).toBeInTheDocument();
    expect(screen.queryByText(/بخش اصلی|زیر.?بخش/)).not.toBeInTheDocument();
  });

  it("keeps page height bounded so only the catalog list scrolls", async () => {
    renderPage("tasks");

    await screen.findByRole("table", { name: "فهرست وظایف" });

    expect(screen.getByTestId("department-catalog-page")).toHaveStyle({
      height: "100%",
      minHeight: "0px",
      overflow: "hidden",
    });
    expect(screen.getByTestId("department-catalog-list")).toHaveStyle({
      flex: "1 1 0%",
      minHeight: "0px",
      maxHeight: "none",
    });
  });

  it("requires confirmation before deactivating a catalog task", async () => {
    const userActions = userEvent.setup();
    renderPage("tasks");

    const deleteButton = await screen.findByRole("button", {
      name: "غیرفعال‌سازی وظیفه وظیفه 1",
    });
    await userActions.click(deleteButton);

    expect(
      await screen.findByRole("heading", { name: "تأیید غیرفعال‌سازی" }),
    ).toBeInTheDocument();
    expect(
      screen.getByText("آیا از غیرفعال‌سازی «وظیفه 1» مطمئن هستید؟"),
    ).toBeInTheDocument();

    await userActions.click(screen.getByRole("button", { name: "انصراف" }));
    expect(jobDescriptionsApi.deactivateDepartmentTask).not.toHaveBeenCalled();

    await userActions.click(deleteButton);
    await userActions.click(
      screen.getByRole("button", { name: "تأیید غیرفعال‌سازی" }),
    );
    expect(jobDescriptionsApi.deactivateDepartmentTask).toHaveBeenCalledWith(1);
  });
});
