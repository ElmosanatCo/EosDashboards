import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { authorizedWorkspaceTargets } from "../navigation/workspaceTargets";
import { CommandSearch } from "./CommandSearch";

describe("command search", () => {
  it("focuses with Ctrl+K and returns only the supplied authorized targets", () => {
    const onSelect = vi.fn();
    render(
      <CommandSearch
        targets={authorizedWorkspaceTargets(["DepartmentManager"])}
        onSelect={onSelect}
      />,
    );

    fireEvent.keyDown(window, { key: "k", ctrlKey: true });

    const input = screen.getByRole("textbox", { name: "جست‌وجوی سراسری" });
    expect(input).toHaveFocus();
    expect(input).toHaveClass("eos-persian-number");
    expect(screen.getByText("Ctrl+K")).toBeVisible();
    expect(
      screen.getByRole("button", { name: "داشبورد بخش" }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "داشبورد مدیرعامل" }),
    ).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "داشبورد بخش" }));
    expect(onSelect).toHaveBeenCalledWith(
      expect.objectContaining({ routeId: "department-dashboard" }),
    );
  });
});
