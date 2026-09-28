#!/usr/bin/env bash
# Nullstiller det lokale miljøet for TrønderLeikan: postgres-container, Aspire-nettverk, datavolum og Zitadel-bootstrap.
# Rører ikke andre prosjekters containere. Stopp AppHost før du kjører dette.

set -u
cd "$(dirname "$0")"

if pgrep -f "TronderLeikan.AppHost" >/dev/null 2>&1; then
  echo "AppHost kjører fortsatt. Stopp den (Ctrl+C) og kjør scriptet igjen."
  exit 1
fi

# Postgres-containeren identifiseres på volumet den monterer, ikke på navn
for c in $(docker ps -aq --filter "label=com.microsoft.developer.usvc-dev.mountsLabel=type=volume,src=leikan-postgres-data"); do
  echo "Fjerner container $(docker inspect "$c" --format '{{.Name}}' | sed 's|^/||')"
  docker rm -f "$c" >/dev/null
done

# Zitadel-containere er ikke persistente, men kan stå igjen etter en krasj
for c in $(docker ps -aq --filter "name=^zitadel-(api|login|proxy)-"); do
  echo "Fjerner container $(docker inspect "$c" --format '{{.Name}}' | sed 's|^/||')"
  docker rm -f "$c" >/dev/null
done

for n in $(docker network ls --format '{{.Name}}' | grep -E '^aspire-persistent-network-.*-TronderLeikan$'); do
  echo "Fjerner nettverk $n"
  docker network rm "$n" >/dev/null
done

if docker volume inspect leikan-postgres-data >/dev/null 2>&1; then
  echo "Fjerner volum leikan-postgres-data"
  docker volume rm leikan-postgres-data >/dev/null
fi

if [ -d src/TronderLeikan.AppHost/zitadel-bootstrap ]; then
  echo "Fjerner src/TronderLeikan.AppHost/zitadel-bootstrap/"
  rm -rf src/TronderLeikan.AppHost/zitadel-bootstrap
fi

echo
echo "Ferdig. Start på nytt med: dotnet run --project src/TronderLeikan.AppHost"
