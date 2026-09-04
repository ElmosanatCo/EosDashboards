import SpaceDashboardOutlinedIcon from "@mui/icons-material/SpaceDashboardOutlined";
import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ComponentProps } from "react";
import type { AuthenticatedUser } from "../features/auth/authTypes";
import type { TabDescriptor } from "../navigation/tabTypes";
import {
  authorizedWorkspaceTargets,
  type WorkspaceTarget,
} from "../navigation/workspaceTargets";
import { getHomeTargetMetadata } from "./home/homeContent";
import { HomePage, HomePageView } from "./HomePage";

const useAuthMock = vi.hoisted(() => vi.fn());
const useTabWorkspaceMock = vi.hoisted(() => vi.fn());

vi.mock("../app/providers/AuthProvider", () => ({ useAuth: useAuthMock }));
vi.mock("../navigation/TabWorkspaceProvider", () => ({
  useTabWorkspace: useTabWorkspaceMock,
}));

afterEach(cleanup);
beforeEach(() => {
  useAuthMock.mockReset();
  useTabWorkspaceMock.mockReset();
});

const user: AuthenticatedUser = {
  id: 1,
  firstName: "مریم",
  lastName: "احمدی",
  roleIds: [1],
  roleCodes: ["SystemAdministrator"],
  mustChangePassword: false,
  department: { id: 1, name: "نرم افزار" },
};

const homeTab: TabDescriptor = {
  key: "home",
  routeId: "home",
  pathname: "/",
  search: "",
  title: "خانه",
  closable: false,
};

function renderHome(
  overrides: Partial<ComponentProps<typeof HomePageView>> = {},
) {
  return render(
    <HomePageView
      user={user}
      targets={authorizedWorkspaceTargets(user.roleCodes)}
      tabs={[homeTab]}
      onOpenTarget={vi.fn()}
      onActivateTab={vi.fn()}
      {...overrides}
    />,
  );
}

describe("HomePageView", () => {
  it("renders the six approved workspace sections", () => {
    renderHome();

    for (const heading of [
      "فضای کاری مدیریت",
      "امکانات در اختیار شما",
      "کارهایی که می‌توانید انجام دهید",
      "هشدارها و کارهای نیازمند اقدام",
      "ادامهٔ کار",
      "امکانات آینده",
    ]) {
      expect(
        screen.getByRole("heading", { name: heading }),
      ).toBeInTheDocument();
    }
  });

  it("shows the signed-in user's context, role guide, and global-search hint", () => {
    renderHome();

    expect(screen.getByText("مریم احمدی")).toBeInTheDocument();
    expect(screen.getByText("واحد: نرم افزار")).toBeInTheDocument();
    expect(
      screen.getByText(
        "در این فضا می‌توانید کاربران، واحدها و رویدادهای امنیتی سامانه را مدیریت کنید.",
      ),
    ).toBeInTheDocument();
    expect(screen.getByText(/جست‌وجوی سراسری/)).toHaveTextContent("Ctrl+K");
  });

  it("renders authorized target cards and actions from the supplied targets", () => {
    const departmentManager = { ...user, roleCodes: ["DepartmentManager"] };
    const unauthorizedTarget: WorkspaceTarget = {
      routeId: "administration-users",
      pathname: "/users",
      title: "مدیریت کاربران",
      keywords: [],
      requiredRoleCodes: ["SystemAdministrator"],
      Icon: SpaceDashboardOutlinedIcon,
      render: () => null,
    };
    const targets = [
      ...authorizedWorkspaceTargets(departmentManager.roleCodes),
      unauthorizedTarget,
    ];
    const firstTarget = targets[0]!;
    const metadata = getHomeTargetMetadata(firstTarget.routeId);

    renderHome({ user: departmentManager, targets });
    const capabilities = screen.getByRole("region", {
      name: "امکانات در اختیار شما",
    });

    expect(
      within(capabilities).getByText(firstTarget.title),
    ).toBeInTheDocument();
    expect(
      within(capabilities).getByText(metadata.summary),
    ).toBeInTheDocument();
    expect(
      within(capabilities).getByRole("button", { name: metadata.actionLabel }),
    ).toBeInTheDocument();
    expect(screen.queryAllByText("مدیریت کاربران")).toHaveLength(0);
  });

  it("uses providers to filter authorized targets and dispatch open and activate actions", async () => {
    const dispatch = vi.fn();
    const recentTab: TabDescriptor = {
      key: "administration-users",
      routeId: "administration-users",
      pathname: "/users",
      search: "",
      title: "مدیریت کاربران",
      closable: true,
    };
    const departmentManager = { ...user, roleCodes: ["DepartmentManager"] };
    useAuthMock.mockReturnValue({ user: departmentManager });
    useTabWorkspaceMock.mockReturnValue({
      tabs: [homeTab, recentTab],
      dispatch,
    });

    render(<HomePage />);

    expect(
      within(
        screen.getByRole("region", { name: "امکانات در اختیار شما" }),
      ).getByText("داشبورد بخش"),
    ).toBeInTheDocument();
    expect(screen.queryByText("داشبورد مدیر سامانه")).not.toBeInTheDocument();
    await userEvent.click(
      within(
        screen.getByRole("region", { name: "امکانات در اختیار شما" }),
      ).getByRole("button", { name: "مشاهده داشبورد" }),
    );
    expect(dispatch).toHaveBeenNthCalledWith(1, {
      type: "open",
      tab: expect.objectContaining({
        key: "department-dashboard",
        routeId: "department-dashboard",
      }),
    });

    await userEvent.click(
      screen.getByRole("button", { name: "ادامه: مدیریت کاربران" }),
    );
    expect(dispatch).toHaveBeenNthCalledWith(2, {
      type: "activate",
      key: "administration-users",
    });
  });

  it("renders a newly registered target with generic metadata", () => {
    const target: WorkspaceTarget = {
      routeId: "future-authorized-target",
      pathname: "/future-authorized-target",
      title: "گزارش‌های آینده",
      keywords: [],
      requiredRoleCodes: ["SystemAdministrator"],
      Icon: SpaceDashboardOutlinedIcon,
      render: () => null,
    };

    renderHome({ targets: [target] });
    const capabilities = screen.getByRole("region", {
      name: "امکانات در اختیار شما",
    });

    expect(
      within(capabilities).getByText("گزارش‌های آینده"),
    ).toBeInTheDocument();
    expect(
      within(capabilities).getByText("باز کردن این بخش از فضای کاری"),
    ).toBeInTheDocument();
    expect(
      within(capabilities).getByRole("button", { name: "باز کردن" }),
    ).toBeInTheDocument();
  });

  it("shows the exact honest alert and future empty states", () => {
    renderHome();

    expect(
      screen.getByText("در حال حاضر موردی برای پیگیری ثبت نشده است."),
    ).toBeInTheDocument();
    expect(
      screen.getByText("با فعال شدن قابلیت‌های جدید، این بخش تکمیل می‌شود."),
    ).toBeInTheDocument();
  });

  it("opens the selected capability through the supplied callback", async () => {
    const onOpenTarget = vi.fn();
    const targets = authorizedWorkspaceTargets(["DepartmentManager"]);
    const departmentManager = { ...user, roleCodes: ["DepartmentManager"] };
    const target = targets[0]!;

    renderHome({ user: departmentManager, targets, onOpenTarget });
    await userEvent.click(
      within(
        screen.getByRole("region", { name: "امکانات در اختیار شما" }),
      ).getByRole("button", {
        name: getHomeTargetMetadata(target.routeId).actionLabel,
      }),
    );

    expect(onOpenTarget).toHaveBeenCalledWith(target);
  });

  it("activates a recent non-Home tab through the supplied callback", async () => {
    const onActivateTab = vi.fn();
    const recentTab: TabDescriptor = {
      key: "administration-users",
      routeId: "administration-users",
      pathname: "/users",
      search: "",
      title: "مدیریت کاربران",
      closable: true,
    };

    renderHome({ tabs: [homeTab, recentTab], onActivateTab });
    await userEvent.click(
      screen.getByRole("button", { name: "ادامه: مدیریت کاربران" }),
    );

    expect(onActivateTab).toHaveBeenCalledWith("administration-users");
  });

  it("fits a 320px mobile viewport without horizontal overflow", () => {
    const previousInnerWidth = window.innerWidth;
    Object.defineProperty(window, "innerWidth", {
      configurable: true,
      value: 320,
    });
    const { getByTestId } = render(
      <div
        data-testid="mobile-viewport"
        style={{ width: "320px", maxWidth: "100%" }}
      >
        <HomePageView
          user={user}
          targets={authorizedWorkspaceTargets(user.roleCodes)}
          tabs={[homeTab]}
          onOpenTarget={vi.fn()}
          onActivateTab={vi.fn()}
        />
      </div>,
    );
    const viewport = getByTestId("mobile-viewport");
    const root = getByTestId("home-workspace");

    expect(
      screen.getByText("نشست باز دیگری برای ادامه وجود ندارد."),
    ).toBeInTheDocument();
    expect(root).toHaveStyle({ minWidth: "0px", maxWidth: "100%" });
    expect(viewport.scrollWidth).toBeLessThanOrEqual(
      viewport.clientWidth || 320,
    );
    expect(root.scrollWidth).toBeLessThanOrEqual(root.clientWidth || 320);

    Object.defineProperty(window, "innerWidth", {
      configurable: true,
      value: previousInnerWidth,
    });
  });
});
