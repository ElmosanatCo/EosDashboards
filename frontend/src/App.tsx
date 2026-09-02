import { AuthProvider } from "./app/providers/AuthProvider";
import { AppShell } from "./layout/AppShell";
import { TabWorkspaceProvider, useTabWorkspace } from "./navigation/TabWorkspaceProvider";

function AuthenticatedWorkspace() {
  const { clearSessionTabs } = useTabWorkspace();
  return <AuthProvider onLogout={clearSessionTabs}><AppShell /></AuthProvider>;
}

export default function App() {
  return <TabWorkspaceProvider><AuthenticatedWorkspace /></TabWorkspaceProvider>;
}
