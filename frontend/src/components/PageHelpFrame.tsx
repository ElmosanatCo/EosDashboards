import AutoAwesomeOutlinedIcon from "@mui/icons-material/AutoAwesomeOutlined";
import HelpOutlineOutlinedIcon from "@mui/icons-material/HelpOutlineOutlined";
import RouteOutlinedIcon from "@mui/icons-material/RouteOutlined";
import TaskAltOutlinedIcon from "@mui/icons-material/TaskAltOutlined";
import WarningAmberOutlinedIcon from "@mui/icons-material/WarningAmberOutlined";
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import type { ReactNode } from "react";
import { useState } from "react";
import { pageGuideFor } from "./pageGuides";

const sectionIcons = {
  "وظایف این صفحه": TaskAltOutlinedIcon,
  امکانات: AutoAwesomeOutlinedIcon,
  "شیوه انجام کار": RouteOutlinedIcon,
  محدودیت‌ها: WarningAmberOutlinedIcon,
} as const;

export function PageHelpFrame({
  routeId,
  children,
}: {
  routeId: string;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const pageGuide = pageGuideFor(routeId);

  return (
    <Box
      data-testid="page-help-frame"
      sx={{
        position: "relative",
        height: "100%",
        minHeight: 0,
      }}
    >
      <IconButton
        size="medium"
        color="primary"
        aria-label={pageGuide.title}
        onClick={() => setOpen(true)}
        sx={{
          position: "absolute",
          top: 0,
          insetInlineEnd: 0,
          zIndex: 2,
          p: 0.75,
        }}
      >
        <HelpOutlineOutlinedIcon fontSize="medium" />
      </IconButton>
      <Box
        data-testid="page-help-content"
        sx={{
          height: "100%",
          minHeight: 0,
          boxSizing: "border-box",
          paddingInlineEnd: 0,
        }}
      >
        {children}
      </Box>
      <Dialog
        open={open}
        onClose={() => setOpen(false)}
        fullWidth
        maxWidth="md"
        aria-labelledby="page-help-dialog-title"
      >
        <DialogTitle id="page-help-dialog-title" sx={{ pb: 1.25 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
            <HelpOutlineOutlinedIcon color="primary" />
            <Box>
              <Typography component="div" variant="h6" sx={{ fontWeight: 750 }}>
                {pageGuide.title}
              </Typography>
              <Typography
                variant="body2"
                color="text.secondary"
                aria-hidden="true"
                sx={{ mt: 0.35 }}
              >
                راهنمای سریع و محدودیت‌های این صفحه
              </Typography>
            </Box>
          </Stack>
        </DialogTitle>
        <Divider />
        <DialogContent>
          <Typography color="text.secondary" sx={{ mb: 2, lineHeight: 2 }}>
            {pageGuide.introduction}
          </Typography>
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: {
                xs: "1fr",
                sm: "repeat(2, minmax(0, 1fr))",
              },
              gap: 1.5,
            }}
          >
            {pageGuide.sections.map((section) => {
              const SectionIcon = sectionIcons[section.title];
              return (
                <Paper
                  key={section.title}
                  variant="outlined"
                  sx={{
                    p: 1.5,
                    height: "100%",
                    borderTop: 2,
                    borderTopColor: "primary.main",
                  }}
                >
                  <Stack
                    direction="row"
                    spacing={0.75}
                    sx={{ alignItems: "center", mb: 1 }}
                  >
                    <SectionIcon
                      fontSize="small"
                      color={
                        section.title === "محدودیت‌ها" ? "warning" : "primary"
                      }
                    />
                    <Typography variant="subtitle2" sx={{ fontWeight: 750 }}>
                      {section.title}
                    </Typography>
                  </Stack>
                  <Box component="ul" sx={{ m: 0, p: 0, listStyle: "none" }}>
                    {section.items.map((item) => (
                      <Typography
                        component="li"
                        key={item}
                        variant="body2"
                        color="text.secondary"
                        sx={{
                          position: "relative",
                          paddingInlineStart: "16px",
                          mb: 0.75,
                          lineHeight: 1.9,
                          listStyle: "none",
                          "&::before": {
                            content: '"•"',
                            position: "absolute",
                            insetInlineStart: 0,
                            color: "primary.main",
                            fontWeight: 700,
                          },
                          "&:last-child": { mb: 0 },
                        }}
                      >
                        {item}
                      </Typography>
                    ))}
                  </Box>
                </Paper>
              );
            })}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>بستن راهنما</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
