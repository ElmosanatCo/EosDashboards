import { useQuery } from "@tanstack/react-query";
import { Box, CircularProgress, Paper, Stack, Typography } from "@mui/material";
import { jobDescriptionsApi } from "../features/jobDescriptions/jobDescriptionsApi";

export function ChiefExecutiveDashboardPage() {
  const warnings = useQuery({
    queryKey: ["job-description-review-warnings"],
    queryFn: jobDescriptionsApi.reviewWarnings,
  });

  if (warnings.isPending) {
    return (
      <Box sx={{ minHeight: 240, display: "grid", placeItems: "center" }}>
        <CircularProgress aria-label="در حال دریافت موارد نیازمند بررسی" />
      </Box>
    );
  }

  if (warnings.isError) {
    return (
      <Typography color="error">
        دریافت موارد نیازمند بررسی ممکن نشد.
      </Typography>
    );
  }

  return (
    <Stack spacing={2.5} sx={{ width: "100%" }}>
      <Box>
        <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
          داشبورد مدیرعامل
        </Typography>
        <Typography color="text.secondary" sx={{ mt: 0.5 }}>
          هشدارهای مدیریتی ثبت‌شده برای شرح وظایف فعال.
        </Typography>
      </Box>
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{ p: 2.5, borderTop: 3, borderTopColor: "warning.main" }}
      >
        <Typography
          component="h2"
          variant="h6"
          sx={{ fontWeight: 700, mb: 1.5 }}
        >
          موارد نیازمند بررسی
        </Typography>
        <Typography color="text.secondary" sx={{ mb: 2 }}>
          این موارد نقص داده نیستند و مانع ارسال شرح وظایف نمی‌شوند؛ فقط نشان
          می‌دهند فرد هنوز یک مهارت الزامی وظیفه را ثبت نکرده است.
        </Typography>
        {warnings.data.length === 0 ? (
          <Typography color="success.main">
            موردی برای بررسی ثبت نشده است.
          </Typography>
        ) : (
          <Stack spacing={1.25}>
            {warnings.data.map((warning) => (
              <Paper
                key={`${warning.versionId}-${warning.taskTitle}-${warning.missingSkillName}`}
                variant="outlined"
                sx={{ p: 1.5 }}
              >
                <Typography sx={{ fontWeight: 700 }}>
                  {warning.personName}
                </Typography>
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{ mt: 0.5 }}
                >
                  {warning.departmentName} · وظیفه: {warning.taskTitle}
                </Typography>
                <Typography
                  variant="body2"
                  color="warning.main"
                  sx={{ mt: 0.5 }}
                >
                  مهارت ثبت‌نشده: {warning.missingSkillName}
                </Typography>
              </Paper>
            ))}
          </Stack>
        )}
      </Paper>
    </Stack>
  );
}
