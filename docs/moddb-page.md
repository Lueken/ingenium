# ModDB page draft: Ingenium

Paste-ready. Page body first, then the release notes for 0.2.2, the debut public release. Internal
versions 0.1.0 through 0.2.1 were never posted and are not referenced.

**Suggested metadata**

| Field | Value |
|---|---|
| Name | Ingenium |
| Summary | Mechanical power, corrected. |
| Side | Universal |
| Game versions | 1.22.0 and up |
| Tags | Tweak, QoL, Utility |
| Dependencies | none |

---

## Page body

### Mechanical power, corrected

Ingenium fixes defects in Vintage Story's mechanical power simulation. It depends on nothing but
the base game.

That is deliberate. Every fix here is written to be something the developers could take upstream,
and a fix that requires a third-party mod is not one they can take.

### What it does not do

Ingenium corrects vanilla mechanical behaviour and adds only the parts required to make the
corrected physics playable. It does not rebalance anything, and it does not gate anything behind
player skill. Where the base game is merely not to somebody's taste, Ingenium leaves it alone.

Nothing here changes how much power a wheel makes or what any machine costs to run. What changes is
that the machinery now does what it visibly claims to do.

---

### The water decides which way a wheel turns. Now it actually does.

**The symptoms.** A wheel built as the mirror variant spins against its own bank and drags it down.
A bank of wheels produces full power or almost none depending on the order you placed the parts,
which reads as superstition. A wheel and the axle bolted through it turn opposite ways. And a wheel
can turn against the water that drives it, on screen, indefinitely.

**The cause.** The base game routes the water's direction through three separate couplings at once:
the flow check rewrites the wheel's network direction, the torque code signs its output from the
placement variant, and the render multiplies by the water direction on top of both. The three do
not agree. Which one wins depends on which mirror variant you placed, the order the network
discovered its parts, and the build history of the shaft. None of those are the water.

The game also forces the worst case on you: attaching a wheel to an existing shaft automatically
places the mirrored variant. A bank assembled in the obvious order, wheels onto shaft, builds a
machine that fights itself. A bank assembled wheels-first works. Nobody could tell you why.

**What Ingenium does.** The wheel becomes a standard mechanical part, like a windmill. The water's
direction enters the simulation in exactly one place, the torque, and everything else follows from
ordinary shaft mechanics.

The results, each verified in game:

- **A wheel turns the way its water pushes it.** Move the water to the other side and the wheel
  slows, stops, and reverses.
- **The wheel and its shaft are rigid.** Axles, gears and machines follow the wheel exactly,
  through every transient, back-driving included.
- **The mirror variant is cosmetic.** Wheels on one shaft in one current pull together whichever
  way each was placed, and their torque adds linearly. Four wheels, four times the stall torque.
- **Wheels in genuinely opposing currents fight, as they should.** Two equal wheels facing opposite
  flows lock the shaft at a standstill. The base game let some of those pairs cooperate, which was
  wrong in the other direction.

**One behaviour change to know about.** Reversing a watercourse used to flip the machine instantly.
It is now a physical event: the network decelerates through zero over a few seconds and re-settles
with the water. A shaft with load behaves like a thing with load.

---

### Your water wheels are eating the river

**The symptom.** A wheel's "suitable power source blocks nearby" count drifts downward over weeks.
Wheels built identically, side by side, report different numbers. A mill that used to keep up
slowly stops keeping up, and nothing you can see has changed.

**The cause.** Once a second, a running wheel converts a nearby rapids block into ordinary water.
Ordinary water does not qualify as a power source, and the conversion never heals: rapids will not
spread back into plain water, so every converted cell is a permanent hole in the wheel's power
supply. Wheels fed by falls suffer worst, because a quirk in the probe geometry makes a falling
column trigger the conversion against the wheel's own supply.

**What Ingenium does.** The wheel stops writing to the world. Nothing about how it measures flow
changes.

This stops further loss. It cannot restore rapids already gone.

---

### Configuration

`ModConfig/ingenium.json`, written on first run.

| Key | Default | Effect |
|---|---|---|
| `freeFloatingWheels` | `true` | The water decides rotation, the shaft follows rigidly, variants are cosmetic |
| `preserveRapids` | `true` | Stop water wheels converting rapids into ordinary water |
| `debugLogging` | `true` | Log what each fix is doing |

Every fix has its own switch. A correctness fix should still be something a server owner can turn
off without uninstalling, because the fix that misbehaves on somebody else's pack is the one nobody
can diagnose.

`debugLogging` is on by default on purpose. These fixes mostly remove things that should not have
been happening, so without a record of what did not happen there is no way to tell a working fix
from one that never attached.

### Compatibility and safety

Verified against 1.22.x. The classes Ingenium touches were decompiled from a live 1.22.5 server and
a 1.22.3 client and compared: identical, so anything in the 1.22 line behaves the same.

Ingenium is **Universal**. The base game runs its water check on both client and server, so the
fixes must apply on both sides or the two would disagree about the world.

Five methods are patched, and every one either belongs to the water wheel itself or passes through
untouched for anything that is not a water wheel. The torque hook ignores windmills, creative
rotors and modded rotors, whose spin genuinely is decided by how they are mounted. The
network-direction hook suppresses exactly one write, the wheel's own flow-driven self-update, and
leaves discovery, rebuilds and merges alone for every block in the game.

Everything here was derived from decompiled source, with the mechanism documented in the code
alongside each patch, then validated in game on a 200 mod server.

Source and issues: https://github.com/Lueken/ingenium

---

## Release notes, 0.2.2 (debut)

**Ingenium 0.2.2: water wheels obey the water.**

Two fixes, both to the water wheel, both verified in game.

**The water now decides which way a wheel turns.** The base game derived a wheel's rotation, its
torque direction and its network direction from three different sources that could disagree, and
the tiebreaker was which mirror variant you happened to place and the order you built the shaft.
The visible results: wheels spinning against their own axles, wheels spinning against the water,
and banks of wheels whose output depended on build order. All of it traces to one root, and the fix
is structural rather than cosmetic: the water's direction now enters the simulation through the
torque alone, and rotation, shaft and render all follow from ordinary mechanics. Wheels turn with
their water. Shafts are rigid. Mirror variants are cosmetic. Wheels on one shaft add their torque.
Opposing currents fight honestly, to a standstill if matched.

**Water wheels no longer destroy the rapids that power them.** A running wheel was converting
nearby rapids into ordinary water once a second, permanently, because rapids never spread back. If
your mill has been quietly losing power over weeks, that was why. The wheel no longer writes to the
world at all.

One behaviour change: reversing a watercourse is now a physical event. The machine decelerates
through zero and re-settles with the water over a few seconds, instead of flipping instantly.

Ingenium depends on nothing but the base game and changes no balance numbers: no new power, no new
costs, no new content. It makes the machinery do what it already claimed to do.
