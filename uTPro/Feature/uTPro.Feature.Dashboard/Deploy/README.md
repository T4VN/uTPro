# uTPro Deploy

One-click deploy from the uTPro backoffice dashboard. When the admin clicks
"Deploy" in the dashboard, the server triggers a platform-specific script that:

1. Queries the latest uTPro release from GitHub
2. Downloads the `publish_output*.zip` asset
3. Stops all configured IIS app pools (Windows) or systemd services (Linux)
4. Removes old `.dll` / `.pdb` files from each site root
5. Copies all new files from the release (skipping preserved data folders)
6. Restarts all app pools / services

## Configuration

Edit `deploy.config.json` to match your server setup:

```json
{
  "github": {
    "repo": "T4VN/uTPro",
    "assetPattern": "publish_output*.zip"
  },
  "sites": [
    {
      "name": "Site1",
      "appPool": "uTPro-Site1",
      "path": "C:\\inetpub\\wwwroot\\uTPro-Site1"
    },
    {
      "name": "Site2",
      "appPool": "uTPro-Site2",
      "path": "C:\\inetpub\\wwwroot\\uTPro-Site2"
    }
  ],
  "preserve": [
    "appsettings.Production.json",
    "appsettings.Development.json",
    "umbraco/Data",
    "wwwroot/media",
    "media",
    "App_Data"
  ]
}
```

### Sites (Windows / IIS)

Each site entry needs:
- `name` — display name (for logs)
- `appPool` — IIS Application Pool name
- `path` — physical path to the site root

### Sites (Linux / macOS)

For Linux/macOS, add a `serviceName` field:
```json
{
  "name": "Site1",
  "serviceName": "utpro-site1",
  "path": "/var/www/utpro-site1"
}
```

### Preserve

Files and folders listed here are **never touched** during deploy. They keep
each site's unique configuration and user data intact across updates.

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| POST | `/umbraco/management/api/v1/utpro/dashboard/deploy` | Trigger deploy (admin only) |
| GET | `/umbraco/management/api/v1/utpro/dashboard/deploy/status` | Check deploy status |

## How It Works

The deploy endpoint launches the script as a **detached process** (fire-and-forget).
This is necessary because the script will restart the app pool that hosts the API
itself. The API returns `200 OK` immediately with a "deploy triggered" confirmation,
then the script takes over.

The script includes a 5-second sleep at the start (Windows) to allow the HTTP
response to complete before the app pool is stopped.

## Security

- Only administrators can trigger a deploy
- Concurrent deploys are blocked (409 Conflict)
- No user input is passed to the script (prevents injection)
- Scripts are shipped as static files (not user-editable via the API)

## Manual Testing

You can run the scripts directly on the server for testing:

**Windows:**
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Deploy\win\deploy.ps1
```

**Linux/macOS:**
```bash
bash Deploy/linux-macos/deploy.sh
```
