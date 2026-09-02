import { AuthProvider } from "./app/providers/AuthProvider";
import { AppThemeProvider } from "./app/providers/AppThemeProvider";
import { UserPreferenceProvider } from "./app/providers/UserPreferenceProvider";
import { AppShell } from "./layout/AppShell";
import { TabWorkspaceProvider, useTabWorkspace } from "./navigation/TabWorkspaceProvider";
import { useAuth } from "./app/providers/AuthProvider";

function UserWorkspace() {
  const { user } = useAuth();
  return (
    <AppThemeProvider userId={user.id}>
      <UserPreferenceProvider userId={user.id}><AppShell /></UserPreferenceProvider>
    </AppThemeProvider>
  );
}

function AuthenticatedWorkspace() {
  const { clearSessionTabs } = useTabWorkspace();
  return <AuthProvider onLogout={clearSessionTabs}><UserWorkspace /></AuthProvider>;
}

export default function App() {
  return <TabWorkspaceProvider><AuthenticatedWorkspace /></TabWorkspaceProvider>;
}
