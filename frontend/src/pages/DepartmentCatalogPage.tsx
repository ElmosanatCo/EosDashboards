import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import CheckCircleOutlineOutlinedIcon from "@mui/icons-material/CheckCircleOutlineOutlined";
import DeleteOutlineOutlinedIcon from "@mui/icons-material/DeleteOutlineOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import {
  Box,
  Button,
  Checkbox,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  IconButton,
  InputAdornment,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tab,
  Tabs,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "../app/providers/AuthProvider";
import { ConfirmActionDialog } from "../components/ConfirmActionDialog";
import { MutationErrorAlert } from "../components/MutationErrorAlert";
import {
  jobDescriptionsApi,
  type JobDescriptionCatalog,
} from "../features/jobDescriptions/jobDescriptionsApi";

const pageSize = 25;

type CatalogKind = "skills" | "tasks";
type CatalogStatus = "active" | "inactive" | "all";
type PendingDeactivation = {
  kind: "skill" | "publicSkill" | "task";
  id: number;
  name: string;
};

export function DepartmentCatalogPage({ kind }: { kind: CatalogKind }) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [departmentId, setDepartmentId] = useState<number | "all">(
    user.department.id,
  );
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<CatalogStatus>("active");
  const [page, setPage] = useState(0);
  const [newName, setNewName] = useState("");
  const [newTaskIsProject, setNewTaskIsProject] = useState(false);
  const [skillTab, setSkillTab] = useState<"public" | "department">(
    "department",
  );
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editingName, setEditingName] = useState("");
  const [requiredSkillsTaskId, setRequiredSkillsTaskId] = useState<
    number | null
  >(null);
  const [pendingDeactivation, setPendingDeactivation] =
    useState<PendingDeactivation | null>(null);
  const departments = useQuery({
    queryKey: ["managed-departments"],
    queryFn: jobDescriptionsApi.managedDepartments,
  });
  const catalog = useQuery({
    queryKey: ["job-description-catalog", departmentId, status !== "active"],
    queryFn: () =>
      jobDescriptionsApi.catalog(
        departmentId === "all" ? undefined : departmentId,
        status !== "active",
      ),
  });
  const invalidateCatalog = () =>
    void queryClient.invalidateQueries({
      queryKey: ["job-description-catalog"],
    });
  const createSkill = useMutation({
    mutationFn: () =>
      jobDescriptionsApi.createSkill(departmentId as number, newName),
    onSuccess: () => {
      setNewName("");
      invalidateCatalog();
    },
  });
  const createPublicSkill = useMutation({
    mutationFn: () =>
      jobDescriptionsApi.createPublicSkill(departmentId as number, newName),
    onSuccess: () => {
      setNewName("");
      invalidateCatalog();
    },
  });
  const createTask = useMutation({
    mutationFn: () =>
      jobDescriptionsApi.createTask(
        departmentId as number,
        newName,
        newTaskIsProject,
      ),
    onSuccess: () => {
      setNewName("");
      setNewTaskIsProject(false);
      invalidateCatalog();
    },
  });
  const renameSkill = useMutation({
    mutationFn: () =>
      jobDescriptionsApi.renameDepartmentSkill(editingId!, editingName),
    onSuccess: () => {
      setEditingId(null);
      invalidateCatalog();
    },
  });
  const renameTask = useMutation({
    mutationFn: () =>
      jobDescriptionsApi.renameDepartmentTask(editingId!, editingName),
    onSuccess: () => {
      setEditingId(null);
      invalidateCatalog();
    },
  });
  const deactivateSkill = useMutation({
    mutationFn: (id: number) =>
      jobDescriptionsApi.deactivateDepartmentSkill(id),
    onSuccess: () => {
      setPendingDeactivation(null);
      invalidateCatalog();
    },
  });
  const activateSkill = useMutation({
    mutationFn: (id: number) => jobDescriptionsApi.activateDepartmentSkill(id),
    onSuccess: invalidateCatalog,
  });
  const renamePublicSkill = useMutation({
    mutationFn: () =>
      jobDescriptionsApi.renamePublicSkill(editingId!, editingName),
    onSuccess: () => {
      setEditingId(null);
      invalidateCatalog();
    },
  });
  const deactivatePublicSkill = useMutation({
    mutationFn: (id: number) => jobDescriptionsApi.deactivatePublicSkill(id),
    onSuccess: () => {
      setPendingDeactivation(null);
      invalidateCatalog();
    },
  });
  const activatePublicSkill = useMutation({
    mutationFn: (id: number) => jobDescriptionsApi.activatePublicSkill(id),
    onSuccess: invalidateCatalog,
  });
  const deactivateTask = useMutation({
    mutationFn: (id: number) => jobDescriptionsApi.deactivateDepartmentTask(id),
    onSuccess: () => {
      setPendingDeactivation(null);
      invalidateCatalog();
    },
  });
  const activateTask = useMutation({
    mutationFn: (id: number) => jobDescriptionsApi.activateDepartmentTask(id),
    onSuccess: invalidateCatalog,
  });
  const saveRequiredSkills = useMutation({
    mutationFn: ({
      taskId,
      skillIds,
    }: {
      taskId: number;
      skillIds: number[];
    }) => jobDescriptionsApi.setTaskRequiredSkills(taskId, skillIds),
    onSuccess: () => {
      setRequiredSkillsTaskId(null);
      invalidateCatalog();
    },
  });

  const items = useMemo(() => {
    if (!catalog.data) return [];
    if (kind === "skills") {
      return catalog.data.skills.filter(
        (skill) =>
          (skillTab === "public"
            ? skill.departmentId === null
            : skill.departmentId !== null &&
              (departmentId === "all" ||
                skill.departmentId === departmentId)) &&
          (status === "all" ||
            (status === "active" ? skill.isActive : !skill.isActive)),
      );
    }
    return catalog.data.tasks.filter(
      (task) =>
        (departmentId === "all" || task.departmentId === departmentId) &&
        (status === "all" ||
          (status === "active" ? task.isActive : !task.isActive)),
    );
  }, [catalog.data, departmentId, kind, skillTab, status]);
  const filteredItems = useMemo(() => {
    const normalized = search.trim().toLocaleLowerCase();
    if (!normalized) return items;
    return items.filter((item) => {
      const value = "name" in item ? item.name : item.title;
      return value.toLocaleLowerCase().includes(normalized);
    });
  }, [items, search]);
  const pageCount = Math.max(1, Math.ceil(filteredItems.length / pageSize));
  const currentPage = Math.min(page, pageCount - 1);
  const visibleItems = filteredItems.slice(
    currentPage * pageSize,
    (currentPage + 1) * pageSize,
  );
  const selectedTask =
    kind === "tasks"
      ? catalog.data?.tasks.find((task) => task.id === requiredSkillsTaskId)
      : undefined;

  const title = kind === "skills" ? "کاتالوگ مهارت‌ها" : "کاتالوگ وظایف";
  const searchLabel =
    kind === "skills" ? "جست‌وجو در مهارت‌ها" : "جست‌وجو در وظایف";
  const canCreate = departmentId !== "all" && newName.trim().length > 0;
  const selectedDepartmentValue = departments.data?.some(
    (department) => department.id === departmentId,
  )
    ? departmentId
    : "all";

  return (
    <Stack
      data-testid="department-catalog-page"
      spacing={2.5}
      sx={{ width: "100%", height: "100%", minHeight: 0, overflow: "hidden" }}
    >
      <Box>
        <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
          {title}
        </Typography>
        <Typography color="text.secondary" sx={{ mt: 0.5 }}>
          {kind === "skills"
            ? "مهارت‌های عمومی و اختصاصی را یکسان‌سازی و مدیریت کنید."
            : "وظایف استاندارد بخش را تعریف و مهارت‌های لازم آن‌ها را تعیین کنید."}
        </Typography>
      </Box>
      {kind === "skills" ? (
        <Paper variant="outlined" className="eos-accent-card" sx={{ px: 1 }}>
          <Tabs
            value={skillTab}
            onChange={(_, value: "public" | "department") => {
              setSkillTab(value);
              setPage(0);
              setSearch("");
            }}
            variant="fullWidth"
            aria-label="دامنه کاتالوگ مهارت‌ها"
          >
            <Tab label="عمومی" value="public" />
            <Tab label="اختصاصی" value="department" />
          </Tabs>
        </Paper>
      ) : null}
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{ p: 2, borderTop: 3, borderTopColor: "primary.main" }}
      >
        <Stack spacing={1.5}>
          <Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
            <FormControl sx={{ minWidth: { xs: "100%", md: 250 } }}>
              <InputLabel id={`${kind}-department-label`}>بخش هدف</InputLabel>
              <Select
                labelId={`${kind}-department-label`}
                value={selectedDepartmentValue}
                label="بخش هدف"
                onChange={(event) => {
                  const value = String(event.target.value);
                  setDepartmentId(value === "all" ? "all" : Number(value));
                  setPage(0);
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
            <TextField
              fullWidth
              label={searchLabel}
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(0);
              }}
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchOutlinedIcon fontSize="small" />
                    </InputAdornment>
                  ),
                },
              }}
            />
            <FormControl sx={{ minWidth: { xs: "100%", md: 160 } }}>
              <InputLabel id={`${kind}-status-label`}>وضعیت</InputLabel>
              <Select
                labelId={`${kind}-status-label`}
                value={status}
                label="وضعیت"
                onChange={(event) => {
                  setStatus(event.target.value as CatalogStatus);
                  setPage(0);
                }}
              >
                <MenuItem value="active">فعال</MenuItem>
                <MenuItem value="inactive">غیرفعال</MenuItem>
                <MenuItem value="all">همه</MenuItem>
              </Select>
            </FormControl>
          </Stack>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
            <TextField
              fullWidth
              label={
                kind === "skills"
                  ? skillTab === "public"
                    ? "مهارت عمومی جدید"
                    : "مهارت اختصاصی جدید"
                  : "وظیفه جدید"
              }
              value={newName}
              onChange={(event) => setNewName(event.target.value)}
            />
            {kind === "tasks" ? (
              <FormControlLabel
                control={
                  <Checkbox
                    checked={newTaskIsProject}
                    onChange={(event) =>
                      setNewTaskIsProject(event.target.checked)
                    }
                  />
                }
                label="پروژه"
              />
            ) : null}
            <Button
              variant="contained"
              startIcon={<AddOutlinedIcon />}
              disabled={
                !canCreate ||
                createSkill.isPending ||
                createPublicSkill.isPending ||
                createTask.isPending
              }
              onClick={() =>
                kind === "skills"
                  ? skillTab === "public"
                    ? createPublicSkill.mutate()
                    : createSkill.mutate()
                  : createTask.mutate()
              }
              sx={{ minWidth: 150, flexShrink: 0 }}
            >
              {kind === "skills" && skillTab === "public"
                ? "ثبت مهارت عمومی"
                : "افزودن"}
            </Button>
          </Stack>
          <MutationErrorAlert
            error={
              [
                createSkill,
                createPublicSkill,
                createTask,
                renameSkill,
                renamePublicSkill,
                renameTask,
                deactivateSkill,
                deactivatePublicSkill,
                deactivateTask,
                activateSkill,
                activatePublicSkill,
                activateTask,
                saveRequiredSkills,
              ].find((mutation) => mutation.isError)?.error
            }
          />
        </Stack>
      </Paper>
      {catalog.isPending ? (
        <Box sx={{ minHeight: 240, display: "grid", placeItems: "center" }}>
          <CircularProgress aria-label={`در حال دریافت ${title}`} />
        </Box>
      ) : catalog.isError ? (
        <Typography color="error">دریافت {title} ممکن نشد.</Typography>
      ) : (
        <Paper
          variant="outlined"
          className="eos-accent-card"
          sx={{
            flex: 1,
            minHeight: 0,
            display: "flex",
            flexDirection: "column",
            borderTop: 3,
            borderTopColor: "primary.main",
            overflow: "hidden",
          }}
        >
          <TableContainer
            data-testid="department-catalog-list"
            sx={{ flex: 1, minHeight: 0, maxHeight: "none" }}
          >
            <Table
              stickyHeader
              aria-label={kind === "skills" ? "فهرست مهارت‌ها" : "فهرست وظایف"}
            >
              <TableHead>
                <TableRow>
                  <TableCell>
                    {kind === "skills" ? "نام مهارت" : "عنوان وظیفه"}
                  </TableCell>
                  {kind === "skills" ? (
                    <TableCell>دامنه</TableCell>
                  ) : (
                    <>
                      <TableCell>نوع</TableCell>
                      <TableCell>مهارت‌های لازم</TableCell>
                    </>
                  )}
                  <TableCell align="left">عملیات</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {visibleItems.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={kind === "skills" ? 3 : 4}>
                      <Typography color="text.secondary">
                        موردی برای نمایش وجود ندارد.
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  visibleItems.map((item) =>
                    kind === "skills" ? (
                      <SkillRow
                        key={item.id}
                        skill={item as JobDescriptionCatalog["skills"][number]}
                        editing={editingId === item.id}
                        editingName={editingName}
                        onStartEdit={() => {
                          setEditingId(item.id);
                          setEditingName(
                            (item as JobDescriptionCatalog["skills"][number])
                              .name,
                          );
                        }}
                        onNameChange={setEditingName}
                        onSave={() =>
                          (item as JobDescriptionCatalog["skills"][number])
                            .departmentId === null
                            ? renamePublicSkill.mutate()
                            : renameSkill.mutate()
                        }
                        onCancel={() => setEditingId(null)}
                        onDeactivate={() => {
                          const skill =
                            item as JobDescriptionCatalog["skills"][number];
                          setPendingDeactivation({
                            kind:
                              skill.departmentId === null
                                ? "publicSkill"
                                : "skill",
                            id: skill.id,
                            name: skill.name,
                          });
                        }}
                        onActivate={() =>
                          (item as JobDescriptionCatalog["skills"][number])
                            .departmentId === null
                            ? activatePublicSkill.mutate(item.id)
                            : activateSkill.mutate(item.id)
                        }
                        departmentName={
                          departments.data?.find(
                            (department) =>
                              department.id ===
                              (item as JobDescriptionCatalog["skills"][number])
                                .departmentId,
                          )?.name ?? "بخش نامشخص"
                        }
                        saving={
                          renameSkill.isPending || renamePublicSkill.isPending
                        }
                        deactivating={
                          deactivateSkill.isPending ||
                          deactivatePublicSkill.isPending
                        }
                        activating={
                          activateSkill.isPending ||
                          activatePublicSkill.isPending
                        }
                      />
                    ) : (
                      <TaskRow
                        key={item.id}
                        task={item as JobDescriptionCatalog["tasks"][number]}
                        editing={editingId === item.id}
                        editingName={editingName}
                        onStartEdit={() => {
                          setEditingId(item.id);
                          setEditingName(
                            (item as JobDescriptionCatalog["tasks"][number])
                              .title,
                          );
                        }}
                        onNameChange={setEditingName}
                        onSave={() => renameTask.mutate()}
                        onCancel={() => setEditingId(null)}
                        onDeactivate={() =>
                          setPendingDeactivation({
                            kind: "task",
                            id: item.id,
                            name: (
                              item as JobDescriptionCatalog["tasks"][number]
                            ).title,
                          })
                        }
                        onActivate={() => activateTask.mutate(item.id)}
                        onRequiredSkills={() =>
                          setRequiredSkillsTaskId(item.id)
                        }
                        saving={renameTask.isPending}
                        deactivating={deactivateTask.isPending}
                        activating={activateTask.isPending}
                      />
                    ),
                  )
                )}
              </TableBody>
            </Table>
          </TableContainer>
          <Stack
            direction="row"
            spacing={1.5}
            sx={{
              p: 1.5,
              alignItems: "center",
              justifyContent: "space-between",
            }}
          >
            <Typography variant="body2" color="text.secondary">
              {filteredItems.length.toLocaleString("fa-IR")} مورد · صفحه{" "}
              {(currentPage + 1).toLocaleString("fa-IR")} از{" "}
              {pageCount.toLocaleString("fa-IR")}
            </Typography>
            <Stack direction="row" spacing={1}>
              <Button
                size="small"
                disabled={currentPage === 0}
                onClick={() => setPage((value) => Math.max(0, value - 1))}
              >
                قبلی
              </Button>
              <Button
                size="small"
                disabled={currentPage >= pageCount - 1}
                onClick={() =>
                  setPage((value) => Math.min(pageCount - 1, value + 1))
                }
              >
                بعدی
              </Button>
            </Stack>
          </Stack>
        </Paper>
      )}
      {selectedTask ? (
        <RequiredSkillsDialog
          open
          task={selectedTask}
          skills={catalog.data?.skills ?? []}
          onClose={() => setRequiredSkillsTaskId(null)}
          onSave={(skillIds) =>
            saveRequiredSkills.mutate({ taskId: selectedTask.id, skillIds })
          }
          saving={saveRequiredSkills.isPending}
        />
      ) : null}
      <ConfirmActionDialog
        open={pendingDeactivation !== null}
        title="تأیید غیرفعال‌سازی"
        message={
          pendingDeactivation
            ? `آیا از غیرفعال‌سازی «${pendingDeactivation.name}» مطمئن هستید؟`
            : ""
        }
        confirmLabel="تأیید غیرفعال‌سازی"
        pending={
          deactivateSkill.isPending ||
          deactivatePublicSkill.isPending ||
          deactivateTask.isPending
        }
        onClose={() => setPendingDeactivation(null)}
        onConfirm={() => {
          if (!pendingDeactivation) return;
          if (pendingDeactivation.kind === "publicSkill") {
            deactivatePublicSkill.mutate(pendingDeactivation.id);
          } else if (pendingDeactivation.kind === "skill") {
            deactivateSkill.mutate(pendingDeactivation.id);
          } else {
            deactivateTask.mutate(pendingDeactivation.id);
          }
        }}
      />
    </Stack>
  );
}

function SkillRow({
  skill,
  editing,
  editingName,
  onStartEdit,
  onNameChange,
  onSave,
  onCancel,
  onDeactivate,
  onActivate,
  departmentName,
  saving,
  deactivating,
  activating,
}: {
  skill: JobDescriptionCatalog["skills"][number];
  editing: boolean;
  editingName: string;
  onStartEdit: () => void;
  onNameChange: (name: string) => void;
  onSave: () => void;
  onCancel: () => void;
  onDeactivate: () => void;
  onActivate: () => void;
  departmentName: string;
  saving: boolean;
  deactivating: boolean;
  activating: boolean;
}) {
  const departmentSkill = skill.departmentId !== null;
  const canEdit = skill.isActive && skill.canEdit;
  const canDelete = skill.isActive && skill.canDelete;
  return (
    <TableRow hover>
      <TableCell>
        {editing ? (
          <TextField
            size="small"
            value={editingName}
            onChange={(event) => onNameChange(event.target.value)}
            autoFocus
          />
        ) : (
          skill.name
        )}
      </TableCell>
      <TableCell>
        {departmentSkill ? `اختصاصی - ${departmentName}` : "عمومی"}
      </TableCell>
      <TableCell align="left">
        {!skill.isActive ? (
          <Tooltip title="فعال‌سازی مهارت">
            <span>
              <IconButton
                aria-label={`فعال‌سازی مهارت ${skill.name}`}
                size="small"
                color="success"
                disabled={!skill.canEdit || activating}
                onClick={onActivate}
              >
                <CheckCircleOutlineOutlinedIcon fontSize="small" />
              </IconButton>
            </span>
          </Tooltip>
        ) : canEdit || canDelete ? (
          editing ? (
            <Stack
              direction="row"
              spacing={0.5}
              sx={{ justifyContent: "flex-start" }}
            >
              <Button
                size="small"
                disabled={!editingName.trim() || saving}
                onClick={onSave}
              >
                ذخیره
              </Button>
              <Button size="small" onClick={onCancel}>
                انصراف
              </Button>
            </Stack>
          ) : (
            <Stack
              direction="row"
              spacing={0.25}
              sx={{ justifyContent: "flex-start" }}
            >
              <Tooltip title="ویرایش مهارت">
                <span>
                  <IconButton
                    aria-label={`ویرایش مهارت ${skill.name}`}
                    size="small"
                    disabled={!canEdit}
                    onClick={onStartEdit}
                  >
                    <EditOutlinedIcon fontSize="small" />
                  </IconButton>
                </span>
              </Tooltip>
              <Tooltip title="غیرفعال‌سازی مهارت">
                <span>
                  <IconButton
                    aria-label={`غیرفعال‌سازی مهارت ${skill.name}`}
                    size="small"
                    color="error"
                    disabled={!canDelete || deactivating}
                    onClick={onDeactivate}
                  >
                    <DeleteOutlineOutlinedIcon fontSize="small" />
                  </IconButton>
                </span>
              </Tooltip>
            </Stack>
          )
        ) : (
          <Typography variant="body2" color="text.secondary">
            {skill.usageDepartmentCount > 1
              ? "پس از استفاده در چند بخش، مدیریت با منابع انسانی است."
              : "فقط خواندنی"}
          </Typography>
        )}
      </TableCell>
    </TableRow>
  );
}

function TaskRow({
  task,
  editing,
  editingName,
  onStartEdit,
  onNameChange,
  onSave,
  onCancel,
  onDeactivate,
  onActivate,
  onRequiredSkills,
  saving,
  deactivating,
  activating,
}: {
  task: JobDescriptionCatalog["tasks"][number];
  editing: boolean;
  editingName: string;
  onStartEdit: () => void;
  onNameChange: (name: string) => void;
  onSave: () => void;
  onCancel: () => void;
  onDeactivate: () => void;
  onActivate: () => void;
  onRequiredSkills: () => void;
  saving: boolean;
  deactivating: boolean;
  activating: boolean;
}) {
  return (
    <TableRow hover>
      <TableCell>
        {editing ? (
          <TextField
            size="small"
            value={editingName}
            onChange={(event) => onNameChange(event.target.value)}
            autoFocus
          />
        ) : (
          task.title
        )}
      </TableCell>
      <TableCell>{task.isProject ? "پروژه" : "وظیفه"}</TableCell>
      <TableCell>
        <Button
          size="small"
          startIcon={<SettingsOutlinedIcon />}
          onClick={onRequiredSkills}
        >
          {task.requiredSkillIds.length.toLocaleString("fa-IR")} مهارت
        </Button>
      </TableCell>
      <TableCell align="left">
        {editing ? (
          <Stack direction="row" spacing={0.5}>
            <Button
              size="small"
              disabled={!editingName.trim() || saving}
              onClick={onSave}
            >
              ذخیره
            </Button>
            <Button size="small" onClick={onCancel}>
              انصراف
            </Button>
          </Stack>
        ) : (
          <Stack direction="row" spacing={0.25}>
            {task.isActive ? (
              <Tooltip title="ویرایش وظیفه">
                <IconButton
                  aria-label={`ویرایش وظیفه ${task.title}`}
                  size="small"
                  onClick={onStartEdit}
                >
                  <EditOutlinedIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            ) : null}
            {task.isActive ? (
              <Tooltip title="غیرفعال‌سازی وظیفه">
                <IconButton
                  aria-label={`غیرفعال‌سازی وظیفه ${task.title}`}
                  size="small"
                  color="error"
                  disabled={deactivating}
                  onClick={onDeactivate}
                >
                  <DeleteOutlineOutlinedIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            ) : (
              <Tooltip title="فعال‌سازی وظیفه">
                <span>
                  <IconButton
                    aria-label={`فعال‌سازی وظیفه ${task.title}`}
                    size="small"
                    color="success"
                    disabled={activating}
                    onClick={onActivate}
                  >
                    <CheckCircleOutlineOutlinedIcon fontSize="small" />
                  </IconButton>
                </span>
              </Tooltip>
            )}
          </Stack>
        )}
      </TableCell>
    </TableRow>
  );
}

function RequiredSkillsDialog({
  open,
  task,
  skills,
  onClose,
  onSave,
  saving,
}: {
  open: boolean;
  task: JobDescriptionCatalog["tasks"][number];
  skills: JobDescriptionCatalog["skills"];
  onClose: () => void;
  onSave: (skillIds: number[]) => void;
  saving: boolean;
}) {
  const [selected, setSelected] = useState(task.requiredSkillIds);
  const [search, setSearch] = useState("");
  const visibleSkills = skills.filter((skill) =>
    skill.name.toLocaleLowerCase().includes(search.trim().toLocaleLowerCase()),
  );
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>مهارت‌های لازم برای «{task.title}»</DialogTitle>
      <DialogContent>
        <Stack spacing={1.5} sx={{ pt: 1 }}>
          <TextField
            label="جست‌وجوی مهارت"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            size="small"
          />
          <Box sx={{ maxHeight: 360, overflowY: "auto" }}>
            <Stack>
              {visibleSkills.map((skill) => (
                <FormControlLabel
                  key={skill.id}
                  control={
                    <Checkbox
                      checked={selected.includes(skill.id)}
                      onChange={(event) =>
                        setSelected((current) =>
                          event.target.checked
                            ? [...current, skill.id]
                            : current.filter((id) => id !== skill.id),
                        )
                      }
                    />
                  }
                  label={skill.name}
                />
              ))}
            </Stack>
          </Box>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>انصراف</Button>
        <Button
          variant="contained"
          disabled={saving}
          onClick={() => onSave(selected)}
        >
          ذخیره مهارت‌ها
        </Button>
      </DialogActions>
    </Dialog>
  );
}
