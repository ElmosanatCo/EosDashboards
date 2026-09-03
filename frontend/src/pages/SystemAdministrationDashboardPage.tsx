import HistoryOutlinedIcon from "@mui/icons-material/HistoryOutlined";
import LoginOutlinedIcon from "@mui/icons-material/LoginOutlined";
import PersonOffOutlinedIcon from "@mui/icons-material/PersonOffOutlined";
import PeopleAltOutlinedIcon from "@mui/icons-material/PeopleAltOutlined";
import SecurityOutlinedIcon from "@mui/icons-material/SecurityOutlined";
import { useQuery } from "@tanstack/react-query";
import {
  Box,
  Button,
  CircularProgress,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { administrationApi } from "../features/administration/administrationApi";
import { eventLabel } from "../features/administration/administrationUi";
import { formatPersianDateTime } from "../lib/date/persianDateTime";
import { useTabWorkspace } from "../navigation/TabWorkspaceProvider";
import {
  createWorkspaceTab,
  targetForRouteId,
} from "../navigation/workspaceTargets";

const metrics = [
  { key: "activeUsers", label: "کاربران فعال", Icon: PeopleAltOutlinedIcon },
  {
    key: "inactiveUsers",
    label: "کاربران غیرفعال",
    Icon: PersonOffOutlinedIcon,
  },
  {
    key: "successfulSignIns",
    label: "ورودهای موفق ۲۴ ساعت اخیر",
    Icon: LoginOutlinedIcon,
  },
  {
    key: "failedSecurityAttempts",
    label: "تلاش ناموفق امنیتی ۲۴ ساعت اخیر",
    Icon: SecurityOutlinedIcon,
  },
  {
    key: "usersWithActiveSessions",
    label: "کاربران دارای نشست فعال",
    Icon: HistoryOutlinedIcon,
  },
] as const;

export function SystemAdministrationDashboardPage() {
  const dashboard = useQuery({
    queryKey: ["administration", "dashboard"],
    queryFn: administrationApi.dashboard,
  });
  const { dispatch } = useTabWorkspace();
  if (dashboard.isPending) return <Loading />;
  if (dashboard.isError) return <Message title="دریافت داشبورد ممکن نشد" />;
  const data = dashboard.data;
  return (
    <Stack spacing={2.5} sx={{ maxWidth: 1320, mx: "auto" }}>
      <Box>
        <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
          داشبورد مدیر سامانه
        </Typography>
        <Typography color="text.secondary" sx={{ mt: 0.5 }}>
          نمای عملیاتی کاربران، ورودها و آخرین رویدادهای ثبت‌شده
        </Typography>
      </Box>
      <Box
        sx={{
          display: "grid",
          gridTemplateColumns: {
            xs: "1fr",
            sm: "repeat(2, 1fr)",
            xl: "repeat(5, 1fr)",
          },
          gap: 1.5,
        }}
      >
        {metrics.map(({ key, label, Icon }) => (
          <Paper
            key={key}
            variant="outlined"
            className="eos-accent-card"
            sx={{ p: 2, borderTop: 3, borderTopColor: "primary.main" }}
          >
            <Stack
              direction="row"
              sx={{ justifyContent: "space-between", alignItems: "flex-start" }}
            >
              <Icon color="primary" />
              <Typography variant="h4" sx={{ fontWeight: 760 }}>
                {data[key]}
              </Typography>
            </Stack>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
              {label}
            </Typography>
          </Paper>
        ))}
      </Box>
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{
          borderTop: 3,
          borderTopColor: "primary.main",
          overflow: "hidden",
        }}
      >
        <Stack
          direction={{ xs: "column", sm: "row" }}
          sx={{
            justifyContent: "space-between",
            alignItems: { sm: "center" },
            p: 2,
            borderBottom: 1,
            borderColor: "divider",
          }}
        >
          <Box>
            <Typography component="h2" variant="h6" sx={{ fontWeight: 700 }}>
              آخرین رویدادهای ممیزی
            </Typography>
            <Typography variant="body2" color="text.secondary">
              رخدادهای مدیریتی و امنیتی از منبع ثبت وقایع سامانه
            </Typography>
          </Box>
          <Button
            sx={{ alignSelf: { xs: "flex-start", sm: "auto" } }}
            onClick={() => {
              const target = targetForRouteId("administration-audit");
              if (target)
                dispatch({ type: "open", tab: createWorkspaceTab(target) });
            }}
          >
            مشاهده همه ممیزی‌ها
          </Button>
        </Stack>
        {data.latestAuditLogs.length === 0 ? (
          <Typography color="text.secondary" sx={{ p: 3 }}>
            رویدادی برای نمایش وجود ندارد.
          </Typography>
        ) : (
          <Box component="ul" sx={{ listStyle: "none", m: 0, p: 0 }}>
            {data.latestAuditLogs.map((item) => (
              <AuditRow key={item.id} item={item} />
            ))}
          </Box>
        )}
      </Paper>
    </Stack>
  );
}

function AuditRow({
  item,
}: {
  item: Awaited<
    ReturnType<typeof administrationApi.dashboard>
  >["latestAuditLogs"][number];
}) {
  const occurred = formatPersianDateTime(new Date(item.occurredAt));
  return (
    <Box
      component="li"
      sx={{
        display: "grid",
        gridTemplateColumns: { xs: "1fr auto", md: "1.2fr 1fr 1fr auto" },
        gap: 1.5,
        alignItems: "center",
        px: 2,
        py: 1.5,
        borderBottom: 1,
        borderColor: "divider",
        "&:last-child": { borderBottom: 0 },
      }}
    >
      <Box>
        <Typography sx={{ fontWeight: 600 }}>
          {eventLabel(item.eventCode)}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {item.actorDisplayName ?? "سامانه"} ← {item.subjectDisplayName ?? "—"}
        </Typography>
      </Box>
      <Typography
        variant="body2"
        color={item.succeeded ? "success.main" : "error.main"}
      >
        {item.succeeded ? "موفق" : "ناموفق"}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {occurred.date}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {occurred.time}
      </Typography>
    </Box>
  );
}

function Loading() {
  return (
    <Box sx={{ display: "grid", placeItems: "center", minHeight: 240 }}>
      <CircularProgress aria-label="در حال دریافت اطلاعات" />
    </Box>
  );
}
function Message({ title }: { title: string }) {
  return (
    <Paper
      variant="outlined"
      className="eos-accent-card"
      sx={{
        maxWidth: 640,
        mx: "auto",
        p: 3,
        borderTop: 3,
        borderTopColor: "error.main",
      }}
    >
      <Typography component="h1" variant="h6">
        {title}
      </Typography>
      <Typography color="text.secondary" sx={{ mt: 1 }}>
        دوباره تلاش کنید یا با مدیر سامانه تماس بگیرید.
      </Typography>
    </Paper>
  );
}
