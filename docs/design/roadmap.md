# Ingenium roadmap notes

Living document. Updated 2026-08-07 after a design conversation with onions, author of Industrial
Story, on the VS Discord, and amended the same day after external review (ordering, API commitment,
sign convention, charter). Direct quotes are theirs or Venah's from that exchange.

## Where this sits

Shipped: the water wheel corrections (0.2.2), the proof of concept. Onions, unprompted:

> would be good if someone actually fixed vanilla mechanical system. its a mess

That is the market speaking, from the author of the largest industrial mod in the ecosystem.

## Release ordering (ruled 2026-08-07)

**0.3 is the drag term**, not the wake. Re-keying vanilla's existing per-node coefficient is the
smallest code change with the largest behavioural payoff on this page: no new blocks, no new
persistent state, no UI, no charter exception, and it delivers onions' economy immediately (more
machines at more torque beats speedmaxxing one helve hammer). It also forces the
material-properties surface into existence, which everything downstream needs anyway.

Behind it, in order of evidence rather than ambition:

1. **0.3**: material-keyed drag + the material surface v0, published and versioned (see The API
   surface below).
2. **Flywheel + the stabilization bench.** Inertia is period-correct, ubiquitous, smaller than
   the governor, and may solve the transient problem alone. The bench decides flywheel-versus-
   governor priority empirically: the two mechanisms differ (flywheel is integrator gain
   reduction, governor is speed-opposed resistance), so the bench runs each ALONE and BOTH
   TOGETHER on the same rig, config-spawned invisible hooks, no blocks. Inertia plus regulation is
   the historically correct pairing, and discovering they interact badly after shipping one would
   be careless.
3. **Governor block**, only if the bench confirms the damping effect. If it does, it is Ingenium's
   signature feature and the charter covers it. If not, it is a resistance block with a slider and
   drops far down the list.
4. **Turbulence wake**, whenever. Its own honest assessment says mostly flavour plus one degenerate
   strategy pre-empted; that is real but it outranks nothing above it.

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

Design rulings (2026-08-07):

- **The material record is immutable and species-level**: base drag, hardness, ignition
  temperature, shear limit. The lubricant lives on the PART INSTANCE as a multiplier, and
  effective drag is computed at network build. If lubrication wrote into the material record, one
  greased bearing would mutate every part of the species, and the bug reports from a 200 mod
  server would be unreadable.
- **Base drag is a first-class field of the record**, not a constant one layer down. The lubricant
  multiplies it; burying it recreates the exact defect this work exists to fix.
- **Perf shape**: `updateNetwork` runs every 5 server ticks. Resolve each node's material key once
  at network build and cache it; a block-registry lookup per node per pass is how a performance
  mod ships a performance regression.
- **Coefficient floor (ruled 2026-08-08).** Drag is not only a coefficient, it is the only damping
  the solver has: the quadratic slope is part of what decides where the fixed-gain integrator
  hunts, and re-keying it downward for cast iron and lubricant removes stabilization nobody
  designed but every network has been relying on. Until the flywheel lands, the effective per-node
  coefficient is FLOORED at vanilla's current effective value, so 0.3 cannot make any existing
  network less stable than it is today. The floor is temporary by declaration, its exit condition
  is the flywheel shipping, and both facts go in the 0.3 release notes. Validation for 0.3 benches
  the hunting band before and after re-keying, and the measured band boundary is recorded HERE as
  a number (slot: ratio-spread boundary = TBD at the 0.3 bench), so the flywheel work has a target
  to beat rather than a vibe.

The economy onions describes (more machines on more torque at lower speed beats speedmaxxing one
machine) is the same conclusion the pack's ENG work reached independently on 2026-08-05: the
reward is efficiency, not speed. Two designers converging on one economy from different directions.

## The API surface (commitment, 2026-08-07)

The biggest architectural commitment on this page, promoted from a subordinate clause. Onions has
publicly said they will build cast iron parts and lubricants on top of this surface. The moment
they build against it, every refactor breaks the largest industrial mod in the ecosystem, which
inverts the compat doctrine and points it at Ingenium's own head. So the interface is defined
now, kept small, versioned, and treated as public from the first release rather than extracted
later from working code.

**The surface, v0**: the immutable species-level material record (species key, base drag,
hardness, ignition temperature, shear limit), the per-instance lubricant multiplier, and the
per-part warp accumulator, one lazily updated float, named in the surface because warp is cast
iron's entire failure mode and onions is building cast iron parts: leaving it implicit in v0 of a
public API guarantees a guess. Five record fields, one multiplier, one accumulator. Publishing
something that small early costs nothing and buys the right to rewrite everything behind it.

**Stability promise, in the 0.3 release notes verbatim**: unstable until 0.4, breaking changes
announced in the VS Discord thread. Onions decides when to start building against it, rather than
finding out from a broken mod.

**The sign convention ships as API design, not documentation.** Vanilla's `Network.Speed` is
signed and stays signed; R18 (Design D, shipped 0.2.2) made negative speed common and legitimate. Documented conventions get
violated by the next consumer who reaches for the obvious member name, so every new observable
Ingenium exposes is magnitude-first BY NAME: a consumer reading `Rate` or `Magnitude` gets an
unsigned value and cannot repeat the bellows failure (two consumers died on this exact reach in
one night, 2026-08-07, see the bellows observations file). Direction stays available under a name
that announces itself as directional.

**Regression canaries, scoped honestly.** thequire's `BigBellowsPatch` consumes
`|Network.Speed| * 50 * GearedRatio` and nothing else, so it is a SPEED-OBSERVABLE regression
check: any release it breaks has changed speed semantics. It never touches torque, drag,
materials, or part stress, which is most of this roadmap, so a torque-side equivalent gets built
separately, or the first refactor breaking torque semantics passes a green canary on the way out
the door.

## The governor

The machine that answers "constantly fighting to figure out which is on top." Historically the
regulator is older than steam: Thomas Mead patented the centrifugal governor for windmills in 1787,
a year before Watt hung one on an engine. Spinning weights fly outward with speed and actuate
something that sheds power. It exists precisely to arbitrate between varying power and varying load
without thrashing, which is the exact failure mode vanilla's network exhibits.

**The motivating problem.** Mixed prime movers on one shaft are legal, historically normal, and
stay that way. Vanilla's droop curves (torque falls linearly toward each source's cap) already
load-share correctly at steady state: the faster-capped source leads, the slower one drops out
above its cap and shoulders in as a reserve when load drags speed down. What ruins it in practice
is regulation, not physics. Vanilla has no inertia and no damping, its fixed-gain integrator hunts
at large ratio spreads and against steep droop slopes, and every transient (clutch-out, burned
stage, water reversal) is an unmanaged swing toward whatever ceiling the topology allows.

**The block.** A governor is a network part with a set speed, adjusted in game by interaction,
never by config file. Behaviour:

- At or below the set speed it contributes nothing but its own small resistance, an ordinary part.
- Above the set speed it adds resistance that grows progressively with the excess, a droop brake.
  The shaft cannot run away past its setting no matter what the sources do.

Three jobs fall out of that one rule:

1. **Overspeed protection.** The clutch-out surge, the cascade after a burned stage, and the
   reversal overshoot all die at the governor's setting. This is the engineering answer to the
   risk systems: gear down pre-clutch, temper the part, or fit a governor.
2. **Mixed-source arbitration.** A governed shaft holds a speed both sources can live with;
   surplus sheds in the governor instead of thrashing the integrator. Wind plus water becomes a
   managed hybrid rather than a tug of war.
3. **Damping the solver, hypothesis to bench.** The governor's progressive resistance has exactly
   the speed-opposing slope vanilla's fixed-gain integrator lacks. A governed network may be
   stable in the ratio-spread band where an ungoverned one hunts. If the bench confirms it, the
   governor is not only a machine, it is a workaround for a vanilla defect that ships as gameplay.

**Bench before block (ruled 2026-08-07, widened 2026-08-08).** The damping effect is the main
reason this block deserves a charter exception, and it is a hypothesis. It gets tested WITHOUT the
block, on the same rig as the flywheel's gain-reduction mechanism: each alone, then both together,
across the ratio-spread band where the ungoverned network hunts. The bench is no longer a
yes-or-no on the governor; it is the experiment that decides flywheel-versus-governor priority and
characterises their interaction before either ships. Overspeed protection and mixed-source
arbitration are real but ordinary machine features; solving a vanilla solver defect through
gameplay is the headline, and the headline gets proven before anything gets built. See Release
ordering.

**Charter position.** Covered by the amended charter below without a fresh justification.

Complementary to the drag term: drag is passive and material-keyed, the governor is active and
player-set. Between them, speed lives in realistic bounds for two different reasons, physics and
regulation, which is how real mills did it. And between them sits the flywheel: mass smooths the
swing, the regulator holds the setpoint, exactly as it paired historically.

## The charter, amended (2026-08-07)

The old rule, "does not add content (yet)", was already conceding its first exception in the
parenthetical, and charters die exactly that way, one earned exception at a time. Amended on our
own terms:

> **Ingenium corrects vanilla mechanical behaviour and adds only the parts required to make the
> corrected physics playable.**

That covers the governor, the flywheel, bearings, and wood-species gears without a fresh
justification each time, and it still excludes the content sprawl the rule was written to prevent.
Propagated 2026-08-08 to all public surfaces: README, ModDB page draft, and the wake doc's charter
citation.

## Part-stress state cost (ruled 2026-08-07)

Shear at load engagement implies every gear interface knows its transmitted torque and its
material limit. Both are DERIVED at network update, never stored. Fatigue accumulation, if wanted,
is one float per part with lazy update, and the imprecision is accepted. A mechanical network on a
200 mod server with months of uptime has thousands of nodes; per-part accumulators are save bloat
and a tick budget problem in a mod whose selling point is fixing performance-adjacent defects.

## Two adopted gaps (2026-08-07)

- **Flywheel.** Vanilla has neither inertia nor damping; drag addresses damping and left inertia
  alone. A flywheel is period-correct, ubiquitous in real mills, smaller than the governor, and
  solves the transient problem the governor was carrying by itself. Scheduled ahead of the
  governor block; see Release ordering.
- **Bearings.** The material ladder rules out wrought iron and steel LARGE parts, correctly, but
  bearings are the historically right place for hard metal, and they are where lubricant
  physically lives. Bearings give the Industrial Story lubricant integration a concrete home
  instead of a coefficient floating on a gear.

## Handbook debt

The no-steel-gears ruling reads as arbitrary gating to a player who has climbed to steel, unless
the handbook says why: cast iron casts into complex tooth geometry cheaply and performs well in
compression; wrought iron would need every tooth forged. That sentence goes in game, or it gets
answered in Discord forever.

## Standing todo, as publicly committed in that conversation

- Speed limits on machines (helve hammer named specifically)
- An actual torque/speed/power system, vanilla-shaped per the compat doctrine
- Additional gear orientations and transmission options
- Wood-species gears with per-species fire behaviour
- Metal (cast iron) gears and parts, warp-not-burn failure
- Bearings as the hard-metal, lubricant-bearing part (adopted 2026-08-07)
- Flywheel: network inertia, scheduled ahead of the governor (adopted 2026-08-07)
- The risk-point inversion: shear at load engagement, gear down pre-clutch or temper the part
- The no-steel-gears handbook sentence
- Prior items: turbulence wake (design drafted, rescheduled behind drag), governor block (design
  above, gated on the bench)
- Re-run the five-class mechanical diff against 1.22.6 before the 0.3 release notes go out; the
  test world already runs 1.22.6 while the compatibility claim is verified against 1.22.3 and
  1.22.5 only, and until the diff passes, release notes state the versions actually verified.
- **Big bellows migration from thequire**, gated. Status per the evidence file, 2026-08-08: the
  yaw mystery (its Finding 1) is CLOSED on a real model, vanilla's own
  `-HorizontalAngleIndex * 90`, which BellowsFix replaced with a constant wrong on east and west;
  the one-line fix is on the test world awaiting four-facing verification. Two gates remain: the
  in-line drive check (its Finding 2, the arm's `side` variant does not encode the assumed axis)
  and the power-entry-side animation dependence (its Finding 3, unexplained, probable shared root
  with Finding 1; if the yaw verification clears it, the gates drop to Finding 2 alone). Code
  with an unexplained behaviour at its centre does not migrate into a mod whose pitch is
  legibility. Evidence:
  [bellows-observations-2026-08-07.md](bellows-observations-2026-08-07.md)
