import {
  Button,
  type ButtonProps,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Typography,
} from "@mui/material";

type ConfirmActionDialogProps = {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  confirmColor?: ButtonProps["color"];
  pending?: boolean;
  onClose: () => void;
  onConfirm: () => void;
};

export function ConfirmActionDialog({
  open,
  title,
  message,
  confirmLabel = "تأیید حذف",
  confirmColor = "error",
  pending = false,
  onClose,
  onConfirm,
}: ConfirmActionDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={pending ? undefined : onClose}
      fullWidth
      maxWidth="xs"
      aria-labelledby="confirm-action-dialog-title"
    >
      <DialogTitle id="confirm-action-dialog-title">{title}</DialogTitle>
      <DialogContent>
        <Typography>{message}</Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={pending}>
          انصراف
        </Button>
        <Button
          color={confirmColor}
          variant="contained"
          onClick={onConfirm}
          disabled={pending}
        >
          {confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
