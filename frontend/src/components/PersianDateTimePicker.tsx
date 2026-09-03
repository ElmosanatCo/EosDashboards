import {
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
} from "@mui/material";

const formatter = new Intl.DateTimeFormat("fa-IR-u-ca-persian-nu-latn", {
  year: "numeric",
  month: "numeric",
  day: "numeric",
});
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
  const time = value
    ? `${String(value.getHours()).padStart(2, "0")}:${String(value.getMinutes()).padStart(2, "0")}`
    : "00:00";
  const update = (
    next: Partial<Record<"year" | "month" | "day", number>>,
    nextTime = time,
  ) =>
    onChange(
      toDate(
        next.year ?? selected.year,
        next.month ?? selected.month,
        next.day ?? selected.day,
        nextTime,
      ),
    );
  return (
    <Stack
      direction={{ xs: "column", sm: "row" }}
      spacing={1}
      aria-label={label}
    >
      <FormControl size="small" sx={{ minWidth: 100 }}>
        <InputLabel>سال</InputLabel>
        <Select
          label="سال"
          value={selected.year}
          onChange={(e) => update({ year: Number(e.target.value) })}
        >
          {Array.from({ length: 12 }, (_, i) => selected.year - 5 + i).map(
            (year) => (
              <MenuItem key={year} value={year}>
                {toPersianDigits(year)}
              </MenuItem>
            ),
          )}
        </Select>
      </FormControl>
      <FormControl size="small" sx={{ minWidth: 86 }}>
        <InputLabel>ماه</InputLabel>
        <Select
          label="ماه"
          value={selected.month}
          onChange={(e) => update({ month: Number(e.target.value) })}
        >
          {Array.from({ length: 12 }, (_, i) => i + 1).map((month) => (
            <MenuItem key={month} value={month}>
              {toPersianDigits(month)}
            </MenuItem>
          ))}
        </Select>
      </FormControl>
      <FormControl size="small" sx={{ minWidth: 86 }}>
        <InputLabel>روز</InputLabel>
        <Select
          label="روز"
          value={selected.day}
          onChange={(e) => update({ day: Number(e.target.value) })}
        >
          {Array.from({ length: 31 }, (_, i) => i + 1).map((day) => (
            <MenuItem key={day} value={day}>
              {toPersianDigits(day)}
            </MenuItem>
          ))}
        </Select>
      </FormControl>
      <TextField
        size="small"
        type="time"
        label="ساعت"
        value={time}
        onChange={(e) => update({}, e.target.value)}
        slotProps={{ inputLabel: { shrink: true } }}
      />
    </Stack>
  );
}

export function toLocalTimestamp(value: Date) {
  const pad = (part: number) => String(part).padStart(2, "0");
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:00`;
}
