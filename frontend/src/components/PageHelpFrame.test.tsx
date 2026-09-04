import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it } from "vitest";
import { PageHelpFrame } from "./PageHelpFrame";

afterEach(cleanup);

describe("PageHelpFrame", () => {
  it("shows structured, page-specific guidance and can be closed", async () => {
    const user = userEvent.setup();
    render(
      <PageHelpFrame routeId="department-task-catalog">
        <div>محتوای صفحه</div>
      </PageHelpFrame>,
    );

    const frame = screen.getByTestId("page-help-frame");
    const helpButton = screen.getByRole("button", {
      name: "راهنمای کاتالوگ وظایف",
    });
    expect(frame).toHaveStyle({ position: "relative" });
    expect(helpButton).toHaveStyle({
      position: "absolute",
      insetInlineEnd: "0px",
      top: "0px",
    });
    expect(screen.getByTestId("page-help-content")).toHaveStyle({
      paddingInlineEnd: "0px",
    });

    await user.click(helpButton);

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
    await waitFor(() =>
      expect(
        screen.queryByRole("heading", { name: "راهنمای کاتالوگ وظایف" }),
      ).not.toBeInTheDocument(),
    );
  });
});
