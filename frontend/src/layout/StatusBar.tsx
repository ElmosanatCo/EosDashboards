import { Box, Typography } from "@mui/material";
import { memo, useEffect, useState } from "react";
import { formatPersianDateTime } from "../lib/date/persianDateTime";

export const statusBarHeight = 54;

const Clock = memo(function Clock() {
  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 1_000);
    return () => window.clearInterval(timer);
  }, []);
  const value = formatPersianDateTime(now);
  return (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        alignItems: "flex-end",
        gap: 0,
      }}
    >
      <Typography aria-label="تاریخ سیستم" variant="caption">
        تاریخ: {value.date}
      </Typography>
      <Typography aria-label="ساعت سیستم" variant="caption">
        ساعت: {value.time}
      </Typography>
    </Box>
  );
});

export function StatusBar() {
  return (
    <Box
      component="footer"
      sx={{
        minHeight: statusBarHeight,
        boxSizing: "border-box",
        px: 2,
        py: 0.5,
        bgcolor: "background.paper",
        borderTop: 1,
        borderColor: "divider",
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        color: "text.secondary",
      }}
    >
      <Typography variant="caption">نسخه {__APP_VERSION__}</Typography>
      <Clock />
    </Box>
  );
}
