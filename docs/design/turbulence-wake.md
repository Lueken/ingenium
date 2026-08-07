# Design draft: turbulence wake

Target: Ingenium 0.3. Status: DRAFT, not approved for build. Prepared 2026-08-07.

## Thesis

Vanilla's rapids demotion had a defensible idea buried in a broken implementation: a water wheel
extracts energy, so the water behind it is disturbed and a second wheel in that wake should get
little. Vanilla expressed this by permanently converting terrain, which also ate the wheel's own
supply, so `preserveRapids` removed the whole mechanism. This feature restores the idea as
arithmetic.

The one-line rule: **a wheel operating in another wheel's wake has its flowRate attenuated.
Computed every tick, written nowhere.**

## The rule

During `CheckWater`, after flowRate is computed and found positive, walk upstream from the hub
along the negative push vector of the qualifying flow, up to `wakeLength` blocks. If the walk
passes through another completed, operating water wheel occupying the same lane, multiply this
wheel's flowRate by `wakeFactor` before it feeds `TargetSpeed` and `TorqueFactor`.

Geometry does the discrimination for free:

- **The wake runs along the flow.** A serial-downstream wheel sits in the upstream wheel's lane and
  gets attenuated.
- **Coaxial stacking runs along the axle**, perpendicular to the flow. The upstream walk can never
  reach a coaxial neighbour, so R19's additive stacking is untouched with no special case.
- **Falls attenuate vertically.** A fall's upstream is straight up, so a wheel below another wheel
  in the same fall column is in its wake, which is physically right.

## Config

| Key | Default | Meaning |
|---|---|---|
| `turbulenceWake` | `true` | The feature switch |
| `wakeLength` | `10` | Blocks of disturbed water behind a working wheel |
| `wakeFactor` | `0.25` | flowRate multiplier inside a wake |

`wakeFactor` 0.25 rather than 0: a wheel in a wake still turns, weakly, which reads as "disturbed
water" rather than "arbitrary dead zone", and it keeps the failure legible in the tooltip rather
than looking like a broken wheel. Setting it to 0 or 1 is the server owner's call.

## Honest impact assessment

The owner's read, going in: probably not much more than flavour and continuity with the physics.
That read is mostly right, and it is worth writing down why it is still worth building.

**What it does not change.** Nobody's current build. Serial-downstream wheels effectively do not
exist in the wild, because vanilla's demotion destroyed the configuration before anyone could
profit from it. Every existing mill, including coaxial banks, reads identical numbers before and
after.

**What it actually does.** It closes the door `preserveRapids` opened, before players find it.
With demotion gone, one long worldgen rapids run can feed an unbounded chain of serial wheels at
full strength, which is free energy scaling with terrain luck rather than with engineering. The
wake caps that at roughly one strong wheel per `wakeLength` of run, which restores vanilla's
economy without vanilla's erosion. On a 200 mod server that runs for months, degenerate strategies
get found; pre-empting one cheaply is worth a small feature.

**The continuity argument.** The physics model this line of work is converging on (torque linear in
intercepted flow, lever arm in the radius term, speed capped by the prime mover, direction through
torque alone) has energy conservation as its missing piece. The wake is that piece at the cheapest
possible fidelity: energy extracted upstream is unavailable downstream. When the fuller
engineering mod arrives, this rule generalises to real flow attenuation instead of being thrown
away.

**Charter position.** This is Ingenium's first knowing addition rather than a pure correction, and
the ModDB page's freshly amended "does not add content (yet)" is the honest cover for it. The
defensible framing: it restores an intent the base game demonstrably had (the demotion targeted
the downstream stretch) with the implementation the intent deserved. It ships config-gated with
its own switch like everything else.

## Implementation notes for the build, when approved

- The walk is cheap: at most `wakeLength` block reads per wheel per second, only when flowRate is
  already positive. No new tick listeners; it rides inside the existing `CheckWater` patch point.
- "Operating wheel in the lane": a completed water wheel multiblock whose plane is parallel to
  this wheel's and within one block laterally, with nonzero flowRate last tick. Detecting
  "operating" without querying the other wheel's live state can be approximated by presence alone
  in v1, documented as such.
- Runs on both sides, like everything in CheckWater, so client prediction stays consistent.
- Tooltip line when attenuated, because a silently weak wheel is a support ticket: something in
  the register of `Turbulent water upstream, this wheel gets {0}% of the flow.` The line is the
  feature's legibility and should not be optional.
- Griefing surface: a hostile player parks a wheel upstream of a mill outside claim range.
  `wakeLength` 10 keeps the reach short, claims cover the normal case, and `wakeFactor` keeps the
  damage partial. Noted, accepted.

## Open questions for the owner, before build

1. `wakeLength` 10 and `wakeFactor` 0.25 are proposals. Better numbers?
2. Should a wheel's own wake also attenuate machines other than wheels? (Current answer: no,
   wheels only. Machines do not drink from the water.)
3. Default on for the public release, or default on only in The Quire's config? Draft assumes
   default on, with the README framing it as intent restoration.
