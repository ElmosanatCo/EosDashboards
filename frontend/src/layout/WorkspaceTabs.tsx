import CloseIcon from "@mui/icons-material/Close";
import { Box, IconButton, Tab, Tabs, Tooltip } from "@mui/material";
import { alpha } from "@mui/material/styles";
import { useUserPreferences } from "../app/providers/UserPreferenceProvider";
import { useTabWorkspace } from "../navigation/TabWorkspaceProvider";

export function WorkspaceTabs() {
  const { tabs, activeKey, dispatch } = useTabWorkspace();
  const { gradientsEnabled } = useUserPreferences();
  return (
    <Box
      data-testid="workspace-tabs-strip"
      sx={{
        bgcolor: "background.paper",
        borderBottom: 1,
        borderColor: "divider",
        minHeight: 42,
      }}
    >
      <Tabs
        value={activeKey}
        variant="scrollable"
        scrollButtons="auto"
        aria-label="صفحه‌های باز"
        sx={{
          minHeight: 42,
          "& .MuiTab-root": { color: "text.secondary" },
          "& .Mui-selected": { color: "text.primary" },
          "& .MuiTab-root.Mui-selected": (theme) => ({
            backgroundImage: gradientsEnabled
              ? `linear-gradient(0deg, ${alpha(theme.palette.primary.main, 0.2)} 0%, ${alpha(theme.palette.primary.main, 0.08)} 26%, transparent 46%)`
              : "none",
            backgroundRepeat: "no-repeat",
          }),
        }}
      >
        {tabs.map((tab) => (
          <Tab
            key={tab.key}
            value={tab.key}
            onClick={() => dispatch({ type: "activate", key: tab.key })}
            label={
              <span
                style={{
                  alignItems: "center",
                  display: "inline-flex",
                  gap: "4px",
                }}
              >
                {tab.title}
                {tab.closable ? (
                  <Tooltip title={`بستن ${tab.title}`}>
                    <IconButton
                      component="span"
                      size="small"
                      aria-label={`بستن ${tab.title}`}
                      onClick={(event) => {
                        event.stopPropagation();
                        dispatch({ type: "close", key: tab.key });
                      }}
                    >
                      <CloseIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                ) : null}
              </span>
            }
          />
        ))}
      </Tabs>
    </Box>
  );
}
