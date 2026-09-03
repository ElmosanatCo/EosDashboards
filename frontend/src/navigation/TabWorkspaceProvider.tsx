import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useReducer,
} from "react";
import type { Dispatch, PropsWithChildren } from "react";
import {
  initialTabState,
  resolveTabPathname,
  restoreTabs,
  tabReducer,
} from "./tabReducer";
import type { TabAction } from "./tabReducer";
import type { TabDescriptor, TabState } from "./tabTypes";

const storageKey = "eos.workspace.tabs.v1";
type TabWorkspace = TabState & {
  dispatch: Dispatch<TabAction>;
  openTab: (tab: TabDescriptor) => void;
  clearSessionTabs: () => void;
};
const Context = createContext<TabWorkspace | null>(null);

export function TabWorkspaceProvider({ children }: PropsWithChildren) {
  const [state, dispatch] = useReducer(tabReducer, initialTabState, () =>
    restoreTabs(sessionStorage.getItem(storageKey)),
  );
  useEffect(() => {
    sessionStorage.setItem(storageKey, JSON.stringify(state));
    const active = state.tabs.find((tab) => tab.key === state.activeKey);
    const pathname = active
      ? resolveTabPathname(active, import.meta.env.BASE_URL)
      : undefined;
    if (
      pathname &&
      active &&
      `${location.pathname}${location.search}` !==
        `${pathname}${active.search}`
    ) {
      history.replaceState(null, "", `${pathname}${active.search}`);
    }
  }, [state]);
  const value = useMemo(
    () => ({
      ...state,
      dispatch,
      openTab: (tab: TabDescriptor) => dispatch({ type: "open", tab }),
      clearSessionTabs: () => {
        sessionStorage.removeItem(storageKey);
        dispatch({ type: "clear" });
      },
    }),
    [state],
  );
  return <Context value={value}>{children}</Context>;
}

export function useTabWorkspace() {
  const value = useContext(Context);
  if (!value)
    throw new Error("useTabWorkspace must be used within TabWorkspaceProvider");
  return value;
}
