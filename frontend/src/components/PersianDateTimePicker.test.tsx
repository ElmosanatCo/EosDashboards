import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it } from "vitest";
import { PersianDateTimePicker } from "./PersianDateTimePicker";

afterEach(cleanup);

describe("PersianDateTimePicker", () => {
  it("uses one Persian date field instead of separate year/month/day selects", () => {
    render(
      <PersianDateTimePicker
        label="از تاریخ و ساعت"
        value={null}
        onChange={() => undefined}
      />,
    );

    expect(
      screen.getByRole("textbox", { name: "از تاریخ و ساعت" }),
    ).toBeInTheDocument();
    expect(screen.getByLabelText("ساعت")).toBeInTheDocument();
    expect(screen.queryAllByRole("combobox")).toHaveLength(0);
  });

  it("opens a month calendar from the date field", async () => {
    const user = userEvent.setup();
    render(
      <PersianDateTimePicker
        label="تا تاریخ و ساعت"
        value={new Date(2026, 8, 4, 12, 30)}
        onChange={() => undefined}
      />,
    );

    await user.click(screen.getByRole("textbox", { name: "تا تاریخ و ساعت" }));
    expect(screen.getByRole("grid")).toBeInTheDocument();
    expect(screen.getByRole("gridcell", { name: "۴" })).toBeInTheDocument();
  });

  it("can be used as a date-only picker", () => {
    render(
      <PersianDateTimePicker
        label="تاریخ شروع"
        value={null}
        includeTime={false}
        onChange={() => undefined}
      />,
    );

    expect(
      screen.getByRole("textbox", { name: "تاریخ شروع" }),
    ).toBeInTheDocument();
    expect(screen.queryByLabelText("ساعت")).not.toBeInTheDocument();
  });
});
