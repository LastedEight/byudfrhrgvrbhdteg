#!/usr/bin/env bash
# Type-checks the mod sources without Unity or VTOL VR installed.
#
# It compiles src/DroneMod against the stub assemblies in ./stubs, which declare
# the UnityEngine and ModLoader surface the mod uses with matching signatures.
# A clean run means the mod is internally consistent; it does NOT prove the real
# engine and loader still expose those members. See README.md in this directory.
#
# Requires: mcs (Mono C# compiler). On Debian/Ubuntu: apt-get install mono-mcs
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd "$here/../.." && pwd)"
out="$here/out"

if ! command -v mcs >/dev/null 2>&1; then
    echo "mcs not found. Install the Mono C# compiler (apt-get install mono-mcs)." >&2
    exit 1
fi

mkdir -p "$out"

echo "Building stub assemblies..."
mcs -target:library -out:"$out/UnityEngine.dll" "$here/stubs/UnityEngine.Stubs.cs"
mcs -target:library -out:"$out/ModLoader.dll" -r:"$out/UnityEngine.dll" "$here/stubs/ModLoader.Stubs.cs"

echo "Type-checking the mod..."
mapfile -t sources < <(find "$root/src/DroneMod" -name '*.cs')
mcs -target:library -out:"$out/DroneMod.dll" \
    -r:"$out/UnityEngine.dll" -r:"$out/ModLoader.dll" \
    -warnaserror- \
    "${sources[@]}"

echo "OK: $(printf '%s\n' "${sources[@]}" | wc -l) source files type-checked."
