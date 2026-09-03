import CloseIcon from "@mui/icons-material/Close";
import { Box, IconButton, Tab, Tabs } from "@mui/material";
import { useTabWorkspace } from "../navigation/TabWorkspaceProvider";

export function WorkspaceTabs() {
  const { tabs, activeKey, dispatch } = useTabWorkspace();
  return (
    <Box
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
                ) : null}
              </span>
            }
          />
        ))}
      </Tabs>
    </Box>
  );
}
