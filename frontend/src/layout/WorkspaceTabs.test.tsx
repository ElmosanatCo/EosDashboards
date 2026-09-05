import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AppThemeProvider } from "../app/providers/AppThemeProvider";
import type { TabDescriptor } from "../navigation/tabTypes";
import { WorkspaceTabs } from "./WorkspaceTabs";

const useTabWorkspaceMock = vi.hoisted(() => vi.fn());
const preferenceMock = vi.hoisted(() => ({ gradientsEnabled: true }));

vi.mock("../navigation/TabWorkspaceProvider", () => ({
  useTabWorkspace: useTabWorkspaceMock,
}));

vi.mock("../app/providers/UserPreferenceProvider", () => ({
  useUserPreferences: () => ({
    gradientsEnabled: preferenceMock.gradientsEnabled,
  }),
}));

afterEach(() => {
  cleanup();
  useTabWorkspaceMock.mockReset();
  preferenceMock.gradientsEnabled = true;
});

const homeTab: TabDescriptor = {
  key: "home",
  routeId: "home",
  title: "خانه",
  pathname: "/",
  search: "",
  closable: false,
};

const usersTab: TabDescriptor = {
  key: "users",
  routeId: "administration-users",
  title: "مدیریت کاربران",
  pathname: "/administration/users",
  search: "",
  closable: true,
};

describe("WorkspaceTabs", () => {
  it("fades the active tab accent upward from its lower edge", () => {
    useTabWorkspaceMock.mockReturnValue({
      tabs: [homeTab, usersTab],
      activeKey: usersTab.key,
      dispatch: vi.fn(),
    });

    render(
      <AppThemeProvider>
        <WorkspaceTabs />
      </AppThemeProvider>,
    );

    const activeTab = screen.getByRole("tab", { name: /مدیریت کاربران/ });
    expect(getComputedStyle(activeTab).backgroundImage).toContain(
      "linear-gradient(0deg",
    );
    expect(getComputedStyle(activeTab).backgroundImage).toContain("46%");
    expect(
      getComputedStyle(screen.getByRole("tab", { name: "خانه" }))
        .backgroundImage,
    ).toBe("none");
  });

  it("removes the active tab gradient when the preference is disabled", () => {
    preferenceMock.gradientsEnabled = false;
    useTabWorkspaceMock.mockReturnValue({
      tabs: [homeTab, usersTab],
      activeKey: usersTab.key,
      dispatch: vi.fn(),
    });

    render(
      <AppThemeProvider>
        <WorkspaceTabs />
      </AppThemeProvider>,
    );

    expect(
      getComputedStyle(screen.getByRole("tab", { name: /مدیریت کاربران/ }))
        .backgroundImage,
    ).toBe("none");
  });
});
