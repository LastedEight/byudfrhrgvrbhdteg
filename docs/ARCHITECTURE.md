# Architecture

## The shape of it

```
Main (VTOLMOD)                      the only loader-facing file
  └── Bootstrap                     loader-agnostic startup
        └── DroneManager            one per session, survives scene loads
              ├── DroneInput        thumbsticks and keyboard → DroneInputState
              ├── FpvGoggles        visor parented to the HMD
              │     └── OsdRenderer text and geometry over the feed
              ├── HeadNotifier      status line for when the visor is up
              └── DroneController   the drone itself, built at deploy time
                    ├── DroneCameraRig   gimbal, camera, render texture
                    ├── DroneBattery     charge, load, voltage sag
                    ├── DroneLink        range, line of sight, latency
                    ├── DroneAutopilot   RTH, auto-land, failsafes
                    ├── DroneAudio       synthesised motor noise
                    └── PropSpinner ×4   cosmetic rotors
```

`GameBridge` sits to one side of all of it and is the only thing that talks to
VTOL VR.

## Decisions worth explaining

### Everything reaches the game by reflection

VTOL VR's `Assembly-CSharp` is not a stable API. Binding to it at compile time
means the mod stops loading outright the first time a field is renamed. Every
game-facing lookup instead goes through `Core/GameBridge.cs`, which finds types and
members by name, caches what it finds, and has a fallback and a one-time log warning
for each.

The cost is that a rename silently degrades a feature. The benefit is that it
degrades *one* feature: if `VRHandController.thumbstickAxis` moves, you lose stick
input and keep everything else, and the log says exactly what went missing.

### The goggles are parented to the head, not the cockpit

This is what makes them goggles. Once the screen is a child of the HMD transform,
looking around does not move the picture, both eyes composite it correctly with no
stereo trickery, and the hood blocks the cockpit because it is physically in the
way. A screen anchored in the cockpit would need head tracking compensation and
would still feel like a monitor.

The visor swings about a hinge above the eyes, so "flip up" is a real rotation onto
the forehead rather than a fade-out.

### The video feed is an ordinary camera and an ordinary render texture

`DroneCameraRig` renders into a `RenderTexture` and the goggle screen samples it.
Nothing about VR needs special handling — the screen is geometry in the world that
both eyes happen to be looking at.

Two details matter:

- The goggles live on an otherwise-unused rendering layer, which the drone camera
  masks out. Without that, looking back at your own aircraft gives you an infinite
  hall of mirrors. `LayerUtil` finds a free layer and adds it to the player's
  cameras' culling masks — rechecked periodically, because the game creates and
  swaps cameras during a flight.
- The drone camera is disabled whenever the visor is up. It is a full extra camera
  pass and there is no reason to pay for it when nobody is looking.

### All geometry is generated at runtime

No AssetBundle. A bundle has to be built against the exact Unity version the game
ships and breaks when that changes; `Util/AssetFactory.cs` and `DroneBuilder` make
the airframe, the goggles, the screen mesh and the video-static texture out of
primitives and procedural meshes instead. The motor noise is synthesised the same
way, from a blade-passing fundamental and its harmonics.

The visible cost is that the drone is a collection of boxes and cylinders. Through
a 1024-pixel FPV feed, it reads fine.

### The flight model applies one force

Lift is a single force along the airframe's up axis. That one choice produces most
of the handling: you cannot move horizontally without tilting, tilting costs you
lift (hence the tilt compensation term, and hence sinking in a hard turn), and
attitude control is therefore also your position control. Drag is per-axis and
quadratic, with the largest coefficient vertically because a quad presents its whole
rotor disc to vertical airflow.

The controllers are ordinary PIDs with integral clamping and derivative on the
measurement rather than the error, so a mode change or a stick step does not produce
a derivative spike.

Sign conventions, since they are easy to get backwards: torque about local X pitches
the nose **down**, about Y yaws **right**, about Z rolls **left**. The controller
negates pitch and roll commands accordingly.

### Input is a value type, so the autopilot is free

`DroneInputState` is one frame of pilot demand, normalised and source-agnostic. The
flight model consumes nothing else. `DroneAutopilot` therefore flies the drone by
synthesising the same structure — return-to-home is not a special case in the
physics, it is a second thing holding the sticks.

The same indirection gives you link latency for free: inputs go into a ring buffer
with timestamps, and the flight model reads the one that is `DroneLink.Latency` old.
At full signal that is 20 ms and imperceptible; at the edge of range it is 180 ms
and you feel it.

### Two failure modes are modelled, and they are the interesting ones

Range and terrain. `DroneLink` raycasts from the pilot to the drone four times a
second and collapses signal quality when anything is in the way. That single fact
is what makes flying behind a ridge a decision rather than a free lunch.

## Known limitations

- **Thermal and low-light are colour grades, not sensors.** Real thermal imaging
  needs a replacement shader, and Unity cannot compile a shader at runtime, so it
  would need an AssetBundle. `DroneCameraRig.ModeTint` is where a real one would
  plug in.
- **Wind comes from the game only if the game exposes it.** `GameBridge.GetWind`
  looks for a weather system by name and returns still air if it finds nothing.
- **Autopilot handoff is best-effort.** If `GameBridge.TrySetAutopilot` cannot find
  a recognisable autopilot, dropping the goggles warns you to mind your aircraft
  instead of silently leaving it uncontrolled.
- **The drone does not interact with the mission.** It is not a sensor the game
  knows about: it cannot lase targets, does not appear on radar, and AI will not
  react to it.
