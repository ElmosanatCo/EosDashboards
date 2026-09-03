import { Box, Typography } from "@mui/material";
import { memo, useEffect, useState } from "react";
import { formatPersianDateTime } from "../lib/date/persianDateTime";

export const statusBarHeight = 38;

const Clock = memo(function Clock() {
  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 1_000);
    return () => window.clearInterval(timer);
  }, []);
  const value = formatPersianDateTime(now);
  return (
    <Typography variant="caption">
      {value.date}، {value.time}
    </Typography>
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
