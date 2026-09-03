import AccountTreeOutlinedIcon from "@mui/icons-material/AccountTreeOutlined";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutlineOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box,
  Button,
  CircularProgress,
  IconButton,
  Paper,
  Stack,
  Tooltip,
  Typography,
} from "@mui/material";
import {
  administrationApi,
  type ManagedDepartment,
} from "../features/administration/administrationApi";
import { problemMessage } from "../features/administration/administrationUi";
import { createTabKey } from "../navigation/routeRegistry";
import { useTabWorkspace } from "../navigation/TabWorkspaceProvider";

export function DepartmentManagementPage() {
  const departments = useQuery({
    queryKey: ["administration", "departments"],
    queryFn: administrationApi.departments,
  });
  const queryClient = useQueryClient();
  const { dispatch } = useTabWorkspace();
  const remove = useMutation({
    mutationFn: (item: ManagedDepartment) =>
      administrationApi.deleteDepartment(item.id, item.rowVersion),
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["administration", "departments"],
      }),
  });
  const openForm = (id?: number) =>
    dispatch({
      type: "open",
      tab: {
        key: createTabKey("department-form", { id: id?.toString() ?? "new" }),
        routeId: "department-form",
        pathname: "/departments/form",
        search: "",
        title: id ? "ویرایش واحد" : "تعریف واحد",
        closable: true,
        state: { id: id ?? null },
      },
    });
  const roots =
    departments.data?.filter((item) => item.parentDepartmentId === null) ?? [];
  const childrenOf = (id: number) =>
    departments.data?.filter((item) => item.parentDepartmentId === id) ?? [];
  return (
    <Stack spacing={2.5} sx={{ maxWidth: 1060, mx: "auto" }}>
      <Stack
        direction={{ xs: "column", sm: "row" }}
        spacing={1}
        sx={{ justifyContent: "space-between", alignItems: { sm: "center" } }}
      >
        <Box>
          <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
            مدیریت واحدها
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 0.5 }}>
            ساختار سازمانی حداکثر در دو سطح تعریف می‌شود.
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<AddOutlinedIcon />}
          onClick={() => openForm()}
        >
          تعریف واحد
        </Button>
      </Stack>
      <Paper
        variant="outlined"
        sx={{ borderTop: 3, borderTopColor: "primary.main" }}
      >
        {departments.isPending ? (
          <Box sx={{ minHeight: 220, display: "grid", placeItems: "center" }}>
            <CircularProgress aria-label="در حال دریافت واحدها" />
          </Box>
        ) : (
          <Box component="ul" sx={{ listStyle: "none", m: 0, p: 0 }}>
            {roots.map((root) => (
              <DepartmentNode
                key={root.id}
                item={root}
                depth={0}
                children={childrenOf(root.id)}
                onEdit={openForm}
                onDelete={(item) => remove.mutate(item)}
              />
            ))}
            {!departments.isError && roots.length === 0 ? (
              <Typography color="text.secondary" sx={{ p: 3 }}>
                واحدی برای نمایش وجود ندارد.
              </Typography>
            ) : null}
          </Box>
        )}
      </Paper>
      {departments.isError ? (
        <Typography color="error">دریافت واحدها ممکن نشد.</Typography>
      ) : null}
      {remove.isError ? (
        <Typography color="error">{problemMessage(remove.error)}</Typography>
      ) : null}
    </Stack>
  );
}

function DepartmentNode({
  item,
  depth,
  children,
  onEdit,
  onDelete,
}: {
  item: ManagedDepartment;
  depth: number;
  children: ManagedDepartment[];
  onEdit: (id: number) => void;
  onDelete: (item: ManagedDepartment) => void;
}) {
  return (
    <Box component="li">
      <Stack
        direction="row"
        spacing={1}
        sx={{
          alignItems: "center",
          pr: depth * 4,
          pl: 1.5,
          py: 1.25,
          borderBottom: 1,
          borderColor: "divider",
        }}
      >
        <AccountTreeOutlinedIcon color={depth === 0 ? "primary" : "action"} />
        <Typography sx={{ flex: 1, fontWeight: depth === 0 ? 650 : 400 }}>
          {item.name}
        </Typography>
        {depth === 0 ? (
          <Typography variant="caption" color="text.secondary">
            واحد مستقل
          </Typography>
        ) : (
          <Typography variant="caption" color="text.secondary">
            زیرواحد
          </Typography>
        )}
        <Tooltip title="ویرایش">
          <IconButton
            aria-label={`ویرایش ${item.name}`}
            onClick={() => onEdit(item.id)}
          >
            <EditOutlinedIcon fontSize="small" />
          </IconButton>
        </Tooltip>
        <Tooltip title="حذف">
          <IconButton
            color="error"
            aria-label={`حذف ${item.name}`}
            onClick={() => onDelete(item)}
          >
            <DeleteOutlineIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Stack>
      {children.map((child) => (
        <DepartmentNode
          key={child.id}
          item={child}
          depth={1}
          children={[]}
          onEdit={onEdit}
          onDelete={onDelete}
        />
      ))}
    </Box>
  );
}
