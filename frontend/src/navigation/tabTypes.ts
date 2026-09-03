export type TabDescriptor = {
  key: string;
  routeId: string;
  pathname: string;
  search: string;
  title: string;
  closable: boolean;
  dirty?: boolean;
  state?: Record<string, string | number | boolean | null>;
};

export type TabState = { tabs: TabDescriptor[]; activeKey: string };
