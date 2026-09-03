import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import LockResetOutlinedIcon from "@mui/icons-material/LockResetOutlined";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  IconButton,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from "@mui/material";
import {
  administrationApi,
  type ManagedUser,
} from "../features/administration/administrationApi";
import { problemMessage } from "../features/administration/administrationUi";
import { UserFormPage } from "./UserFormPage";

export function UserManagementPage() {
  const users = useQuery({
    queryKey: ["administration", "users", 1],
    queryFn: () => administrationApi.users(),
  });
  const queryClient = useQueryClient();
  const [selectedUserId, setSelectedUserId] = useState<number | null>();
  const changeStatus = useMutation({
    mutationFn: (user: ManagedUser) =>
      administrationApi.setUserActive(user.id, !user.isActive, user.rowVersion),
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["administration", "users"],
      }),
  });
  const openForm = (id?: number) => setSelectedUserId(id ?? null);
  return (
    <Stack spacing={2.5} sx={{ width: "100%" }}>
      <Stack
        direction={{ xs: "column", sm: "row" }}
        spacing={1}
        sx={{ justifyContent: "space-between", alignItems: { sm: "center" } }}
      >
        <Box>
          <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
            مدیریت کاربران
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 0.5 }}>
            حساب، نقش، واحد و وضعیت دسترسی کاربران را مدیریت کنید.
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<AddOutlinedIcon />}
          onClick={() => openForm()}
        >
          تعریف کاربر
        </Button>
      </Stack>
      <Paper
        variant="outlined"
        className="eos-accent-card"
        sx={{ borderTop: 3, borderTopColor: "primary.main", overflowX: "auto" }}
      >
        {users.isPending ? (
          <Box sx={{ minHeight: 220, display: "grid", placeItems: "center" }}>
            <CircularProgress aria-label="در حال دریافت کاربران" />
          </Box>
        ) : (
          <Table aria-label="فهرست کاربران">
            <TableHead>
              <TableRow>
                <TableCell>نام و نام خانوادگی</TableCell>
                <TableCell>کد پرسنلی</TableCell>
                <TableCell>نام کاربری</TableCell>
                <TableCell>واحد</TableCell>
                <TableCell>شماره همراه</TableCell>
                <TableCell>وضعیت</TableCell>
                <TableCell align="left">عملیات</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {users.data?.items.map((user) => (
                <TableRow key={user.id} hover>
                  <TableCell sx={{ fontWeight: 600 }}>
                    {user.firstName} {user.lastName}
                  </TableCell>
                  <TableCell>{user.personnelCode}</TableCell>
                  <TableCell dir="ltr">
                    {user.username ?? user.personnelCode}
                  </TableCell>
                  <TableCell>{user.departmentName ?? "—"}</TableCell>
                  <TableCell dir="ltr">{user.maskedMobile}</TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      color={user.isActive ? "success" : "default"}
                      label={user.isActive ? "فعال" : "غیرفعال"}
                    />
                  </TableCell>
                  <TableCell align="left">
                    <Tooltip title="ویرایش">
                      <IconButton
                        aria-label={`ویرایش ${user.firstName} ${user.lastName}`}
                        onClick={() => openForm(user.id)}
                      >
                        <EditOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="بازنشانی رمز">
                      <IconButton
                        aria-label={`بازنشانی رمز ${user.firstName} ${user.lastName}`}
                        onClick={() => openForm(user.id)}
                      >
                        <LockResetOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Switch
                      checked={user.isActive}
                      disabled={changeStatus.isPending}
                      slotProps={{
                        input: {
                          "aria-label": `${user.isActive ? "غیرفعال کردن" : "فعال کردن"} ${user.firstName} ${user.lastName}`,
                        },
                      }}
                      onChange={() => changeStatus.mutate(user)}
                    />
                  </TableCell>
                </TableRow>
              ))}
              {!users.isError && users.data?.items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} align="center">
                    کاربری برای نمایش وجود ندارد.
                  </TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        )}
      </Paper>
      {users.isError ? (
        <Typography color="error">دریافت کاربران ممکن نشد.</Typography>
      ) : null}
      {changeStatus.isError ? (
        <Typography color="error">
          {problemMessage(changeStatus.error)}
        </Typography>
      ) : null}
      <Dialog
        open={selectedUserId !== undefined}
        onClose={() => setSelectedUserId(undefined)}
        fullWidth
        maxWidth="md"
      >
        {selectedUserId !== undefined ? (
          <UserFormPage
            userId={selectedUserId ?? undefined}
            onClose={() => setSelectedUserId(undefined)}
            onSaved={() => setSelectedUserId(undefined)}
          />
        ) : null}
      </Dialog>
    </Stack>
  );
}
