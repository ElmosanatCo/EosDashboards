import { Box, Typography } from "@mui/material";
import { memo, useEffect, useState } from "react";
import { formatPersianDateTime } from "../lib/date/persianDateTime";

const Clock = memo(function Clock() {
  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 1_000);
    return () => window.clearInterval(timer);
  }, []);
  const value = formatPersianDateTime(now);
  return <Typography variant="caption">{value.date}، {value.time}</Typography>;
});

export function StatusBar() {
  return (
    <Box component="footer" sx={{ px: 2, py: 0.75, borderTop: 1, borderColor: "divider", display: "flex", justifyContent: "space-between" }}>
      <Typography variant="caption">نسخه {__APP_VERSION__}</Typography><Clock />
    </Box>
  );
}
