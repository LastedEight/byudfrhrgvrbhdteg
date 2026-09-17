# Compatibility

This mod was written without a VTOL VR install to test against, so the assumptions
it makes about the game are listed here explicitly, along with what happens if each
one is wrong and how to confirm it.

Turn on **Verbose logging** in the mod's settings page first. Everything the mod
prints is prefixed `[FPVDrone]`, so in the game's `output_log.txt`:

```
findstr "[FPVDrone]" output_log.txt
```

## The loader API — `src/DroneMod/Main.cs`

This is the only file that references `ModLoader.dll`, and the only one that can
fail at **compile** time rather than degrading at runtime. It assumes:

| Assumption | If wrong |
| --- | --- |
| `VTOLMOD` base class with a virtual `ModLoaded()` | Build error; fix the signature |
| `VTOLAPI.SceneLoaded` event taking a `VTOLScenes` | Build error; or drop the subscription — the manager probes for a flight scene every two seconds anyway |
| `Settings` with `CreateCustomLabel` / `CreateBoolSetting` / `CreateFloatSetting` | Build error; the settings menu is optional, delete `BuildSettingsMenu()` and the defaults in `ModConfig` still apply |
| `VTOLAPI.CreateSettingsMenu(Settings)` | As above |

Scene names are matched as strings against a list of known non-flight scenes
(`Bootstrap.NonFlightScenes`), rather than against specific enum members, so a new
or renamed scene is treated as flyable rather than crashing the mod.

If the loader's API has moved on entirely, delete `Main.cs` and call
`DroneMod.Bootstrap.Initialise()` from whatever loader you are using.

## The game's runtime — `src/DroneMod/Core/GameBridge.cs`

All of these are looked up by name at runtime. Each has a fallback, and each logs a
warning exactly once if it fails.

| What | Looked up as | Fallback if missing |
| --- | --- | --- |
| HMD transform | `VRHead.instance` | `Camera.main.transform` — works, but may not track the head |
| Player aircraft | `VTOLAPI.GetPlayersVehicleGameObject()`, then a `FlightInfo` / `PlayerVehicleSetup` / `VehicleMaster` component, then the rigidbody above the head | Deploy refuses with "NO AIRCRAFT FOUND" |
| Thumbsticks | `VRHandController` (or `VRHand` / `VRController`), member `thumbstickAxis` / `stickAxis` / `thumbstick` / `joystickAxis` / `primary2DAxis` | Keyboard bindings only; logged once |
| Grip (gimbal modifier) | `gripAxis` / `gripValue` / `grip` / `gripPressed` | Gimbal is keyboard-only (`Q` / `E`) |
| Autopilot | `Autopilot` / `AutoPilot` / `AutopilotModule`, then `SetAutopilot(bool)`-style methods, then `autopilotEnabled`-style fields | Dropping the goggles warns "NO AUTOPILOT — MIND YOUR AIRCRAFT" |
| Wind | `WeatherSystem` / `WindManager` / `EnvironmentManager` singleton, member `windVector` / `wind` / `currentWind` | Still air |
| Flight scene | presence of `FlightSceneManager`, or a resolvable player vehicle | Manager waits and retries |

To check which of these resolved, look for `[FPVDrone] [trace]` lines such as
`Head transform resolved from VRHead.instance.` and for any `[FPVDrone]` warnings.

## Unity assumptions

These are stable API, but worth knowing about:

- `Rigidbody.velocity` — correct for Unity 2017 through 2022. Unity 6 renamed it to
  `linearVelocity`; if VTOL VR ever moves to Unity 6, `DroneController`,
  `DroneAutopilot`, `DroneLink` and `GameBridge` need that rename.
- Built-in shaders `Unlit/Color`, `Sprites/Default` and `Standard` must be present
  in the build. `AssetFactory` tries several candidates for each role and logs if
  none is found.
- `Font.CreateDynamicFontFromOSFont` with `Consolas` / `Courier New` /
  `DejaVu Sans Mono` / `Liberation Mono` / `Arial`. If none resolves, the OSD
  renders without text and everything else still works.
- A free rendering layer must exist for the goggles. `LayerUtil` walks layers 31
  down to 8 looking for an unnamed one; if the game has claimed all of them, the
  drone camera may see the goggles when it looks back at your aircraft.

## First-run checklist

1. Load into a flight scene. The log should show `Goggles ready.`
2. Press `F9`. The visor should swing down and black out the cockpit.
3. Press `F8` on the ground. The drone should appear off the right wing and the
   feed should come up.
4. Push the left stick up. The drone should climb.
5. Fly behind a hill. The picture should break up.

If step 2 works but step 3 does not, it is the player-vehicle lookup. If steps 2 and
3 work but the sticks do nothing, it is the thumbstick lookup — try the keyboard
fallback (`W` / `S` / `A` / `D` and the arrow keys) to confirm the rest is fine.
