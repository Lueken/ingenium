# Changelog

## 0.1.0

### Fixed
- Water wheels no longer convert the rapids that power them into ordinary water. The base game's
  `ReplaceRapidWater` strips the `rapidwater` prefix from a nearby block's code, permanently
  removing a cell that qualified the wheel as powered. For worldgen `rapidwater-still-7` the loss
  cannot heal, because liquid spreading will neither raise a cell to source level nor drain one.
