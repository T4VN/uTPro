<#
    uTPro Deploy Script (Windows / IIS)
    ------------------------------------
    Called by the uTPro Dashboard "Deploy" button via the management API.
    Downloads the latest GitHub release publish asset, stops all configured IIS
    app pools, removes old DLLs, overlays the new release (preserving data folders),
    and restarts the app pools.

    Exit codes:
      0 = success (DEPLOY_SUCCESS or NO_CHANGES in stdout)
      1 = error (message in stderr / stdout)

    Usage:
      powershell -NoProfile -ExecutionPolicy Bypass -File deploy.ps1 [-ConfigFile path]
#>
param(
    [string]$ConfigFile = (Join-Path $PSScriptRoot '..\deploy.config.json')
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

# --- Load configuration ---
if (-not (Test-Path $ConfigFile)) {
    Write-Error "Config file not found: $ConfigFile"
    exit 1
}
$cfg = Get-Content $ConfigFile -Raw | ConvertFrom-Json
$repo = $cfg.github.repo
$assetPattern = $cfg.github.assetPattern
$sites = $cfg.sites
$preserve = $cfg.preserve

if (-not $sites -or $sites.Count -eq 0) {
    Write-Error "No sites configured in deploy.config.json"
    exit 1
}

$headers = @{ 'User-Agent' = 'uTPro-Deploy' }
$tempDir = Join-Path $env:TEMP "utpro-deploy-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

try {
    # --- 1. Query the latest release from GitHub ---
    Write-Output "[1/5] Querying latest release from GitHub ($repo)..."
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/latest" -Headers $headers -TimeoutSec 30
    $tag = $release.tag_name
    Write-Output "       Latest release: $tag"

    # --- 2. Check if update is needed (compare with first site's marker) ---
    $markerFile = Join-Path $sites[0].path '.utpro-release'
    if (Test-Path $markerFile) {
        $installed = (Get-Content $markerFile -Raw).Trim()
        if ($installed -eq $tag) {
            Write-Output "NO_CHANGES"
            Write-Output "       All sites are already on $tag."
            exit 0
        }
        Write-Output "       Installed: $installed -> Updating to: $tag"
    }

    # --- 3. Download and extract the release asset ---
    Write-Output "[2/5] Downloading release asset..."
    $asset = $release.assets | Where-Object { $_.name -like $assetPattern } | Select-Object -First 1
    if ($null -eq $asset) {
        Write-Error "No asset matching '$assetPattern' found on release $tag."
        exit 1
    }

    New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
    $zipPath = Join-Path $tempDir 'release.zip'
    $extractPath = Join-Path $tempDir 'extracted'

    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zipPath -Headers $headers -TimeoutSec 1800
    Write-Output "       Downloaded $($asset.name) ($([math]::Round($asset.size / 1MB, 1)) MB)"

    Write-Output "[3/5] Extracting..."
    Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force
    # The archive typically contains a single root folder; find it.
    $releaseRoot = Get-ChildItem -Path $extractPath -Directory | Select-Object -First 1
    if ($null -eq $releaseRoot) {
        # Flat archive (files directly in extract root)
        $releaseRoot = Get-Item $extractPath
    }
    $releasePath = $releaseRoot.FullName

    # --- 4. Stop all app pools ---
    Write-Output "[4/5] Stopping app pools..."
    Import-Module WebAdministration -ErrorAction SilentlyContinue
    foreach ($site in $sites) {
        try {
            Stop-WebAppPool -Name $site.appPool -ErrorAction Stop
            Write-Output "       Stopped: $($site.appPool)"
        }
        catch {
            Write-Output "       Warning: could not stop $($site.appPool): $_"
        }
    }
    # Brief wait for processes to release file locks.
    Start-Sleep -Seconds 5

    # --- 5. Update each site ---
    Write-Output "[5/5] Deploying to $($sites.Count) site(s)..."
    foreach ($site in $sites) {
        $sitePath = $site.path
        Write-Output "       -> $($site.name) ($sitePath)"

        if (-not (Test-Path $sitePath)) {
            Write-Output "          Site path does not exist, creating..."
            New-Item -ItemType Directory -Force -Path $sitePath | Out-Null
        }

        # 5a. Remove old DLL/PDB files from site root (not recursive into data dirs).
        $dllFiles = Get-ChildItem -Path $sitePath -Filter '*.dll' -File -ErrorAction SilentlyContinue
        $pdbFiles = Get-ChildItem -Path $sitePath -Filter '*.pdb' -File -ErrorAction SilentlyContinue
        $removed = 0
        foreach ($f in @($dllFiles) + @($pdbFiles)) {
            if ($null -ne $f) { Remove-Item $f.FullName -Force; $removed++ }
        }
        Write-Output "          Removed $removed old .dll/.pdb files"

        # 5b. Copy new files, skipping preserved paths.
        $copied = 0
        Get-ChildItem $releasePath -Recurse | ForEach-Object {
            $rel = $_.FullName.Substring($releasePath.Length + 1)
            $skip = $false
            foreach ($p in $preserve) {
                $normalized = $p.Replace('/', '\')
                if ($rel -eq $normalized -or $rel.StartsWith("$normalized\", [StringComparison]::OrdinalIgnoreCase)) {
                    $skip = $true
                    break
                }
            }
            if (-not $skip) {
                $dest = Join-Path $sitePath $rel
                if ($_.PSIsContainer) {
                    New-Item -ItemType Directory -Force -Path $dest | Out-Null
                }
                else {
                    $destDir = Split-Path $dest
                    if (-not (Test-Path $destDir)) {
                        New-Item -ItemType Directory -Force -Path $destDir | Out-Null
                    }
                    Copy-Item $_.FullName -Destination $dest -Force
                    $copied++
                }
            }
        }
        Write-Output "          Copied $copied files"

        # 5c. Stamp the version marker.
        Set-Content -Path (Join-Path $sitePath '.utpro-release') -Value $tag -Encoding UTF8 -NoNewline
    }

    # --- 6. Start all app pools ---
    foreach ($site in $sites) {
        try {
            Start-WebAppPool -Name $site.appPool -ErrorAction Stop
            Write-Output "       Started: $($site.appPool)"
        }
        catch {
            Write-Output "       Warning: could not start $($site.appPool): $_"
        }
    }

    Write-Output "DEPLOY_SUCCESS"
    Write-Output "       Deployed $tag to $($sites.Count) site(s)."
}
catch {
    # Attempt to restart app pools even on failure.
    foreach ($site in $sites) {
        try { Start-WebAppPool -Name $site.appPool -ErrorAction SilentlyContinue } catch {}
    }
    Write-Error "Deploy failed: $_"
    exit 1
}
finally {
    # Clean up temp files.
    if (Test-Path $tempDir) {
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
