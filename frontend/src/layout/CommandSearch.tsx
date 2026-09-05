import {
  Box,
  InputAdornment,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Paper,
  Popper,
  TextField,
  Tooltip,
} from "@mui/material";
import { useEffect, useMemo, useRef, useState } from "react";
import type { WorkspaceTarget } from "../navigation/workspaceTargets";

export function CommandSearch({
  targets,
  onSelect,
}: {
  targets: readonly WorkspaceTarget[];
  onSelect: (target: WorkspaceTarget) => void;
}) {
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const matches = useMemo(() => {
    const normalized = query.trim();
    if (!normalized) return targets;
    return targets.filter((target) =>
      [target.title, ...target.keywords].some((label) =>
        label.includes(normalized),
      ),
    );
  }, [query, targets]);
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        inputRef.current?.focus();
        setAnchorEl(inputRef.current);
        setOpen(true);
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);
  return (
    <>
      <TextField
        inputRef={inputRef}
        value={query}
        onChange={(event) => setQuery(event.target.value)}
        onFocus={(event) => {
          setAnchorEl(event.currentTarget);
          setOpen(true);
        }}
        onBlur={() => window.setTimeout(() => setOpen(false), 120)}
        placeholder="جست‌وجو در عملیات و صفحه‌های مجاز…"
        size="small"
        slotProps={{
          htmlInput: {
            "aria-label": "جست‌وجوی سراسری",
            className: "eos-command-search-input",
          },
          input: {
            endAdornment: (
              <InputAdornment position="end">
                <Tooltip
                  title="باز کردن جست‌وجو با میانبر Ctrl+K"
                  placement="top"
                >
                  <Box
                    component="kbd"
                    aria-label="میانبر Ctrl+K"
                    dir="ltr"
                    sx={{
                      px: 0.75,
                      py: 0.25,
                      border: "1px solid",
                      borderColor: "divider",
                      borderRadius: 0.75,
                      color: "text.secondary",
                      fontFamily: 'Vazirmatn, "Segoe UI", sans-serif',
                      fontSize: "0.7rem",
                      lineHeight: 1.2,
                      whiteSpace: "nowrap",
                    }}
                  >
                    Ctrl+K
                  </Box>
                </Tooltip>
              </InputAdornment>
            ),
          },
        }}
        sx={{
          width: "min(410px, 38vw)",
          minWidth: 150,
        }}
      />
      <Popper
        open={open && matches.length > 0}
        anchorEl={anchorEl}
        placement="bottom-start"
        sx={{
          zIndex: (theme) => theme.zIndex.modal + 1,
          width: "min(410px, 84vw)",
        }}
      >
        <Paper variant="outlined" sx={{ mt: 0.75 }}>
          <List dense aria-label="نتیجه‌های جست‌وجوی سراسری">
            {matches.map((target) => (
              <ListItemButton
                key={target.routeId}
                onMouseDown={(event) => event.preventDefault()}
                onClick={() => {
                  onSelect(target);
                  setOpen(false);
                }}
              >
                <ListItemIcon>
                  <target.Icon fontSize="small" />
                </ListItemIcon>
                <ListItemText primary={target.title} />
              </ListItemButton>
            ))}
          </List>
        </Paper>
      </Popper>
    </>
  );
}
