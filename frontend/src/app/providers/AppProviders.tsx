import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";
import type { PropsWithChildren } from "react";
import { AppThemeProvider } from "./AppThemeProvider";

export function AppProviders({ children }: PropsWithChildren) {
  const [queryClient] = useState(() => new QueryClient({
    defaultOptions: { queries: { retry: false, staleTime: 30_000 } },
  }));

  return (
    <QueryClientProvider client={queryClient}>
      <AppThemeProvider>{children}</AppThemeProvider>
    </QueryClientProvider>
  );
}
