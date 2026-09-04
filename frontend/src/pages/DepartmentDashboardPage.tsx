import { useQuery } from "@tanstack/react-query";
import {
  Box,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Typography,
} from "@mui/material";
import { useState } from "react";
import { jobDescriptionsApi } from "../features/jobDescriptions/jobDescriptionsApi";

export function DepartmentDashboardPage() {
  const [departmentId, setDepartmentId] = useState<number | "all">("all");
  const departments = useQuery({
    queryKey: ["managed-departments"],
    queryFn: jobDescriptionsApi.managedDepartments,
  });
  const dashboard = useQuery({
    queryKey: ["department-dashboard", departmentId],
    queryFn: () =>
      jobDescriptionsApi.dashboard(
        departmentId === "all" ? undefined : departmentId,
      ),
  });

  if (dashboard.isPending) {
    return (
      <Box sx={{ minHeight: 240, display: "grid", placeItems: "center" }}>
        <CircularProgress aria-label="در حال دریافت داشبورد" />
      </Box>
    );
  }
  if (dashboard.isError) {
    return <Typography color="error">دریافت داشبورد بخش ممکن نشد.</Typography>;
  }

  const metrics = dashboard.data;
  return (
    <Stack spacing={2.5} sx={{ width: "100%" }}>
      <Box>
        <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
          داشبورد بخش
        </Typography>
        <Typography color="text.secondary" sx={{ mt: 0.5 }}>
          نمایش آمار ثبت‌شده برای بخش‌های تحت مدیریت شما.
        </Typography>
      </Box>
      {departments.isSuccess && departments.data.length > 0 ? (
        <FormControl sx={{ minWidth: { xs: "100%", sm: 280 } }}>
          <InputLabel id="dashboard-department-label">محدوده نمایش</InputLabel>
          <Select
            labelId="dashboard-department-label"
            value={departmentId}
            label="محدوده نمایش"
            onChange={(event) => {
              const value = String(event.target.value);
              setDepartmentId(value === "all" ? "all" : Number(value));
            }}
          >
            <MenuItem value="all">همه بخش‌ها</MenuItem>
            {departments.data.map((department) => (
              <MenuItem key={department.id} value={department.id}>
                {department.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      ) : null}
      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1.5 }}>
        <MetricCard label="کل پرسنل" value={metrics.personnelCount} />
        <MetricCard label="پرسنل فعال" value={metrics.activePersonnelCount} />
        <MetricCard
          label="پرسنل بایگانی‌شده"
          value={metrics.archivedPersonnelCount}
        />
        <MetricCard label="شرح سالم" value={metrics.healthyDescriptionCount} />
        <MetricCard
          label="شرح ناقص"
          value={metrics.incompleteDescriptionCount}
          tone="warning"
        />
        <MetricCard label="پروژه‌های فعال" value={metrics.activeProjectCount} />
        <MetricCard
          label="افراد روی پروژه‌ها"
          value={metrics.peopleWorkingOnActiveProjectsCount}
        />
      </Box>
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{ p: 2.5, borderTop: 3, borderTopColor: "primary.main" }}
      >
        <Typography
          component="h2"
          variant="h6"
          sx={{ fontWeight: 700, mb: 1.5 }}
        >
          کارهای نیازمند پیگیری
        </Typography>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
          <Typography>
            منتظر رفع نقص:{" "}
            <strong className="eos-persian-number">
              {metrics.pendingDataCompletionCount}
            </strong>
          </Typography>
          <Typography>
            منتظر تأیید مدیر:{" "}
            <strong className="eos-persian-number">
              {metrics.pendingDepartmentApprovalCount}
            </strong>
          </Typography>
          <Typography>
            در حال بررسی منابع انسانی:{" "}
            <strong className="eos-persian-number">
              {metrics.underHumanResourcesReviewCount}
            </strong>
          </Typography>
          <Typography>
            رد شده:{" "}
            <strong className="eos-persian-number">
              {metrics.rejectedDescriptionCount}
            </strong>
          </Typography>
        </Stack>
      </Paper>
    </Stack>
  );
}

function MetricCard({
  label,
  value,
  tone = "default",
}: {
  label: string;
  value: number;
  tone?: "default" | "warning";
}) {
  return (
    <Paper
      variant="outlined"
      className="eos-accent-card"
      sx={{
        p: 2,
        flex: "1 1 170px",
        borderTop: 3,
        borderTopColor: tone === "warning" ? "warning.main" : "primary.main",
      }}
    >
      <Typography color="text.secondary" variant="body2">
        {label}
      </Typography>
      <Typography
        className="eos-persian-number"
        variant="h4"
        sx={{ mt: 0.5, fontWeight: 750 }}
      >
        {value.toLocaleString("fa-IR")}
      </Typography>
    </Paper>
  );
}
