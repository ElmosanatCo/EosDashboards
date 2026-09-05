import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box,
  Button,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import { administrationApi } from "../features/administration/administrationApi";
import { problemMessage } from "../features/administration/administrationUi";
import { useTabWorkspace } from "../navigation/TabWorkspaceProvider";

export function DepartmentFormPage({
  departmentId,
  onClose,
  onSaved,
}: {
  departmentId?: number;
  onClose?: () => void;
  onSaved?: () => void;
}) {
  const departments = useQuery({
    queryKey: ["administration", "departments"],
    queryFn: administrationApi.departments,
  });
  const current = departments.data?.find((item) => item.id === departmentId);
  const [name, setName] = useState("");
  const [parent, setParent] = useState("");
  const [loaded, setLoaded] = useState<number>();
  const { activeKey, dispatch } = useTabWorkspace();
  const client = useQueryClient();
  useEffect(() => {
    if (current && loaded !== current.id) {
      setLoaded(current.id);
      setName(current.name);
      setParent(current.parentDepartmentId?.toString() ?? "");
    }
  }, [current, loaded]);
  const roots =
    departments.data?.filter(
      (item) => item.parentDepartmentId === null && item.id !== departmentId,
    ) ?? [];
  const save = useMutation({
    mutationFn: () =>
      departmentId && current
        ? administrationApi.updateDepartment(
            departmentId,
            name.trim(),
            parent ? Number(parent) : null,
            current.rowVersion,
          )
        : administrationApi.createDepartment(
            name.trim(),
            parent ? Number(parent) : null,
          ),
    onSuccess: () => {
      void client.invalidateQueries({
        queryKey: ["administration", "departments"],
      });
      if (onSaved) onSaved();
      else dispatch({ type: "markDirty", key: activeKey, dirty: false });
    },
  });
  if (departments.isPending)
    return (
      <Box sx={{ minHeight: 220, display: "grid", placeItems: "center" }}>
        <CircularProgress aria-label="در حال دریافت فرم واحد" />
      </Box>
    );
  if (departments.isError || (departmentId && !current))
    return <Typography color="error">واحد موردنظر پیدا نشد.</Typography>;
  return (
    <Paper
      component="form"
      variant="outlined"
      className="eos-accent-card"
      sx={{
        width: "100%",
        maxWidth: 700,
        mx: "auto",
        p: { xs: 2, md: 3 },
        borderTop: 3,
        borderTopColor: "primary.main",
      }}
      onSubmit={(e) => {
        e.preventDefault();
        if (name.trim()) save.mutate();
      }}
    >
      <Stack spacing={2.5}>
        <Box>
          <Typography component="h1" variant="h5" sx={{ fontWeight: 750 }}>
            {departmentId ? "ویرایش واحد" : "تعریف واحد"}
          </Typography>
        </Box>
        <TextField
          autoFocus
          required
          label="نام واحد"
          value={name}
          onChange={(e) => {
            setName(e.target.value);
            dispatch({ type: "markDirty", key: activeKey, dirty: true });
          }}
        />
        <FormControl>
          <InputLabel id="parent-label">واحد والد</InputLabel>
          <Select
            labelId="parent-label"
            label="واحد والد"
            value={parent}
            onChange={(e) => {
              setParent(e.target.value);
              dispatch({ type: "markDirty", key: activeKey, dirty: true });
            }}
          >
            <MenuItem value="">بدون واحد والد (مستقل)</MenuItem>
            {roots.map((root) => (
              <MenuItem key={root.id} value={String(root.id)}>
                {root.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
        {save.isError ? (
          <Typography color="error">{problemMessage(save.error)}</Typography>
        ) : null}
        {save.isSuccess ? (
          <Typography color="success.main">اطلاعات واحد ذخیره شد.</Typography>
        ) : null}
        <Stack direction="row" spacing={1}>
          <Button
            type="submit"
            variant="contained"
            disabled={!name.trim() || save.isPending}
          >
            {save.isPending ? "در حال ثبت…" : "ذخیره"}
          </Button>
          <Button
            type="button"
            onClick={() =>
              onClose?.() ??
              dispatch({ type: "close", key: activeKey, confirmed: true })
            }
          >
            بستن
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
}
