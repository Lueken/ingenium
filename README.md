# Ingenium

Mechanical power, corrected.

Ingenium fixes defects in Vintage Story's mechanical power simulation. It depends on nothing but the
base game.

That is deliberate. Every fix here is a candidate for upstream, and a fix that requires a
third-party mod is not one the developers can take.

## Scope

Ingenium corrects behaviour that is **inconsistent with itself**. It does not rebalance, it does not
add content, and it does not gate anything on player skill. Where the base game is merely not to
somebody's taste, Ingenium leaves it alone.

Every patch documents its own evidence in its source file: the decompiled code it corrects, what the
original does, what breaks without the fix, and how to observe it.

## What it fixes

### Water wheels destroy the rapids that power them

`BEBehaviorMPWaterWheel.ReplaceRapidWater` rebuilds a nearby block's code with the `rapidwater`
prefix stripped, so `rapidwater-e-7` becomes `water-e-7`. Ordinary water fails the
`flowspeed > requiresMinFlowSpeed` test that qualifies a cell as a power source, so every conversion
permanently removes one of the cells holding the wheel up.

For a level-7 source the loss cannot heal: liquid spreading refuses to raise a cell to source level
and refuses to drain one. Worldgen writes `rapidwater-still-7`, so worldgen rapids are exactly the
case that never comes back.

Symptoms: a wheel whose "suitable power source blocks nearby" count drifts downward, and several
identically built wheels reading different numbers because each is at a different point in eating
its own supply. It also makes build order matter, since a wheel built on existing rapids and a wheel
that had rapids routed to it later end up in different worlds.

Ingenium reproduces the boolean the caller consumes and skips the write. Nothing else about
`CheckWater` changes.

## Compatibility

Built and verified against **1.22.x**. The mechanical classes Ingenium patches were decompiled from
a live 1.22.5 server and a 1.22.3 client install and diffed: `BEBehaviorMPWaterWheel`,
`BEBehaviorMPRotor`, `BEBehaviorMPBase`, `MechanicalNetwork` and `BlockWindmillRotor` are all
identical between them.

**Universal.** `CheckWater` registers its tick listener with no side gate, so it runs on client and
server both, and the fix has to apply on both or a client will locally predict a conversion the
server never performs.

Ingenium patches one non-public method and touches none of the shared seams that every mechanical
block routes through. It does not patch `BEBehaviorMPBase.GetTorque`, `BEBehaviorMPRotor.GetTorque`
or `SetPropagationDirection`.

## Config

`ModConfig/ingenium.json`, written on first run.

| Key | Default | Effect |
|---|---|---|
| `preserveRapids` | `true` | Stop water wheels converting rapids into ordinary water |

Each fix ships behind its own switch. A correctness fix should still be something a server owner can
turn off without uninstalling, because the fix that misbehaves on somebody else's pack is the one
nobody can diagnose.

## Building

Set `VINTAGE_STORY` to a 1.22.x install, then `dotnet build -c Release`. Output lands in
`bin/Release/Mods/ingenium`.

## Licence

See LICENSE.
