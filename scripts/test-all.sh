#!/usr/bin/env sh
set -eu

(
  cd frontend
  corepack enable >/dev/null 2>&1 || true
  pnpm install --frozen-lockfile
  pnpm generate:api
  pnpm lint
  pnpm typecheck
  pnpm test
  pnpm build
)

(
  cd backend
  dotnet restore test/RetailSystem.Api.Tests.csproj
  dotnet test test/RetailSystem.Api.Tests.csproj --configuration Release --no-restore
)
