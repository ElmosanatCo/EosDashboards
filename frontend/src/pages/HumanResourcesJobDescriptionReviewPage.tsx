import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import CancelOutlinedIcon from "@mui/icons-material/CancelOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
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
import { toPersianDigits } from "../lib/format/persianDigits";

export function HumanResourcesJobDescriptionReviewPage() {
  const queryClient = useQueryClient();
  const review = useQuery({
    queryKey: ["human-resources-review"],
    queryFn: jobDescriptionsApi.humanResourcesReview,
  });
  const approve = useMutation({
    mutationFn: jobDescriptionsApi.approveByHumanResources,
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["human-resources-review"],
      }),
  });
  const reject = useMutation({
    mutationFn: ({ id, reason }: { id: number; reason: string }) =>
      jobDescriptionsApi.rejectByHumanResources(id, reason),
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["human-resources-review"],
      }),
  });
  const [selected, setSelected] = useState<JobDescriptionListItem | null>(null);
  const [detailId, setDetailId] = useState<number | null>(null);
  const [catalogOpen, setCatalogOpen] = useState(false);
  const [reason, setReason] = useState("");
  return (
    <Stack spacing={2.5} sx={{ width: "100%" }}>
      <Box>
        <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
          بازبینی شرح وظایف
        </Typography>
        <Typography color="text.secondary" sx={{ mt: 0.5 }}>
          پرونده‌هایی که مدیر بخش برای تأیید منابع انسانی ارسال کرده است.
        </Typography>
        <Button
          sx={{ mt: 1 }}
          variant="outlined"
          startIcon={<SettingsOutlinedIcon />}
          onClick={() => setCatalogOpen(true)}
        >
          مدیریت مهارت‌های عمومی
        </Button>
        {approve.isError ? <MutationErrorAlert error={approve.error} /> : null}
        {reject.isError ? <MutationErrorAlert error={reject.error} /> : null}
      </Box>
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{ borderTop: 3, borderTopColor: "primary.main", overflowX: "auto" }}
      >
        {review.isPending ? (
          <Box sx={{ minHeight: 220, display: "grid", placeItems: "center" }}>
            <CircularProgress aria-label="در حال دریافت کارتابل" />
          </Box>
        ) : review.isError ? (
          <Typography color="error" sx={{ p: 3 }}>
            دریافت کارتابل منابع انسانی ممکن نشد.
          </Typography>
        ) : review.data.length === 0 ? (
          <Typography color="text.secondary" sx={{ p: 3 }}>
            پرونده‌ای برای بازبینی وجود ندارد.
          </Typography>
        ) : (
          <Table aria-label="کارتابل بازبینی شرح وظایف">
            <TableHead>
              <TableRow>
                <TableCell>پرسنل</TableCell>
                <TableCell>واحد</TableCell>
                <TableCell>کیفیت</TableCell>
                <TableCell>عملیات</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {review.data.map((item) => (
                <TableRow key={item.id} hover>
                  <TableCell sx={{ fontWeight: 650 }}>
                    {item.personName}
                  </TableCell>
                  <TableCell className="eos-persian-number">
                    {item.departmentId}
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
                  <TableCell>
                    <Tooltip title="مشاهده جزئیات">
                      <IconButton
                        aria-label={`مشاهده ${item.personName}`}
                        onClick={() => setDetailId(item.id)}
                      >
                        <VisibilityOutlinedIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="تأیید">
                      <IconButton
                        aria-label={`تأیید ${item.personName}`}
                        onClick={() => approve.mutate(item.id)}
                      >
                        <CheckCircleOutlineOutlinedIcon />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="رد با دلیل">
                      <IconButton
                        aria-label={`رد ${item.personName}`}
                        onClick={() => {
                          setSelected(item);
                          setReason("");
                        }}
                      >
                        <CancelOutlinedIcon />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Paper>
      <Dialog
        open={selected !== null}
        onClose={() => setSelected(null)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>رد شرح وظیفه</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            multiline
            minRows={3}
            label="دلیل رد"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            sx={{ mt: 1 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSelected(null)}>انصراف</Button>
          <Button
            color="error"
            variant="contained"
            disabled={!reason.trim() || reject.isPending}
            onClick={() =>
              selected &&
              reject.mutate(
                { id: selected.id, reason },
                { onSuccess: () => setSelected(null) },
              )
            }
          >
            ثبت رد
          </Button>
        </DialogActions>
      </Dialog>
      <HumanResourcesDetailDialog
        versionId={detailId}
        onClose={() => setDetailId(null)}
      />
      <PublicSkillCatalogDialog
        open={catalogOpen}
        onClose={() => setCatalogOpen(false)}
      />
    </Stack>
  );
}

function PublicSkillCatalogDialog({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<"active" | "inactive" | "all">("active");
  const catalog = useQuery({
    queryKey: ["human-resources-catalog", status !== "active"],
    queryFn: () =>
      jobDescriptionsApi.humanResourcesCatalog(status !== "active"),
    enabled: open,
  });
  const [names, setNames] = useState<Record<number, string>>({});
  const [pendingDelete, setPendingDelete] = useState<PublicSkill | null>(null);
  useEffect(() => {
    if (catalog.data)
      setNames(
        Object.fromEntries(catalog.data.map((skill) => [skill.id, skill.name])),
      );
  }, [catalog.data]);
  const rename = useMutation({
    mutationFn: ({ id, name }: { id: number; name: string }) =>
      jobDescriptionsApi.renamePublicSkill(id, name),
    onSuccess: () => {
      setPendingDelete(null);
      void queryClient.invalidateQueries({
        queryKey: ["human-resources-catalog"],
      });
    },
  });
  const deactivate = useMutation({
    mutationFn: (id: number) => jobDescriptionsApi.deactivatePublicSkill(id),
    onSuccess: () => {
      setPendingDelete(null);
      void queryClient.invalidateQueries({
        queryKey: ["human-resources-catalog"],
      });
    },
  });
  const activate = useMutation({
    mutationFn: (id: number) => jobDescriptionsApi.activatePublicSkill(id),
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["human-resources-catalog"],
      }),
  });
  const visibleSkills =
    catalog.data?.filter((skill) =>
      status === "all"
        ? true
        : status === "active"
          ? skill.isActive
          : !skill.isActive,
    ) ?? [];
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>مدیریت مهارت‌های عمومی</DialogTitle>
      <DialogContent>
        <Stack spacing={1.25} sx={{ pt: 1 }}>
          <FormControl size="small" fullWidth>
            <InputLabel id="human-resources-catalog-status-label">
              وضعیت
            </InputLabel>
            <Select
              labelId="human-resources-catalog-status-label"
              label="وضعیت"
              value={status}
              onChange={(event) =>
                setStatus(event.target.value as "active" | "inactive" | "all")
              }
            >
              <MenuItem value="active">فعال</MenuItem>
              <MenuItem value="inactive">غیرفعال</MenuItem>
              <MenuItem value="all">همه</MenuItem>
            </Select>
          </FormControl>
          <MutationErrorAlert
            error={rename.error ?? deactivate.error ?? activate.error}
          />
          {visibleSkills.map((skill) => (
            <Stack
              key={skill.id}
              direction="row"
              spacing={1}
              sx={{ alignItems: "center" }}
            >
              <TextField
                fullWidth
                size="small"
                label={`مهارت ${skill.id}`}
                value={names[skill.id] ?? skill.name}
                onChange={(event) =>
                  setNames((current) => ({
                    ...current,
                    [skill.id]: event.target.value,
                  }))
                }
              />
              {skill.isActive ? (
                <>
                  <Button
                    size="small"
                    disabled={!names[skill.id]?.trim() || rename.isPending}
                    onClick={() =>
                      rename.mutate({ id: skill.id, name: names[skill.id] })
                    }
                  >
                    ذخیره
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
                  startIcon={<CheckCircleOutlineOutlinedIcon />}
                  disabled={activate.isPending}
                  onClick={() => activate.mutate(skill.id)}
                  aria-label={`فعال‌سازی مهارت ${skill.name}`}
                >
                  فعال‌سازی
                </Button>
              )}
            </Stack>
          ))}
          {catalog.isSuccess && visibleSkills.length === 0 ? (
            <Typography color="text.secondary">
              مهارتی با این وضعیت وجود ندارد.
            </Typography>
          ) : null}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>بستن</Button>
      </DialogActions>
      <ConfirmActionDialog
        open={pendingDelete !== null}
        title="تأیید حذف مهارت عمومی"
        message={
          pendingDelete ? `آیا از حذف «${pendingDelete.name}» مطمئن هستید؟` : ""
        }
        pending={deactivate.isPending}
        onClose={() => setPendingDelete(null)}
        onConfirm={() => {
          if (pendingDelete) deactivate.mutate(pendingDelete.id);
        }}
      />
    </Dialog>
  );
}

function HumanResourcesDetailDialog({
  versionId,
  onClose,
}: {
  versionId: number | null;
  onClose: () => void;
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
      <DialogTitle>جزئیات برای بازبینی منابع انسانی</DialogTitle>
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
            <Typography>کیفیت: {detail.data.qualityStatus}</Typography>
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
                  متوسط ساعت کاری مورد نیاز در هفته:{" "}
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
        <Button onClick={onClose}>بستن</Button>
      </DialogActions>
    </Dialog>
  );
}
