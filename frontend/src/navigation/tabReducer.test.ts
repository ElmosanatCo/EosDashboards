import { describe, expect, it } from "vitest";
import { homeTab, initialTabState, restoreTabs, tabReducer } from "./tabReducer";

const report = { key: "report?id=1", routeId: "report", pathname: "/report/1", search: "", title: "گزارش", closable: true };

describe("tabReducer", () => {
  it("deduplicates, keeps home fixed, and falls back after close", () => {
    const opened = tabReducer(tabReducer(initialTabState, { type: "open", tab: report }), { type: "open", tab: report });
    expect(opened.tabs).toHaveLength(2);
    expect(tabReducer(opened, { type: "close", key: homeTab.key })).toBe(opened);
    expect(tabReducer(opened, { type: "close", key: report.key })).toEqual(initialTabState);
  });

  it("rejects dirty close and safely recovers corrupt storage", () => {
    const dirty = tabReducer(tabReducer(initialTabState, { type: "open", tab: report }), { type: "markDirty", key: report.key, dirty: true });
    expect(tabReducer(dirty, { type: "close", key: report.key })).toBe(dirty);
    expect(tabReducer(dirty, { type: "close", key: report.key, confirmed: true })).toEqual(initialTabState);
    expect(restoreTabs("not-json")).toEqual(initialTabState);
  });
});
