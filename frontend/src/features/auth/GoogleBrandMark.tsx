import { Box } from "@mui/material";

export function GoogleBrandMark() {
  return (
    <Box
      component="span"
      data-testid="google-brand-g"
      aria-hidden="true"
      sx={{
        display: "inline-block",
        fontFamily: "Arial, sans-serif",
        fontSize: "1.25rem",
        fontWeight: 800,
        lineHeight: 1,
        background:
          "conic-gradient(from -45deg, #4285F4 0deg 90deg, #34A853 90deg 180deg, #FBBC05 180deg 270deg, #EA4335 270deg 360deg)",
        backgroundClip: "text",
        WebkitBackgroundClip: "text",
        color: "transparent",
      }}
    >
      G
    </Box>
  );
}
