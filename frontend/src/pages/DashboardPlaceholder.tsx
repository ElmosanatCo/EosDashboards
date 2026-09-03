import { Box, Paper, Stack, Typography } from "@mui/material";

export function DashboardPlaceholder({ title }: { title: string }) {
  return (
    <Box sx={{ width: "100%" }}>
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{
          p: { xs: 2.5, md: 3 },
          borderTop: 3,
          borderTopColor: "primary.main",
        }}
      >
        <Stack spacing={1.25}>
          <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
            {title}
          </Typography>
          <Typography color="text.secondary">
            داده‌ای برای نمایش وجود ندارد.
          </Typography>
        </Stack>
      </Paper>
    </Box>
  );
}
