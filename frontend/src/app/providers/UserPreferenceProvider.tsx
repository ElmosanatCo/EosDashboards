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
import { normalizePaletteId } from "../../theme/palettes";
import type { PaletteId } from "../../theme/palettes";

type PreferenceContextValue = {
  appearanceMode: AppearanceMode;
  resolvedAppearanceMode: "light" | "dark";
  palette: PaletteId;
  sidebarCollapsed: boolean;
  gradientsEnabled: boolean;
  updateAppearance: (mode: AppearanceMode) => void;
  toggleAppearance: () => void;
  updatePalette: (palette: PaletteId) => void;
  updateGradientsEnabled: (enabled: boolean) => void;
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
  const [gradientsEnabled, setGradientsEnabled] = useState(true);
  const preferenceRef = useRef<UserPreference>({
    appearanceMode: mode,
    palette,
    sidebarCollapsed,
    gradientsEnabled: true,
  });
  const sidebarTimer = useRef<number | undefined>(undefined);
  const hasLocalChanges = useRef(false);
  const hasLocalGradientChange = useRef(false);
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
        gradientsEnabled: hasLocalGradientChange.current
          ? preferenceRef.current.gradientsEnabled
          : (query.data.gradientsEnabled ?? true),
      };
      hasLocalChanges.current = true;
      preferenceRef.current = next;
      void persist(next);
      setSidebarCollapsed(query.data.sidebarCollapsed);
      setGradientsEnabled(next.gradientsEnabled);
      return;
    }
    const normalized = {
      ...query.data,
      palette: normalizePaletteId(query.data.palette),
      gradientsEnabled: query.data.gradientsEnabled ?? true,
    };
    preferenceRef.current = normalized;
    setMode(normalized.appearanceMode);
    setPalette(normalized.palette);
    setSidebarCollapsed(normalized.sidebarCollapsed);
    setGradientsEnabled(normalized.gradientsEnabled);
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

  const updateGradientsEnabled = useCallback(
    (enabled: boolean) => {
      const next = { ...preferenceRef.current, gradientsEnabled: enabled };
      hasLocalChanges.current = true;
      hasLocalGradientChange.current = true;
      preferenceRef.current = next;
      setGradientsEnabled(enabled);
      void persist(next);
    },
    [persist],
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
      gradientsEnabled,
      updateAppearance,
      toggleAppearance,
      updatePalette,
      updateGradientsEnabled,
      updateSidebarCollapsed,
    }),
    [
      mode,
      resolvedMode,
      palette,
      sidebarCollapsed,
      gradientsEnabled,
      toggleAppearance,
      updateAppearance,
      updatePalette,
      updateGradientsEnabled,
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
