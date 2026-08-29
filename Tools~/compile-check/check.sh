#!/usr/bin/env bash
# Type checks the whole package without Unity installed.
#
#   ./Tools~/compile-check/check.sh
#
# Needs the .NET SDK: brew install dotnet
set -euo pipefail
cd "$(dirname "$0")"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
if [ -d /opt/homebrew/opt/dotnet/libexec ]; then
  export DOTNET_ROOT=/opt/homebrew/opt/dotnet/libexec
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found. Install it with: brew install dotnet" >&2
  exit 127
fi

dotnet build -v q --nologo
