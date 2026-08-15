# Backup PassDo SQL + uploads volume to a host folder.
# Usage:
#   powershell -File scripts/backup-docker.ps1
#   powershell -File scripts/backup-docker.ps1 -OutDir E:\PassDoBackup
param(
    [string]$OutDir = "E:\PassDoBackup"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

function Import-DotEnv {
    param([string]$Path)
    if (-not (Test-Path $Path)) { throw "Missing $Path" }
    Get-Content $Path | ForEach-Object {
        if ($_ -match '^\s*#' -or $_ -notmatch '=') { return }
        $k, $v = $_.Split('=', 2)
        Set-Item -Path "Env:$($k.Trim())" -Value $v.Trim()
    }
}

function Wait-ContainerHealthy {
    param([string]$Name, [int]$TimeoutSec = 90)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    do {
        $status = docker inspect -f "{{.State.Status}} {{if .State.Health}}{{.State.Health.Status}}{{end}}" $Name 2>$null
        if ($status -match "running healthy") { return }
        if ((Get-Date) -ge $deadline) {
            throw "Container $Name not healthy after ${TimeoutSec}s. Last status: $status"
        }
        Start-Sleep 3
    } while ($true)
}

Import-DotEnv (Join-Path $Root ".env")
if ([string]::IsNullOrWhiteSpace($env:MSSQL_SA_PASSWORD)) {
    throw "MSSQL_SA_PASSWORD is empty. Check .env"
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$OutDirResolved = (Resolve-Path $OutDir).Path
$OutDirUnix = $OutDirResolved -replace '\\', '/'

docker compose up -d database
Wait-ContainerHealthy -Name "passdo-database"

docker exec passdo-database mkdir -p /var/opt/mssql/backup
$backupOut = docker exec passdo-database /opt/mssql-tools18/bin/sqlcmd `
    -S localhost -U sa -P "$env:MSSQL_SA_PASSWORD" -C `
    -Q "BACKUP DATABASE [PassDoDb] TO DISK = N'/var/opt/mssql/backup/PassDoDb.bak' WITH INIT" 2>&1
Write-Output $backupOut
if ($LASTEXITCODE -ne 0 -or ($backupOut -join "`n") -notmatch "BACKUP DATABASE successfully") {
    throw "SQL backup failed. Is passdo-database running/healthy? Check MSSQL_SA_PASSWORD."
}

docker cp passdo-database:/var/opt/mssql/backup/PassDoDb.bak (Join-Path $OutDirResolved "PassDoDb.bak")
if ($LASTEXITCODE -ne 0) { throw "docker cp PassDoDb.bak failed" }

docker run --rm `
    -v webpassdo_uploads_data:/data `
    -v "${OutDirUnix}:/backup" `
    alpine tar czf /backup/uploads.tar.gz -C /data .
if ($LASTEXITCODE -ne 0) { throw "uploads tar failed" }

Copy-Item (Join-Path $Root ".env") (Join-Path $OutDirResolved ".env") -Force

$bak = Get-Item (Join-Path $OutDirResolved "PassDoDb.bak")
$tar = Get-Item (Join-Path $OutDirResolved "uploads.tar.gz")
$cutoff = (Get-Date).AddMinutes(-5)
if ($bak.LastWriteTime -lt $cutoff) { throw "PassDoDb.bak was not overwritten (still $($bak.LastWriteTime))" }
if ($tar.LastWriteTime -lt $cutoff) { throw "uploads.tar.gz was not overwritten (still $($tar.LastWriteTime))" }
if ($bak.Length -lt 1MB) { throw "PassDoDb.bak too small: $($bak.Length) bytes" }

Write-Output ""
Write-Output "Backup OK -> $OutDirResolved"
Get-ChildItem $OutDirResolved | Format-Table Mode, LastWriteTime, Length, Name -AutoSize
if ($tar.Length -lt 200) {
    Write-Warning "uploads.tar.gz is tiny ($($tar.Length) bytes) = volume anh trong."
}
