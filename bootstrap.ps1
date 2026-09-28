#Requires -Version 7
# Sjekker at verktøyene TrønderLeikan trenger er på plass. Installerer ingenting utover dotnet-verktøy fra manifestet.
# Grønn linje = ok, rød linje = mangler (med lenke), gul linje = anbefalt men ikke påkrevd.
# Exit-kode 1 hvis noe påkrevd mangler.
#
# Kjør slik hvis Windows nekter å kjøre script:
#   pwsh -ExecutionPolicy Bypass -File .\bootstrap.ps1

$ErrorActionPreference = "SilentlyContinue"
Set-Location $PSScriptRoot
$script:missing = $false

function Ok($text)          { Write-Host "[OK] " -ForegroundColor Green -NoNewline; Write-Host $text }
function Fail($text, $hint) { Write-Host "[MANGLER] " -ForegroundColor Red -NoNewline; Write-Host $text; Write-Host "   -> $hint"; $script:missing = $true }
function Warn($text, $hint) { Write-Host "[ANBEFALT] " -ForegroundColor Yellow -NoNewline; Write-Host $text; Write-Host "   -> $hint" }
function Has($cmd)          { return [bool](Get-Command $cmd -ErrorAction SilentlyContinue) }
# Kjører en kommando stille og returnerer true hvis exit-koden er 0. Docker skriver til stdout selv når daemonen er nede,
# så vi kan ikke teste på output.
function Succeeds([scriptblock]$cmd) { & $cmd *> $null; return $LASTEXITCODE -eq 0 }

Write-Host "TrønderLeikan - sjekk av utviklingsmiljø" -ForegroundColor White
Write-Host ""

# .NET SDK 10
$sdk10 = if (Has dotnet) { dotnet --list-sdks 2>$null | Where-Object { $_ -match '^10\.' } | Select-Object -Last 1 } else { $null }
if ($sdk10) { Ok ".NET SDK 10 ($(($sdk10 -split ' ')[0]))" }
else { Fail ".NET SDK 10 mangler" "https://dotnet.microsoft.com/download/dotnet/10.0" }

# HTTPS-utviklersertifikat. Aspire-dashboardet kjører på https og nettleseren klager uten et klarert sertifikat
if (Has dotnet) {
    if (Succeeds { dotnet dev-certs https --check --trust }) { Ok "HTTPS-utviklersertifikat er klarert" }
    else { Warn "HTTPS-utviklersertifikatet er ikke klarert. Dashboardet gir sertifikatadvarsel" "Kjør: dotnet dev-certs https --trust" }
}

# Docker daemon
if ((Has docker) -and (Succeeds { docker info })) {
    Ok "Docker kjører ($(docker version --format '{{.Server.Version}}' 2>$null))"
    $memBytes = [long](docker info --format '{{.MemTotal}}' 2>$null)
    if ($memBytes -gt 0 -and $memBytes -lt 4000000000) {
        Warn "Docker har under 4 GB minne ($([int]($memBytes / 1MB)) MB). Zitadel kan bli treg ved første oppstart" "Øk minne i Docker Desktop -> Settings -> Resources"
    }
}
else { Fail "Docker daemon svarer ikke" "https://www.docker.com/products/docker-desktop/ (start Docker Desktop)" }

# Node.js 24+ og npm. Frontend kjøres med npm av AppHost
$nodeMajor = if (Has node) { [int](node -p 'process.versions.node.split(".")[0]' 2>$null) } else { 0 }
if ($nodeMajor -ge 24 -and (Has npm)) { Ok "Node.js $(node --version), npm $(npm --version 2>$null)" }
else { Fail "Node.js 24 eller nyere med npm mangler" "https://nodejs.org/en/download (LTS)" }

# Git
if (Has git) { Ok "$(git --version)" }
else { Fail "Git mangler" "https://git-scm.com/downloads" }

# GitHub CLI er anbefalt til issues og PR-er i workshopen. GitHub MCP eller nettleseren er alternativer
if (Has gh) {
    if (Succeeds { gh auth status }) { Ok "GitHub CLI innlogget" }
    else { Warn "GitHub CLI finnes men er ikke innlogget" "Kjør: gh auth login, eller bruk GitHub MCP/nettleseren" }
}
else { Warn "GitHub CLI mangler (anbefalt, GitHub MCP eller nettleseren er alternativer)" "https://cli.github.com/" }

# Aspire CLI er valgfritt: AppHost bygger med Aspire.AppHost.Sdk fra NuGet, men CLI gir «aspire run» og MCP-server
if (Has aspire) { Ok "Aspire CLI $((aspire --version 2>$null | Select-Object -First 1) -split '\+' | Select-Object -First 1)" }
else { Warn "Aspire CLI mangler (valgfritt, men trengs for Aspire MCP)" "https://aspire.dev/get-started/install-cli/" }

# Agent-harness. Claude Code er default i workshopen, men andre er lov
# 2.1.281 er første versjon som leser AGENTS.md i alle sesjoner, også med telemetri av
$claudeMin = [version]'2.1.281'
if (Has claude) {
    $claudeVersion = if ((claude --version 2>$null | Select-Object -First 1) -match '(\d+\.\d+\.\d+)') { [version]$Matches[1] } else { $null }
    if ($claudeVersion -and $claudeVersion -ge $claudeMin) { Ok "Claude Code $claudeVersion" }
    else { Warn "Claude Code $(if ($claudeVersion) { $claudeVersion } else { 'ukjent versjon' }) er eldre enn $claudeMin og leser kanskje ikke AGENTS.md" "Kjør: claude update" }
}
else { Warn "Claude Code mangler. Bruker du en annen harness er det greit" "https://code.claude.com/docs/en/quickstart" }

# dotnet-verktøy fra .config/dotnet-tools.json (dotnet-ef til migrasjoner)
if (Has dotnet) {
    if (Succeeds { dotnet tool restore }) { Ok "dotnet-verktøy gjenopprettet (dotnet ef)" }
    else { Fail "dotnet tool restore feilet" "Kjør: dotnet tool restore, og se feilmeldingen" }
}

Write-Host ""
if ($script:missing) {
    Write-Host "Noe mangler. Installer det som er rødt og kjør scriptet igjen." -ForegroundColor Red
    exit 1
}

Write-Host "Alt påkrevd er på plass." -ForegroundColor Green
Write-Host ""
Write-Host @"
Neste steg:

  dotnet run --project src/TronderLeikan.AppHost

Passord og nøkler genereres automatisk første gang og lagres i user secrets.
Første oppstart laster ned containere og initialiserer Zitadel, regn med 2-5 minutter.
Aspire-dashboardet åpnes i nettleseren (URL med token står i terminalen).

  Frontend:  lenken på «frontend» i dashboardet (porten velges av Aspire)
  Admin:     samme adresse + /admin
  Bruker:    zitadel-admin@zitadel.localhost
  Passord:   Password1!
"@
exit 0
