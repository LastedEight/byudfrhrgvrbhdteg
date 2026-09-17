# Testing checklist

This mod has never been run inside VTOL VR — it was written without the game
available. That makes the first run a real test rather than a formality, so this
checklist is ordered to isolate faults: each step exercises one subsystem, and the
notes say what a failure points at.

Turn on **Verbose logging** on the mod's settings page first. Everything it prints
is prefixed `[FPVDrone]`. The log is at:

```
%USERPROFILE%\AppData\LocalLow\Boundless Dynamics, LLC\VTOLVR\Player.log
```

Filter it with:

```powershell
Select-String -Path "$env:USERPROFILE\AppData\LocalLow\Boundless Dynamics, LLC\VTOLVR\Player.log" -Pattern "\[FPVDrone\]"
```

## 0. It loads

- [ ] The mod appears in the loader's list and can be enabled.
- [ ] The log shows `[FPVDrone] Configuration loaded.` and `[FPVDrone] Manager created.`

**If the mod does not appear at all:** `Info.json` is the suspect — the loader's
schema has changed across versions. Regenerate it with the loader's own project
tool, keeping the `Name`, `Description` and `Version` values.

**If it appears but nothing logs:** `ModLoaded()` is not being called, or it threw.
Look for a stack trace near the mod's name in the log.

## 1. The scene hooks up

- [ ] Load into any flyable mission.
- [ ] The log shows `[FPVDrone] Goggles ready.`

**If it never appears:** the head transform could not be resolved. Look for
`Head transform resolved from VRHead.instance.` (good) or the warning
`VRHead was not found; falling back to the main camera` (usable but the goggles may
not track your head). If neither shows, the flight scene was never detected —
check for `Flight scene detected by probe.`

## 2. The goggles

- [ ] Press `F9`. A visor swings down from above your eyes.
- [ ] With it down, the cockpit is hidden behind an opaque hood.
- [ ] Press `F9` again. It swings up onto your forehead and the cockpit returns.

**If nothing appears:** most likely the goggle layer. The mod puts its geometry on
an unused rendering layer and adds that layer to the player's cameras; look for
`Using layer N for the goggle geometry.` If instead you see `No free rendering
layer was available`, the game has claimed all 32 and the geometry may be culled.

**If it appears but is in the wrong place or does not follow your head:** the head
transform fell back to `Camera.main` (see step 1).

**If the screen is there but text is missing:** no OS font resolved. Look for the
warning `No OS font could be loaded`. Everything else still works.

## 3. Deploying

- [ ] Sitting on the ground, press `F8`. A small quadcopter appears off your right
      wing and you get a `DRONE DEPLOYED` message.
- [ ] With the goggles down, the screen shows the view from the drone.

**If you get `NO AIRCRAFT FOUND`:** the player-vehicle lookup failed. The log names
which strategy was tried.

**If the drone appears but the screen stays black or full of static:** the camera or
the render texture. Check for `Allocated video feed at WxH.`

**If you see an infinite hall of mirrors when the drone looks at your aircraft:** the
goggle layer is not being masked out of the drone camera — same cause as step 2.

## 4. Flying

- [ ] Push the left thumbstick up. The drone climbs.
- [ ] Left stick left/right yaws it; right stick pitches and rolls it.
- [ ] Centring the sticks in `ALT` mode holds altitude.
- [ ] `F11` cycles ACRO → ANGLE → ALT → POSN, shown top-left on the OSD.

**If the sticks do nothing but the keyboard works** (`W`/`S` throttle, `A`/`D` yaw,
arrow keys pitch and roll): the thumbstick lookup failed. Look for
`No VR thumbstick input available`. This is the single most likely thing to need
adjusting — see the table in [COMPATIBILITY.md](COMPATIBILITY.md) for the member
names tried.

**If it flies but feels wrong:** that is tuning, not a fault. Thrust-to-weight, max
tilt, yaw rate, stick expo and deadzone are all on the settings page.

**If it flips or oscillates:** the PID gains are wrong for your settings. Lower
thrust-to-weight first.

## 5. Systems

- [ ] Hold the right grip and the right stick moves the camera instead of the drone.
- [ ] `F10` cycles the camera tint (DAY → NV → IR on the OSD).
- [ ] Fly behind a ridge. The picture breaks up and the signal bars drop.
- [ ] Keep going until the link is lost. The drone starts `RTH` on its own.
- [ ] Let the battery run down. You get `BATTERY LOW`, then `BATTERY CRITICAL`, then
      a failsafe.
- [ ] `F12` toggles return-to-home manually.

**If the grip modifier does nothing:** grip lookup failed; use `Q`/`E` for the gimbal.

**If the picture never degrades with distance or terrain:** the line-of-sight raycast
is hitting something unexpected, or link range is set too high on the settings page.

## 6. Interaction with your aircraft

- [ ] Dropping the goggles gives either `AUTOPILOT ENGAGED` or
      `NO AUTOPILOT - MIND YOUR AIRCRAFT`.
- [ ] Raising the goggles while the drone is flying puts it into `POSN` hold.
- [ ] Deploying above 60 m/s is refused with `AIRSPEED TOO HIGH`.
- [ ] Deploying in flight releases the drone below and behind you, and it does not
      collide with your aircraft.

**`NO AUTOPILOT` is expected-but-degraded**, not a crash: the autopilot lookup is the
least certain thing in the mod. Your aircraft simply keeps doing whatever it was
doing while you are in the goggles.

## Reporting a problem

The useful details are: which step above failed, and every `[FPVDrone]` line from
the log. The warnings are written to name the exact lookup that failed.
