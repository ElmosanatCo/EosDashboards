import { Box, Paper, Stack, Typography } from "@mui/material";

export function HomePage() {
  return (
    <Box sx={{ width: "100%", maxWidth: 1120, mx: "auto" }}>
      <Paper
        variant="outlined"
        sx={{
          borderTop: "3px solid",
          borderTopColor: "primary.main",
          p: { xs: 2.5, md: 3 },
          textAlign: "right",
          transition: "border-color 175ms ease",
          "&:hover": { borderTopColor: "#E0A13A" },
        }}
      >
        <Stack spacing={1.25} sx={{ maxWidth: 560 }}>
          <Typography
            variant="overline"
            color="primary.main"
            sx={{ textAlign: "start" }}
          >
            خانه
          </Typography>
          <Typography
            variant="h5"
            component="h1"
            sx={{ textAlign: "start", fontWeight: 750 }}
          >
            فضای کاری مدیریت
          </Typography>
          <Typography color="text.secondary" sx={{ textAlign: "start" }}>
            داده‌ای برای نمایش وجود ندارد.
          </Typography>
          <Typography
            variant="body2"
            color="text.secondary"
            sx={{ textAlign: "start" }}
          >
            پس از تعریف و اتصال نخستین داشبورد، اطلاعات مربوط به نقش شما در این
            فضا نمایش داده می‌شود.
          </Typography>
        </Stack>
      </Paper>
    </Box>
  );
}
