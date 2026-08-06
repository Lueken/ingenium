# Changelog

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
