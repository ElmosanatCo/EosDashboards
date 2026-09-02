# System Administrator provisioning

Provisioning is a deployment operation, not an API endpoint.

1. Apply the reviewed idempotent migration script in `backend/artifacts/sql/InitialIdentity.sql` to the intended development database.
2. Configure the provisioner user-secret store with the database connection, independent security keys, and persistent key-ring path. SMS configuration is not required.
3. From `backend/`, run:

```powershell
dotnet run --project tools/EosDashboards.AdminProvisioner
```

4. Enter the organizational stable ID, account name, first and last name, and mobile number only at the interactive prompts. Confirm explicitly.
5. Verify only the returned masked mobile and successful exit code. Re-running is safe and updates the same administrator.

Never pass personal data on the command line, capture the complete mobile in logs, or delete the Data Protection key ring after protected numbers have been stored.
