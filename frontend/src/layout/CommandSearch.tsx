import {
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Paper,
  Popper,
  TextField,
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
        slotProps={{ htmlInput: { "aria-label": "جست‌وجوی سراسری" } }}
        sx={{ width: "min(410px, 38vw)", minWidth: 150 }}
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
