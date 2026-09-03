# EosDashboards frontend

React SPA for the Persian RTL dashboard shell.

```powershell
npm ci
npm run dev
```

`predev`, `pretest`, and `prebuild` copy the approved shared resources into the ignored `public/generated-assets/` directory. Set `VITE_API_BASE_URL` outside source control when the API is on another origin.

The application keeps access tokens only in memory. Do not add tokens or personal data to browser storage or logs.
