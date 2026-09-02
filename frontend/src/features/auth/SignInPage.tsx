import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import type { Challenge } from "./authTypes";
import { OtpForm } from "./OtpForm";

type Props = {
  challenge: Challenge | null;
  busy: boolean;
  error?: string;
  onStart: () => Promise<void>;
  onVerify: (code: string) => Promise<void>;
  onBack: () => void;
};

export function SignInPage({
  challenge,
  busy,
  error,
  onStart,
  onVerify,
  onBack,
}: Props) {
  return (
    <Box
      component="main"
      sx={{ minHeight: "100dvh", display: "grid", placeItems: "center", p: 2 }}
    >
      <Paper
        elevation={4}
        sx={{ width: "min(100%, 420px)", p: { xs: 3, sm: 5 } }}
      >
        <Stack spacing={3}>
          <Typography variant="h4" component="h1">
            سامانه داشبورد علم و صنعت
          </Typography>
          {error ? <Alert severity="error">{error}</Alert> : null}
          {challenge ? (
            <OtpForm
              maskedMobile={challenge.maskedMobile}
              expiresAtUtc={challenge.expiresAtUtc}
              busy={busy}
              error={error}
              onSubmit={onVerify}
              onBack={onBack}
            />
          ) : (
            <Button
              variant="contained"
              size="large"
              disabled={busy}
              onClick={() => void onStart()}
            >
              {busy ? (
                <CircularProgress size={22} color="inherit" />
              ) : (
                "ورود با حساب سازمانی"
              )}
            </Button>
          )}
        </Stack>
      </Paper>
    </Box>
  );
}
