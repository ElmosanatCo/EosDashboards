import { Box, Paper, Stack, Typography } from "@mui/material";

export function HomePage() {
  return (
    <Box sx={{ width: "100%" }}>
      <Paper
        variant="outlined"
        sx={{ p: { xs: 3, md: 5 }, textAlign: "right" }}
      >
        <Stack spacing={1.25}>
          <Typography
            variant="overline"
            color="primary.main"
            sx={{ textAlign: "start" }}
          >
            صفحهٔ اصلی
          </Typography>
          <Typography variant="h4" component="h1" sx={{ textAlign: "start" }}>
            خوش آمدید
          </Typography>
          <Typography color="text.secondary" sx={{ textAlign: "start" }}>
            داشبوردها به‌زودی اضافه می‌شوند
          </Typography>
        </Stack>
      </Paper>
    </Box>
  );
}
