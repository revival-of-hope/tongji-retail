#!/usr/bin/env sh
set -eu

ROOT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
cd "$ROOT_DIR"

(
  cd backend
  dotnet restore test/RetailSystem.Api.Tests.csproj
  dotnet build test/RetailSystem.Api.Tests.csproj --configuration Release --no-restore
  git diff --exit-code -- openapi/retail-system.json
  dotnet test test/RetailSystem.Api.Tests.csproj --configuration Release --no-restore --no-build
)

(
  cd frontend
  corepack enable >/dev/null 2>&1 || true
  pnpm install --frozen-lockfile
  pnpm generate:api
  git diff --exit-code -- lib/api/generated
  pnpm lint
  pnpm typecheck
  pnpm test
  pnpm build
)
