# Compile check

Type-checks `src/DroneMod` on a machine with neither Unity nor VTOL VR installed.

```bash
sudo apt-get install mono-mcs
./check.sh
```

`stubs/` declares the UnityEngine and ModLoader members the mod uses, with
signatures matching the real ones. `check.sh` builds those into throwaway
assemblies and compiles the mod against them.

## What this proves, and what it does not

A clean run means the mod's sources are internally consistent: no typos, no
signature mismatches between its own pieces, no missing usings.

It does **not** mean the mod will load. The stubs are written from the documented
Unity and mod-loader APIs, not extracted from the real assemblies, so a member that
the real engine does not have would still type-check here. Building against the game's
own DLLs (see [../../docs/BUILDING.md](../../docs/BUILDING.md)) is the real check.

Nothing in this directory ships with the mod.
