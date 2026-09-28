#Requires -Version 7
# Nullstiller det lokale miljøet for TrønderLeikan: postgres-container, Aspire-nettverk, datavolum og Zitadel-bootstrap.
# Rører ikke andre prosjekters containere. Stopp AppHost før du kjører dette.

$ErrorActionPreference = "SilentlyContinue"
Set-Location $PSScriptRoot

if (Get-Process -Name "TronderLeikan.AppHost" -ErrorAction SilentlyContinue) {
    Write-Host "AppHost kjører fortsatt. Stopp den (Ctrl+C) og kjør scriptet igjen."
    exit 1
}

# Postgres-containeren identifiseres på volumet den monterer, ikke på navn
foreach ($c in docker ps -aq --filter "label=com.microsoft.developer.usvc-dev.mountsLabel=type=volume,src=leikan-postgres-data") {
    Write-Host "Fjerner container $((docker inspect $c --format '{{.Name}}').TrimStart('/'))"
    docker rm -f $c | Out-Null
}

# Zitadel-containere er ikke persistente, men kan stå igjen etter en krasj
foreach ($c in docker ps -aq --filter "name=^zitadel-(api|login|proxy)-") {
    Write-Host "Fjerner container $((docker inspect $c --format '{{.Name}}').TrimStart('/'))"
    docker rm -f $c | Out-Null
}

foreach ($n in (docker network ls --format '{{.Name}}' | Where-Object { $_ -match '^aspire-persistent-network-.*-TronderLeikan$' })) {
    Write-Host "Fjerner nettverk $n"
    docker network rm $n | Out-Null
}

docker volume inspect leikan-postgres-data *> $null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Fjerner volum leikan-postgres-data"
    docker volume rm leikan-postgres-data | Out-Null
}

if (Test-Path "src/TronderLeikan.AppHost/zitadel-bootstrap") {
    Write-Host "Fjerner src/TronderLeikan.AppHost/zitadel-bootstrap/"
    Remove-Item -Recurse -Force "src/TronderLeikan.AppHost/zitadel-bootstrap"
}

Write-Host ""
Write-Host "Ferdig. Start på nytt med: dotnet run --project src/TronderLeikan.AppHost"
exit 0
