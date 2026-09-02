import { Paper, Stack, Typography } from "@mui/material";

export function HomePage() {
  return (
    <Paper sx={{ p: { xs: 3, md: 5 }, textAlign: "center" }}>
      <Stack spacing={2}>
        <Typography variant="h4" component="h1">
          خوش آمدید
        </Typography>
        <Typography color="text.secondary">
          داشبوردها به‌زودی اضافه می‌شوند
        </Typography>
      </Stack>
    </Paper>
  );
}
