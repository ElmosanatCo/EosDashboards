import { Box, Button, Paper, Stack, Typography } from "@mui/material";
import type { ReactNode } from "react";
import type { AuthenticatedUser } from "../features/auth/authTypes";
import { useAuth } from "../app/providers/AuthProvider";
import type { TabDescriptor } from "../navigation/tabTypes";
import { useTabWorkspace } from "../navigation/TabWorkspaceProvider";
import {
  authorizedWorkspaceTargets,
  createWorkspaceTab,
  type WorkspaceTarget,
} from "../navigation/workspaceTargets";
import {
  getHomeGuideText,
  getHomeTargetMetadata,
  initialHomeAlerts,
  selectRecentHomeTabs,
} from "./home/homeContent";

export function HomePage() {
  const { user } = useAuth();
  const { tabs, dispatch } = useTabWorkspace();
  const targets = authorizedWorkspaceTargets(user.roleCodes);

  return (
    <HomePageView
      user={user}
      targets={targets}
      tabs={tabs}
      onOpenTarget={(target) =>
        dispatch({ type: "open", tab: createWorkspaceTab(target) })
      }
      onActivateTab={(key) => dispatch({ type: "activate", key })}
    />
  );
}

export type HomePageViewProps = {
  user: AuthenticatedUser;
  targets: readonly WorkspaceTarget[];
  tabs: readonly TabDescriptor[];
  onOpenTarget: (target: WorkspaceTarget) => void;
  onActivateTab: (key: string) => void;
};

export function HomePageView({
  user,
  targets,
  tabs,
  onOpenTarget,
  onActivateTab,
}: HomePageViewProps) {
  const recentTabs = selectRecentHomeTabs(tabs);
  const roleCodes = new Set(user.roleCodes);
  const visibleTargets = targets.filter((target) =>
    target.requiredRoleCodes.some((roleCode) => roleCodes.has(roleCode)),
  );

  return (
    <Stack
      data-testid="home-workspace"
      spacing={{ xs: 2, md: 2.5 }}
      sx={{ minWidth: 0, maxWidth: "100%" }}
    >
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{
          borderTop: 3,
          borderTopColor: "primary.main",
          p: { xs: 2.5, md: 3 },
        }}
      >
        <Stack spacing={1.25}>
          <Typography variant="overline" color="primary.main">
            خانه
          </Typography>
          <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
            فضای کاری مدیریت
          </Typography>
          <Stack
            direction={{ xs: "column", sm: "row" }}
            spacing={{ xs: 0.5, sm: 2 }}
            sx={{ alignItems: { sm: "center" }, minWidth: 0 }}
          >
            <Typography sx={{ fontWeight: 650 }}>
              {user.firstName} {user.lastName}
            </Typography>
            <Typography color="text.secondary">
              واحد: {user.department.name}
            </Typography>
          </Stack>
          <Typography color="text.secondary">
            {getHomeGuideText(user.roleCodes)}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            برای جست‌وجوی سراسری، Ctrl+K را فشار دهید.
          </Typography>
        </Stack>
      </Paper>

      <HomeSection title="امکانات در اختیار شما">
        <TargetGrid targets={visibleTargets} onOpenTarget={onOpenTarget} />
      </HomeSection>

      <HomeSection title="کارهایی که می‌توانید انجام دهید">
        <Stack spacing={1.25}>
          {visibleTargets.map((target) => (
            <ActionCard
              key={target.routeId}
              target={target}
              onOpenTarget={onOpenTarget}
            />
          ))}
        </Stack>
      </HomeSection>

      <HomeSection title="هشدارها و کارهای نیازمند اقدام">
        {initialHomeAlerts.length === 0 ? (
          <Typography color="text.secondary">
            در حال حاضر موردی برای پیگیری ثبت نشده است.
          </Typography>
        ) : (
          <Stack spacing={1}>
            {initialHomeAlerts.map((alert) => (
              <Box key={alert.id}>
                <Typography sx={{ fontWeight: 650 }}>{alert.title}</Typography>
                <Typography color="text.secondary">{alert.summary}</Typography>
              </Box>
            ))}
          </Stack>
        )}
      </HomeSection>

      <HomeSection title="ادامهٔ کار">
        {recentTabs.length === 0 ? (
          <Typography color="text.secondary">
            نشست باز دیگری برای ادامه وجود ندارد.
          </Typography>
        ) : (
          <Stack spacing={1}>
            {recentTabs.map((tab) => (
              <Button
                key={tab.key}
                variant="outlined"
                onClick={() => onActivateTab(tab.key)}
                sx={{ alignSelf: "flex-start", maxWidth: "100%" }}
              >
                ادامه: {tab.title}
              </Button>
            ))}
          </Stack>
        )}
      </HomeSection>

      <HomeSection title="امکانات آینده">
        <Typography color="text.secondary">
          با فعال شدن قابلیت‌های جدید، این بخش تکمیل می‌شود.
        </Typography>
      </HomeSection>
    </Stack>
  );
}

function HomeSection({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <Box component="section" aria-label={title}>
      <Typography
        component="h2"
        variant="h6"
        sx={{ mb: 1.25, fontWeight: 700 }}
      >
        {title}
      </Typography>
      {children}
    </Box>
  );
}

function TargetGrid({
  targets,
  onOpenTarget,
}: {
  targets: readonly WorkspaceTarget[];
  onOpenTarget: (target: WorkspaceTarget) => void;
}) {
  return (
    <Box
      sx={{
        display: "grid",
        gridTemplateColumns: {
          xs: "1fr",
          sm: "repeat(2, minmax(0, 1fr))",
          lg: "repeat(3, minmax(0, 1fr))",
        },
        gap: 1.5,
      }}
    >
      {targets.map((target) => (
        <TargetCard
          key={target.routeId}
          target={target}
          onOpenTarget={onOpenTarget}
        />
      ))}
    </Box>
  );
}

function TargetCard({
  target,
  onOpenTarget,
}: {
  target: WorkspaceTarget;
  onOpenTarget: (target: WorkspaceTarget) => void;
}) {
  const metadata = getHomeTargetMetadata(target.routeId);
  const Icon = target.Icon;

  return (
    <Paper
      variant="outlined"
      className="eos-accent-card"
      sx={{
        minWidth: 0,
        height: "100%",
        display: "flex",
        flexDirection: "column",
        p: 2,
        borderTop: 3,
        borderTopColor: "primary.main",
      }}
    >
      <Stack direction="row" spacing={1.25} sx={{ minWidth: 0 }}>
        <Icon color="primary" aria-hidden="true" />
        <Typography sx={{ minWidth: 0, fontWeight: 700 }}>
          {target.title}
        </Typography>
      </Stack>
      <Typography color="text.secondary" sx={{ mt: 1.25, flex: 1 }}>
        {metadata.summary}
      </Typography>
      <Button
        onClick={() => onOpenTarget(target)}
        sx={{ alignSelf: "flex-start", mt: 1.5 }}
      >
        {metadata.actionLabel}
      </Button>
    </Paper>
  );
}

function ActionCard({
  target,
  onOpenTarget,
}: {
  target: WorkspaceTarget;
  onOpenTarget: (target: WorkspaceTarget) => void;
}) {
  const metadata = getHomeTargetMetadata(target.routeId);
  const Icon = target.Icon;

  return (
    <Paper
      variant="outlined"
      className="eos-accent-card"
      sx={{
        minWidth: 0,
        p: { xs: 1.5, sm: 2 },
        borderTop: 3,
        borderTopColor: "primary.main",
      }}
    >
      <Stack
        direction={{ xs: "column", sm: "row" }}
        spacing={1.5}
        sx={{ alignItems: { sm: "center" }, minWidth: 0 }}
      >
        <Stack direction="row" spacing={1.25} sx={{ minWidth: 0, flex: 1 }}>
          <Icon color="primary" aria-hidden="true" />
          <Box sx={{ minWidth: 0 }}>
            <Typography sx={{ fontWeight: 700 }}>{target.title}</Typography>
            <Typography variant="body2" color="text.secondary">
              {metadata.summary}
            </Typography>
          </Box>
        </Stack>
        <Button
          onClick={() => onOpenTarget(target)}
          sx={{ alignSelf: { xs: "flex-start", sm: "center" }, flexShrink: 0 }}
        >
          {metadata.actionLabel}
        </Button>
      </Stack>
    </Paper>
  );
}
