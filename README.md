# FPV Drone — a VTOL VR mod

Carry a quadcopter in your aircraft, release it, and fly it yourself while watching
through FPV goggles that flip down over your eyes.

The drone is a separate aircraft with its own flight model, battery and radio link.
The goggles are a physical object parented to your headset: pull them down and the
cockpit disappears behind the video feed; flip them up and you are back in the jet.

## What it does

- **A real quadcopter.** Thrust along the airframe's up axis, per-axis drag, ground
  effect, wind, tilt-compensated hover. You cannot move without tilting, and tilting
  costs you lift, so it flies like a quad rather than like a free camera.
- **Four flight modes.** Acro (pure rate mode), Angle, Altitude hold, Position hold.
- **Head-mounted goggles.** A curved screen 14 cm from your eyes inside an opaque
  hood, with a hinge that swings the whole thing onto your forehead.
- **A camera that is actually a camera.** Two-axis gimbal, optional horizon
  stabilisation, zoom, and day / low-light / thermal modes.
- **A video link with range.** Signal quality falls off with distance and collapses
  when terrain gets between you and the drone. A weak link means picture breakup and
  control latency; losing it entirely hands the drone to the failsafe.
- **Battery endurance.** Power draw scales with thrust, voltage sags under load, and
  a critical battery triggers return-to-home or an auto-land.
- **An OSD** with the readouts a real one has: mode, battery, altitude, speed,
  heading, distance and bearing home, signal bars, artificial horizon, flight timer.
- **Autopilot handoff.** Dropping the goggles engages your aircraft's autopilot, and
  raising them puts the drone into position hold, so neither vehicle is ever being
  flown by nobody.

## Install

1. Install the [VTOL VR Mod Loader](https://vtolvr-mods.com/) and the
   [.NET SDK](https://dotnet.microsoft.com/download).
2. Run `build.cmd` (double-click) or `./build.sh` on Linux/macOS.
3. Launch through the mod loader and enable **FPV Drone**.

The build script finds your VTOL VR install and the mod loader, compiles the DLL
against the game's own assemblies, and copies it into the loader's `mods` folder.
If it cannot find the game, pass it explicitly:

```powershell
.uild.ps1 -VtolVrDir "D:\SteamLibrary\steamapps\common\VTOL VR"
```

**There is no prebuilt DLL, and there cannot be one.** The mod compiles against
`UnityEngine.dll` from `VTOLVR_Data\Managed` and `ModLoader.dll` from the mod
loader. Neither is redistributable, and a binary built against different versions
of them would not load. Building takes a few seconds once the SDK is installed.

Then work through [docs/TESTING.md](docs/TESTING.md) — it is ordered so that each
step isolates one subsystem, and says what a failure points at.

## Controls

Short version: `F8` deploys, `F9` flips the goggles down, then fly it with your
thumbsticks. Full bindings, including the gimbal modifier and the desktop fallback
keys, are in [docs/CONTROLS.md](docs/CONTROLS.md).

Everything is configurable from the mod's page in the in-game settings menu —
stick shaping, handling limits, battery life, link range, screen geometry, feed
resolution, camera field of view.

## Status and honesty about what is verified

Every source file here type-checks cleanly (`tools/compile-check/check.sh`), and
the physics, link and battery models are self-contained and reviewed. What has
**not** happened is a run inside VTOL VR itself — this was written without the game
or its assemblies to hand, so the parts that touch the game rather than Unity are
written defensively and are the parts to check first:

- The mod-loader entry point (`src/DroneMod/Main.cs`) is the only file that
  references `ModLoader.dll`. If the loader's API has moved, that one file is what
  needs adjusting, and `DroneMod.Bootstrap` can be started from any other injector.
- Everything that reaches into the game — the HMD transform, the player's aircraft,
  the VR thumbsticks, the autopilot — goes through `Core/GameBridge.cs` by
  reflection, with a fallback and a log warning for each. A member that has been
  renamed costs you one feature, not the mod.

See [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md) for the specific assumptions and
[docs/TESTING.md](docs/TESTING.md) for a checklist that confirms each one in order.

## Layout

```
src/DroneMod/
  Main.cs             Mod-loader entry point (the only loader-facing file)
  Bootstrap.cs        Loader-agnostic startup
  Core/               Manager, reflection bridge to the game, head-up messages
  Drone/              Airframe, flight model, autopilot, camera, battery, link
  Input/              Thumbstick and keyboard handling
  View/               Goggles and OSD
  Util/               Maths, PID, runtime asset generation, logging
build.ps1 / build.cmd Build and install on Windows
build.sh              Build and install on Linux and macOS
docs/                 Building, controls, testing, architecture, compatibility
tools/compile-check/  Type-checks the sources without Unity installed
```

## Licence

MIT. See [LICENSE](LICENSE).
