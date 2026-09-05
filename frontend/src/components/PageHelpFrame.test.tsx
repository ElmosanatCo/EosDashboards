import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { PageHelpFrame } from "./PageHelpFrame";

afterEach(cleanup);

describe("PageHelpFrame", () => {
  it("shows structured, page-specific guidance and can be closed", async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <PageHelpFrame routeId="department-task-catalog" open onClose={onClose}>
        <div>محتوای صفحه</div>
      </PageHelpFrame>,
    );

    const frame = screen.getByTestId("page-help-frame");
    expect(frame).toHaveStyle({ position: "relative" });
    expect(
      screen.queryByRole("button", { name: "راهنمای کاتالوگ وظایف" }),
    ).not.toBeInTheDocument();
    expect(screen.getByTestId("page-help-content")).toHaveStyle({
      paddingInlineEnd: "0px",
    });

    expect(
      screen.getByRole("heading", { name: "راهنمای کاتالوگ وظایف" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "وظایف این صفحه" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "امکانات" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "شیوه انجام کار" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("heading", { name: "محدودیت‌ها" }),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/کاتالوگ وظایف مرجع عنوان‌های استاندارد هر بخش است/),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/فهرست وظایف با نمایش عنوان.*تعداد مهارت‌های لازم/),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        /عملیات «مهارت» برای مشاهده و انتخاب مهارت‌های عمومی و اختصاصی/,
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/در فهرست، تعداد مهارت‌های لازم هر ردیف را بررسی کنید/),
    ).toBeInTheDocument();
    expect(screen.getAllByRole("list")[0]).toHaveStyle({
      paddingRight: "0px",
    });
    const firstListItem = screen.getAllByRole("listitem")[0];
    expect(firstListItem).toHaveStyle({
      listStyle: "none",
      paddingInlineStart: "16px",
    });

    await user.click(screen.getByRole("button", { name: "بستن راهنما" }));
    expect(onClose).toHaveBeenCalledOnce();
  });
});
