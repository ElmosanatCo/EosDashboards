import { useMutation, useQuery } from "@tanstack/react-query";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import type { PropsWithChildren } from "react";
import { preferencesApi } from "../../features/preferences/preferencesApi";
import type { UserPreference } from "../../features/preferences/preferencesApi";
import { useAppearance } from "./AppThemeProvider";
import type { AppearanceMode } from "./AppThemeProvider";
import { nextAppearanceForToggle } from "../../theme/appearanceToggle";
import type { PaletteId } from "../../theme/palettes";

type PreferenceContextValue = {
  appearanceMode: AppearanceMode;
  resolvedAppearanceMode: "light" | "dark";
  palette: PaletteId;
  sidebarCollapsed: boolean;
  updateAppearance: (mode: AppearanceMode) => void;
  toggleAppearance: () => void;
  updatePalette: (palette: PaletteId) => void;
  updateSidebarCollapsed: (collapsed: boolean) => void;
};
const Context = createContext<PreferenceContextValue | null>(null);

export function UserPreferenceProvider({
  children,
  userId,
}: PropsWithChildren<{ userId: number }>) {
  const {
    mode,
    resolvedMode,
    palette,
    hasPersistedAppearance,
    setMode,
    setPalette,
  } = useAppearance();
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const preferenceRef = useRef<UserPreference>({
    appearanceMode: mode,
    palette,
    sidebarCollapsed,
  });
  const sidebarTimer = useRef<number | undefined>(undefined);
  const hasLocalChanges = useRef(false);
  const query = useQuery({
    queryKey: ["preferences", userId],
    queryFn: preferencesApi.get,
  });
  const mutation = useMutation({ mutationFn: preferencesApi.update });

  const persist = useCallback(
    async (next: UserPreference) => {
      preferenceRef.current = next;
      try {
        preferenceRef.current = await mutation.mutateAsync(next);
      } catch {
        // Keep the user's per-device choice until the next synchronization attempt.
      }
    },
    [mutation],
  );

  useEffect(() => {
    if (!query.data || hasLocalChanges.current) return;
    if (hasPersistedAppearance) {
      const next = {
        ...query.data,
        appearanceMode: mode,
        palette,
      };
      hasLocalChanges.current = true;
      preferenceRef.current = next;
      void persist(next);
      setSidebarCollapsed(query.data.sidebarCollapsed);
      return;
    }
    preferenceRef.current = query.data;
    setMode(query.data.appearanceMode);
    setPalette(query.data.palette);
    setSidebarCollapsed(query.data.sidebarCollapsed);
  }, [
    hasPersistedAppearance,
    mode,
    palette,
    persist,
    query.data,
    setMode,
    setPalette,
  ]);

  const updateAppearance = useCallback(
    (appearanceMode: AppearanceMode) => {
      const next = { ...preferenceRef.current, appearanceMode };
      hasLocalChanges.current = true;
      setMode(appearanceMode);
      void persist(next);
    },
    [persist, setMode],
  );

  const updateSidebarCollapsed = useCallback(
    (collapsed: boolean) => {
      const next = { ...preferenceRef.current, sidebarCollapsed: collapsed };
      hasLocalChanges.current = true;
      preferenceRef.current = next;
      setSidebarCollapsed(collapsed);
      window.clearTimeout(sidebarTimer.current);
      sidebarTimer.current = window.setTimeout(() => void persist(next), 400);
    },
    [persist],
  );

  const updatePalette = useCallback(
    (nextPalette: PaletteId) => {
      const next = { ...preferenceRef.current, palette: nextPalette };
      hasLocalChanges.current = true;
      setPalette(nextPalette);
      void persist(next);
    },
    [persist, setPalette],
  );

  const toggleAppearance = useCallback(
    () => updateAppearance(nextAppearanceForToggle(resolvedMode)),
    [resolvedMode, updateAppearance],
  );

  useEffect(() => () => window.clearTimeout(sidebarTimer.current), []);
  const value = useMemo(
    () => ({
      appearanceMode: mode,
      resolvedAppearanceMode: resolvedMode,
      palette,
      sidebarCollapsed,
      updateAppearance,
      toggleAppearance,
      updatePalette,
      updateSidebarCollapsed,
    }),
    [
      mode,
      resolvedMode,
      palette,
      sidebarCollapsed,
      toggleAppearance,
      updateAppearance,
      updatePalette,
      updateSidebarCollapsed,
    ],
  );
  return <Context value={value}>{children}</Context>;
}

export function useUserPreferences() {
  const value = useContext(Context);
  if (!value)
    throw new Error(
      "useUserPreferences must be used within UserPreferenceProvider",
    );
  return value;
}
