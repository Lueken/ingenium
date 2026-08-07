# Upstream report: water wheel defects in Vintage Story 1.22

Draft for Anego Studios, prepared 2026-08-07. Intended for the official issue tracker, one issue
per section; the sections are written to stand alone. Everything below was diagnosed from
decompiled 1.22 assemblies, adversarially reviewed, and validated in game on a live 1.22.5 server
running roughly 200 mods. Line numbers are from ilspycmd 10.1 output against the 1.22.5
`VSSurvivalMod.dll`; the relevant classes are byte-identical between 1.22.3 and 1.22.5.

A working fix for all of it ships as the mod Ingenium (https://github.com/Lueken/ingenium), which
depends on nothing but the base game and was written so the approach could be taken upstream
directly. Patch-level implementation notes are in its source, one documented Harmony patch per
defect. Happy to answer questions, provide the full derivation, or PR against the real source if
that is useful.

---

## Issue 1: Mirrored water wheels on one shaft cancel instead of adding, and placement forces the mirror

**Summary.** Two water wheels attached to one shaft from opposite ends produce equal and opposite
torque in the same current and deadlock the shaft. The trigger is automatic: placement against an
existing mechanical connector selects the mirrored `side` variant, so the natural build order
(wheels onto an existing shaft) constructs the failing configuration, while placing both wheels
before the connecting axle avoids it. Players experience this as build-order superstition.

**Reproduction.** Creative world. Place an axle run, then attach a water wheel to each end (the
game auto-selects mirrored variants). Feed both wheels the same current. Observed: the shaft parks
near zero, tooltip torque roughly zero with full resistance. Rebuild with both wheels placed first,
then the axle: the same wheels add.

**Mechanism.** `BEBehaviorMPRotor.GetTorque` signs its output by a frame test,
`num = (propagationDir == OutFacingForNetworkDiscovery) ? 1 : -1` (rotor, line 86), and
`OutFacingForNetworkDiscovery` derives from the placement variant (line 54). `CheckWater` writes
`propagationDir = dir < 0 ? facing.Opposite : facing` (wheel, lines 228-232), which is
mirror-canonical (the double flip cancels), so mirrored wheels share one `propagationDir` but have
opposite `OutFacingForNetworkDiscovery`, giving opposite `num` from identical water. The network
sums contributions linearly with no frame correction. `BlockWindmillRotor.GetFacingForPlacement`
writes `CodeWithVariant("side", face.Opposite.Code)` whenever the wheel is placed against an
existing connector (line 67), which is what forces the failing variant pair.

The same root produces the inverse error: mixed variants in genuinely opposing currents compute
matching `num` and spuriously cooperate, when they should fight.

**Suggested fix.** Sign the torque from the water rather than from the variant frame: fold
`sign(dir)` into the frame test (equivalently, evaluate the test against `propagationDir.Opposite`
when `dir < 0`). The sign must enter the whole method, not the return value, because `num` also
selects the back-driven branch, which sets both the resistance term and the magnitude.

---

## Issue 2: ReplaceRapidWater permanently converts rapids to plain water, including the wheel's own power supply

**Summary.** A running water wheel converts nearby `rapidwater` blocks into plain `water` once a
second. Plain water fails the `flowspeed > requiresMinFlowSpeed` qualification, so every conversion
permanently removes a cell from the wheel's own power supply. The conversion never heals. Wheels
visibly decay over real-world weeks: the "suitable power source blocks nearby" count drifts down,
identical wheels read different numbers, and mills stop keeping up with nothing visible changed.

**Reproduction.** Build a wheel on worldgen rapids and leave it running; log
`suitablePowerSourceBlockCount` over a few hours of uptime. Fastest case: a wheel fed by a vertical
fall (`rapidwater-d-*`), which can convert its own qualifying sample cell (see mechanism).

**Mechanism.** `ReplaceRapidWater` (wheel, line 242) takes `block.Code.FirstCodePart()` and
rebuilds the code as `"water" + path.Substring(...)`, so `rapidwater-e-7` becomes `water-e-7`, then
`SetBlock`s it into the fluid layer. The guard `!behavior.multiplySpread` selects rapidwater and
nothing else, since `rapidwater.json` is the only blocktype setting `multiplySpread` false. The
loss is permanent at every liquid level: rapids refuse to spread into plain water, and for level-7
sources `CanSpreadIntoBlock` and `TryLoweringLiquidLevel` refuse to restore or drain a source.

Two aggravating details. First, the intent appears to be energy extraction downstream of the wheel,
but the probe chain also targets the cell below the wheel's own selected sample, so the wheel eats
its own supply, not only the downstream stretch. Second, `BlockPos.Add(float, float, float)`
truncates componentwise with an `(int)` cast, so for a vertical fall (push vector `(0, -0.006, 0)`,
normalised `(0, -1, 0)`) the horizontal probe offset `(n.X * 1.5, 0, n.Z * 1.5)` adds `(0, 0, 0)`
and the probe rewrites the sample cell itself.

**Suggested fix.** Remove the `SetBlock` (the boolean the caller consumes can be computed without
writing), or if the downstream energy-extraction intent matters, implement it as computed flow
attenuation rather than terrain conversion, and fix the truncating probe offset so a fall cannot
target its own sample.

---

## Issue 3: The water wheel's rendered rotation disagrees with its own axle, and can defy the water

**Summary.** A water wheel and the axle bolted through it can visibly rotate in opposite directions
indefinitely, and in half the flow cases do. The wheel's rendered sense and the axle's rendered
sense are derived from different sources that nothing reconciles.

**Reproduction.** Creative world. Build a single wheel with a current such that its water-derived
`dir` is negative (feed it from the flank that produces reversed flow for its variant), attach an
axle: the wheel and axle render counter-rotating while transmitting power normally.

**Mechanism.** Every mechanical block renders `s(propagationDir) x networkAngle`, where s is the
reversal set {DOWN, EAST, SOUTH} in `IsRotationReversed` (base, lines 106-117). The wheel alone
multiplies by `dir` on top (`AngleRad => base.AngleRad * dir`, wheel line 53). Because `CheckWater`
writes `propagationDir` from `dir` (lines 228-232), the composition collapses: the wheel renders at
sense `s(facing)`, effectively variant-anchored, while its axle renders at
`s(discovery-time propagationDir)`, a build-history constant that the wheel's flow-driven rewrite
never updates. The two agree or disagree based on build history and flow sign, not on anything
physical.

A related consequence worth noting: `CheckWater`'s `SetPropagationDirection` call passes
`gearingRatio 1`, and `SetPropagationDirection` assigns `GearedRatio` from the path, so every
flow-direction change resets a geared wheel's ratio to 1 until the next topology rebuild.

**Suggested fix.** Treat the wheel as a standard node. Stop rewriting `propagationDir` from the
flow, carry the water's direction exclusively through the torque sign (Issue 1's fix), and render
the plain shaft angle with no `dir` factor. Direction then reaches the wheel and every axle through
the network's signed speed, the same channel a windmill already uses, so wheel-with-water and
wheel-rigid-with-axle both hold by construction. This is the structure Ingenium implements, and it
was validated in game: wheels follow their water, reversing a watercourse produces a physical
deceleration through zero, mirrored pairs add, and genuinely opposed wheels fight to an honest
standstill.

---

## Context on scale, for prioritisation

The water wheel is the only mechanical power source whose drive direction comes from the world
rather than from its mounting, and all three issues trace to that one difference being threaded
through three couplings (the propagationDir rewrite, the variant-anchored torque frame, the render
factor) that can disagree. One structural decision, water speaks through torque alone, resolves all
three at once, which is why the suggested fixes are presented together.
