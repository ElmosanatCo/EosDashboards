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

type PreferenceContextValue = {
  appearanceMode: AppearanceMode;
  sidebarCollapsed: boolean;
  updateAppearance: (mode: AppearanceMode) => void;
  updateSidebarCollapsed: (collapsed: boolean) => void;
  saveFailed: boolean;
};
const Context = createContext<PreferenceContextValue | null>(null);

export function UserPreferenceProvider({
  children,
  userId,
}: PropsWithChildren<{ userId: number }>) {
  const { mode, setMode } = useAppearance();
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [saveFailed, setSaveFailed] = useState(false);
  const preferenceRef = useRef<UserPreference>({
    appearanceMode: mode,
    palette: "navyTeal",
    sidebarCollapsed,
  });
  const sidebarTimer = useRef<number | undefined>(undefined);
  const query = useQuery({
    queryKey: ["preferences", userId],
    queryFn: preferencesApi.get,
  });
  const mutation = useMutation({ mutationFn: preferencesApi.update });

  useEffect(() => {
    if (!query.data) return;
    preferenceRef.current = query.data;
    setMode(query.data.appearanceMode);
    setSidebarCollapsed(query.data.sidebarCollapsed);
  }, [query.data, setMode]);

  const persist = useCallback(
    async (next: UserPreference, previous: UserPreference) => {
      preferenceRef.current = next;
      setSaveFailed(false);
      try {
        preferenceRef.current = await mutation.mutateAsync(next);
      } catch {
        preferenceRef.current = previous;
        setMode(previous.appearanceMode);
        setSidebarCollapsed(previous.sidebarCollapsed);
        setSaveFailed(true);
      }
    },
    [mutation, setMode],
  );

  const updateAppearance = useCallback(
    (appearanceMode: AppearanceMode) => {
      const previous = preferenceRef.current;
      const next = { ...previous, appearanceMode };
      setMode(appearanceMode);
      void persist(next, previous);
    },
    [persist, setMode],
  );

  const updateSidebarCollapsed = useCallback(
    (collapsed: boolean) => {
      const previous = preferenceRef.current;
      const next = { ...previous, sidebarCollapsed: collapsed };
      preferenceRef.current = next;
      setSidebarCollapsed(collapsed);
      window.clearTimeout(sidebarTimer.current);
      sidebarTimer.current = window.setTimeout(
        () => void persist(next, previous),
        400,
      );
    },
    [persist],
  );

  useEffect(() => () => window.clearTimeout(sidebarTimer.current), []);
  const value = useMemo(
    () => ({
      appearanceMode: mode,
      sidebarCollapsed,
      updateAppearance,
      updateSidebarCollapsed,
      saveFailed,
    }),
    [
      mode,
      saveFailed,
      sidebarCollapsed,
      updateAppearance,
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
