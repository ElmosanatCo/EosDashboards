import { Alert } from "@mui/material";
import { problemMessage } from "../lib/api/problemMessage";

export function MutationErrorAlert({ error }: { error: unknown }) {
  if (!error) return null;

  return <Alert severity="error">{problemMessage(error)}</Alert>;
}
