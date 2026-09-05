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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { useState } from "react";
import { jobDescriptionsApi } from "../features/jobDescriptions/jobDescriptionsApi";
import { formatPersianDateTime } from "../lib/date/persianDateTime";
import { formatPersianNumber } from "../lib/format/persianDigits";

export function HumanResourcesDashboardPage() {
  const [departmentId, setDepartmentId] = useState<number | "all">("all");
  const departments = useQuery({
    queryKey: ["human-resources-departments"],
    queryFn: jobDescriptionsApi.humanResourcesDepartments,
  });
  const dashboard = useQuery({
    queryKey: ["human-resources-dashboard", departmentId],
    queryFn: () =>
      jobDescriptionsApi.humanResourcesDashboard(
        departmentId === "all" ? undefined : departmentId,
      ),
  });

  if (dashboard.isPending) {
    return <LoadingState label="در حال دریافت داشبورد منابع انسانی" />;
  }
  if (dashboard.isError) {
    return (
      <Typography color="error">
        دریافت داشبورد منابع انسانی ممکن نشد.
      </Typography>
    );
  }

  const { metrics, changeSummaries, changes, totalChangeCount } =
    dashboard.data;
  return (
    <Stack spacing={2.5} sx={{ width: "100%" }}>
      <Box>
        <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
          داشبورد منابع انسانی
        </Typography>
        <Typography color="text.secondary" sx={{ mt: 0.5 }}>
          نمای سازمانی وضعیت شرح وظایف، کارتابل تأیید و روند تغییرات بخش‌ها.
        </Typography>
      </Box>
      {departments.isSuccess ? (
        <FormControl sx={{ minWidth: { xs: "100%", sm: 280 } }}>
          <InputLabel id="human-resources-dashboard-department-label">
            محدوده نمایش
          </InputLabel>
          <Select
            labelId="human-resources-dashboard-department-label"
            aria-label="محدوده نمایش"
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
        <MetricCard label="شرح سالم" value={metrics.healthyDescriptionCount} />
        <MetricCard
          label="شرح ناقص"
          value={metrics.incompleteDescriptionCount}
          tone="warning"
        />
        <MetricCard
          label="در انتظار بررسی منابع انسانی"
          value={metrics.underHumanResourcesReviewCount}
          tone="warning"
        />
        <MetricCard
          label="شرح تأییدشده"
          value={metrics.approvedDescriptionCount}
          tone="success"
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
          آمار تغییرات هر بخش
        </Typography>
        {changeSummaries.length === 0 ? (
          <Typography color="text.secondary">
            تغییری برای نمایش ثبت نشده است.
          </Typography>
        ) : (
          <Table size="small" aria-label="آمار تغییرات هر بخش">
            <TableHead>
              <TableRow>
                <TableCell>بخش</TableCell>
                <TableCell>تعداد تغییر</TableCell>
                <TableCell>آخرین تغییر</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {changeSummaries.map((summary) => (
                <TableRow key={summary.departmentId}>
                  <TableCell>{summary.departmentName}</TableCell>
                  <TableCell className="eos-persian-number">
                    {formatPersianNumber(summary.changeCount)}
                  </TableCell>
                  <TableCell>
                    {summary.latestChangedAt
                      ? formatDate(summary.latestChangedAt)
                      : "—"}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Paper>
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{ p: 2.5, borderTop: 3, borderTopColor: "primary.main" }}
      >
        <Box sx={{ display: "flex", justifyContent: "space-between", mb: 1.5 }}>
          <Typography component="h2" variant="h6" sx={{ fontWeight: 700 }}>
            تاریخچه تغییرات
          </Typography>
          <Typography color="text.secondary" className="eos-persian-number">
            {formatPersianNumber(totalChangeCount)} مورد
          </Typography>
        </Box>
        {changes.length === 0 ? (
          <Typography color="text.secondary">
            تاریخچه‌ای برای نمایش ثبت نشده است.
          </Typography>
        ) : (
          <Table size="small" aria-label="تاریخچه تغییرات">
            <TableHead>
              <TableRow>
                <TableCell>شرح وظیفه</TableCell>
                <TableCell>بخش</TableCell>
                <TableCell>نوع تغییر</TableCell>
                <TableCell>زمان</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {changes.map((change) => (
                <TableRow key={change.versionId}>
                  <TableCell sx={{ fontWeight: 650 }}>
                    {change.personName}
                  </TableCell>
                  <TableCell>{change.departmentName}</TableCell>
                  <TableCell>{change.changeType}</TableCell>
                  <TableCell>{formatDate(change.changedAt)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Paper>
    </Stack>
  );
}

function LoadingState({ label }: { label: string }) {
  return (
    <Box sx={{ minHeight: 240, display: "grid", placeItems: "center" }}>
      <CircularProgress aria-label={label} />
    </Box>
  );
}

function formatDate(value: string) {
  const formatted = formatPersianDateTime(new Date(value));
  return `${formatted.date} · ${formatted.time}`;
}

function MetricCard({
  label,
  value,
  tone = "default",
}: {
  label: string;
  value: number;
  tone?: "default" | "warning" | "success";
}) {
  return (
    <Paper
      variant="outlined"
      className="eos-accent-card"
      sx={{
        p: 2,
        flex: "1 1 190px",
        borderTop: 3,
        borderTopColor:
          tone === "warning"
            ? "warning.main"
            : tone === "success"
              ? "success.main"
              : "primary.main",
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
        {formatPersianNumber(value)}
      </Typography>
    </Paper>
  );
}
