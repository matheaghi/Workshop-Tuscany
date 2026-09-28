#!/usr/bin/env bash
# Sjekker at verktøyene TrønderLeikan trenger er på plass. Installerer ingenting utover dotnet-verktøy fra manifestet.
# Grønn linje = ok, rød linje = mangler (med lenke), gul linje = anbefalt men ikke påkrevd.
# Exit-kode 1 hvis noe påkrevd mangler.

set -u
cd "$(dirname "$0")"

GREEN=$'\033[32m'; RED=$'\033[31m'; YELLOW=$'\033[33m'; BOLD=$'\033[1m'; RESET=$'\033[0m'
missing=0

ok()   { printf '%s[OK]%s %s\n' "$GREEN" "$RESET" "$1"; }
fail() { printf '%s[MANGLER]%s %s\n   -> %s\n' "$RED" "$RESET" "$1" "$2"; missing=1; }
warn() { printf '%s[ANBEFALT]%s %s\n   -> %s\n' "$YELLOW" "$RESET" "$1" "$2"; }

printf '%sTrønderLeikan - sjekk av utviklingsmiljø%s\n\n' "$BOLD" "$RESET"

# .NET SDK 10
if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
  ok ".NET SDK 10 ($(dotnet --list-sdks | grep '^10\.' | tail -1 | cut -d' ' -f1))"
else
  fail ".NET SDK 10 mangler" "https://dotnet.microsoft.com/download/dotnet/10.0"
fi

# HTTPS-utviklersertifikat. Aspire-dashboardet kjører på https og nettleseren klager uten et klarert sertifikat
if command -v dotnet >/dev/null 2>&1; then
  if dotnet dev-certs https --check --trust >/dev/null 2>&1; then
    ok "HTTPS-utviklersertifikat er klarert"
  else
    warn "HTTPS-utviklersertifikatet er ikke klarert. Dashboardet gir sertifikatadvarsel" "Kjør: dotnet dev-certs https --trust"
  fi
fi

# Docker daemon
if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
  ok "Docker kjører ($(docker version --format '{{.Server.Version}}' 2>/dev/null))"
  mem_bytes=$(docker info --format '{{.MemTotal}}' 2>/dev/null || echo 0)
  if [ "${mem_bytes:-0}" -gt 0 ] && [ "$mem_bytes" -lt 4000000000 ]; then
    warn "Docker har under 4 GB minne ($((mem_bytes / 1024 / 1024)) MB). Zitadel kan bli treg ved første oppstart" "Øk minne i Docker Desktop -> Settings -> Resources"
  fi
else
  fail "Docker daemon svarer ikke" "https://www.docker.com/products/docker-desktop/ (start Docker Desktop)"
fi

# Node.js 24+ og npm. Frontend kjøres med npm av AppHost
if command -v node >/dev/null 2>&1 && command -v npm >/dev/null 2>&1 && [ "$(node -p 'process.versions.node.split(".")[0]')" -ge 24 ]; then
  ok "Node.js $(node --version), npm $(npm --version 2>/dev/null)"
else
  fail "Node.js 24 eller nyere med npm mangler" "https://nodejs.org/en/download (LTS)"
fi

# Git
if command -v git >/dev/null 2>&1; then
  ok "$(git --version)"
else
  fail "Git mangler" "https://git-scm.com/downloads"
fi

# GitHub CLI er anbefalt til issues og PR-er i workshopen. GitHub MCP eller nettleseren er alternativer
if command -v gh >/dev/null 2>&1; then
  if gh auth status >/dev/null 2>&1; then
    ok "GitHub CLI innlogget"
  else
    warn "GitHub CLI finnes men er ikke innlogget" "Kjør: gh auth login, eller bruk GitHub MCP/nettleseren"
  fi
else
  warn "GitHub CLI mangler (anbefalt, GitHub MCP eller nettleseren er alternativer)" "https://cli.github.com/"
fi

# Aspire CLI er valgfritt: AppHost bygger med Aspire.AppHost.Sdk fra NuGet, men CLI gir «aspire run» og MCP-server
if command -v aspire >/dev/null 2>&1; then
  ok "Aspire CLI $(aspire --version 2>/dev/null | head -1 | cut -d'+' -f1)"
else
  warn "Aspire CLI mangler (valgfritt, men trengs for Aspire MCP)" "https://aspire.dev/get-started/install-cli/"
fi

# Agent-harness. Claude Code er default i workshopen, men andre er lov
# 2.1.281 er første versjon som leser AGENTS.md i alle sesjoner, også med telemetri av
claude_min="2.1.281"
if command -v claude >/dev/null 2>&1; then
  claude_version=$(claude --version 2>/dev/null | head -1 | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' | head -1)
  if [ -n "$claude_version" ] && [ "$(printf '%s\n%s\n' "$claude_min" "$claude_version" | sort -V | head -1)" = "$claude_min" ]; then
    ok "Claude Code $claude_version"
  else
    warn "Claude Code ${claude_version:-ukjent versjon} er eldre enn $claude_min og leser kanskje ikke AGENTS.md" "Kjør: claude update"
  fi
else
  warn "Claude Code mangler. Bruker du en annen harness er det greit" "https://code.claude.com/docs/en/quickstart"
fi

# dotnet-verktøy fra .config/dotnet-tools.json (dotnet-ef til migrasjoner)
if command -v dotnet >/dev/null 2>&1; then
  if dotnet tool restore >/dev/null 2>&1; then
    ok "dotnet-verktøy gjenopprettet (dotnet ef)"
  else
    fail "dotnet tool restore feilet" "Kjør: dotnet tool restore, og se feilmeldingen"
  fi
fi

echo
if [ "$missing" -ne 0 ]; then
  printf '%sNoe mangler. Installer det som er rødt og kjør scriptet igjen.%s\n' "$RED" "$RESET"
  exit 1
fi

printf '%sAlt påkrevd er på plass.%s\n\n' "$GREEN" "$RESET"
cat <<'NEXT'
Neste steg:

  dotnet run --project src/TronderLeikan.AppHost

Passord og nøkler genereres automatisk første gang og lagres i user secrets.
Første oppstart laster ned containere og initialiserer Zitadel, regn med 2-5 minutter.
Aspire-dashboardet åpnes i nettleseren (URL med token står i terminalen).

  Frontend:  lenken på «frontend» i dashboardet (porten velges av Aspire)
  Admin:     samme adresse + /admin
  Bruker:    zitadel-admin@zitadel.localhost
  Passord:   Password1!
NEXT
