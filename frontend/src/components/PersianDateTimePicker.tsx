import CalendarMonthOutlinedIcon from "@mui/icons-material/CalendarMonthOutlined";
import ChevronLeftOutlinedIcon from "@mui/icons-material/ChevronLeftOutlined";
import ChevronRightOutlinedIcon from "@mui/icons-material/ChevronRightOutlined";
import {
  Box,
  IconButton,
  Paper,
  Popover,
  Stack,
  TextField,
  Typography,
  Button,
} from "@mui/material";
import { useEffect, useMemo, useState } from "react";

const formatter = new Intl.DateTimeFormat("fa-IR-u-ca-persian-nu-latn", {
  year: "numeric",
  month: "numeric",
  day: "numeric",
});
const monthNames = [
  "فروردین",
  "اردیبهشت",
  "خرداد",
  "تیر",
  "مرداد",
  "شهریور",
  "مهر",
  "آبان",
  "آذر",
  "دی",
  "بهمن",
  "اسفند",
];
const weekDays = [
  "شنبه",
  "یکشنبه",
  "دوشنبه",
  "سه‌شنبه",
  "چهارشنبه",
  "پنجشنبه",
  "جمعه",
];
const toPersianDigits = (value: number) =>
  value.toLocaleString("fa-IR", { useGrouping: false });

function parts(value: Date) {
  const values = formatter
    .formatToParts(value)
    .filter((item) => item.type !== "literal");
  return Object.fromEntries(
    values.map((item) => [item.type, Number(item.value)]),
  ) as Record<string, number>;
}

function toDate(year: number, month: number, day: number, time: string) {
  const [hour, minute] = time.split(":").map(Number);
  const cursor = new Date(year + 621, 2, 18, hour || 0, minute || 0);
  for (let index = 0; index < 370; index += 1) {
    const candidate = new Date(cursor.getTime());
    candidate.setDate(cursor.getDate() + index);
    const candidateParts = parts(candidate);
    if (
      candidateParts.year === year &&
      candidateParts.month === month &&
      candidateParts.day === day
    )
      return candidate;
  }
  return null;
}

function daysInMonth(year: number, month: number) {
  if (toDate(year, month, 31, "00:00")) return 31;
  if (toDate(year, month, 30, "00:00")) return 30;
  return 29;
}

function monthOffset(year: number, month: number, offset: number) {
  const index = year * 12 + month - 1 + offset;
  return { year: Math.floor(index / 12), month: (index % 12) + 1 };
}

function formatDate(value: Date | null) {
  if (!value) return "";
  const selected = parts(value);
  const pad = (number: number) => toPersianDigits(number).padStart(2, "۰");
  return `${toPersianDigits(selected.year)}/${pad(selected.month)}/${pad(selected.day)}`;
}

export function PersianDateTimePicker({
  label,
  value,
  onChange,
}: {
  label: string;
  value: Date | null;
  onChange: (value: Date | null) => void;
}) {
  const selected = value ? parts(value) : parts(new Date());
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const [view, setView] = useState({
    year: selected.year,
    month: selected.month,
  });
  const time = value
    ? `${String(value.getHours()).padStart(2, "0")}:${String(value.getMinutes()).padStart(2, "0")}`
    : "00:00";

  useEffect(() => {
    if (value) setView({ year: selected.year, month: selected.month });
  }, [value]);

  const calendarDays = useMemo(() => {
    const first = toDate(view.year, view.month, 1, "00:00");
    if (!first) return [];
    const leading = (first.getDay() + 1) % 7;
    return [
      ...Array.from({ length: leading }, () => null),
      ...Array.from(
        { length: daysInMonth(view.year, view.month) },
        (_, index) => index + 1,
      ),
    ];
  }, [view]);

  const chooseDate = (day: number) => {
    const next = toDate(view.year, view.month, day, time);
    if (next) onChange(next);
    setAnchorEl(null);
  };

  return (
    <Stack
      direction={{ xs: "column", sm: "row" }}
      spacing={1}
      sx={{ width: { xs: "100%", sm: "auto" }, flex: 1, minWidth: 0 }}
    >
      <TextField
        size="small"
        label={label}
        value={formatDate(value)}
        onClick={(event) => setAnchorEl(event.currentTarget)}
        placeholder="روز/ماه/سال"
        sx={{ minWidth: { sm: 220 }, flex: 1 }}
        slotProps={{
          input: {
            readOnly: true,
            endAdornment: (
              <CalendarMonthOutlinedIcon color="action" fontSize="small" />
            ),
          },
        }}
      />
      <TextField
        size="small"
        type="time"
        label="ساعت"
        value={time}
        onChange={(event) => {
          const next = toDate(
            selected.year,
            selected.month,
            selected.day,
            event.target.value,
          );
          if (next) onChange(next);
        }}
        slotProps={{ inputLabel: { shrink: true } }}
        sx={{ minWidth: { sm: 120 } }}
      />
      <Popover
        open={Boolean(anchorEl)}
        anchorEl={anchorEl}
        onClose={() => setAnchorEl(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
      >
        <Paper sx={{ p: 1.5, width: 292 }} dir="rtl">
          <Stack
            direction="row"
            sx={{ alignItems: "center", justifyContent: "space-between" }}
          >
            <IconButton
              size="small"
              aria-label="ماه بعد"
              onClick={() => setView(monthOffset(view.year, view.month, 1))}
            >
              <ChevronRightOutlinedIcon />
            </IconButton>
            <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
              {monthNames[view.month - 1]} {toPersianDigits(view.year)}
            </Typography>
            <IconButton
              size="small"
              aria-label="ماه قبل"
              onClick={() => setView(monthOffset(view.year, view.month, -1))}
            >
              <ChevronLeftOutlinedIcon />
            </IconButton>
          </Stack>
          <Box
            role="grid"
            aria-label={`${monthNames[view.month - 1]} ${toPersianDigits(view.year)}`}
            sx={{
              display: "grid",
              gridTemplateColumns: "repeat(7, 1fr)",
              gap: 0.25,
              mt: 1,
            }}
          >
            {weekDays.map((day) => (
              <Typography
                key={day}
                variant="caption"
                color="text.secondary"
                sx={{ textAlign: "center", py: 0.5 }}
              >
                {day.slice(0, 1)}
              </Typography>
            ))}
            {calendarDays.map((day, index) =>
              day ? (
                <Button
                  key={day}
                  role="gridcell"
                  size="small"
                  variant={
                    selected.year === view.year &&
                    selected.month === view.month &&
                    selected.day === day
                      ? "contained"
                      : "text"
                  }
                  onClick={() => chooseDate(day)}
                  sx={{ minWidth: 0, p: 0.5, borderRadius: 1 }}
                >
                  {toPersianDigits(day)}
                </Button>
              ) : (
                <Box key={`empty-${index}`} />
              ),
            )}
          </Box>
        </Paper>
      </Popover>
    </Stack>
  );
}

export function toLocalTimestamp(value: Date) {
  const pad = (part: number) => String(part).padStart(2, "0");
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:00`;
}
