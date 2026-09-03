import { apiFetch } from "../../lib/api/apiClient";
import type { AppearanceMode } from "../../app/providers/AppThemeProvider";
import type { PaletteId } from "../../theme/palettes";

export type UserPreference = {
  appearanceMode: AppearanceMode;
  palette: PaletteId;
  sidebarCollapsed: boolean;
};

export const preferencesApi = {
  get: () => apiFetch<UserPreference>("/api/v1/users/me/preferences/"),
  update: (preference: UserPreference) =>
    apiFetch<UserPreference>("/api/v1/users/me/preferences/", {
      method: "PUT",
      body: JSON.stringify(preference),
    }),
};
