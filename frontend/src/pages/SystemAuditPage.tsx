import { useQuery } from "@tanstack/react-query";
import {
  Box,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { useState } from "react";
import { administrationApi } from "../features/administration/administrationApi";
import { eventLabel } from "../features/administration/administrationUi";
import { formatPersianDateTime } from "../lib/date/persianDateTime";
import {
  PersianDateTimePicker,
  toLocalTimestamp,
} from "../components/PersianDateTimePicker";

type Range = "LastSevenDays" | "LastThirtyDays" | "Custom";
export function SystemAuditPage() {
  const [range, setRange] = useState<Range>("LastSevenDays");
  const [result, setResult] = useState<"" | "true" | "false">("");
  const [eventCode, setEventCode] = useState("");
  const [actorUserId, setActorUserId] = useState("");
  const [subjectUserId, setSubjectUserId] = useState("");
  const [from, setFrom] = useState<Date | null>(null);
  const [to, setTo] = useState<Date | null>(null);
  const query = new URLSearchParams({ range, pageSize: "50" });
  if (result) query.set("succeeded", result);
  if (eventCode.trim()) query.set("eventCode", eventCode.trim());
  if (actorUserId) query.set("actorUserId", actorUserId);
  if (subjectUserId) query.set("subjectUserId", subjectUserId);
  if (range === "Custom" && from && to) {
    query.set("from", toLocalTimestamp(from));
    query.set("to", toLocalTimestamp(to));
  }
  const audit = useQuery({
    queryKey: [
      "administration",
      "audit",
      range,
      result,
      eventCode,
      actorUserId,
      subjectUserId,
      from,
      to,
    ],
    queryFn: () => administrationApi.auditLogs(query),
    enabled: range !== "Custom" || Boolean(from && to),
  });
  return (
    <Stack spacing={2.5} sx={{ maxWidth: 1320, mx: "auto" }}>
      <Box>
        <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
          ممیزی سامانه
        </Typography>
        <Typography color="text.secondary" sx={{ mt: 0.5 }}>
          رویدادهای مدیریتی، ورود و امنیت را بررسی کنید.
        </Typography>
      </Box>
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{ p: 2, borderTop: 3, borderTopColor: "primary.main" }}
      >
        <Stack
          direction={{ xs: "column", sm: "row" }}
          spacing={1.5}
          sx={{ alignItems: { sm: "center" } }}
        >
          <FormControl size="small" sx={{ minWidth: 190 }}>
            <InputLabel id="range-label">بازه زمانی</InputLabel>
            <Select
              labelId="range-label"
              label="بازه زمانی"
              value={range}
              onChange={(event) => setRange(event.target.value as Range)}
            >
              <MenuItem value="LastSevenDays">هفت روز اخیر</MenuItem>
              <MenuItem value="LastThirtyDays">سی روز اخیر</MenuItem>
              <MenuItem value="Custom">بازه سفارشی</MenuItem>
            </Select>
          </FormControl>
          <TextField
            size="small"
            label="کد رویداد"
            value={eventCode}
            onChange={(event) => setEventCode(event.target.value)}
          />
          <TextField
            size="small"
            label="شناسه انجام‌دهنده"
            value={actorUserId}
            onChange={(event) =>
              setActorUserId(event.target.value.replace(/\D/g, ""))
            }
          />
          <TextField
            size="small"
            label="شناسه کاربر هدف"
            value={subjectUserId}
            onChange={(event) =>
              setSubjectUserId(event.target.value.replace(/\D/g, ""))
            }
          />
          <FormControl size="small" sx={{ minWidth: 150 }}>
            <InputLabel id="result-label">نتیجه</InputLabel>
            <Select
              labelId="result-label"
              label="نتیجه"
              value={result}
              onChange={(event) => setResult(event.target.value)}
            >
              <MenuItem value="">همه</MenuItem>
              <MenuItem value="true">موفق</MenuItem>
              <MenuItem value="false">ناموفق</MenuItem>
            </Select>
          </FormControl>
          <Typography variant="body2" color="text.secondary">
            {audit.data
              ? `${audit.data.totalCount.toLocaleString("fa-IR")} رویداد`
              : ""}
          </Typography>
        </Stack>
        {range === "Custom" ? (
          <Stack
            direction={{ xs: "column", sm: "row" }}
            spacing={1.5}
            sx={{ mt: 1.5 }}
          >
            <PersianDateTimePicker
              label="از تاریخ و ساعت"
              value={from}
              onChange={setFrom}
            />
            <PersianDateTimePicker
              label="تا تاریخ و ساعت"
              value={to}
              onChange={setTo}
            />
          </Stack>
        ) : null}
      </Paper>
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{
          overflowX: "auto",
          borderTop: 3,
          borderTopColor: "primary.main",
        }}
      >
        <Table size="small" aria-label="فهرست ممیزی سامانه">
          <TableHead>
            <TableRow>
              <TableCell>رویداد</TableCell>
              <TableCell>انجام‌دهنده</TableCell>
              <TableCell>کاربر مؤثر</TableCell>
              <TableCell>نتیجه</TableCell>
              <TableCell>تاریخ</TableCell>
              <TableCell>ساعت</TableCell>
              <TableCell>IP</TableCell>
              <TableCell>دستگاه</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {audit.data?.items.map((item) => {
              const occurred = formatPersianDateTime(new Date(item.occurredAt));
              return (
                <TableRow key={item.id}>
                  <TableCell>{eventLabel(item.eventCode)}</TableCell>
                  <TableCell>{item.actorDisplayName ?? "سامانه"}</TableCell>
                  <TableCell>{item.subjectDisplayName ?? "—"}</TableCell>
                  <TableCell
                    sx={{
                      color: item.succeeded ? "success.main" : "error.main",
                    }}
                  >
                    {item.succeeded ? "موفق" : "ناموفق"}
                  </TableCell>
                  <TableCell>{occurred.date}</TableCell>
                  <TableCell>{occurred.time}</TableCell>
                  <TableCell dir="ltr">
                    {item.clientIpAddress ?? "ثبت نشده"}
                  </TableCell>
                  <TableCell>{deviceLabel(item.clientDeviceKind)}</TableCell>
                </TableRow>
              );
            })}
            {!audit.isPending && audit.data?.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} align="center">
                  رویدادی در این بازه وجود ندارد.
                </TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      </Paper>
      {audit.isError ? (
        <Typography color="error">دریافت رویدادها ممکن نشد.</Typography>
      ) : null}
      <Button
        sx={{ alignSelf: "flex-start" }}
        onClick={() => void audit.refetch()}
      >
        تازه‌سازی
      </Button>
    </Stack>
  );
}

function deviceLabel(value: string | null) {
  return (
    { Desktop: "رایانه", Mobile: "موبایل", Tablet: "تبلت", Unknown: "نامشخص" }[
      value ?? ""
    ] ?? "ثبت نشده"
  );
}
