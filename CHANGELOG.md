# Changelog

## 0.2.2

### Fixed
- Water wheels now turn with the water, the shaft follows rigidly, and which mirror variant was
  placed is cosmetic. This replaces both halves of 0.2.0/0.2.1's approach after an adversarially
  verified review found the 0.2.1 render change had severed the water coupling entirely: vanilla
  routes the water's direction through three couplings at once (the propagationDir rewrite, the
  torque sign, the render factor), and correcting any one in isolation moves the defect instead of
  removing it. The wheel is now a standard mechanical node: its propagationDir is frozen at the
  topology value, the water enters through the torque sign alone, and the render is the plain shaft
  angle. Proven across all eight facing, flow and network-direction cases.
- The 0.2.1 torque patch's polarity gate inverted all-mirrored wheel banks that vanilla handled
  correctly. The gate is now anchored to the water (dir below zero) rather than to the block
  variant, which fixes that regression and keeps mirrored pairs additive.
- Singleplayer no longer double-patches. Both sides share one process and one Harmony state, and
  the second application silently reverted the fixes while the log reported success.
- The torque frame restore is now exception-safe (Harmony finalizer), so a foreign patch throwing
  mid-call can never leave a wheel's frame flipped.

### Changed
- Water reversal is now a physical event. The instant direction flip vanilla performed on a water
  change is gone; the network decelerates through zero over a few seconds and re-settles with the
  water, with a brief client-side freeze at the crossing.

## 0.2.1

### Fixed
- Water wheels and the axles through them now turn the same way. The wheel was double counting its
  own direction: every other mechanical block renders `s(propagationDir) * networkAngle`, and
  `propagationDir` already encodes the flow direction, but the wheel multiplied by `dir` a second
  time on top of it. In unmodified vanilla this makes a wheel spin opposite to its own axle whenever
  `dir` is negative.

## 0.2.0

### Fixed
- Water wheels on one shaft in one current now add their torque instead of cancelling. A wheel's
  `side` variant fixed both an axle axis, which is physical, and an axis polarity, which is not.
  Vanilla hung the sensing tangent, the torque sign and the rendered spin off that polarity, so
  placing the mirror variant inverted all three. `BlockWindmillRotor.GetFacingForPlacement` writes
  `CodeWithVariant("side", val.Opposite.Code)` whenever a wheel is attached to an existing shaft,
  which forces the mirror and turns a supported build into a wheel that brakes its neighbours.
- Water wheels now visibly turn with the water rather than with the variant they were built on.
  Contrary to how it reads, `AngleRad => base.AngleRad * dir` does not make the render follow the
  water: the `* dir` factor cancels exactly against `IsRotationReversed`, leaving the rendered sense
  as `s(facing) * sign(network.Speed)`.

Genuinely opposing currents still oppose. Only opposition caused by which mirror was placed is
removed.

## 0.1.0

### Fixed
- Water wheels no longer convert the rapids that power them into ordinary water. The base game's
  `ReplaceRapidWater` strips the `rapidwater` prefix from a nearby block's code, permanently
  removing a cell that qualified the wheel as powered. For worldgen `rapidwater-still-7` the loss
  cannot heal, because liquid spreading will neither raise a cell to source level nor drain one.
