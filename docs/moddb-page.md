# ModDB page draft: Ingenium

Paste-ready. Page body first, then the release changelog for 0.2.0.

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

Ingenium corrects behaviour that is inconsistent with itself. It does not rebalance anything, it
does not add content, and it does not gate anything behind player skill. Where the base game is
merely not to somebody's taste, Ingenium leaves it alone.

Nothing here changes how much power a wheel makes, how fast anything turns, or what any machine
costs to run.

---

### Your water wheels are eating the river

**The symptom.** A wheel's "suitable power source blocks nearby" count drifts downward over time.
Wheels built identically, side by side, report different numbers. A mill that used to run well
slowly stops keeping up, and nothing you can see has changed.

**The cause.** Once a second, a running wheel converts a nearby rapids block into ordinary water.
Ordinary water does not qualify as a power source, so every conversion permanently removes one of
the cells holding the wheel up. The wheel is consuming its own fuel.

For the rapids that worldgen places, the loss cannot heal. Liquid spreading will neither raise a
cell back to source level nor drain one, so those blocks never come back.

Vertical falls suffer worst. A wheel fed by falling water rewrites the very cell it just counted.

**What Ingenium does.** The wheel stops writing to the world. Nothing else about how it measures
flow changes.

This stops further loss. It cannot restore rapids that have already gone.

---

### One wheel spins backwards and fights the others

**The symptom.** In a bank of wheels on one shaft, one turns the opposite way from the rest. The
bank produces noticeably less power than the number of wheels suggests. Rebuilding the same bank in
a different order sometimes fixes it, which makes the whole thing feel like superstition.

**The cause.** A wheel's `side` variant fixes two different things at once. It sets the axle axis,
which is physical and should matter, since a wheel whose axle lies along the current cannot be
driven by it. But it also sets an axis polarity, and the base game hangs three things off that
polarity: how the wheel senses flow, which way its torque points, and which way it appears to turn.

Place the mirror variant and all three invert. Two wheels on one shaft, in one current, then produce
equal and opposite torque and cancel each other out.

This is not an exotic build or a user error. Attaching a wheel to an existing shaft **forces** the
mirrored variant, so a bank assembled in the obvious order fights itself, and a bank where every
wheel was placed before the axle does not.

**What Ingenium does.** It treats one member of each axis as the reference, and corrects wheels
built on the other one. Wheels on a shaft in the same current add their torque and turn the same
way, whichever variant they carry.

Genuinely opposing currents still oppose. Only opposition caused by which mirror you happened to
place is removed.

---

### Configuration

`ModConfig/ingenium.json`, written on first run.

| Key | Default | Effect |
|---|---|---|
| `preserveRapids` | `true` | Stop water wheels converting rapids into ordinary water |
| `freeFloatingWheels` | `true` | Make a wheel's spin follow the water rather than its build variant |
| `debugLogging` | `true` | Log what each fix is doing |

Every fix has its own switch. A correctness fix should still be something a server owner can turn
off without uninstalling, because the fix that misbehaves on somebody else's pack is the one nobody
can diagnose.

`debugLogging` is on by default on purpose. These fixes mostly remove things that should not have
been happening, so without a record of what did not happen there is no way to tell a working fix
from one that never attached.

### Compatibility and safety

Verified against 1.22.x. The classes Ingenium touches were decompiled from a live 1.22.5 server and
a 1.22.3 client and compared: they are identical, so anything in the 1.22 line behaves the same.

Ingenium is **Universal**. The base game runs its flow check on both client and server, so the fix
has to apply on both or a client will briefly predict a change the server never makes.

Ingenium does **not** patch `BEBehaviorMPBase.GetTorque` or `SetPropagationDirection`, the two seams
every mechanical block in the game routes through. Its torque work is gated to water wheels
specifically, so windmills, creative rotors and modded rotors are untouched, which is correct:
a wind rotor's spin genuinely is decided by how it is mounted.

Source and issues: https://github.com/Lueken/ingenium

---

## Release changelog, 0.2.0

**Water wheels on one shaft now add their power instead of cancelling.**

If you have ever built a bank of wheels and found that adding the fourth made no difference, or
watched one wheel turn backwards against its neighbours, this is why. A wheel's build variant was
deciding which way its power pointed, and attaching a wheel to an existing shaft forces the opposite
variant. A bank assembled in the obvious order was fighting itself.

Wheels in one current now pull together whichever variant they carry, and they turn the way the
water pushes them rather than the way they were built. Wheels in genuinely opposing currents still
oppose, as they should.

**Also in this release:** water wheels stopped destroying the rapids that power them, added in 0.1.0
and worth repeating here since 0.1.0 was brief. A running wheel was converting nearby rapids into
ordinary water once a second, permanently for anything worldgen placed. If your mill has been
quietly losing power for months, that was why.

Ingenium still depends on nothing but the base game, and still changes no numbers: no rebalancing,
no new content, no power gained that the base game did not already intend you to have.
