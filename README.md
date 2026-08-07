# Ingenium

Mechanical power, corrected.

Ingenium fixes defects in Vintage Story's mechanical power simulation. It depends on nothing but the
base game.

That is deliberate. Every fix here is a candidate for upstream, and a fix that requires a
third-party mod is not one the developers can take.

## Scope

Ingenium corrects behaviour that is **inconsistent with itself**. It does not rebalance, it does not
add content (yet), and it does not gate anything on player skill. Where the base game is merely not to
somebody's taste, Ingenium leaves it alone.

Every patch documents its own evidence in its source file: the decompiled code it corrects, what the
original does, what breaks without the fix, and how to observe it.

## What it fixes

### The water decides which way a wheel turns

Vanilla routes the water's direction through three couplings at once: `CheckWater` rewrites the
wheel's `propagationDir` from the flow, `GetTorque` signs its output from the placement variant's
frame, and `AngleRad` multiplies the render by the flow direction on top of both. The three
disagree, and the tiebreakers are the mirror variant placed, network discovery order, and shaft
build history. The visible failures: a mirrored wheel spins against its own bank and brakes it, a
wheel and the axle through it turn opposite ways, banks produce full power or almost none depending
on assembly order, and a wheel can render turning against the water indefinitely.

Vanilla also forces the worst case: `BlockWindmillRotor.GetFacingForPlacement` writes the mirrored
`side` variant whenever a wheel is placed against an existing shaft, so the obvious build order
constructs a machine that fights itself.

Ingenium makes the wheel a standard mechanical node. Its `propagationDir` is frozen at the value
network topology gives it (only `CheckWater`'s self-write is suppressed, identified by an
in-CheckWater marker AND the path carrying the wheel's own position). The water enters the
simulation in exactly one place: the torque sign, swapped for the duration of `GetTorque` when
`dir` is negative, restored by a finalizer. The render is the plain shaft angle.

Proven across all eight facing, flow and network-direction cases, then validated in game: wheels
turn with their water, the shaft follows rigidly through every transient, mirror variants are
cosmetic, coaxial wheels add torque linearly, and genuinely opposing currents fight to an honest
standstill (vanilla let some opposing pairs spuriously cooperate).

Deliberate behaviour change: water reversal is a physical event now. The network decelerates
through zero over a few seconds and re-settles with the water, instead of flipping instantly.

### Water wheels destroy the rapids that power them

`BEBehaviorMPWaterWheel.ReplaceRapidWater` rebuilds a nearby block's code with the `rapidwater`
prefix stripped, so `rapidwater-e-7` becomes `water-e-7`. Ordinary water fails the
`flowspeed > requiresMinFlowSpeed` test that qualifies a cell as a power source, so every conversion
permanently removes one of the cells holding the wheel up. The loss never heals: rapids refuse to
spread into plain water at any level, and the column above feeds the demoted cell forever. Falls
suffer worst, since coordinate truncation in the probe geometry makes a vertical column trigger the
conversion against the wheel's own supply.

Symptoms: a wheel whose "suitable power source blocks nearby" count drifts downward, and identically
built wheels reading different numbers because each is at a different point in eating its own
supply.

Ingenium reproduces the boolean the caller consumes and skips the write. Nothing else about
`CheckWater` changes.

## Compatibility

Built and verified against **1.22.x**. The mechanical classes Ingenium patches were decompiled from
a live 1.22.5 server and a 1.22.3 client install and diffed: `BEBehaviorMPWaterWheel`,
`BEBehaviorMPRotor`, `BEBehaviorMPBase`, `MechanicalNetwork` and `BlockWindmillRotor` are all
identical between them.

**Universal.** `CheckWater` runs on client and server both, so every patch applies on both sides or
the two would disagree about the world.

Five methods are patched. Each either belongs to the water wheel itself (`ReplaceRapidWater`,
`CheckWater`, `get_AngleRad`) or is instance-gated so everything that is not a water wheel passes
through untouched (`BEBehaviorMPRotor.GetTorque`, where windmills, creative rotors and modded
rotors are ignored because their spin genuinely is mounting-determined, and
`BEBehaviorMPBase.SetPropagationDirection`, where exactly one write is suppressed, the wheel's own
flow-driven self-update, leaving discovery, rebuilds and merges alone for every block in the game).

Singleplayer is guarded against double-patching (both sides share one Harmony state), and the
torque frame restore is exception-safe, so a foreign patch throwing mid-call cannot corrupt a
wheel's frame.

## Config

`ModConfig/ingenium.json`, written on first run.

| Key | Default | Effect |
|---|---|---|
| `freeFloatingWheels` | `true` | The water decides rotation, the shaft follows rigidly, variants are cosmetic |
| `preserveRapids` | `true` | Stop water wheels converting rapids into ordinary water |
| `debugLogging` | `true` | Log what each fix is doing |

Each fix ships behind its own switch. A correctness fix should still be something a server owner can
turn off without uninstalling, because the fix that misbehaves on somebody else's pack is the one
nobody can diagnose.

## Building

Set `VINTAGE_STORY` to a 1.22.x install, then `dotnet build -c Release`. Output lands in
`bin/Release/Mods/ingenium`.

## Licence

See LICENSE.
