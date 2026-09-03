import type { PropsWithChildren } from "react";

export function DirtyPageGuard({
  dirty,
  children,
}: PropsWithChildren<{ dirty: boolean }>) {
  return <div data-dirty={dirty || undefined}>{children}</div>;
}
