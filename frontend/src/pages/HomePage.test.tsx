import SpaceDashboardOutlinedIcon from "@mui/icons-material/SpaceDashboardOutlined";
import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { ComponentProps } from "react";
import type { AuthenticatedUser } from "../features/auth/authTypes";
import type { TabDescriptor } from "../navigation/tabTypes";
import {
  authorizedWorkspaceTargets,
  type WorkspaceTarget,
} from "../navigation/workspaceTargets";
import { getHomeTargetMetadata } from "./home/homeContent";
import { HomePageView } from "./HomePage";

afterEach(cleanup);

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
    const targets = authorizedWorkspaceTargets(["DepartmentManager"]);
    const firstTarget = targets[0]!;
    const metadata = getHomeTargetMetadata(firstTarget.routeId);

    renderHome({ targets });
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
    expect(screen.queryByText("مدیریت کاربران")).not.toBeInTheDocument();
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
    const target = targets[0]!;

    renderHome({ targets, onOpenTarget });
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

  it("shows a useful continuation empty state and keeps the root layout fluid", () => {
    const { getByTestId } = renderHome();
    const root = getByTestId("home-workspace");

    expect(
      screen.getByText("نشست باز دیگری برای ادامه وجود ندارد."),
    ).toBeInTheDocument();
    expect(root.getAttribute("style") ?? "").not.toMatch(
      /width:\s*\d+px|overflow/,
    );
  });
});
