import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box,
  Button,
  Checkbox,
  CircularProgress,
  FormControl,
  FormControlLabel,
  FormGroup,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import {
  administrationApi,
  type ManagedUser,
  type UserInput,
} from "../features/administration/administrationApi";
import { problemMessage } from "../features/administration/administrationUi";
import { useTabWorkspace } from "../navigation/TabWorkspaceProvider";

const blank = {
  personnelCode: "",
  firstName: "",
  lastName: "",
  username: "",
  departmentId: "",
  roleIds: [] as number[],
  mobile: "",
  temporaryPassword: "",
};
export function UserFormPage({
  userId,
  onClose,
  onSaved,
}: {
  userId?: number;
  onClose?: () => void;
  onSaved?: () => void;
}) {
  const details = useQuery({
    queryKey: ["administration", "user", userId],
    queryFn: () => administrationApi.user(userId!),
    enabled: Boolean(userId),
  });
  const roles = useQuery({
    queryKey: ["administration", "roles"],
    queryFn: administrationApi.roles,
  });
  const departments = useQuery({
    queryKey: ["administration", "departments"],
    queryFn: administrationApi.departments,
  });
  const queryClient = useQueryClient();
  const { activeKey, dispatch } = useTabWorkspace();
  const [form, setForm] = useState(blank);
  const [loadedId, setLoadedId] = useState<number>();
  useEffect(() => {
    const user = details.data;
    if (user && loadedId !== user.id) {
      setLoadedId(user.id);
      setForm(toForm(user));
    }
  }, [details.data, loadedId]);
  const save = useMutation({
    mutationFn: async () => {
      const common: UserInput = {
        personnelCode: form.personnelCode.trim(),
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        username: form.username.trim() || undefined,
        departmentId: Number(form.departmentId),
        roleIds: form.roleIds,
      };
      if (userId && details.data)
        return administrationApi.updateUser(userId, {
          ...common,
          username: common.username ?? "",
          replacementMobile: form.mobile.trim() || null,
          rowVersion: details.data.rowVersion,
        });
      return administrationApi.createUser({
        ...common,
        mobile: form.mobile.trim(),
        temporaryPassword: form.temporaryPassword,
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["administration", "users"],
      });
      void queryClient.invalidateQueries({
        queryKey: ["administration", "dashboard"],
      });
      if (onSaved) onSaved();
      else dispatch({ type: "markDirty", key: activeKey, dirty: false });
    },
  });
  const resetPassword = useMutation({
    mutationFn: () =>
      administrationApi.resetPassword(
        userId!,
        form.temporaryPassword,
        details.data!.rowVersion,
      ),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: ["administration", "users"],
      });
      void queryClient.invalidateQueries({
        queryKey: ["administration", "user", userId],
      });
      setForm((previous) => ({ ...previous, temporaryPassword: "" }));
    },
  });
  // The details query is intentionally disabled while creating a user. React
  // Query still reports a disabled query as pending, so it must not keep the
  // create form behind an endless spinner.
  const busy =
    (Boolean(userId) && details.isPending) ||
    roles.isPending ||
    departments.isPending;
  if (busy)
    return (
      <Box sx={{ minHeight: 220, display: "grid", placeItems: "center" }}>
        <CircularProgress aria-label="در حال دریافت فرم کاربر" />
      </Box>
    );
  if (details.isError || roles.isError || departments.isError)
    return <Typography color="error">دریافت اطلاعات فرم ممکن نشد.</Typography>;
  const valid = Boolean(
    form.personnelCode &&
    form.firstName &&
    form.lastName &&
    form.departmentId &&
    form.roleIds.length &&
    (userId || (form.mobile && form.temporaryPassword.length >= 8)),
  );
  const update = <K extends keyof typeof form>(
    key: K,
    nextValue: (typeof form)[K],
  ) => {
    setForm((previous) => ({ ...previous, [key]: nextValue }));
    dispatch({ type: "markDirty", key: activeKey, dirty: true });
  };
  return (
    <Paper
      component="form"
      variant="outlined"
      className="eos-accent-card"
      sx={{
        width: "100%",
        maxWidth: 900,
        mx: "auto",
        p: { xs: 2, md: 3 },
        borderTop: 3,
        borderTopColor: "primary.main",
      }}
      onSubmit={(event) => {
        event.preventDefault();
        if (valid) save.mutate();
      }}
    >
      <Stack spacing={2.5}>
        <Box>
          <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
            {userId ? "ویرایش کاربر" : "تعریف کاربر"}
          </Typography>
          <Typography
            className="eos-persian-number"
            color="text.secondary"
            sx={{ mt: 0.5 }}
          >
            {userId
              ? `شماره همراه فعلی: ${details.data?.maskedMobile ?? "—"}`
              : "اگر نام کاربری وارد نشود، کد پرسنلی برای آن استفاده می‌شود."}
          </Typography>
        </Box>
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", sm: "repeat(2, 1fr)" },
            gap: 2,
          }}
        >
          <TextField
            required
            className="eos-persian-number"
            label="کد پرسنلی"
            value={form.personnelCode}
            onChange={(e) => update("personnelCode", e.target.value)}
          />
          <TextField
            required
            label="نام"
            value={form.firstName}
            onChange={(e) => update("firstName", e.target.value)}
          />
          <TextField
            required
            label="نام خانوادگی"
            value={form.lastName}
            onChange={(e) => update("lastName", e.target.value)}
          />
          <TextField
            className="eos-persian-number"
            label="نام کاربری"
            helperText={
              userId
                ? "مستقل از کد پرسنلی قابل تغییر است."
                : "در صورت خالی‌بودن، کد پرسنلی ثبت می‌شود."
            }
            value={form.username}
            onChange={(e) => update("username", e.target.value)}
            slotProps={{ htmlInput: { dir: "ltr" } }}
            sx={{ "& input": { textAlign: "left" } }}
          />
          <FormControl required>
            <InputLabel id="department-label">واحد</InputLabel>
            <Select
              labelId="department-label"
              label="واحد"
              value={form.departmentId}
              onChange={(e) => update("departmentId", e.target.value)}
            >
              {departments.data?.map((item) => (
                <MenuItem key={item.id} value={String(item.id)}>
                  {item.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          {!userId ? (
            <>
              <TextField
                required
                className="eos-persian-number"
                label="شماره همراه"
                value={form.mobile}
                onChange={(e) => update("mobile", e.target.value)}
                slotProps={{ htmlInput: { dir: "ltr" } }}
                sx={{ "& input": { textAlign: "left" } }}
              />
              <TextField
                required
                className="eos-persian-number"
                type="password"
                label="رمز موقت"
                helperText="حداقل ۸ کاراکتر"
                value={form.temporaryPassword}
                onChange={(e) => update("temporaryPassword", e.target.value)}
                slotProps={{ htmlInput: { dir: "ltr" } }}
                sx={{ "& input": { textAlign: "left" } }}
              />
            </>
          ) : (
            <TextField
              className="eos-persian-number"
              label="شماره همراه جدید"
              helperText="فقط در صورت نیاز وارد کنید؛ مقدار قبلی نمایش داده نمی‌شود."
              value={form.mobile}
              onChange={(e) => update("mobile", e.target.value)}
              slotProps={{ htmlInput: { dir: "ltr" } }}
              sx={{ "& input": { textAlign: "left" } }}
            />
          )}
        </Box>
        <FormControl required>
          <Typography component="h2" variant="subtitle2">
            نقش‌ها
          </Typography>
          <FormGroup row>
            {roles.data?.map((role) => (
              <FormControlLabel
                key={role.id}
                control={
                  <Checkbox
                    checked={form.roleIds.includes(role.id)}
                    onChange={(event) =>
                      update(
                        "roleIds",
                        event.target.checked
                          ? [...form.roleIds, role.id]
                          : form.roleIds.filter((id) => id !== role.id),
                      )
                    }
                  />
                }
                label={role.displayName}
              />
            ))}
          </FormGroup>
        </FormControl>
        {save.isError ? (
          <Typography color="error">{problemMessage(save.error)}</Typography>
        ) : null}
        {save.isSuccess ? (
          <Typography color="success.main">اطلاعات کاربر ذخیره شد.</Typography>
        ) : null}
        <Stack
          direction="row"
          spacing={1}
          sx={{ justifyContent: "flex-start" }}
        >
          <Button
            type="submit"
            variant="contained"
            disabled={!valid || save.isPending}
          >
            {save.isPending ? "در حال ثبت…" : "ذخیره"}
          </Button>
          <Button
            type="button"
            disabled={save.isPending}
            onClick={() =>
              onClose?.() ??
              dispatch({ type: "close", key: activeKey, confirmed: true })
            }
          >
            بستن
          </Button>
        </Stack>
        {userId ? (
          <Box sx={{ pt: 2, borderTop: 1, borderColor: "divider" }}>
            <Typography
              component="h2"
              variant="subtitle1"
              sx={{ fontWeight: 700 }}
            >
              بازنشانی رمز عبور
            </Typography>
            <Stack
              direction={{ xs: "column", sm: "row" }}
              spacing={1}
              sx={{ mt: 1 }}
            >
              <TextField
                size="small"
                className="eos-persian-number"
                type="password"
                label="رمز موقت جدید"
                value={form.temporaryPassword}
                onChange={(e) => update("temporaryPassword", e.target.value)}
                slotProps={{ htmlInput: { dir: "ltr" } }}
                sx={{ "& input": { textAlign: "left" } }}
              />
              <Button
                color="warning"
                variant="outlined"
                disabled={
                  form.temporaryPassword.length < 8 || resetPassword.isPending
                }
                onClick={() => resetPassword.mutate()}
              >
                {resetPassword.isPending ? "در حال بازنشانی…" : "ثبت رمز موقت"}
              </Button>
            </Stack>
            {resetPassword.isError ? (
              <Typography color="error" variant="body2" sx={{ mt: 1 }}>
                {problemMessage(resetPassword.error)}
              </Typography>
            ) : null}
            {resetPassword.isSuccess ? (
              <Typography color="success.main" variant="body2" sx={{ mt: 1 }}>
                رمز موقت ثبت شد؛ کاربر در ورود بعدی باید آن را تغییر دهد.
              </Typography>
            ) : null}
          </Box>
        ) : null}
      </Stack>
    </Paper>
  );
}
function toForm(user: ManagedUser) {
  return {
    personnelCode: user.personnelCode,
    firstName: user.firstName,
    lastName: user.lastName,
    username: user.username ?? "",
    departmentId: String(user.departmentId),
    roleIds: user.roleIds,
    mobile: "",
    temporaryPassword: "",
  };
}
