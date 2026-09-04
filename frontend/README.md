# EosDashboards frontend

React SPA for the Persian RTL dashboard shell.

```powershell
npm ci
npm run dev
```

`predev`, `pretest`, and `prebuild` copy the approved shared resources into the ignored `public/generated-assets/` directory. Set `VITE_API_BASE_URL` outside source control when the API is on another origin.

For a local full-stack browser demonstration, run the API on `http://localhost:5171` and start Vite with `VITE_API_PROXY_TARGET=http://localhost:5171`; the development proxy then forwards directly to the API instead of the IIS prefix.

Use the installed `https://localhost/EosDashboards/` application for manual username/password, OTP, refresh, logout, and preference-persistence checks. The Vite HTTP preview is for frontend development and automated mock flows only: it cannot be treated as a secure refresh-session host because the application uses a `Secure`, `__Host-` refresh cookie. Do not weaken that cookie policy for development.

The application keeps access tokens only in memory. Do not add tokens or personal data to browser storage or logs.
