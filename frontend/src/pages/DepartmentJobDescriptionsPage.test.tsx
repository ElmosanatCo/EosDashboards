import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  cleanup,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { AuthenticatedUser } from "../features/auth/authTypes";
import { jobDescriptionsApi } from "../features/jobDescriptions/jobDescriptionsApi";
import { DepartmentJobDescriptionsPage } from "./DepartmentJobDescriptionsPage";

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
      list: vi.fn(),
      detail: vi.fn(),
      catalog: vi.fn(),
      analysis: vi.fn(),
      createTask: vi.fn(),
      archive: vi.fn(),
      delete: vi.fn(),
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
  vi.mocked(jobDescriptionsApi.list).mockResolvedValue([]);
  vi.mocked(jobDescriptionsApi.archive).mockResolvedValue(undefined);
  vi.mocked(jobDescriptionsApi.delete).mockResolvedValue(undefined);
});

describe("DepartmentJobDescriptionsPage", () => {
  it("shows and applies the all-departments filter", async () => {
    const userActions = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    render(
      <QueryClientProvider client={queryClient}>
        <DepartmentJobDescriptionsPage />
      </QueryClientProvider>,
    );

    expect(
      await screen.findByRole("heading", { name: "مدیریت شرح وظایف" }),
    ).toBeInTheDocument();
    const selector = await screen.findByRole("combobox", { name: "بخش هدف" });
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
      expect(jobDescriptionsApi.list).toHaveBeenLastCalledWith(undefined);
    });

    await userActions.click(selector);
    await userActions.click(screen.getByRole("option", { name: "نرم افزار" }));
    await waitFor(() => {
      expect(jobDescriptionsApi.list).toHaveBeenLastCalledWith(1);
    });
  });

  it("shows raw imported values and resolution controls", async () => {
    const userActions = userEvent.setup();
    vi.mocked(jobDescriptionsApi.list).mockResolvedValue([
      {
        id: 7,
        departmentId: 1,
        personName: "علی نمونه",
        workflowStatus: "منتظر رفع نقص",
        qualityStatus: "ناقص",
        updatedAt: "2026-09-04T10:00:00",
        needsReview: false,
      },
    ]);
    vi.mocked(jobDescriptionsApi.detail).mockResolvedValue({
      id: 7,
      departmentId: 1,
      personName: "علی نمونه",
      personnelCode: null,
      education: "",
      fieldOfStudy: "",
      minimumExperience: "",
      skillIds: [],
      tasks: [],
      unresolvedSkills: [{ rawName: "مهارت خام", sortOrder: 1 }],
      unresolvedTasks: [
        {
          rawTitle: "وظیفه خام",
          description: "شرح خام",
          startDate: null,
          endDate: null,
          sortOrder: 1,
        },
      ],
      workflowStatus: "منتظر رفع نقص",
      qualityStatus: "ناقص",
      rejectionReason: null,
      needsReview: false,
    });
    vi.mocked(jobDescriptionsApi.catalog).mockResolvedValue({
      skills: [
        {
          id: 1,
          departmentId: null,
          name: "مهارت عمومی",
          ownerDepartmentId: 1,
          usageDepartmentCount: 0,
          isActive: true,
          canEdit: true,
          canDelete: true,
        },
        {
          id: 2,
          departmentId: 1,
          name: "مهارت اختصاصی",
          ownerDepartmentId: null,
          usageDepartmentCount: 0,
          isActive: true,
          canEdit: true,
          canDelete: true,
        },
      ],
      tasks: [
        {
          id: 3,
          departmentId: 1,
          title: "وظیفه پروژه‌ای",
          isProject: true,
          isActive: true,
          requiredSkillIds: [],
        },
      ],
    });
    vi.mocked(jobDescriptionsApi.analysis).mockResolvedValue([]);
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    render(
      <QueryClientProvider client={queryClient}>
        <DepartmentJobDescriptionsPage />
      </QueryClientProvider>,
    );

    await userActions.click(
      await screen.findByRole("button", { name: "مشاهده شرح وظایف علی نمونه" }),
    );
    expect(
      await screen.findByText("تطبیق‌نشده: مهارت خام"),
    ).toBeInTheDocument();
    expect(
      await screen.findByText("تطبیق‌نشده: وظیفه خام"),
    ).toBeInTheDocument();
    await userActions.click(screen.getByRole("button", { name: "ویرایش" }));
    expect(
      await screen.findByText("۶ مورد نیازمند رسیدگی"),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /مهارت‌ها ناقص/ }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /وظایف ناقص/ }),
    ).toBeInTheDocument();
    await userActions.click(screen.getAllByRole("combobox")[1]);
    await userActions.click(
      screen.getByRole("option", { name: "مهارت عمومی" }),
    );
    expect(screen.getByRole("button", { name: "مهارت عمومی" })).toHaveClass(
      "MuiChip-colorPrimary",
    );
    const skillResolution = screen.getAllByRole("combobox")[1];
    await userActions.click(skillResolution);
    await userActions.click(
      screen.getByRole("option", { name: "مهارت اختصاصی" }),
    );
    expect(screen.getByRole("button", { name: "مهارت عمومی" })).not.toHaveClass(
      "MuiChip-colorPrimary",
    );
    expect(screen.getByRole("button", { name: "مهارت اختصاصی" })).toHaveClass(
      "MuiChip-colorPrimary",
    );
    expect(
      screen.getByRole("button", { name: /مهارت‌ها/ }),
    ).not.toHaveTextContent("ناقص");
    expect(
      screen.getByRole("combobox", { name: "بخش هدف" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "کد پرسنلی" })).toBeRequired();
    expect(
      screen.getByRole("button", { name: "ثبت نسخه‌ی جدید" }),
    ).toBeDisabled();
    expect(screen.queryByLabelText("ساعت")).not.toBeInTheDocument();
    expect(
      await screen.findByRole("button", { name: "ایجاد مهارت جدید" }),
    ).toBeInTheDocument();
    await userActions.click(
      screen.getByRole("button", { name: "ایجاد مهارت جدید" }),
    );
    expect(screen.getByRole("checkbox", { name: "عمومی" })).toBeInTheDocument();
    await userActions.click(
      screen.getByRole("button", { name: "ایجاد وظیفه جدید" }),
    );
    expect(screen.getByRole("checkbox", { name: "پروژه" })).toBeInTheDocument();
    const firstTaskAccordion = screen
      .getAllByRole("heading", { name: /وظیفه ۱ · وظیفه خام/ })[0]
      .closest(".MuiAccordion-root");
    expect(firstTaskAccordion).not.toBeNull();
    expect(
      within(firstTaskAccordion as HTMLElement).getByRole("textbox", {
        name: "عنوان وظیفه جدید",
      }),
    ).toHaveValue("وظیفه خام");
  });

  it("does not offer department submission for an incomplete description", async () => {
    vi.mocked(jobDescriptionsApi.list).mockResolvedValue([
      {
        id: 30,
        departmentId: 1,
        personName: "پرسنل ناقص",
        workflowStatus: "منتظر تأیید",
        qualityStatus: "ناقص",
        updatedAt: "2026-09-04T10:00:00",
        needsReview: false,
      },
    ]);
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <DepartmentJobDescriptionsPage />
      </QueryClientProvider>,
    );

    expect(await screen.findByText("پرسنل ناقص")).toBeInTheDocument();
    expect(
      screen.queryByRole("button", {
        name: "تأیید شرح وظایف پرسنل ناقص",
      }),
    ).not.toBeInTheDocument();
  });

  it("shows a review warning without blocking a healthy submission", async () => {
    vi.mocked(jobDescriptionsApi.list).mockResolvedValue([
      {
        id: 31,
        departmentId: 1,
        personName: "پرسنل نیازمند بررسی",
        workflowStatus: "منتظر تأیید",
        qualityStatus: "سالم",
        updatedAt: "2026-09-04T10:00:00",
        needsReview: true,
      },
    ]);
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <DepartmentJobDescriptionsPage />
      </QueryClientProvider>,
    );

    expect(await screen.findByText("نیازمند بررسی")).toBeInTheDocument();
    expect(
      screen.getByRole("button", {
        name: "تأیید شرح وظایف پرسنل نیازمند بررسی",
      }),
    ).toBeInTheDocument();
  });

  it("allows replacing a selected task and sends new task creation to the API", async () => {
    const userActions = userEvent.setup();
    vi.mocked(jobDescriptionsApi.list).mockResolvedValue([
      {
        id: 8,
        departmentId: 1,
        personName: "پرسنل نمونه",
        workflowStatus: "منتظر تأیید",
        qualityStatus: "سالم",
        updatedAt: "2026-09-04T10:00:00",
        needsReview: false,
      },
    ]);
    vi.mocked(jobDescriptionsApi.detail).mockResolvedValue({
      id: 8,
      departmentId: 1,
      personName: "پرسنل نمونه",
      personnelCode: "P-8",
      education: "لیسانس",
      fieldOfStudy: "نرم افزار",
      minimumExperience: "۳ سال",
      skillIds: [1],
      tasks: [
        {
          taskCatalogItemId: 3,
          title: "وظیفه انتخاب‌شده",
          description: "شرح وظیفه",
          startDate: "2026-09-01",
          endDate: null,
          sortOrder: 1,
          weeklyHours: 40,
        },
      ],
      unresolvedSkills: [],
      unresolvedTasks: [],
      workflowStatus: "منتظر تأیید",
      qualityStatus: "سالم",
      rejectionReason: null,
      needsReview: false,
    });
    vi.mocked(jobDescriptionsApi.catalog).mockResolvedValue({
      skills: [],
      tasks: [
        {
          id: 3,
          departmentId: 1,
          title: "وظیفه انتخاب‌شده",
          isProject: false,
          isActive: true,
          requiredSkillIds: [],
        },
      ],
    });
    vi.mocked(jobDescriptionsApi.analysis).mockResolvedValue([]);
    vi.mocked(jobDescriptionsApi.createTask).mockResolvedValue({ id: 9 });
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    render(
      <QueryClientProvider client={queryClient}>
        <DepartmentJobDescriptionsPage />
      </QueryClientProvider>,
    );

    await userActions.click(
      await screen.findByRole("button", {
        name: "مشاهده شرح وظایف پرسنل نمونه",
      }),
    );
    await userActions.click(
      await screen.findByRole("button", { name: "ویرایش" }),
    );
    await userActions.click(
      screen.getByRole("button", { name: "ایجاد وظیفه جدید" }),
    );
    await userActions.type(
      screen.getByRole("textbox", { name: "عنوان وظیفه جدید" }),
      "وظیفه تازه",
    );
    await userActions.click(
      screen.getByRole("button", { name: "ثبت و استفاده از وظیفه" }),
    );

    await waitFor(() => {
      expect(jobDescriptionsApi.createTask).toHaveBeenCalledWith(
        1,
        "وظیفه تازه",
        false,
      );
    });
  });

  it("requires confirmation before archiving an approved description", async () => {
    const userActions = userEvent.setup();
    vi.mocked(jobDescriptionsApi.list).mockResolvedValue([
      {
        id: 21,
        departmentId: 1,
        personName: "پرسنل تأییدشده",
        workflowStatus: "تأیید شده",
        qualityStatus: "سالم",
        updatedAt: "2026-09-04T10:00:00",
        needsReview: false,
      },
    ]);
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    render(
      <QueryClientProvider client={queryClient}>
        <DepartmentJobDescriptionsPage />
      </QueryClientProvider>,
    );

    await userActions.click(
      await screen.findByRole("button", {
        name: "آرشیو شرح وظایف پرسنل تأییدشده",
      }),
    );
    await userActions.click(screen.getByRole("button", { name: "انصراف" }));
    expect(jobDescriptionsApi.archive).not.toHaveBeenCalled();
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });

    await userActions.click(
      screen.getByRole("button", {
        name: "آرشیو شرح وظایف پرسنل تأییدشده",
      }),
    );
    await userActions.click(
      screen.getByRole("button", { name: "تأیید آرشیو" }),
    );
    await waitFor(() => {
      expect(jobDescriptionsApi.archive).toHaveBeenCalledWith(21);
    });
  });

  it("requires confirmation before deleting an unapproved draft", async () => {
    const userActions = userEvent.setup();
    vi.mocked(jobDescriptionsApi.list).mockResolvedValue([
      {
        id: 22,
        departmentId: 1,
        personName: "پرسنل پیش‌نویس",
        workflowStatus: "منتظر رفع نقص",
        qualityStatus: "ناقص",
        updatedAt: "2026-09-04T10:00:00",
        needsReview: false,
      },
    ]);
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    render(
      <QueryClientProvider client={queryClient}>
        <DepartmentJobDescriptionsPage />
      </QueryClientProvider>,
    );

    await userActions.click(
      await screen.findByRole("button", {
        name: "حذف شرح وظایف پرسنل پیش‌نویس",
      }),
    );
    await userActions.click(screen.getByRole("button", { name: "تأیید حذف" }));
    await waitFor(() => {
      expect(jobDescriptionsApi.delete).toHaveBeenCalledWith(22);
    });
  });
});
