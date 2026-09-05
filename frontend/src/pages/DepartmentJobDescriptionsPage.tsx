import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import ExpandMoreOutlinedIcon from "@mui/icons-material/ExpandMoreOutlined";
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import CloudUploadOutlinedIcon from "@mui/icons-material/CloudUploadOutlined";
import DownloadOutlinedIcon from "@mui/icons-material/DownloadOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import ArchiveOutlinedIcon from "@mui/icons-material/ArchiveOutlined";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Button,
  Chip,
  Checkbox,
  CircularProgress,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  InputLabel,
  Link,
  MenuItem,
  Select,
  TextField,
} from "@mui/material";
import { useEffect, useState } from "react";
import { useAuth } from "../app/providers/AuthProvider";
import { ConfirmActionDialog } from "../components/ConfirmActionDialog";
import { MutationErrorAlert } from "../components/MutationErrorAlert";
import { PersianDateTimePicker } from "../components/PersianDateTimePicker";
import { toPersianDigits } from "../lib/format/persianDigits";
import {
  jobDescriptionsApi,
  type CreateJobDescriptionInput,
  type JobDescriptionDetail,
  type JobDescriptionListItem,
  type ManagedDepartment,
} from "../features/jobDescriptions/jobDescriptionsApi";

function parseWeeklyHours(value: string) {
  const normalized = value
    .replace(/[۰-۹]/g, (digit) => String("۰۱۲۳۴۵۶۷۸۹".indexOf(digit)))
    .replace(/[٠-٩]/g, (digit) => String("٠١٢٣٤٥٦٧٨٩".indexOf(digit)))
    .replace("٫", ".")
    .replace(",", ".")
    .trim();
  if (!normalized) return null;
  const hours = Number(normalized);
  return Number.isFinite(hours) && hours >= 0 && hours <= 168 ? hours : null;
}

export function DepartmentJobDescriptionsPage() {
  const { user } = useAuth();
  const [openCreate, setOpenCreate] = useState(false);
  const [uploadInputKey, setUploadInputKey] = useState(0);
  const [departmentId, setDepartmentId] = useState<number | "all">("all");
  const [selectedVersionId, setSelectedVersionId] = useState<number | null>(
    null,
  );
  const [editingVersion, setEditingVersion] =
    useState<JobDescriptionDetail | null>(null);
  const [pendingArchive, setPendingArchive] =
    useState<JobDescriptionListItem | null>(null);
  const [pendingDelete, setPendingDelete] =
    useState<JobDescriptionListItem | null>(null);
  const queryClient = useQueryClient();
  const departments = useQuery({
    queryKey: ["managed-departments"],
    queryFn: jobDescriptionsApi.managedDepartments,
  });
  const descriptions = useQuery({
    queryKey: ["job-descriptions", departmentId],
    queryFn: () =>
      jobDescriptionsApi.list(
        departmentId === "all" ? undefined : departmentId,
      ),
  });
  const approve = useMutation({
    mutationFn: (id: number) =>
      jobDescriptionsApi.approveByDepartmentManager(id),
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: ["job-descriptions"] }),
  });
  const archive = useMutation({
    mutationFn: (id: number) => jobDescriptionsApi.archive(id),
    onSuccess: () => {
      setPendingArchive(null);
      void queryClient.invalidateQueries({ queryKey: ["job-descriptions"] });
      void queryClient.invalidateQueries({
        queryKey: ["department-dashboard"],
      });
    },
  });
  const remove = useMutation({
    mutationFn: (id: number) => jobDescriptionsApi.delete(id),
    onSuccess: () => {
      setPendingDelete(null);
      void queryClient.invalidateQueries({ queryKey: ["job-descriptions"] });
      void queryClient.invalidateQueries({
        queryKey: ["department-dashboard"],
      });
    },
  });
  const importWorkbooks = useMutation({
    mutationFn: (files: File[]) => jobDescriptionsApi.import(files),
    onSuccess: () =>
      void queryClient.invalidateQueries({ queryKey: ["job-descriptions"] }),
  });
  const download = async (item: JobDescriptionListItem) => {
    const blob = await jobDescriptionsApi.download(item.id);
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = `شرح-وظایف-${item.personName}.xlsx`;
    anchor.click();
    URL.revokeObjectURL(url);
  };

  return (
    <Stack spacing={2.5} sx={{ width: "100%" }}>
      <Stack
        direction={{ xs: "column", sm: "row" }}
        spacing={1}
        sx={{ justifyContent: "space-between", alignItems: { sm: "center" } }}
      >
        <Box>
          <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
            مدیریت شرح وظایف
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 0.5 }}>
            فهرست پرسنل، وضعیت بررسی و اقدامات شرح وظایف بخش‌های تحت مدیریت.
          </Typography>
        </Box>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
          <Button
            variant="outlined"
            component="label"
            startIcon={<CloudUploadOutlinedIcon />}
          >
            آپلود اکسل
            <input
              key={uploadInputKey}
              hidden
              type="file"
              accept=".xlsx"
              multiple
              onChange={(event) => {
                const files = Array.from(event.target.files ?? []);
                if (files.length > 0) importWorkbooks.mutate(files);
                setUploadInputKey((key) => key + 1);
              }}
            />
          </Button>
          <Button
            variant="contained"
            startIcon={<AddOutlinedIcon />}
            onClick={() => setOpenCreate(true)}
          >
            شرح وظیفه جدید
          </Button>
        </Stack>
      </Stack>
      {approve.isError ? <MutationErrorAlert error={approve.error} /> : null}
      {archive.isError ? <MutationErrorAlert error={archive.error} /> : null}
      {remove.isError ? <MutationErrorAlert error={remove.error} /> : null}
      {importWorkbooks.isError ? (
        <MutationErrorAlert error={importWorkbooks.error} />
      ) : null}
      {departments.isSuccess && departments.data.length > 0 ? (
        <FormControl sx={{ minWidth: { xs: "100%", sm: 280 } }}>
          <InputLabel id="job-description-department-label">بخش هدف</InputLabel>
          <Select
            labelId="job-description-department-label"
            value={departmentId}
            label="بخش هدف"
            onChange={(event) => {
              const value = String(event.target.value);
              setDepartmentId(value === "all" ? "all" : Number(value));
            }}
          >
            <MenuItem value="all">همه بخش‌ها</MenuItem>
            {departments.data.map((department) => (
              <MenuItem key={department.id} value={department.id}>
                {department.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      ) : null}
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{ borderTop: 3, borderTopColor: "primary.main", overflowX: "auto" }}
      >
        {descriptions.isPending ? (
          <Box sx={{ minHeight: 220, display: "grid", placeItems: "center" }}>
            <CircularProgress aria-label="در حال دریافت شرح وظایف" />
          </Box>
        ) : descriptions.isError ? (
          <Typography color="error" sx={{ p: 3 }}>
            دریافت فهرست شرح وظایف ممکن نشد.
          </Typography>
        ) : descriptions.data.length === 0 ? (
          <Typography color="text.secondary" sx={{ p: 3 }}>
            شرح وظیفه‌ای برای نمایش وجود ندارد.
          </Typography>
        ) : (
          <Table aria-label="فهرست شرح وظایف">
            <TableHead>
              <TableRow>
                <TableCell>پرسنل</TableCell>
                <TableCell>وضعیت گردش</TableCell>
                <TableCell>کیفیت داده</TableCell>
                <TableCell>آخرین تغییر</TableCell>
                <TableCell align="left">عملیات</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {descriptions.data.map((item) => (
                <DescriptionRow
                  key={item.id}
                  item={item}
                  approving={approve.isPending}
                  onApprove={() => approve.mutate(item.id)}
                  onDownload={() => void download(item)}
                  onView={() => setSelectedVersionId(item.id)}
                  onArchive={() => setPendingArchive(item)}
                  onDelete={() => setPendingDelete(item)}
                />
              ))}
            </TableBody>
          </Table>
        )}
      </Paper>
      {importWorkbooks.data?.map((result) => (
        <Paper key={result.fileName} variant="outlined" sx={{ p: 1.5 }}>
          <Typography color={result.succeeded ? "success.main" : "error.main"}>
            {result.fileName}: {result.message}
          </Typography>
          {result.suggestions.length > 0 ? (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              {result.suggestions.join("، ")}
            </Typography>
          ) : null}
        </Paper>
      ))}
      <ConfirmActionDialog
        open={pendingArchive !== null}
        title="تأیید آرشیو شرح وظیفه"
        message={`آیا از آرشیو شرح وظایف «${pendingArchive?.personName ?? ""}» مطمئن هستید؟`}
        confirmLabel="تأیید آرشیو"
        confirmColor="warning"
        pending={archive.isPending}
        onClose={() => setPendingArchive(null)}
        onConfirm={() => {
          if (pendingArchive) archive.mutate(pendingArchive.id);
        }}
      />
      <ConfirmActionDialog
        open={pendingDelete !== null}
        title="تأیید حذف شرح وظیفه"
        message={`آیا از حذف شرح وظایف «${pendingDelete?.personName ?? ""}» مطمئن هستید؟`}
        pending={remove.isPending}
        onClose={() => setPendingDelete(null)}
        onConfirm={() => {
          if (pendingDelete) remove.mutate(pendingDelete.id);
        }}
      />
      <ManualJobDescriptionDialog
        open={openCreate}
        departmentId={
          editingVersion?.departmentId ??
          (departmentId === "all" ? user.department.id : departmentId)
        }
        managedDepartments={departments.data ?? []}
        initial={editingVersion}
        revisionId={editingVersion?.id ?? null}
        onClose={() => setOpenCreate(false)}
        onSaved={() => {
          setOpenCreate(false);
          setEditingVersion(null);
          void queryClient.invalidateQueries({
            queryKey: ["job-descriptions"],
          });
        }}
      />
      <JobDescriptionDetailDialog
        versionId={selectedVersionId}
        onClose={() => setSelectedVersionId(null)}
        onEdit={(version) => {
          setSelectedVersionId(null);
          setEditingVersion(version);
          setOpenCreate(true);
        }}
      />
    </Stack>
  );
}

function ManualJobDescriptionDialog({
  open,
  departmentId,
  managedDepartments,
  onClose,
  onSaved,
  initial,
  revisionId,
}: {
  open: boolean;
  departmentId: number;
  managedDepartments: ManagedDepartment[];
  onClose: () => void;
  onSaved: () => void;
  initial: JobDescriptionDetail | null;
  revisionId: number | null;
}) {
  const [targetDepartmentId, setTargetDepartmentId] = useState(departmentId);
  const catalog = useQuery({
    queryKey: ["job-description-catalog", targetDepartmentId],
    queryFn: () => jobDescriptionsApi.catalog(targetDepartmentId),
    enabled: open,
  });
  const create = useMutation({
    mutationFn: (input: CreateJobDescriptionInput) =>
      revisionId
        ? jobDescriptionsApi.revise(revisionId, input)
        : jobDescriptionsApi.create(input),
  });
  const [personName, setPersonName] = useState("");
  const [personnelCode, setPersonnelCode] = useState("");
  const [education, setEducation] = useState("");
  const [fieldOfStudy, setFieldOfStudy] = useState("");
  const [minimumExperience, setMinimumExperience] = useState("");
  const [skillIds, setSkillIds] = useState<number[]>([]);
  const [unresolvedSkillSelections, setUnresolvedSkillSelections] = useState<
    Record<string, number | "">
  >({});
  const [newSkillName, setNewSkillName] = useState("");
  const [newSkillScope, setNewSkillScope] = useState<"department" | "public">(
    "department",
  );
  const [newSkillTarget, setNewSkillTarget] = useState<string | null>(null);
  type DraftTask = {
    taskId: number | "";
    title: string;
    description: string;
    startDate: Date | null;
    endDate: Date | null;
    weeklyHours: string;
  };
  const emptyTask = (): DraftTask => ({
    taskId: "",
    title: "",
    description: "",
    startDate: null,
    endDate: null,
    weeklyHours: "",
  });
  const [tasks, setTasks] = useState<DraftTask[]>([emptyTask()]);
  const [newTaskTargetIndex, setNewTaskTargetIndex] = useState<number | null>(
    null,
  );
  const [pendingTaskDeleteIndex, setPendingTaskDeleteIndex] = useState<
    number | null
  >(null);
  const [newTaskTitle, setNewTaskTitle] = useState("");
  const [newTaskIsProject, setNewTaskIsProject] = useState(false);
  const [expandedTaskIndex, setExpandedTaskIndex] = useState<number | null>(0);
  const createSkill = useMutation({
    mutationFn: () =>
      newSkillScope === "public"
        ? jobDescriptionsApi.createPublicSkill(targetDepartmentId, newSkillName)
        : jobDescriptionsApi.createSkill(targetDepartmentId, newSkillName),
    onSuccess: (result) => {
      setSkillIds((current) => [...new Set([...current, result.id])]);
      if (newSkillTarget && newSkillTarget !== "__new__") {
        setUnresolvedSkillSelections((current) => {
          const next = { ...current };
          delete next[newSkillTarget];
          return next;
        });
      }
      setNewSkillName("");
      setNewSkillTarget(null);
      void catalog.refetch();
    },
  });
  const createTask = useMutation({
    mutationFn: (request: {
      targetIndex: number;
      title: string;
      isProject: boolean;
    }) =>
      jobDescriptionsApi.createTask(
        targetDepartmentId,
        request.title,
        request.isProject,
      ),
    onSuccess: (result, request) => {
      setTasks((current) =>
        current.map((task, index) =>
          index === request.targetIndex
            ? { ...task, taskId: result.id, title: request.title }
            : task,
        ),
      );
      setExpandedTaskIndex(request.targetIndex);
      setNewTaskTitle("");
      setNewTaskIsProject(false);
      setNewTaskTargetIndex(null);
      void catalog.refetch();
    },
  });
  useEffect(() => {
    if (!open) return;
    setTargetDepartmentId(departmentId);
    if (!initial) {
      setPersonName("");
      setPersonnelCode("");
      setEducation("");
      setFieldOfStudy("");
      setMinimumExperience("");
      setSkillIds([]);
      setUnresolvedSkillSelections({});
      setNewSkillName("");
      setNewSkillScope("department");
      setNewSkillTarget(null);
      setNewTaskTargetIndex(null);
      setPendingTaskDeleteIndex(null);
      setExpandedTaskIndex(0);
      setTasks([emptyTask()]);
      return;
    }
    setPersonName(initial.personName);
    setPersonnelCode(initial.personnelCode ?? "");
    setEducation(initial.education);
    setFieldOfStudy(initial.fieldOfStudy);
    setMinimumExperience(initial.minimumExperience);
    setSkillIds(initial.skillIds);
    setUnresolvedSkillSelections(
      Object.fromEntries(
        initial.unresolvedSkills.map((skill) => [skill.rawName, ""]),
      ),
    );
    setNewSkillName("");
    setNewSkillScope("department");
    setNewSkillTarget(null);
    setNewTaskTargetIndex(null);
    setExpandedTaskIndex(0);
    const draftTasks: DraftTask[] = [
      ...initial.tasks.map((task) => ({
        taskId: task.taskCatalogItemId,
        title: task.title,
        description: task.description,
        startDate: task.startDate
          ? new Date(`${task.startDate}T00:00:00`)
          : null,
        endDate: task.endDate ? new Date(`${task.endDate}T00:00:00`) : null,
        weeklyHours:
          task.weeklyHours === null
            ? ""
            : toPersianDigits(String(task.weeklyHours)),
      })),
      ...initial.unresolvedTasks.map((task) => ({
        taskId: "" as const,
        title: task.rawTitle,
        description: task.description,
        startDate: task.startDate
          ? new Date(`${task.startDate}T00:00:00`)
          : null,
        endDate: task.endDate ? new Date(`${task.endDate}T00:00:00`) : null,
        weeklyHours: "",
      })),
    ];
    setTasks(draftTasks.length > 0 ? draftTasks : [emptyTask()]);
  }, [departmentId, initial, open]);
  const reset = () => {
    setPersonName("");
    setPersonnelCode("");
    setEducation("");
    setFieldOfStudy("");
    setMinimumExperience("");
    setSkillIds([]);
    setUnresolvedSkillSelections({});
    setNewSkillName("");
    setNewSkillScope("department");
    setNewSkillTarget(null);
    setNewTaskTargetIndex(null);
    setPendingTaskDeleteIndex(null);
    setTasks([emptyTask()]);
  };
  const submit = () => {
    if (
      !catalog.data ||
      !personName.trim() ||
      !personnelCode.trim() ||
      tasks.length === 0 ||
      tasks.some(
        (task) =>
          !task.taskId ||
          !task.description.trim() ||
          parseWeeklyHours(task.weeklyHours) === null,
      ) ||
      Object.values(unresolvedSkillSelections).some((skillId) => !skillId)
    )
      return;
    const input: CreateJobDescriptionInput = {
      personName,
      departmentId: targetDepartmentId,
      personnelCode: personnelCode.trim(),
      education,
      fieldOfStudy,
      minimumExperience,
      skillIds: [
        ...skillIds,
        ...Object.values(unresolvedSkillSelections).filter(
          (skillId): skillId is number => typeof skillId === "number",
        ),
      ],
      tasks: tasks.map((draft, index) => {
        const task = catalog.data!.tasks.find(
          (item) => item.id === draft.taskId,
        )!;
        return {
          taskCatalogItemId: task.id,
          title: task.title,
          description: draft.description,
          startDate: draft.startDate ? toDateOnly(draft.startDate) : null,
          endDate: draft.endDate ? toDateOnly(draft.endDate) : null,
          weeklyHours: parseWeeklyHours(draft.weeklyHours),
          sortOrder: index + 1,
        };
      }),
    };
    create.mutate(input, {
      onSuccess: () => {
        reset();
        onSaved();
      },
    });
  };
  const missingProfileCount = [
    personName,
    personnelCode,
    education,
    fieldOfStudy,
    minimumExperience,
  ].filter((value) => !value.trim()).length;
  const unresolvedSkillCount = Object.values(unresolvedSkillSelections).filter(
    (skillId) => typeof skillId !== "number",
  ).length;
  const mappedSkillIds = Object.values(unresolvedSkillSelections).filter(
    (skillId): skillId is number => typeof skillId === "number",
  );
  const selectedSkillIds = new Set([...skillIds, ...mappedSkillIds]);
  const selectedSkillCount = selectedSkillIds.size;
  const taskIssueCount = tasks.filter(
    (task) =>
      !task.taskId ||
      !task.description.trim() ||
      !task.startDate ||
      parseWeeklyHours(task.weeklyHours) === null,
  ).length;
  const issueCount =
    missingProfileCount + unresolvedSkillCount + taskIssueCount;
  const renderNewTaskEditor = () => (
    <Paper variant="outlined" sx={{ p: 1.5 }}>
      <Stack spacing={1}>
        <TextField
          label="عنوان وظیفه جدید"
          value={newTaskTitle}
          onChange={(event) => setNewTaskTitle(event.target.value)}
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={newTaskIsProject}
              onChange={(event) => setNewTaskIsProject(event.target.checked)}
            />
          }
          label="پروژه"
        />
        <Stack direction="row" spacing={1}>
          <Button
            variant="contained"
            disabled={createTask.isPending || !newTaskTitle.trim()}
            onClick={() => {
              if (newTaskTargetIndex === null) return;
              createTask.mutate({
                targetIndex: newTaskTargetIndex,
                title: newTaskTitle.trim(),
                isProject: newTaskIsProject,
              });
            }}
          >
            ثبت و استفاده از وظیفه
          </Button>
          <Button
            onClick={() => {
              setNewTaskTitle("");
              setNewTaskIsProject(false);
              setNewTaskTargetIndex(null);
            }}
          >
            انصراف
          </Button>
        </Stack>
        {createTask.isError ? (
          <MutationErrorAlert error={createTask.error} />
        ) : null}
      </Stack>
    </Paper>
  );
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
      <DialogTitle>
        {revisionId ? "ویرایش شرح وظیفه" : "ثبت دستی شرح وظیفه"}
      </DialogTitle>
      <DialogContent>
        <Stack spacing={1.5} sx={{ pt: 1 }}>
          <Paper
            variant="outlined"
            sx={{
              p: 1.25,
              borderColor: issueCount > 0 ? "warning.main" : "success.main",
            }}
          >
            <Stack
              direction={{ xs: "column", sm: "row" }}
              spacing={1}
              sx={{ alignItems: { sm: "center" }, flexWrap: "wrap" }}
            >
              <Typography variant="subtitle2">وضعیت فرم</Typography>
              <Chip
                size="small"
                color={issueCount > 0 ? "warning" : "success"}
                label={
                  issueCount > 0
                    ? `${toPersianDigits(issueCount)} مورد نیازمند رسیدگی`
                    : "آماده بررسی"
                }
              />
              {missingProfileCount > 0 ? (
                <Chip
                  size="small"
                  variant="outlined"
                  color="warning"
                  label={`اطلاعات پرسنل: ${toPersianDigits(missingProfileCount)}`}
                />
              ) : null}
              {unresolvedSkillCount > 0 ? (
                <Chip
                  size="small"
                  variant="outlined"
                  color="warning"
                  label={`مهارت تطبیق‌نشده: ${toPersianDigits(unresolvedSkillCount)}`}
                />
              ) : null}
              {taskIssueCount > 0 ? (
                <Chip
                  size="small"
                  variant="outlined"
                  color="warning"
                  label={`وظیفه ناقص: ${toPersianDigits(taskIssueCount)}`}
                />
              ) : null}
            </Stack>
          </Paper>
          <Accordion defaultExpanded disableGutters>
            <AccordionSummary expandIcon={<ExpandMoreOutlinedIcon />}>
              <Stack
                direction="row"
                spacing={1}
                sx={{ alignItems: "center", flexWrap: "wrap" }}
              >
                <Typography variant="subtitle1">اطلاعات پرسنل</Typography>
                {missingProfileCount > 0 ? (
                  <Chip
                    size="small"
                    color="warning"
                    label={`ناقص · ${toPersianDigits(missingProfileCount)}`}
                  />
                ) : (
                  <Chip size="small" color="success" label="کامل" />
                )}
              </Stack>
            </AccordionSummary>
            <AccordionDetails>
              <Stack spacing={1.5}>
                <FormControl fullWidth>
                  <InputLabel id="job-description-form-department-label">
                    بخش هدف
                  </InputLabel>
                  <Select
                    labelId="job-description-form-department-label"
                    value={targetDepartmentId}
                    label="بخش هدف"
                    onChange={(event) =>
                      setTargetDepartmentId(Number(event.target.value))
                    }
                  >
                    {managedDepartments.map((department) => (
                      <MenuItem key={department.id} value={department.id}>
                        {department.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <TextField
                  label="نام و نام خانوادگی"
                  value={personName}
                  onChange={(event) => setPersonName(event.target.value)}
                  required
                />
                <TextField
                  label="کد پرسنلی"
                  value={personnelCode}
                  onChange={(event) => setPersonnelCode(event.target.value)}
                  required
                />
                <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
                  <TextField
                    fullWidth
                    label="مدرک تحصیلی"
                    value={education}
                    onChange={(event) => setEducation(event.target.value)}
                  />
                  <TextField
                    fullWidth
                    label="رشته تحصیلی"
                    value={fieldOfStudy}
                    onChange={(event) => setFieldOfStudy(event.target.value)}
                  />
                </Stack>
                <TextField
                  label="حداقل سابقه تخصصی"
                  value={minimumExperience}
                  onChange={(event) => setMinimumExperience(event.target.value)}
                />
              </Stack>
            </AccordionDetails>
          </Accordion>
          <Accordion defaultExpanded={unresolvedSkillCount > 0} disableGutters>
            <AccordionSummary expandIcon={<ExpandMoreOutlinedIcon />}>
              <Stack
                direction="row"
                spacing={1}
                sx={{ alignItems: "center", flexWrap: "wrap" }}
              >
                <Typography variant="subtitle1">مهارت‌ها</Typography>
                {unresolvedSkillCount > 0 ? (
                  <Chip
                    size="small"
                    color="warning"
                    label={`ناقص · ${toPersianDigits(unresolvedSkillCount)}`}
                  />
                ) : selectedSkillCount === 0 ? (
                  <Chip
                    size="small"
                    color="warning"
                    label="مهارتی انتخاب نشده"
                  />
                ) : (
                  <Chip
                    size="small"
                    color="success"
                    label={`${toPersianDigits(selectedSkillCount)} مهارت`}
                  />
                )}
              </Stack>
            </AccordionSummary>
            <AccordionDetails>
              <Stack spacing={1.5}>
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
                  {catalog.data?.skills.map((skill) => (
                    <Chip
                      key={skill.id}
                      clickable
                      label={skill.name}
                      color={
                        selectedSkillIds.has(skill.id) ? "primary" : "default"
                      }
                      onClick={() =>
                        setSkillIds((current) =>
                          current.includes(skill.id)
                            ? current.filter((id) => id !== skill.id)
                            : [...current, skill.id],
                        )
                      }
                    />
                  ))}
                </Box>
                {Object.entries(unresolvedSkillSelections).map(
                  ([rawName, selectedId]) => (
                    <Stack
                      key={rawName}
                      direction={{ xs: "column", sm: "row" }}
                      spacing={1}
                      sx={{ alignItems: { sm: "center" } }}
                    >
                      <Typography color="warning.main" sx={{ minWidth: 180 }}>
                        مهارت واردشده: {rawName}
                      </Typography>
                      <FormControl fullWidth>
                        <InputLabel id={`resolve-skill-${rawName}`}>
                          تبدیل به مهارت کاتالوگ
                        </InputLabel>
                        <Select
                          labelId={`resolve-skill-${rawName}`}
                          label="تبدیل به مهارت کاتالوگ"
                          value={selectedId}
                          onChange={(event) =>
                            setUnresolvedSkillSelections((current) => ({
                              ...current,
                              [rawName]: event.target.value
                                ? Number(event.target.value)
                                : "",
                            }))
                          }
                        >
                          <MenuItem value="">انتخاب کنید</MenuItem>
                          {catalog.data?.skills.map((skill) => (
                            <MenuItem key={skill.id} value={skill.id}>
                              {skill.name}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </Stack>
                  ),
                )}
                <Button
                  variant="outlined"
                  onClick={() => {
                    const target =
                      Object.keys(unresolvedSkillSelections)[0] ?? "__new__";
                    setNewSkillTarget(target);
                    setNewSkillName(target === "__new__" ? "" : target);
                  }}
                >
                  ایجاد مهارت جدید
                </Button>
                {newSkillTarget !== null || newSkillName ? (
                  <Paper variant="outlined" sx={{ p: 1.5 }}>
                    <Stack spacing={1}>
                      <TextField
                        label="نام مهارت جدید"
                        value={newSkillName}
                        onChange={(event) =>
                          setNewSkillName(event.target.value)
                        }
                      />
                      <FormControlLabel
                        control={
                          <Checkbox
                            checked={newSkillScope === "public"}
                            onChange={(event) =>
                              setNewSkillScope(
                                event.target.checked ? "public" : "department",
                              )
                            }
                          />
                        }
                        label="عمومی"
                      />
                      <Stack direction="row" spacing={1}>
                        <Button
                          variant="contained"
                          disabled={
                            createSkill.isPending || !newSkillName.trim()
                          }
                          onClick={() => createSkill.mutate()}
                        >
                          ثبت و استفاده از مهارت
                        </Button>
                        <Button
                          onClick={() => {
                            setNewSkillName("");
                            setNewSkillTarget(null);
                          }}
                        >
                          انصراف
                        </Button>
                      </Stack>
                      {createSkill.isError ? (
                        <MutationErrorAlert error={createSkill.error} />
                      ) : null}
                    </Stack>
                  </Paper>
                ) : null}
              </Stack>
            </AccordionDetails>
          </Accordion>
          <Accordion defaultExpanded disableGutters>
            <AccordionSummary expandIcon={<ExpandMoreOutlinedIcon />}>
              <Stack
                direction="row"
                spacing={1}
                sx={{ alignItems: "center", flexWrap: "wrap" }}
              >
                <Typography variant="subtitle1">وظایف</Typography>
                {taskIssueCount > 0 ? (
                  <Chip
                    size="small"
                    color="warning"
                    label={`ناقص · ${toPersianDigits(taskIssueCount)}`}
                  />
                ) : (
                  <Chip
                    size="small"
                    color="success"
                    label={`${toPersianDigits(tasks.length)} وظیفه`}
                  />
                )}
              </Stack>
            </AccordionSummary>
            <AccordionDetails>
              <Stack spacing={1.25}>
                {tasks.map((draft, index) => (
                  <Accordion
                    key={index}
                    expanded={expandedTaskIndex === index}
                    onChange={(_, isExpanded) =>
                      setExpandedTaskIndex(isExpanded ? index : null)
                    }
                    disableGutters
                  >
                    <AccordionSummary expandIcon={<ExpandMoreOutlinedIcon />}>
                      <Stack
                        direction={{ xs: "column", sm: "row" }}
                        spacing={1}
                        sx={{ alignItems: { sm: "center" }, flexWrap: "wrap" }}
                      >
                        <Typography variant="subtitle2">
                          وظیفه {toPersianDigits(index + 1)} ·{" "}
                          {draft.title || "بدون عنوان"}
                        </Typography>
                        <Stack
                          direction="row"
                          spacing={0.75}
                          sx={{ flexWrap: "wrap" }}
                        >
                          <Chip
                            size="small"
                            color={draft.taskId ? "success" : "warning"}
                            label={draft.taskId ? "تطبیق‌شده" : "تطبیق‌نشده"}
                          />
                          {!draft.description.trim() ? (
                            <Chip
                              size="small"
                              color="warning"
                              label="شرح ناقص"
                            />
                          ) : null}
                          {!draft.startDate ? (
                            <Chip
                              size="small"
                              color="warning"
                              label="تاریخ شروع ثبت نشده"
                            />
                          ) : null}
                          {parseWeeklyHours(draft.weeklyHours) === null ? (
                            <Chip
                              size="small"
                              color="warning"
                              label="متوسط ساعت کاری ثبت نشده یا نامعتبر"
                            />
                          ) : null}
                        </Stack>
                      </Stack>
                    </AccordionSummary>
                    <AccordionDetails sx={{ pt: 0 }}>
                      <Stack spacing={1.25}>
                        <Stack
                          direction={{ xs: "column", sm: "row" }}
                          spacing={1}
                          sx={{ alignItems: { sm: "flex-start" } }}
                        >
                          <Stack spacing={0.5} sx={{ flex: 1, minWidth: 0 }}>
                            <FormControl fullWidth>
                              <InputLabel id={`job-task-label-${index}`}>
                                وظیفه {toPersianDigits(index + 1)}
                              </InputLabel>
                              <Select
                                labelId={`job-task-label-${index}`}
                                label={`وظیفه ${toPersianDigits(index + 1)}`}
                                value={draft.taskId}
                                onChange={(event) =>
                                  setTasks((current) =>
                                    current.map((item, itemIndex) =>
                                      itemIndex === index
                                        ? {
                                            ...item,
                                            taskId: Number(event.target.value),
                                            title:
                                              catalog.data?.tasks.find(
                                                (task) =>
                                                  task.id ===
                                                  Number(event.target.value),
                                              )?.title ?? item.title,
                                          }
                                        : item,
                                    ),
                                  )
                                }
                              >
                                {catalog.data?.tasks.map((task) => (
                                  <MenuItem key={task.id} value={task.id}>
                                    {task.title}
                                    {task.isProject ? " · پروژه" : ""}
                                  </MenuItem>
                                ))}
                              </Select>
                            </FormControl>
                            {!draft.taskId && draft.title ? (
                              <Box
                                sx={{
                                  px: 1,
                                  py: 0.5,
                                  borderRadius: 1,
                                  bgcolor: "rgba(255, 183, 77, 0.08)",
                                }}
                              >
                                <Typography
                                  variant="caption"
                                  color="warning.main"
                                  sx={{ lineHeight: 1.8 }}
                                >
                                  عنوان واردشده: {draft.title} — برای رفع نقص یک
                                  وظیفه از کاتالوگ انتخاب کنید.
                                </Typography>
                              </Box>
                            ) : null}
                          </Stack>
                          <Button
                            size="small"
                            onClick={() => {
                              setNewTaskTargetIndex(index);
                              setNewTaskTitle(draft.taskId ? "" : draft.title);
                              createTask.reset();
                            }}
                          >
                            ایجاد وظیفه جدید
                          </Button>
                          {tasks.length > 1 ? (
                            <Button
                              color="error"
                              onClick={() => setPendingTaskDeleteIndex(index)}
                            >
                              حذف
                            </Button>
                          ) : null}
                        </Stack>
                        {newTaskTargetIndex === index
                          ? renderNewTaskEditor()
                          : null}
                        <TextField
                          label="متوسط ساعت کاری مورد نیاز در هفته"
                          value={draft.weeklyHours}
                          onChange={(event) =>
                            setTasks((current) =>
                              current.map((item, itemIndex) =>
                                itemIndex === index
                                  ? { ...item, weeklyHours: event.target.value }
                                  : item,
                              ),
                            )
                          }
                          slotProps={{ htmlInput: { inputMode: "decimal" } }}
                          helperText="عددی بین ۰ تا ۱۶۸ ساعت وارد کنید."
                          required
                        />
                        <TextField
                          label="شرح وظیفه"
                          value={draft.description}
                          onChange={(event) =>
                            setTasks((current) =>
                              current.map((item, itemIndex) =>
                                itemIndex === index
                                  ? { ...item, description: event.target.value }
                                  : item,
                              ),
                            )
                          }
                          multiline
                          minRows={3}
                          required
                        />
                        <Stack
                          direction={{ xs: "column", sm: "row" }}
                          spacing={1}
                        >
                          <PersianDateTimePicker
                            label="تاریخ شروع"
                            value={draft.startDate}
                            includeTime={false}
                            onChange={(value) =>
                              setTasks((current) =>
                                current.map((item, itemIndex) =>
                                  itemIndex === index
                                    ? { ...item, startDate: value }
                                    : item,
                                ),
                              )
                            }
                          />
                          <PersianDateTimePicker
                            label="تاریخ پایان (اختیاری)"
                            value={draft.endDate}
                            includeTime={false}
                            onChange={(value) =>
                              setTasks((current) =>
                                current.map((item, itemIndex) =>
                                  itemIndex === index
                                    ? { ...item, endDate: value }
                                    : item,
                                ),
                              )
                            }
                          />
                        </Stack>
                      </Stack>
                    </AccordionDetails>
                  </Accordion>
                ))}
                <Button
                  variant="text"
                  onClick={() =>
                    setTasks((current) => [...current, emptyTask()])
                  }
                >
                  افزودن وظیفه دیگر
                </Button>
                {catalog.isSuccess && catalog.data.tasks.length === 0 ? (
                  <Typography color="warning.main">
                    ابتدا یک وظیفه در کاتالوگ بخش تعریف کنید.
                  </Typography>
                ) : null}
                {create.isError ? (
                  <MutationErrorAlert error={create.error} />
                ) : null}
              </Stack>
            </AccordionDetails>
          </Accordion>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>انصراف</Button>
        <Button
          variant="contained"
          onClick={submit}
          disabled={
            create.isPending ||
            tasks.length === 0 ||
            tasks.some(
              (task) =>
                !task.taskId ||
                !task.description.trim() ||
                parseWeeklyHours(task.weeklyHours) === null,
            ) ||
            !personName.trim() ||
            !personnelCode.trim()
          }
        >
          {revisionId ? "ثبت نسخه‌ی جدید" : "ثبت پیش‌نویس"}
        </Button>
      </DialogActions>
      <ConfirmActionDialog
        open={pendingTaskDeleteIndex !== null}
        title="تأیید حذف وظیفه"
        message={
          pendingTaskDeleteIndex !== null
            ? `آیا از حذف «${tasks[pendingTaskDeleteIndex]?.title || "وظیفه بدون عنوان"}» از پیش‌نویس مطمئن هستید؟`
            : ""
        }
        onClose={() => setPendingTaskDeleteIndex(null)}
        onConfirm={() => {
          if (pendingTaskDeleteIndex === null) return;
          setTasks((current) =>
            current.filter(
              (_, itemIndex) => itemIndex !== pendingTaskDeleteIndex,
            ),
          );
          setNewTaskTargetIndex(null);
          setPendingTaskDeleteIndex(null);
        }}
      />
    </Dialog>
  );
}

function JobDescriptionDetailDialog({
  versionId,
  onClose,
  onEdit,
}: {
  versionId: number | null;
  onClose: () => void;
  onEdit: (version: JobDescriptionDetail) => void;
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
      <DialogTitle>جزئیات شرح وظیفه</DialogTitle>
      <DialogContent>
        {detail.isPending ? (
          <CircularProgress aria-label="در حال دریافت جزئیات" />
        ) : null}
        {detail.data ? (
          <Stack spacing={1.5} sx={{ pt: 1 }}>
            <Typography variant="h6">{detail.data.personName}</Typography>
            <Typography color="text.secondary">
              {detail.data.education || "مدرک ثبت نشده"} ·{" "}
              {detail.data.fieldOfStudy || "رشته ثبت نشده"}
            </Typography>
            <Typography>
              وضعیت: {detail.data.workflowStatus} · کیفیت:{" "}
              {detail.data.qualityStatus}
            </Typography>
            <Box id="skills">
              <Typography variant="subtitle2">مهارت‌های انتخاب‌شده</Typography>
              <Box
                sx={{ display: "flex", flexWrap: "wrap", gap: 0.75, mt: 0.75 }}
              >
                {detail.data.selectedSkills.map((skill) => (
                  <Chip key={skill.id} size="small" label={skill.name} />
                ))}
              </Box>
              {detail.data.unresolvedSkills.map((skill) => (
                <Chip
                  key={skill.sortOrder}
                  size="small"
                  color="warning"
                  label={`تطبیق‌نشده: ${skill.rawName}`}
                  sx={{ mt: 0.75, mr: 0.75 }}
                />
              ))}
            </Box>
            {detail.data.rejectionReason ? (
              <Typography color="error">
                دلیل رد: {detail.data.rejectionReason}
              </Typography>
            ) : null}
            <Typography variant="subtitle2">وظایف</Typography>
            {detail.data.tasks.length === 0 &&
            detail.data.unresolvedTasks.length === 0 ? (
              <Typography color="text.secondary">
                وظیفه‌ای ثبت نشده است.
              </Typography>
            ) : (
              detail.data.tasks.map((task) => (
                <Paper
                  key={`${task.sortOrder}-${task.taskCatalogItemId}`}
                  id={`task:${task.sortOrder}/startDate`}
                  variant="outlined"
                  sx={{ p: 1.5 }}
                >
                  <Typography sx={{ fontWeight: 650 }}>{task.title}</Typography>
                  <Typography sx={{ mt: 0.5 }}>{task.description}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    متوسط ساعت کاری مورد نیاز در هفته:{" "}
                    {task.weeklyHours === null
                      ? "ثبت نشده"
                      : toPersianDigits(String(task.weeklyHours))}
                  </Typography>
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{ mt: 0.5 }}
                  >
                    شروع: {task.startDate ?? "ثبت نشده"} · پایان:{" "}
                    {task.endDate ?? "جاری"}
                  </Typography>
                </Paper>
              ))
            )}
            {detail.data.unresolvedTasks.map((task) => (
              <Paper
                key={`unresolved-task-${task.sortOrder}`}
                variant="outlined"
                sx={{ p: 1.5, borderColor: "warning.main" }}
              >
                <Typography color="warning.main" sx={{ fontWeight: 650 }}>
                  تطبیق‌نشده: {task.rawTitle}
                </Typography>
                <Typography sx={{ mt: 0.5 }}>{task.description}</Typography>
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{ mt: 0.5 }}
                >
                  برای ارسال، در ویرایش یک وظیفه از کاتالوگ انتخاب کنید.
                </Typography>
              </Paper>
            ))}
            <Typography variant="subtitle2">نتیجه تحلیل کیفیت</Typography>
            {analysis.data?.length === 0 ? (
              <Typography color="success.main">
                موردی برای پیگیری پیدا نشد.
              </Typography>
            ) : (
              analysis.data?.map((finding) => (
                <Typography
                  key={`${finding.code}-${finding.actionTarget}`}
                  color="warning.main"
                >
                  {finding.message} · محل اقدام:{" "}
                  <Link
                    href={`#${finding.actionTarget}`}
                    color="inherit"
                    underline="always"
                  >
                    رفتن به محل
                  </Link>
                </Typography>
              ))
            )}
          </Stack>
        ) : null}
      </DialogContent>
      <DialogActions>
        {detail.data ? (
          <Button
            startIcon={<EditOutlinedIcon />}
            onClick={() => onEdit(detail.data)}
          >
            ویرایش
          </Button>
        ) : null}
        <Button onClick={onClose}>بستن</Button>
      </DialogActions>
    </Dialog>
  );
}

function toDateOnly(value: Date) {
  const pad = (part: number) => String(part).padStart(2, "0");
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}`;
}

function DescriptionRow({
  item,
  approving,
  onApprove,
  onDownload,
  onView,
  onArchive,
  onDelete,
}: {
  item: JobDescriptionListItem;
  approving: boolean;
  onApprove: () => void;
  onDownload: () => void;
  onView: () => void;
  onArchive: () => void;
  onDelete: () => void;
}) {
  const canApprove =
    item.workflowStatus === "منتظر تأیید" && item.qualityStatus === "سالم";
  const canArchive = item.workflowStatus === "تأیید شده";
  const canDelete =
    item.workflowStatus === "منتظر تأیید" ||
    item.workflowStatus === "منتظر رفع نقص";
  return (
    <TableRow hover>
      <TableCell sx={{ fontWeight: 650 }}>{item.personName}</TableCell>
      <TableCell>
        <Chip
          size="small"
          color={item.workflowStatus === "تأیید شده" ? "success" : "default"}
          label={item.workflowStatus}
        />
      </TableCell>
      <TableCell>
        <Chip
          size="small"
          color={item.qualityStatus === "سالم" ? "success" : "warning"}
          label={item.qualityStatus}
        />
        {item.needsReview ? (
          <Chip
            size="small"
            color="warning"
            label="نیازمند بررسی"
            sx={{ mr: 0.75, mt: { xs: 0.75, sm: 0 } }}
          />
        ) : null}
      </TableCell>
      <TableCell className="eos-persian-number">
        {new Intl.DateTimeFormat("fa-IR", { dateStyle: "medium" }).format(
          new Date(item.updatedAt),
        )}
      </TableCell>
      <TableCell align="left">
        <Tooltip title="مشاهده">
          <IconButton
            aria-label={`مشاهده شرح وظایف ${item.personName}`}
            onClick={onView}
          >
            <VisibilityOutlinedIcon fontSize="small" />
          </IconButton>
        </Tooltip>
        <Tooltip title="دانلود اکسل استاندارد">
          <IconButton
            aria-label={`دانلود شرح وظایف ${item.personName}`}
            onClick={onDownload}
          >
            <DownloadOutlinedIcon fontSize="small" />
          </IconButton>
        </Tooltip>
        {canApprove ? (
          <Tooltip title="تأیید و ارسال برای منابع انسانی">
            <IconButton
              aria-label={`تأیید شرح وظایف ${item.personName}`}
              disabled={approving}
              onClick={onApprove}
              color="primary"
            >
              <CheckCircleOutlineOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        ) : null}
        {canArchive ? (
          <Tooltip title="آرشیو شرح وظایف">
            <IconButton
              aria-label={`آرشیو شرح وظایف ${item.personName}`}
              onClick={onArchive}
              color="warning"
            >
              <ArchiveOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        ) : null}
        {canDelete ? (
          <Tooltip title="حذف شرح وظایف">
            <IconButton
              aria-label={`حذف شرح وظایف ${item.personName}`}
              onClick={onDelete}
              color="error"
            >
              <DeleteOutlineOutlinedIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        ) : null}
      </TableCell>
    </TableRow>
  );
}
