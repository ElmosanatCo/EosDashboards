import CloseIcon from "@mui/icons-material/Close";
import { Box, IconButton, Tab, Tabs } from "@mui/material";
import { useTabWorkspace } from "../navigation/TabWorkspaceProvider";

export function WorkspaceTabs() {
  const { tabs, activeKey, dispatch } = useTabWorkspace();
  return (
    <Box sx={{ borderBottom: 1, borderColor: "divider" }}>
      <Tabs
        value={activeKey}
        variant="scrollable"
        scrollButtons="auto"
        aria-label="صفحه‌های باز"
      >
        {tabs.map((tab) => (
          <Tab
            key={tab.key}
            value={tab.key}
            onClick={() => dispatch({ type: "activate", key: tab.key })}
            label={
              <span>
                {tab.title}
                {tab.closable ? (
                  <IconButton
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
