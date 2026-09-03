import createCache from "@emotion/cache";
import { CacheProvider } from "@emotion/react";
import { CssBaseline, useMediaQuery } from "@mui/material";
import { ThemeProvider } from "@mui/material/styles";
import { prefixer } from "stylis";
import rtlPlugin from "stylis-plugin-rtl";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import type { PropsWithChildren } from "react";
import { createAppTheme } from "../../theme/createAppTheme";

export type AppearanceMode = "light" | "dark" | "system";

type AppearanceContextValue = {
  mode: AppearanceMode;
  resolvedMode: "light" | "dark";
  setMode: (mode: AppearanceMode) => void;
};

const cache = createCache({
  key: "eos-rtl",
  stylisPlugins: [prefixer, rtlPlugin],
});
const AppearanceContext = createContext<AppearanceContextValue | null>(null);

function readStoredMode(storageKey: string): AppearanceMode {
  const value = localStorage.getItem(storageKey);
  return value === "light" || value === "dark" || value === "system"
    ? value
    : "system";
}

export function AppThemeProvider({
  children,
  userId,
}: PropsWithChildren<{ userId?: number }>) {
  const storageKey = userId
    ? `eos.appearance.user.${userId}`
    : "eos.appearance.anonymous";
  const prefersDark = useMediaQuery("(prefers-color-scheme: dark)");
  const [mode, setModeState] = useState<AppearanceMode>(() =>
    readStoredMode(storageKey),
  );

  useEffect(() => setModeState(readStoredMode(storageKey)), [storageKey]);
  const setMode = useCallback(
    (nextMode: AppearanceMode) => {
      localStorage.setItem(storageKey, nextMode);
      setModeState(nextMode);
    },
    [storageKey],
  );
  const resolvedMode =
    mode === "system" ? (prefersDark ? "dark" : "light") : mode;
  const theme = useMemo(() => createAppTheme(resolvedMode), [resolvedMode]);
  const value = useMemo(
    () => ({ mode, resolvedMode, setMode }),
    [mode, resolvedMode, setMode],
  );

  useEffect(() => {
    document.documentElement.dir = "rtl";
    document.documentElement.lang = "fa";
  }, []);

  return (
    <CacheProvider value={cache}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <AppearanceContext value={value}>{children}</AppearanceContext>
      </ThemeProvider>
    </CacheProvider>
  );
}

export function useAppearance() {
  const value = useContext(AppearanceContext);
  if (!value)
    throw new Error("useAppearance must be used within AppThemeProvider");
  return value;
}
