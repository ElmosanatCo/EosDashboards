import { Button } from "@mui/material";
import { GoogleBrandMark } from "./GoogleBrandMark";

type Props = {
  available: boolean;
  busy: boolean;
  onStart: () => void;
};

export function GoogleSignInButton({ available, busy, onStart }: Props) {
  if (!available) return null;

  return (
    <Button
      type="button"
      variant="outlined"
      fullWidth
      size="large"
      startIcon={<GoogleBrandMark />}
      disabled={busy}
      onClick={onStart}
      sx={{
        minHeight: 48,
        borderColor: "divider",
        color: "text.primary",
        fontWeight: 700,
        "&:hover": {
          borderColor: "primary.main",
          bgcolor: "action.hover",
        },
      }}
    >
      ورود با Google
    </Button>
  );
}
