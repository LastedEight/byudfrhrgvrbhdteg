#!/usr/bin/env bash
# Builds the FPV Drone mod on Linux or macOS (Proton / Whisky installs).
#
# The mod compiles against the game's own Unity assemblies, so VTOL VR has to be
# installed and reachable. Point VTOLVR_DIR at it, or pass it as the first
# argument:
#
#     ./build.sh "$HOME/.steam/steam/steamapps/common/VTOL VR"
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
vtol_dir="${1:-${VTOLVR_DIR:-}}"

if [[ -z "$vtol_dir" ]]; then
    for candidate in \
        "$HOME/.steam/steam/steamapps/common/VTOL VR" \
        "$HOME/.local/share/Steam/steamapps/common/VTOL VR" \
        "$HOME/Library/Application Support/Steam/steamapps/common/VTOL VR"
    do
        if [[ -d "$candidate/VTOLVR_Data/Managed" ]]; then
            vtol_dir="$candidate"
            break
        fi
    done
fi

if [[ -z "$vtol_dir" || ! -d "$vtol_dir/VTOLVR_Data/Managed" ]]; then
    echo "Could not find VTOL VR. Pass its path as the first argument, or set VTOLVR_DIR." >&2
    exit 1
fi

echo "==> VTOL VR: $vtol_dir"

mod_loader_dll="$(find "$vtol_dir" -name ModLoader.dll -print -quit 2>/dev/null || true)"
if [[ -z "$mod_loader_dll" ]]; then
    echo "ModLoader.dll not found under '$vtol_dir'. Install the VTOL VR Mod Loader first." >&2
    exit 1
fi

mod_loader_dir="$(dirname "$mod_loader_dll")"
echo "==> Mod loader: $mod_loader_dir"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "The .NET SDK is not installed. Get it from https://dotnet.microsoft.com/download" >&2
    exit 1
fi

echo "==> Building"
dotnet build "$root/src/DroneMod/DroneMod.csproj" -c Release \
    -p:VtolVrDir="$vtol_dir" \
    -p:VtolModLoaderDir="$mod_loader_dir" \
    --nologo

staged="$root/Builds/FpvDrone"
[[ -f "$staged/FpvDroneMod.dll" ]] || { echo "Build succeeded but $staged/FpvDroneMod.dll is missing." >&2; exit 1; }

destination="$mod_loader_dir/mods/FpvDrone"
mkdir -p "$destination"
cp -f "$staged"/* "$destination"/

echo "==> Installed to $destination"
echo
echo "Launch VTOL VR through the mod loader, enable \"FPV Drone\", then see docs/TESTING.md."
