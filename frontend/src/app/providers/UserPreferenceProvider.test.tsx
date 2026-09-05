import { Button } from "@mui/material";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AppThemeProvider } from "./AppThemeProvider";
import {
  UserPreferenceProvider,
  useUserPreferences,
} from "./UserPreferenceProvider";
import { preferencesApi } from "../../features/preferences/preferencesApi";

vi.mock("../../features/preferences/preferencesApi", () => ({
  preferencesApi: { get: vi.fn(), update: vi.fn() },
}));

afterEach(() => {
  cleanup();
  localStorage.clear();
  vi.resetAllMocks();
});

function PreferenceProbe() {
  const {
    appearanceMode,
    gradientsEnabled,
    sidebarCollapsed,
    updateAppearance,
    updateGradientsEnabled,
    updateSidebarCollapsed,
  } = useUserPreferences();
  return (
    <>
      <output>{`${appearanceMode}:${sidebarCollapsed}:${gradientsEnabled}`}</output>
      <Button onClick={() => updateAppearance("dark")}>تغییر به تیره</Button>
      <Button onClick={() => updateSidebarCollapsed(true)}>بستن منو</Button>
      <Button onClick={() => updateGradientsEnabled(false)}>
        خاموش کردن گرادیانت
      </Button>
    </>
  );
}

describe("UserPreferenceProvider", () => {
  it("keeps the stored device appearance and safely migrates its retired palette", async () => {
    localStorage.setItem("eos.appearance.last-used", "dark");
    localStorage.setItem("eos.palette.last-used", "burgundy");
    vi.mocked(preferencesApi.get).mockResolvedValue({
      appearanceMode: "system",
      palette: "navyTeal",
      sidebarCollapsed: false,
      gradientsEnabled: true,
    });
    vi.mocked(preferencesApi.update).mockResolvedValue({
      appearanceMode: "dark",
      palette: "teal",
      sidebarCollapsed: false,
      gradientsEnabled: true,
    });
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <AppThemeProvider>
          <UserPreferenceProvider userId={1}>
            <PreferenceProbe />
          </UserPreferenceProvider>
        </AppThemeProvider>
      </QueryClientProvider>,
    );

    await waitFor(() =>
      expect(screen.getByRole("status")).toHaveTextContent("dark:false:true"),
    );
    await waitFor(() =>
      expect(vi.mocked(preferencesApi.update).mock.calls[0]?.[0]).toEqual({
        appearanceMode: "dark",
        palette: "teal",
        sidebarCollapsed: false,
        gradientsEnabled: true,
      }),
    );
  });

  it("keeps a selected appearance when the preference service cannot save it", async () => {
    vi.mocked(preferencesApi.get).mockResolvedValue({
      appearanceMode: "system",
      palette: "navyTeal",
      sidebarCollapsed: false,
      gradientsEnabled: true,
    });
    vi.mocked(preferencesApi.update).mockRejectedValueOnce(
      new Error("preference service unavailable"),
    );
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <AppThemeProvider userId={1}>
          <UserPreferenceProvider userId={1}>
            <PreferenceProbe />
          </UserPreferenceProvider>
        </AppThemeProvider>
      </QueryClientProvider>,
    );

    await user.click(screen.getByRole("button", { name: "تغییر به تیره" }));

    await waitFor(() =>
      expect(screen.getByRole("status")).toHaveTextContent("dark:false:true"),
    );
  });

  it("keeps the user's theme and menu choices when initial preferences arrive late", async () => {
    let resolveInitialPreferences: (value: {
      appearanceMode: "system";
      palette: "navyTeal";
      sidebarCollapsed: false;
      gradientsEnabled: true;
    }) => void;
    const initialPreferences = new Promise<{
      appearanceMode: "system";
      palette: "navyTeal";
      sidebarCollapsed: false;
      gradientsEnabled: true;
    }>((resolve) => {
      resolveInitialPreferences = resolve;
    });
    vi.mocked(preferencesApi.get).mockReturnValueOnce(initialPreferences);
    vi.mocked(preferencesApi.update).mockResolvedValue({
      appearanceMode: "dark",
      palette: "navyTeal",
      sidebarCollapsed: true,
      gradientsEnabled: false,
    });
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <AppThemeProvider userId={1}>
          <UserPreferenceProvider userId={1}>
            <PreferenceProbe />
          </UserPreferenceProvider>
        </AppThemeProvider>
      </QueryClientProvider>,
    );

    await user.click(screen.getByRole("button", { name: "تغییر به تیره" }));
    await user.click(screen.getByRole("button", { name: "بستن منو" }));
    resolveInitialPreferences!({
      appearanceMode: "system",
      palette: "navyTeal",
      sidebarCollapsed: false,
      gradientsEnabled: true,
    });

    await waitFor(() =>
      expect(screen.getByRole("status")).toHaveTextContent("dark:true:true"),
    );
  });

  it("persists the gradient toggle with the user's other preferences", async () => {
    vi.mocked(preferencesApi.get).mockResolvedValue({
      appearanceMode: "dark",
      palette: "teal",
      sidebarCollapsed: false,
      gradientsEnabled: true,
    });
    vi.mocked(preferencesApi.update).mockResolvedValue({
      appearanceMode: "dark",
      palette: "teal",
      sidebarCollapsed: false,
      gradientsEnabled: false,
    });
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    render(
      <QueryClientProvider client={queryClient}>
        <AppThemeProvider>
          <UserPreferenceProvider userId={1}>
            <PreferenceProbe />
          </UserPreferenceProvider>
        </AppThemeProvider>
      </QueryClientProvider>,
    );

    await waitFor(() =>
      expect(screen.getByRole("status")).toHaveTextContent("dark:false:true"),
    );
    await user.click(
      screen.getByRole("button", { name: "خاموش کردن گرادیانت" }),
    );

    await waitFor(() =>
      expect(vi.mocked(preferencesApi.update).mock.calls.at(-1)?.[0]).toEqual({
        appearanceMode: "dark",
        palette: "teal",
        sidebarCollapsed: false,
        gradientsEnabled: false,
      }),
    );
  });
});
