# EosDashboards frontend

React SPA for the Persian RTL dashboard shell.

```powershell
npm ci
npm run dev
```

`predev`, `pretest`, and `prebuild` copy the approved shared resources into the ignored `public/generated-assets/` directory. Set `VITE_API_BASE_URL` outside source control when the API is on another origin.

Use the installed `https://localhost/EosDashboards/` application for manual username/password, OTP, refresh, logout, and preference-persistence checks. The Vite HTTP preview is for frontend development and automated mock flows only: it cannot be treated as a secure refresh-session host because the application uses a `Secure`, `__Host-` refresh cookie. Do not weaken that cookie policy for development.

The application keeps access tokens only in memory. Do not add tokens or personal data to browser storage or logs.
