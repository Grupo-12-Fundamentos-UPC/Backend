#!/usr/bin/env sh
set -eu

export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://0.0.0.0:${PORT:-8080}}"

exec dotnet HairyPaws.Api.dll
