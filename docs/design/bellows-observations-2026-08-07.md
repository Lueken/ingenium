# Big bellows: test observations, 2026-08-07

Evidence file, not a plan. The plan items derived from this live in [roadmap.md](roadmap.md)
(Standing todo, bellows migration). Recorded the day the pack replaced BellowsFix with thequire's
own BigBellowsPatch; all tests on Quire-Test-World, VS 1.22.6, thequire 0.1.27/0.1.28.

## Context: what the module is when it arrives here

Vanilla ships the big bellows (`bellows-large-*`, entity `MechPoweredBellows`) with shapes,
sounds, and a disabled recipe, but no air-moving code and no working animator init. Onions'
BellowsFix 1.0.3 supplied both; its stroke counter had three defects (reversed-branch starvation,
aliasing past ~12.6 rad/s, phantom 2pi deltas near standstill) plus a `Speed <= 0` gate that
killed consumers on negative-speed networks, all proven in server logs 2026-08-07. thequire's
replacement derives strokes from `|Network.Speed| * 50 * GearedRatio`, one stroke per pi, 0.2 air
per stroke, gate on `|Speed| < 0.001`. That part is settled and verified in production (blast
furnace 1400, small smelter 1200, windmill and water wheel networks, both rotation senses).

What is NOT settled is everything below. This is the open inheritance if the module migrates.

## Finding 1: CLOSED (2026-08-08). Not a mirror. A wrong constant in BellowsFix's patch.

The path-vs-path diff (there is only ONE shape file; "mechpoweredbellows" is an animator cache
key, not a shape) settled it deterministically:

- The static mesh is the raw shape plus the composite's `rotateY` (north 270, east 180, south 90,
  west 0). The animated path (`BlockEntityAnimationUtil.CreateMesh`, VSEssentials) loads the raw
  file and applies ONLY the rotation argument passed to `InitializeAnimator`.
- The correct argument is vanilla's own `-HorizontalAngleIndex * 90` (indices: E 0, N 1, W 2,
  S 3, from `vsapi/Math/BlockFacing.cs:46-58`), which reproduces the empirically needed yaw on
  all four facings.
- BellowsFix's rotation patch replaced that correct formula with `HorizontalAngleIndex * 90 +
  180`, which coincides with the correct value on north and south and is 180 degrees off on east
  and west. The "flip at placement" is the static mesh (right) handing off to the animated rig
  (wrong) on exactly those two facings.
- Answering the review's taxonomy: the difference is NOT negated element coordinates, NOT an
  inverted rotation origin, NOT UV mapping, and NOT animation keyframes (all keyframe rotations
  sit on fold/plate pivots, no whole-model yaw). It is a wrong constant in third-party code over
  an un-mirrored shape.
- Dissolved along the way: the earlier "mirror" formula `180 - rotateY` is numerically identical
  to `-HorizontalAngleIndex * 90` over the four facings, and the 0.1.28 shape-yaw attempt was
  numerically identical to BellowsFix's formula, which is why that experiment changed nothing.
  The E/W flip exists in BellowsFix 1.0.3 and thequire 0.1.27 stable alike; it was first NOTICED
  during the 0.1.28 test, not caused by it.

**Fix, one line**: pass `-facing.HorizontalAngleIndex * 90`. Shipped to the test world as
thequire 0.1.28 (stable base plus only this change, so the test isolates yaw). Awaiting in-game
verification on all four facings.

## Finding 2: the in-line drive check via the arm's `side` variant does not work

The perpendicular-drive rule (linkage arm must sit crank-fashion, across the blow direction) was
implemented as: read the block at `Pos.Down() + facing.Opposite`, take its `Variant["side"]`,
reject when that facing's Axis equals the bellows facing's Axis. In-game, an in-line arrangement
still delivered air: the check never fired.

Conclusion: the axlearm's `side` variant does not encode the axis this check assumed. Unknown
what it does encode (plausibly the direction toward its linkage crank, which is perpendicular to
its shaft). Next attempt should read the arm's actual power-input geometry, or the position of the
linkage (crank) block relative to the arm, rather than trusting the variant name. A debug-overlay
block code from a standing in-line build would settle the encoding in one reading.

## Finding 3: animation correctness depends on which side power enters. UNEXPLAINED.

Observed with rotors placed on either side of the linkage in turn, rotor rotation always
counter-clockwise: with the bellows facing north and power entering from the west side, the
animation runs correctly; entering from the other side, wrongly. Described by the tester as
"power coming in from the right of the bellows, if front is the way air blows, animations fine."

Air delivery is correct in every arrangement; this is rendering only. The suspected mechanism is
propagation direction determining the sign of the consumer's `AngleRad` and therefore the
animation's play direction, but the 0.1.28 experiment that forced the animation forward from
|speed| did NOT clear the tester's report, so the suspicion is unproven and something about the
mechanism is not understood.

**Probable shared root with Finding 1 (2026-08-08).** A bellows yawed 180 degrees has its hinge
at the nozzle end, so a correctly playing animation READS as pumping backwards relative to its
crank, and the impression would correlate with which side the observer approached from. If the
Finding 1 yaw fix clears this observation in the same test, Finding 3 dissolves and the migration
gates drop to one (Finding 2).

**This is the migration gate.** Until this dependence has an explanation, the module carries an
unexplained behaviour at its centre, and it stays in thequire.

## Status of the experimental fixes

thequire 0.1.27 (published): stroke model + inherited BellowsFix rendering, no drive restriction.
thequire 0.1.28 (test world only, re-cut 2026-08-08): stable base plus ONLY the vanilla yaw
formula, so the next test isolates yaw. The earlier 0.1.28 (shape-yaw, forward-forced animation,
axis-variant drive check) is retired: its yaw change was a numeric no-op, its drive check was
defeated per Finding 2, and its forward-forcing needs retesting only if Finding 3 survives the
yaw fix.
