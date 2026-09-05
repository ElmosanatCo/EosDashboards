import CompareArrowsOutlinedIcon from "@mui/icons-material/CompareArrowsOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import CancelOutlinedIcon from "@mui/icons-material/CancelOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { ConfirmActionDialog } from "../components/ConfirmActionDialog";
import { MutationErrorAlert } from "../components/MutationErrorAlert";
import {
  jobDescriptionsApi,
  type JobDescriptionListItem,
  type PublicSkill,
} from "../features/jobDescriptions/jobDescriptionsApi";
import { formatPersianDateTime } from "../lib/date/persianDateTime";
import { toPersianDigits } from "../lib/format/persianDigits";

type TabKey = "review" | "approved" | "skills";

export function HumanResourcesJobDescriptionReviewPage() {
  const [tab, setTab] = useState<TabKey>("review");
  const [departmentId, setDepartmentId] = useState<number | "all">("all");
  const [detailId, setDetailId] = useState<number | null>(null);
  const [comparisonId, setComparisonId] = useState<number | null>(null);
  const [rejecting, setRejecting] = useState<JobDescriptionListItem | null>(
    null,
  );
  const [reason, setReason] = useState("");
  const queryClient = useQueryClient();
  const departments = useQuery({
    queryKey: ["human-resources-departments"],
    queryFn: jobDescriptionsApi.humanResourcesDepartments,
  });
  const selectedDepartment = departmentId === "all" ? undefined : departmentId;
  const review = useQuery({
    queryKey: ["human-resources-review", selectedDepartment],
    queryFn: () => jobDescriptionsApi.humanResourcesReview(selectedDepartment),
    enabled: tab === "review",
  });
  const approved = useQuery({
    queryKey: ["human-resources-approved", selectedDepartment],
    queryFn: () =>
      jobDescriptionsApi.humanResourcesApproved(selectedDepartment),
    enabled: tab === "approved",
  });
  const approve = useMutation({
    mutationFn: jobDescriptionsApi.approveByHumanResources,
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["human-resources-review"],
      });
      void queryClient.invalidateQueries({
        queryKey: ["human-resources-approved"],
      });
      void queryClient.invalidateQueries({
        queryKey: ["human-resources-dashboard"],
      });
    },
  });
  const reject = useMutation({
    mutationFn: ({
      id,
      rejectionReason,
    }: {
      id: number;
      rejectionReason: string;
    }) => jobDescriptionsApi.rejectByHumanResources(id, rejectionReason),
    onSuccess: () => {
      setRejecting(null);
      setReason("");
      void queryClient.invalidateQueries({
        queryKey: ["human-resources-review"],
      });
      void queryClient.invalidateQueries({
        queryKey: ["human-resources-dashboard"],
      });
    },
  });

  async function download(id: number, personName: string) {
    const blob = await jobDescriptionsApi.download(id);
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `شرح-وظایف-${personName}.xlsx`;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  return (
    <Stack spacing={2.5} sx={{ width: "100%" }}>
      <Box>
        <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
          مدیریت شرح وظایف
        </Typography>
        <Typography color="text.secondary" sx={{ mt: 0.5 }}>
          کارتابل تأیید، شرح وظایف تأییدشده و مهارت‌های عمومی را از یک فضای کاری
          مدیریت کنید.
        </Typography>
      </Box>
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{ borderTop: 3, borderTopColor: "primary.main" }}
      >
        <Tabs
          value={tab}
          onChange={(_, value: TabKey) => setTab(value)}
          aria-label="بخش‌های مدیریت شرح وظایف"
        >
          <Tab value="review" label="منتظر تأیید" />
          <Tab value="approved" label="تأیید شده" />
          <Tab
            value="skills"
            label="مهارت‌های عمومی"
            icon={<SettingsOutlinedIcon />}
            iconPosition="start"
          />
        </Tabs>
      </Paper>
      {tab !== "skills" ? (
        <FormControl sx={{ minWidth: { xs: "100%", sm: 280 } }}>
          <InputLabel id="human-resources-management-department-label">
            بخش
          </InputLabel>
          <Select
            labelId="human-resources-management-department-label"
            aria-label="بخش"
            label="بخش"
            value={departmentId}
            onChange={(event) => {
              const value = String(event.target.value);
              setDepartmentId(value === "all" ? "all" : Number(value));
            }}
          >
            <MenuItem value="all">همه بخش‌ها</MenuItem>
            {departments.data?.map((department) => (
              <MenuItem key={department.id} value={department.id}>
                {department.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      ) : null}
      {approve.isError || reject.isError ? (
        <MutationErrorAlert error={approve.error ?? reject.error} />
      ) : null}
      {tab === "skills" ? <PublicSkillManagement /> : null}
      {tab === "review" ? (
        <JobDescriptionTable
          title="کارتابل منتظر تأیید"
          emptyMessage="پرونده‌ای برای بازبینی وجود ندارد."
          query={review}
          onView={setDetailId}
          onDownload={download}
          onApprove={(id) => approve.mutate(id)}
          onReject={(item) => {
            setRejecting(item);
            setReason("");
          }}
          showReviewActions
        />
      ) : null}
      {tab === "approved" ? (
        <JobDescriptionTable
          title="شرح وظایف تأیید شده"
          emptyMessage="شرح وظیفهٔ تأییدشده‌ای برای این بخش وجود ندارد."
          query={approved}
          onView={setDetailId}
          onDownload={download}
          onCompare={setComparisonId}
        />
      ) : null}
      <JobDescriptionDetailDialog
        versionId={detailId}
        onClose={() => setDetailId(null)}
        onDownload={download}
      />
      <ComparisonDialog
        versionId={comparisonId}
        onClose={() => setComparisonId(null)}
      />
      <Dialog
        open={rejecting !== null}
        onClose={() => setRejecting(null)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>رد شرح وظیفه</DialogTitle>
        <DialogContent>
          <Typography color="text.secondary" sx={{ mb: 1.5 }}>
            دلیل رد برای «{rejecting?.personName}» ثبت می‌شود و به مدیر بخش
            نمایش داده خواهد شد.
          </Typography>
          <TextField
            autoFocus
            fullWidth
            multiline
            minRows={3}
            label="دلیل رد"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRejecting(null)}>انصراف</Button>
          <Button
            color="error"
            variant="contained"
            disabled={!reason.trim() || reject.isPending}
            onClick={() =>
              rejecting &&
              reject.mutate({
                id: rejecting.id,
                rejectionReason: reason.trim(),
              })
            }
          >
            ثبت رد
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}

function JobDescriptionTable({
  title,
  emptyMessage,
  query,
  onView,
  onDownload,
  onApprove,
  onReject,
  onCompare,
  showReviewActions = false,
}: {
  title: string;
  emptyMessage: string;
  query: ReturnType<typeof useQuery<JobDescriptionListItem[]>>;
  onView: (id: number) => void;
  onDownload: (id: number, personName: string) => void;
  onApprove?: (id: number) => void;
  onReject?: (item: JobDescriptionListItem) => void;
  onCompare?: (id: number) => void;
  showReviewActions?: boolean;
}) {
  return (
    <Paper
      variant="outlined"
      className="eos-accent-card"
      sx={{ borderTop: 3, borderTopColor: "primary.main", overflowX: "auto" }}
    >
      <Typography
        component="h2"
        variant="h6"
        sx={{ p: 2.5, pb: 1, fontWeight: 700 }}
      >
        {title}
      </Typography>
      {query.isPending ? (
        <Box sx={{ minHeight: 220, display: "grid", placeItems: "center" }}>
          <CircularProgress aria-label="در حال دریافت فهرست" />
        </Box>
      ) : null}
      {query.isError ? (
        <Typography color="error" sx={{ p: 2.5 }}>
          دریافت فهرست شرح وظایف ممکن نشد.
        </Typography>
      ) : null}
      {query.isSuccess && query.data.length === 0 ? (
        <Typography color="text.secondary" sx={{ p: 2.5 }}>
          {emptyMessage}
        </Typography>
      ) : null}
      {query.isSuccess && query.data.length > 0 ? (
        <Table aria-label={title}>
          <TableHead>
            <TableRow>
              <TableCell>پرسنل</TableCell>
              <TableCell>وضعیت کیفیت</TableCell>
              <TableCell>آخرین تغییر</TableCell>
              <TableCell>عملیات</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {query.data.map((item) => (
              <TableRow key={item.id} hover>
                <TableCell sx={{ fontWeight: 650 }}>
                  {item.personName}
                </TableCell>
                <TableCell>
                  <Chip
                    size="small"
                    label={item.qualityStatus}
                    color={
                      item.qualityStatus === "سالم" ? "success" : "warning"
                    }
                  />
                </TableCell>
                <TableCell>{formatDate(item.updatedAt)}</TableCell>
                <TableCell>
                  <Tooltip title="مشاهده در فرم مدیریت">
                    <IconButton
                      aria-label={`مشاهده ${item.personName}`}
                      onClick={() => onView(item.id)}
                    >
                      <VisibilityOutlinedIcon />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title="دانلود">
                    <IconButton
                      aria-label={`دانلود ${item.personName}`}
                      onClick={() => void onDownload(item.id, item.personName)}
                    >
                      <DownloadOutlinedIcon />
                    </IconButton>
                  </Tooltip>
                  {onCompare ? (
                    <Tooltip title="مقایسه با شرح قبلی">
                      <IconButton
                        aria-label={`مقایسه ${item.personName}`}
                        onClick={() => onCompare(item.id)}
                      >
                        <CompareArrowsOutlinedIcon />
                      </IconButton>
                    </Tooltip>
                  ) : null}
                  {showReviewActions && onApprove && onReject ? (
                    <>
                      <Tooltip title="تأیید">
                        <IconButton
                          aria-label={`تأیید ${item.personName}`}
                          onClick={() => onApprove(item.id)}
                        >
                          <CheckCircleOutlineOutlinedIcon />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="رد با دلیل">
                        <IconButton
                          aria-label={`رد ${item.personName}`}
                          onClick={() => onReject(item)}
                        >
                          <CancelOutlinedIcon />
                        </IconButton>
                      </Tooltip>
                    </>
                  ) : null}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      ) : null}
    </Paper>
  );
}

function PublicSkillManagement() {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<"active" | "inactive" | "all">("active");
  const [names, setNames] = useState<Record<number, string>>({});
  const [pendingDelete, setPendingDelete] = useState<PublicSkill | null>(null);
  const [pendingActivate, setPendingActivate] = useState<PublicSkill | null>(
    null,
  );
  const [mergeSource, setMergeSource] = useState<PublicSkill | null>(null);
  const [survivingSkillId, setSurvivingSkillId] = useState("");
  const catalog = useQuery({
    queryKey: ["human-resources-catalog", status !== "active"],
    queryFn: () =>
      jobDescriptionsApi.humanResourcesCatalog(status !== "active"),
  });
  const invalidate = () =>
    void queryClient.invalidateQueries({
      queryKey: ["human-resources-catalog"],
    });
  const rename = useMutation({
    mutationFn: ({ id, name }: { id: number; name: string }) =>
      jobDescriptionsApi.renamePublicSkill(id, name),
    onSuccess: invalidate,
  });
  const deactivate = useMutation({
    mutationFn: (id: number) => jobDescriptionsApi.deactivatePublicSkill(id),
    onSuccess: () => {
      setPendingDelete(null);
      invalidate();
    },
  });
  const activate = useMutation({
    mutationFn: (id: number) => jobDescriptionsApi.activatePublicSkill(id),
    onSuccess: () => {
      setPendingActivate(null);
      invalidate();
    },
  });
  const merge = useMutation({
    mutationFn: ({
      sourceId,
      targetId,
    }: {
      sourceId: number;
      targetId: number;
    }) => jobDescriptionsApi.mergePublicSkill(sourceId, targetId),
    onSuccess: () => {
      setMergeSource(null);
      setSurvivingSkillId("");
      invalidate();
    },
  });
  const visibleSkills =
    catalog.data?.filter(
      (skill) =>
        status === "all" ||
        (status === "active" ? skill.isActive : !skill.isActive),
    ) ?? [];
  const activeTargets =
    catalog.data?.filter(
      (skill) => skill.isActive && skill.id !== mergeSource?.id,
    ) ?? [];
  return (
    <Paper
      variant="outlined"
      className="eos-accent-card"
      sx={{ p: 2.5, borderTop: 3, borderTopColor: "primary.main" }}
    >
      <Box
        sx={{
          display: "flex",
          flexDirection: { xs: "column", sm: "row" },
          justifyContent: "space-between",
          gap: 1.5,
          mb: 2,
        }}
      >
        <Box>
          <Typography component="h2" variant="h6" sx={{ fontWeight: 700 }}>
            مدیریت مهارت‌های عمومی
          </Typography>
          <Typography color="text.secondary" variant="body2" sx={{ mt: 0.5 }}>
            مهارت‌های عمومی به‌صورت سازمانی مدیریت می‌شوند و واحد اختصاصی
            ندارند.
          </Typography>
        </Box>
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel id="public-skill-status-label">وضعیت</InputLabel>
          <Select
            labelId="public-skill-status-label"
            label="وضعیت"
            value={status}
            onChange={(event) => setStatus(event.target.value as typeof status)}
          >
            <MenuItem value="active">فعال</MenuItem>
            <MenuItem value="inactive">غیرفعال</MenuItem>
            <MenuItem value="all">همه</MenuItem>
          </Select>
        </FormControl>
      </Box>
      <MutationErrorAlert
        error={
          rename.error ?? deactivate.error ?? activate.error ?? merge.error
        }
      />
      <Stack spacing={1.25}>
        {visibleSkills.map((skill) => (
          <Paper key={skill.id} variant="outlined" sx={{ p: 1.25 }}>
            <Box
              sx={{
                display: "flex",
                flexDirection: { xs: "column", md: "row" },
                gap: 1,
                alignItems: { md: "center" },
              }}
            >
              <TextField
                fullWidth
                size="small"
                label={`مهارت ${toPersianDigits(skill.id)}`}
                value={names[skill.id] ?? skill.name}
                onChange={(event) =>
                  setNames((current) => ({
                    ...current,
                    [skill.id]: event.target.value,
                  }))
                }
              />
              <Stack direction="row" spacing={0.5}>
                {skill.isActive ? (
                  <>
                    <Button
                      size="small"
                      disabled={!names[skill.id]?.trim() || rename.isPending}
                      onClick={() =>
                        rename.mutate({ id: skill.id, name: names[skill.id] })
                      }
                    >
                      ویرایش
                    </Button>
                    <Button
                      size="small"
                      onClick={() => {
                        setMergeSource(skill);
                        setSurvivingSkillId("");
                      }}
                    >
                      تلفیق
                    </Button>
                    <Button
                      size="small"
                      color="error"
                      disabled={deactivate.isPending}
                      onClick={() => setPendingDelete(skill)}
                    >
                      حذف
                    </Button>
                  </>
                ) : (
                  <Button
                    size="small"
                    disabled={activate.isPending}
                    onClick={() => setPendingActivate(skill)}
                  >
                    فعال‌سازی
                  </Button>
                )}
              </Stack>
            </Box>
          </Paper>
        ))}
      </Stack>
      {catalog.isSuccess && visibleSkills.length === 0 ? (
        <Typography color="text.secondary" sx={{ mt: 2 }}>
          مهارتی با این وضعیت وجود ندارد.
        </Typography>
      ) : null}
      <ConfirmActionDialog
        open={pendingDelete !== null}
        title="تأیید حذف مهارت عمومی"
        message={
          pendingDelete ? `آیا از حذف «${pendingDelete.name}» مطمئن هستید؟` : ""
        }
        pending={deactivate.isPending}
        onClose={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && deactivate.mutate(pendingDelete.id)}
      />
      <ConfirmActionDialog
        open={pendingActivate !== null}
        title="تأیید فعال‌سازی"
        message={
          pendingActivate
            ? `آیا از فعال‌سازی «${pendingActivate.name}» مطمئن هستید؟`
            : ""
        }
        confirmLabel="تأیید فعال‌سازی"
        confirmColor="success"
        pending={activate.isPending}
        onClose={() => setPendingActivate(null)}
        onConfirm={() => pendingActivate && activate.mutate(pendingActivate.id)}
      />
      <Dialog
        open={mergeSource !== null}
        onClose={() => setMergeSource(null)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>تأیید تلفیق مهارت‌های عمومی</DialogTitle>
        <DialogContent>
          <Typography sx={{ mb: 1.5 }}>
            مهارت «{mergeSource?.name}» غیرفعال می‌شود و ارجاع‌های آن به مهارت
            باقی‌مانده منتقل خواهد شد.
          </Typography>
          <FormControl fullWidth>
            <InputLabel id="surviving-public-skill-label">
              مهارتی که باقی می‌ماند
            </InputLabel>
            <Select
              labelId="surviving-public-skill-label"
              label="مهارتی که باقی می‌ماند"
              value={survivingSkillId}
              onChange={(event) =>
                setSurvivingSkillId(String(event.target.value))
              }
            >
              {activeTargets.map((skill) => (
                <MenuItem key={skill.id} value={skill.id}>
                  {skill.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          {survivingSkillId ? (
            <Typography color="text.secondary" sx={{ mt: 1.5 }}>
              مهارت باقی‌مانده:{" "}
              {
                activeTargets.find(
                  (skill) => String(skill.id) === survivingSkillId,
                )?.name
              }
            </Typography>
          ) : null}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setMergeSource(null)}>انصراف</Button>
          <Button
            variant="contained"
            disabled={!survivingSkillId || merge.isPending}
            onClick={() =>
              mergeSource &&
              merge.mutate({
                sourceId: mergeSource.id,
                targetId: Number(survivingSkillId),
              })
            }
          >
            تأیید تلفیق
          </Button>
        </DialogActions>
      </Dialog>
    </Paper>
  );
}

function JobDescriptionDetailDialog({
  versionId,
  onClose,
  onDownload,
}: {
  versionId: number | null;
  onClose: () => void;
  onDownload: (id: number, personName: string) => void;
}) {
  const detail = useQuery({
    queryKey: ["job-description-detail", versionId],
    queryFn: () => jobDescriptionsApi.detail(versionId!),
    enabled: versionId !== null,
  });
  const analysis = useQuery({
    queryKey: ["job-description-analysis", versionId],
    queryFn: () => jobDescriptionsApi.analysis(versionId!),
    enabled: versionId !== null,
  });
  return (
    <Dialog open={versionId !== null} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>شرح وظیفه در فرم مدیریت</DialogTitle>
      <DialogContent>
        {detail.isPending ? (
          <CircularProgress aria-label="در حال دریافت جزئیات" />
        ) : null}
        {detail.data ? (
          <Stack spacing={1.25} sx={{ pt: 1 }}>
            <Typography variant="h6">{detail.data.personName}</Typography>
            <Typography color="text.secondary">
              کد پرسنلی: {detail.data.personnelCode ?? "ثبت نشده"}
            </Typography>
            <Typography>
              وضعیت: {detail.data.workflowStatus} · کیفیت:{" "}
              {detail.data.qualityStatus}
            </Typography>
            {detail.data.rejectionReason ? (
              <Typography color="error.main">
                دلیل رد: {detail.data.rejectionReason}
              </Typography>
            ) : null}
            <Typography sx={{ fontWeight: 650 }}>وظایف</Typography>
            {detail.data.tasks.map((task) => (
              <Paper
                key={`${task.sortOrder}-${task.taskCatalogItemId}`}
                variant="outlined"
                sx={{ p: 1.25 }}
              >
                <Typography sx={{ fontWeight: 650 }}>{task.title}</Typography>
                <Typography variant="body2" sx={{ mt: 0.5 }}>
                  {task.description}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  ساعت هفتگی:{" "}
                  {task.weeklyHours === null
                    ? "ثبت نشده"
                    : toPersianDigits(String(task.weeklyHours))}
                </Typography>
              </Paper>
            ))}
            {analysis.data?.map((finding) => (
              <Typography
                key={`${finding.code}-${finding.actionTarget}`}
                color="warning.main"
              >
                {finding.message}
              </Typography>
            ))}
          </Stack>
        ) : null}
      </DialogContent>
      <DialogActions>
        {detail.data ? (
          <Button
            startIcon={<DownloadOutlinedIcon />}
            onClick={() =>
              void onDownload(detail.data!.id, detail.data!.personName)
            }
          >
            دانلود اکسل
          </Button>
        ) : null}
        <Button onClick={onClose}>بستن</Button>
      </DialogActions>
    </Dialog>
  );
}

function ComparisonDialog({
  versionId,
  onClose,
}: {
  versionId: number | null;
  onClose: () => void;
}) {
  const comparison = useQuery({
    queryKey: ["job-description-comparison", versionId],
    queryFn: () => jobDescriptionsApi.comparison(versionId!),
    enabled: versionId !== null,
  });
  return (
    <Dialog open={versionId !== null} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>مقایسه با شرح قبلی</DialogTitle>
      <DialogContent>
        {comparison.isPending ? (
          <CircularProgress aria-label="در حال آماده‌سازی مقایسه" />
        ) : null}
        {comparison.isError ? (
          <Typography color="error">مقایسه این نسخه در دسترس نیست.</Typography>
        ) : null}
        {comparison.data ? (
          <Stack spacing={1.25} sx={{ pt: 1 }}>
            {comparison.data.previous ? (
              <Typography color="text.secondary">
                نسخهٔ فعلی با بلافاصله قبلی مقایسه شده است؛{" "}
                {formatDate(comparison.data.previous.updatedAt)} ←{" "}
                {formatDate(comparison.data.current.updatedAt)}
              </Typography>
            ) : (
              <Typography color="text.secondary">
                برای این شرح وظیفه نسخهٔ قبلی ثبت نشده است.
              </Typography>
            )}
            {comparison.data.changes.length === 0 ? (
              <Typography>تفاوتی بین دو نسخه پیدا نشد.</Typography>
            ) : (
              <Table size="small" aria-label="تفاوت نسخه‌ها">
                <TableHead>
                  <TableRow>
                    <TableCell>فیلد</TableCell>
                    <TableCell>قبلی</TableCell>
                    <TableCell>فعلی</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {comparison.data.changes.map((change, index) => (
                    <TableRow key={`${change.field}-${index}`}>
                      <TableCell>
                        {comparisonFieldLabel(change.field)}
                      </TableCell>
                      <TableCell>{change.before ?? "—"}</TableCell>
                      <TableCell>{change.after ?? "—"}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </Stack>
        ) : null}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>بستن</Button>
      </DialogActions>
    </Dialog>
  );
}

function formatDate(value: string) {
  const formatted = formatPersianDateTime(new Date(value));
  return `${formatted.date} · ${formatted.time}`;
}

function comparisonFieldLabel(field: string) {
  const labels: Record<string, string> = {
    personName: "نام پرسنل",
    departmentId: "بخش",
    personnelCode: "کد پرسنلی",
    education: "تحصیلات",
    fieldOfStudy: "رشته تحصیلی",
    minimumExperience: "حداقل سابقه",
    workflowStatus: "وضعیت گردش",
    qualityStatus: "وضعیت کیفیت",
    skillAdded: "مهارت افزوده‌شده",
    skillRemoved: "مهارت حذف‌شده",
    taskAdded: "وظیفه افزوده‌شده",
    taskRemoved: "وظیفه حذف‌شده",
  };
  return labels[field] ?? field;
}
