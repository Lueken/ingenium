# Ingenium roadmap notes

Living document. Updated 2026-08-07 after a design conversation with onions, author of Industrial
Story, on the VS Discord. Direct quotes are theirs or Venah's from that exchange.

## Where this sits

Shipped: the water wheel corrections (0.2.2), the proof of concept. Onions, unprompted:

> would be good if someone actually fixed vanilla mechanical system. its a mess

That is the market speaking, from the author of the largest industrial mod in the ecosystem.

## The compat doctrine, now externally confirmed

Onions on why they never touched the mechanical system:

> ripping out and replacing such a large vanilla system would be hellish with mod compat. even if
> you keep all the interfaces exactly the same all other mechanical mods (such as ppex which adds
> steam engines and such) rely on certain assumptions

This is the strongest argument yet for the posture Ingenium already has: surgical, vanilla-shaped,
behaviour-preserving outside the defect being fixed. The future torque/speed/power system must be
built the same way, extending vanilla's network rather than replacing it, because third-party
mechanical mods depend on vanilla's ASSUMPTIONS, not only its interfaces. This constraint is
permanent and non-negotiable.

## The risk-point inversion (the big one)

Venah, in the exchange, stating the direction:

> Right now the risk is zero load when its spinning at what ever geared up power comes from the
> source, shear and splintering would happen at clutch engagement at the helve. But you should
> need to gear down pre-clutch for it to not destroy anything.

Onions, completing it:

> sitting at a destructive speed passively should be basically impossible. because the load-bearing
> part last in the chain (for example the last small gear large gear interface) would splinter due
> to the force multiplication. so rather than the entire windmill randomly setting on fire, it
> would be that small gear breaking. also makes it very obvious what the limits of the parts are
> and where the problems in the design are, for players

Decoded into mechanics:

1. **Primary failure is mechanical, at load, at the weakest part.** Shear and splintering happen
   where force concentrates: gear interfaces, toggles, the final small-gear contact, at the moment
   load engages (clutch-in). Torque transmitted, not speed attained, is the risk variable.
2. **Passive overspeed stops being the headline killer.** With part stress modelled, a chain breaks
   at its weakest interface long before an unloaded shaft reaches exotic speeds. The realistic
   ceiling comes from parts, not from a speed constant.
3. **Failure is diagnostic by construction.** The broken part IS the report: it names the limit and
   the location of the design error. This is the same legibility principle the gross/net torque
   line follows, and the same lesson taken from Create: the value is a readable system.
4. **Fire remains, subordinated and material-keyed.** Fire becomes the failure mode of sustained
   friction where nothing shears (per-species ignition, pine before ebony), not a random penalty
   for speed. Consistent with the prior ruling that material properties are unaffected by player
   skill.

Consequence for the risk model already shipped in the wider pack: the overheat/fire work remains
correct as the vanilla-derived interim, and the long arc bends toward part stress as the primary
failure with fire as the wood-specific secondary.

## Material ladder (onions' guidance, accepted)

> historically speaking the vintage-story type mechanical power transmission infrastructure was
> made from cast iron once wood became too weak to work with. cast iron is actually pretty good as
> a gear or axle. try not to add wrought iron and steel large parts though, they dont really make
> sense. realistically speaking, assuming you rebalance everything properly, getting to
> wood-splintering torques with windmills or water wheels should be rather difficult

- **Wood tier**: gears and parts by species, hardness mirrored from In Dappled Groves, per-species
  ignition temperatures. Wood is the default and stays viable for wind/water power levels.
- **Cast iron tier**: the historical successor for gears and axles. Defeats fire to a point;
  fails by WARPING under sustained overstress rather than igniting, degrading effectiveness.
- **No wrought iron or steel large parts.** Historically wrong and mechanically unnecessary at
  wind/water torque levels. Exception: gearboxes and transmission internals.
- Wood-splintering torque should be HARD to reach from wind and water sources under proper balance,
  which keeps the wood tier honest rather than a trap.

## Collaboration: Industrial Story

Onions, on what they will build on top:

> whenever you get around to that I've had a system with industrial lubricants and cast iron
> mechanical parts in mind for eventual addition to industrialstory. I'm definitely gonna be
> adding a set of cast iron parts when that is out and working

Shape of the relationship: Ingenium provides the physics substrate and the part-stress model;
Industrial Story consumes it for lubricants and its own cast iron set. Same soft-integration
posture as the Almanac and Marginalia lines: one-way, optional, no hard dependency in either
direction. This obligates Ingenium to eventually expose a material-properties surface other mods
can feed, which reinforces the library instinct.

Also offered by Venah in the exchange: rolling a "big bellows fix" from onions into Ingenium
eventually, if they want to drop it in.

## The drag term (onions, later the same conversation)

> But also just a passive drag term on parts would do wonders for keeping speed in more realistic
> bounds. Adding more power gets diminishing returns on speed since part drag is speed squared and
> stuff. Operating more machines on more torque at a slower speed becomes the superior strategy vs
> speedmaxxing 1 helvehammer

**Vanilla already has this term.** `MechanicalNetwork.updateNetwork` adds
`speed^2 x gearedRatio^2 / 1000` per node per pass, verified in source and measured on the bench:
it is the hard ceiling that pulled a 166x chain from network speed 0.76 to 0.065, the reason no
source ever reaches its rated speed, and a component of the resistance figure the TCM panel
prints. Diminishing returns on speed are already vanilla arithmetic; vanilla simply never surfaced
it or keyed it to anything physical.

Ingenium's version therefore keeps vanilla's shape and replaces the anonymous coefficient with a
per-part, material-keyed one:

- Wood species drag by hardness (In Dappled Groves mirror)
- Cast iron drags less, warps instead of burning
- **Industrial lubricants reduce the drag coefficient.** This is the first concrete API surface
  the Industrial Story collaboration needs, and it fell directly out of onions' own suggestion.

The economy onions describes (more machines on more torque at lower speed beats speedmaxxing one
machine) is the same conclusion the pack's ENG work reached independently on 2026-08-05: the
reward is efficiency, not speed. Two designers converging on one economy from different directions.

## Standing todo, as publicly committed in that conversation

- Speed limits on machines (helve hammer named specifically)
- An actual torque/speed/power system, vanilla-shaped per the compat doctrine
- Additional gear orientations and transmission options
- Wood-species gears with per-species fire behaviour
- Metal (cast iron) gears and parts, warp-not-burn failure
- The risk-point inversion: shear at load engagement, gear down pre-clutch or temper the part
- Prior items: turbulence wake (design drafted), governor concept for mixed-source regulation
