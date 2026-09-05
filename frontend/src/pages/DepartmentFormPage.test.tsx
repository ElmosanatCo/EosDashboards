import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { Dialog } from "@mui/material";
import { describe, expect, beforeEach, it, vi } from "vitest";
import { DepartmentFormPage } from "./DepartmentFormPage";
import { administrationApi } from "../features/administration/administrationApi";
import { TabWorkspaceProvider } from "../navigation/TabWorkspaceProvider";

vi.mock("../features/administration/administrationApi", () => ({
  administrationApi: {
    departments: vi.fn(),
    createDepartment: vi.fn(),
    updateDepartment: vi.fn(),
    deleteDepartment: vi.fn(),
  },
}));

function renderForm() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <TabWorkspaceProvider>
        <Dialog open fullWidth maxWidth="sm">
          <DepartmentFormPage />
        </Dialog>
      </TabWorkspaceProvider>
    </QueryClientProvider>,
  );
}

describe("DepartmentFormPage", () => {
  beforeEach(() => {
    vi.mocked(administrationApi.departments).mockResolvedValue([
      { id: 1, name: "نرم افزار", parentDepartmentId: null, rowVersion: "v1" },
    ]);
  });

  it("fills the dialog width so its card has no desktop side gap", async () => {
    renderForm();

    await screen.findByRole("heading", { name: "تعریف واحد" });
    const form = document.querySelector("form");
    expect(form).not.toBeNull();
    expect(form).toHaveStyle({ width: "100%" });
    expect(
      screen.queryByText(
        "واحد مستقل است مگر یکی از واحدهای مستقل را به‌عنوان والد انتخاب کنید.",
      ),
    ).not.toBeInTheDocument();
  });
});
