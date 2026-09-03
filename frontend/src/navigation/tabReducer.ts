import type { TabDescriptor, TabState } from "./tabTypes";

export const homeTab: TabDescriptor = {
  key: "home",
  routeId: "home",
  pathname: "/",
  search: "",
  title: "خانه",
  closable: false,
};

export const initialTabState: TabState = {
  tabs: [homeTab],
  activeKey: homeTab.key,
};

export function resolveTabPathname(tab: TabDescriptor, applicationBasePath: string) {
  return tab.key === homeTab.key ? applicationBasePath : tab.pathname;
}

export type TabAction =
  | { type: "open"; tab: TabDescriptor }
  | { type: "activate"; key: string }
  | { type: "close"; key: string; confirmed?: boolean }
  | { type: "closeOthers"; key: string; confirmed?: boolean }
  | { type: "closeAll"; confirmed?: boolean }
  | { type: "markDirty"; key: string; dirty: boolean }
  | { type: "setState"; key: string; state: TabDescriptor["state"] }
  | { type: "clear" };

export function tabReducer(state: TabState, action: TabAction): TabState {
  switch (action.type) {
    case "open": {
      const existing = state.tabs.find((tab) => tab.key === action.tab.key);
      return existing
        ? { ...state, activeKey: existing.key }
        : {
            tabs: [...state.tabs, action.tab],
            activeKey: action.tab.key,
          };
    }
    case "activate":
      return state.tabs.some((tab) => tab.key === action.key)
        ? { ...state, activeKey: action.key }
        : state;
    case "close": {
      const target = state.tabs.find((tab) => tab.key === action.key);
      if (!target?.closable || (target.dirty && !action.confirmed))
        return state;
      const index = state.tabs.indexOf(target);
      const tabs = state.tabs.filter((tab) => tab.key !== action.key);
      const activeKey =
        state.activeKey === action.key
          ? (tabs[Math.min(index, tabs.length - 1)] ?? homeTab).key
          : state.activeKey;
      return { tabs, activeKey };
    }
    case "closeOthers": {
      const keep = state.tabs.find((tab) => tab.key === action.key) ?? homeTab;
      if (
        !action.confirmed &&
        state.tabs.some((tab) => tab.key !== keep.key && tab.dirty)
      )
        return state;
      return {
        tabs: keep.key === homeTab.key ? [homeTab] : [homeTab, keep],
        activeKey: keep.key,
      };
    }
    case "closeAll":
      return !action.confirmed && state.tabs.some((tab) => tab.dirty)
        ? state
        : initialTabState;
    case "markDirty":
      return {
        ...state,
        tabs: state.tabs.map((tab) =>
          tab.key === action.key ? { ...tab, dirty: action.dirty } : tab,
        ),
      };
    case "setState":
      return {
        ...state,
        tabs: state.tabs.map((tab) =>
          tab.key === action.key ? { ...tab, state: action.state } : tab,
        ),
      };
    case "clear":
      return initialTabState;
  }
}

export function restoreTabs(serialized: string | null): TabState {
  if (!serialized) return initialTabState;
  try {
    const parsed = JSON.parse(serialized) as TabState;
    if (
      !Array.isArray(parsed.tabs) ||
      !parsed.tabs.some((tab) => tab.key === homeTab.key) ||
      !parsed.tabs.some((tab) => tab.key === parsed.activeKey)
    )
      return initialTabState;
    return parsed;
  } catch {
    return initialTabState;
  }
}
