# Building

## What you need

- The [.NET SDK](https://dotnet.microsoft.com/download) (6.0 or newer), or Visual
  Studio / Rider.
- VTOL VR installed.
- The [VTOL VR Mod Loader](https://vtolvr-mods.com/) installed, for `ModLoader.dll`.

The project targets `net472`, which is what the game's Mono runtime expects. On
Linux and macOS the `Microsoft.NETFramework.ReferenceAssemblies` package supplies
the reference assemblies, so a cross-platform build works as long as you can reach
the game's `Managed` folder.

## Build

The quickest path is the script, which finds the game and the mod loader for you
and installs the result:

```powershell
.\build.ps1                 # Windows; build.cmd is a double-clickable wrapper
```

```bash
./build.sh                   # Linux / macOS
```

To drive MSBuild yourself:

```bash
dotnet build -c Release -p:VtolVrDir="C:\Program Files (x86)\Steam\steamapps\common\VTOL VR"
```

You can also set a `VTOLVR_DIR` environment variable, or edit the `VtolVrDir`
default at the top of `src/DroneMod/DroneMod.csproj`.

The build stages the result at `Builds/FpvDrone/`:

```
Builds/FpvDrone/
  FpvDroneMod.dll
  Info.json
```

Copy that folder into the mod loader's `mods` directory, launch through the loader,
and enable **FPV Drone**.

## What the project references

- `UnityEngine.dll` and the `CoreModule`, `PhysicsModule`, `AudioModule`,
  `TextRenderingModule` and `InputLegacyModule` assemblies from the game's
  `VTOLVR_Data/Managed` folder. Each is referenced conditionally, so both the old
  single-assembly and the modern split layouts work.
- `ModLoader.dll` from the mod loader.

It deliberately does **not** reference `Assembly-CSharp.dll`. Everything the mod
needs from the game goes through reflection in `Core/GameBridge.cs`, so a game
update that reshapes its internals does not stop the mod loading.

## About `Info.json`

The mod loader reads this file to list the mod. The field names here follow the
loader's published format, but the loader's schema has changed across versions — if
it refuses to list the mod, regenerate `Info.json` with the loader's own project
creation tool and keep the `Name`, `Description` and `Version` values from this one.

## Type-checking without Unity

`tools/compile-check/check.sh` compiles the sources against stub declarations of
the UnityEngine and ModLoader surfaces, so you can catch a typo without a game
install:

```bash
sudo apt-get install mono-mcs      # or use any C# compiler
tools/compile-check/check.sh
```

This proves the mod is internally consistent. It does not prove that the real
engine and loader still expose those members — see
[COMPATIBILITY.md](COMPATIBILITY.md).

## Using a different loader

`src/DroneMod/Main.cs` is the only file that references the mod loader. To run the
mod under BepInEx or any other injector, delete it and call:

```csharp
DroneMod.Bootstrap.Initialise();
```

once at startup, then `DroneMod.Bootstrap.NotifySceneChanged(sceneName)` on each
scene change. The manager also probes for a flight scene on its own every two
seconds, so even the scene callback is optional.
