# Controls

## Hotkeys

| Key | Action |
| --- | --- |
| `F8` | Deploy the drone / recover it |
| `F9` | Flip the goggles down / up |
| `F10` | Cycle camera mode (day → low light → thermal) |
| `F11` | Cycle flight mode (acro → angle → altitude hold → position hold) |
| `F12` | Return to home (press again to cancel) |
| `Home` | Recentre the gimbal and reset zoom |

All of these are fields on `ModConfig` and can be changed in source; the in-game
settings menu covers the analogue values rather than the key bindings.

## Flying it

The drone answers the sticks **only while the goggles are down**. With the visor up
your hands belong to the aircraft, which is the point of the goggles being a
physical object rather than a toggle.

### Mode 2 (default)

| Control | Axis |
| --- | --- |
| Left thumbstick, up/down | Throttle (climb rate in altitude hold, motor power in acro/angle) |
| Left thumbstick, left/right | Yaw |
| Right thumbstick, up/down | Pitch |
| Right thumbstick, left/right | Roll |

Mode 1 swaps the two vertical axes; turn off "Mode 2" in the settings menu.

### Camera

Hold the **right grip** and the right thumbstick moves the camera instead of the
drone: up/down tilts the gimbal, left/right pans it. Release the grip and the stick
goes back to flying.

### Desktop fallback

Useful for testing outside a headset, and additive with the controllers.

| Key | Action |
| --- | --- |
| `W` / `S` | Throttle up / down |
| `A` / `D` | Yaw left / right |
| Arrow up / down | Pitch forward / back |
| Arrow left / right | Roll left / right |
| `Q` / `E` | Gimbal up / down |
| `Page Up` / `Page Down` | Zoom in / out |

## Flight modes

- **ACRO** — rate mode. The sticks command angular rates and nothing self-levels.
  Throttle is absolute: the stick adds to and subtracts from a motor setting that
  stays where you leave it.
- **ANGLE** — the sticks command bank and pitch angle, capped at the configured
  maximum tilt. Centre them and the drone levels itself. Throttle is still absolute.
- **ALT** — angle mode plus altitude hold. The throttle stick becomes a climb-rate
  command; centre it and the drone holds the height it is at.
- **POSN** — altitude hold plus position hold. Let go of everything and it parks,
  leaning into the wind to stay there.

Raising the goggles while the drone is flying in acro or angle mode drops it into
position hold automatically, so it does not fly away while you are looking at your
instruments. Turn that off in the settings if you would rather it kept going.

## Deployment

Press `F8` on the ground and the drone appears off your right wing. Press it in
flight and the drone is released below and behind you, matched to your velocity.

Above 60 m/s the launch is refused — a 780 g quadcopter does not survive being
dropped into a 120-knot slipstream. Raise the limit, or switch on "Allow launch at
any airspeed", in the settings menu if you want to try anyway.

## Failsafes

- **Link lost** for more than 1.5 s → return to home (climb to 60 m above the launch
  point, track home, descend and land).
- **Battery critical** (8%) → return to home if it is close enough to make it,
  otherwise land immediately.
- **Battery flat** → the motors stop. It falls.
- Getting the link back cancels a link failsafe; a battery failsafe stands.
