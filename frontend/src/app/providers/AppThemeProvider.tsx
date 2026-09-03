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
import { defaultPaletteId, normalizePaletteId } from "../../theme/palettes";
import type { PaletteId } from "../../theme/palettes";

export type AppearanceMode = "light" | "dark" | "system";

type AppearanceContextValue = {
  mode: AppearanceMode;
  resolvedMode: "light" | "dark";
  palette: PaletteId;
  hasPersistedAppearance: boolean;
  setMode: (mode: AppearanceMode) => void;
  setPalette: (palette: PaletteId) => void;
};

const cache = createCache({
  key: "eos-rtl",
  stylisPlugins: [prefixer, rtlPlugin],
});
const AppearanceContext = createContext<AppearanceContextValue | null>(null);
const lastUsedModeStorageKey = "eos.appearance.last-used";
const lastUsedPaletteStorageKey = "eos.palette.last-used";

function readStoredMode(...storageKeys: string[]): AppearanceMode {
  for (const storageKey of storageKeys) {
    const value = localStorage.getItem(storageKey);
    if (value === "light" || value === "dark" || value === "system") {
      return value;
    }
  }
  return "dark";
}

function readStoredPalette(...storageKeys: string[]): PaletteId {
  for (const storageKey of storageKeys) {
    const value = localStorage.getItem(storageKey);
    if (value) return normalizePaletteId(value);
  }
  return defaultPaletteId;
}

function hasStoredAppearance(...storageKeys: string[]) {
  return storageKeys.some(
    (storageKey) => localStorage.getItem(storageKey) !== null,
  );
}

export function AppThemeProvider({
  children,
  userId,
}: PropsWithChildren<{ userId?: number }>) {
  const storageKey = userId
    ? `eos.appearance.user.${userId}`
    : "eos.appearance.anonymous";
  const paletteStorageKey = userId
    ? `eos.palette.user.${userId}`
    : "eos.palette.anonymous";
  const modeStorageKeys = userId
    ? [storageKey, lastUsedModeStorageKey]
    : [lastUsedModeStorageKey, storageKey];
  const paletteStorageKeys = userId
    ? [paletteStorageKey, lastUsedPaletteStorageKey]
    : [lastUsedPaletteStorageKey, paletteStorageKey];
  const prefersDark = useMediaQuery("(prefers-color-scheme: dark)");
  const [mode, setModeState] = useState<AppearanceMode>(() =>
    readStoredMode(...modeStorageKeys),
  );
  const [palette, setPaletteState] = useState<PaletteId>(() =>
    readStoredPalette(...paletteStorageKeys),
  );
  const [hasPersistedAppearance] = useState(() =>
    hasStoredAppearance(...modeStorageKeys, ...paletteStorageKeys),
  );

  useEffect(
    () => setModeState(readStoredMode(...modeStorageKeys)),
    [storageKey, userId],
  );
  useEffect(
    () => setPaletteState(readStoredPalette(...paletteStorageKeys)),
    [paletteStorageKey, userId],
  );
  const setMode = useCallback(
    (nextMode: AppearanceMode) => {
      localStorage.setItem(storageKey, nextMode);
      localStorage.setItem(lastUsedModeStorageKey, nextMode);
      setModeState(nextMode);
    },
    [storageKey],
  );
  const setPalette = useCallback(
    (nextPalette: PaletteId) => {
      localStorage.setItem(paletteStorageKey, nextPalette);
      localStorage.setItem(lastUsedPaletteStorageKey, nextPalette);
      setPaletteState(nextPalette);
    },
    [paletteStorageKey],
  );
  const resolvedMode =
    mode === "system" ? (prefersDark ? "dark" : "light") : mode;
  const theme = useMemo(
    () => createAppTheme(resolvedMode, palette),
    [palette, resolvedMode],
  );
  const value = useMemo(
    () => ({
      mode,
      resolvedMode,
      palette,
      hasPersistedAppearance,
      setMode,
      setPalette,
    }),
    [hasPersistedAppearance, mode, palette, resolvedMode, setMode, setPalette],
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
