using System;
using HarmonyLib;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace Ingenium.Patches;

/// <summary>
/// A water wheel is a free floating gear: the water decides which way it turns, the shaft follows
/// rigidly, and which mirror variant was placed is cosmetic.
///
/// Design D, adopted 2026-08-07 after an adversarially verified review of two failed attempts
/// (eng-overheat-research/04-waterwheel-review-021.md in the owner's project records). The lesson
/// both failures taught: vanilla routes the water's direction through THREE couplings at once, the
/// propagationDir rewrite in CheckWater, the torque sign in GetTorque, and the render factor in
/// AngleRad. Correcting any one of them in isolation moves the defect instead of removing it.
/// Attempt one (negate mirrored renders) made wheels agree with each other and disagree with their
/// axles. Attempt two (delete the render factor as a supposed double count) made the whole assembly
/// rigid and indifferent to the water, because the two remaining factors each track the water and
/// an even number of water-tracking factors is a constant.
///
/// Design D collapses the three couplings to ONE. The wheel becomes a standard mechanical node,
/// exactly like a windmill:
///
///   1. Its propagationDir is frozen at the value network topology gives it. CheckWater's
///      water-driven rewrite is suppressed (PdFreeze below).
///   2. The water enters the system in one place only: the torque sign. When dir is negative the
///      wheel's frame is swapped for the duration of GetTorque (TorqueSign below), so the wheel
///      pushes the network toward the water's direction, for either mirror variant, and the
///      back-driven test means what it should: physical rotation opposing this wheel's water.
///   3. The render is the plain shaft angle (Render below). Direction reaches the wheel the same
///      way it reaches every axle, through the network's signed speed.
///
/// Proven across all eight facing x dir x NetworkDir cases in the review: the wheel renders
/// s(facing) * dir, the with-water sign, and it evaluates the IDENTICAL expression as its axle, so
/// wheel-with-water and wheel-rigid-with-axle both hold by construction, through every transient,
/// back-driving included. Genuinely opposing currents produce opposite torque and still fight.
///
/// Accepted behaviour change, deliberate: suppressing the pd rewrite removes vanilla's instant
/// TurnDir flip on water reversal. A reversal is now a physical event, the network decelerates
/// through zero over a few seconds and re-settles with the water, with a sub-second client freeze
/// at the crossing. That is the free floating gear behaving like a thing with load, not a bug.
/// </summary>
public static class WheelAxis
{
    public static readonly AccessTools.FieldRef<BEBehaviorMPWaterWheel, BlockFacing> Facing =
        AccessTools.FieldRefAccess<BEBehaviorMPWaterWheel, BlockFacing>("facing");

    public static readonly AccessTools.FieldRef<BEBehaviorMPWaterWheel, float> Dir =
        AccessTools.FieldRefAccess<BEBehaviorMPWaterWheel, float>("dir");

    public static readonly AccessTools.FieldRef<BEBehaviorMPBase, BlockFacing> PropagationDir =
        AccessTools.FieldRefAccess<BEBehaviorMPBase, BlockFacing>("propagationDir");

    public static readonly AccessTools.FieldRef<BEBehaviorMPBase, float> LastKnownAngleRad =
        AccessTools.FieldRefAccess<BEBehaviorMPBase, float>("lastKnownAngleRad");

    public static readonly AccessTools.FieldRef<MechPowerPath, BlockPos> FromPos =
        AccessTools.FieldRefAccess<MechPowerPath, BlockPos>("fromPos");
}

/// <summary>Marks the wheel currently executing CheckWater on this thread, so the pd freeze can
/// tell CheckWater's self-write apart from legitimate topology writes with zero ambiguity. The
/// review's simpler fromPos-only discriminator has a hole on closed gear loops, where a
/// wheel-originated discovery path can return to the wheel carrying its own position; requiring
/// BOTH conditions (inside this wheel's CheckWater AND fromPos is the wheel itself) closes it.
/// ThreadStatic because client and server tick on different threads in singleplayer.</summary>
[HarmonyPatch(typeof(BEBehaviorMPWaterWheel), "CheckWater")]
public static class WaterWheelCheckWaterMarker
{
    public static bool Prepare() => IngeniumModSystem.Config.freeFloatingWheels;

    [ThreadStatic] public static BEBehaviorMPWaterWheel? Current;

    public static void Prefix(BEBehaviorMPWaterWheel __instance) => Current = __instance;

    public static void Finalizer() => Current = null;
}

/// <summary>Patch 1: the pd freeze. The wheel keeps the propagationDir topology gave it, like
/// every other mechanical block. Suppresses only the one write CheckWater makes about itself;
/// discovery, rebuilds and merges all pass through untouched. Side benefit, verified in review:
/// this also stops CheckWater resetting the wheel's GearedRatio to 1 every time the water flips,
/// a live vanilla defect on geared networks.</summary>
[HarmonyPatch(typeof(BEBehaviorMPBase), nameof(BEBehaviorMPBase.SetPropagationDirection))]
public static class WaterWheelPdFreezePatch
{
    public static bool Prepare() => IngeniumModSystem.Config.freeFloatingWheels;

    /// <summary>Suppressed self-writes. Expect roughly one per wheel per flow change, not per
    /// tick: CheckWater only calls SetPropagationDirection when dir CHANGES.</summary>
    public static long Suppressed;

    public static bool Prefix(BEBehaviorMPBase __instance, MechPowerPath path)
    {
        if (__instance is not BEBehaviorMPWaterWheel wheel) return true;
        if (!ReferenceEquals(WaterWheelCheckWaterMarker.Current, wheel)) return true;

        BlockPos? from = path == null ? null : WheelAxis.FromPos(path);
        if (from == null || !from.Equals(wheel.Pos)) return true;

        Suppressed++;
        return false;
    }
}

/// <summary>Patch 2: the water enters through torque, and only torque.
///
/// Vanilla's GetTorque signs its result by num = (propagationDir == OutFacingForNetworkDiscovery),
/// a frame test. With pd frozen at topology, the water's direction must be folded in here: when
/// dir is negative, swap the frame for the duration of the call. Effective torque sign becomes
/// frame * sign(dir), which for a mirrored pair in one current is identical (their frames and
/// their dirs both flip, the product does not), so coaxial wheels add for any variant mix, and
/// wheels in genuinely opposing currents get opposite signs and fight, as they should.
///
/// The swap must be the frame and not the returned float, for the same reason as ever: num feeds
/// the back-driven test (num * speed &lt; 0), which selects BOTH the resistance branch and the
/// magnitude. Swapping the frame makes the whole method consistent; negating the result would hand
/// a back-driven wheel roughly three times its correct torque with the wrong drag.
///
/// This replaces the 0.2.1 polarity-gated swap entirely. The polarity gate was anchored to the
/// block variant rather than to the water, which regressed all-mirrored banks that vanilla handled
/// correctly. dir &lt; 0 is the water-anchored condition. Restore lives in a Finalizer so a foreign
/// patch throwing mid-call can never leave the frame flipped.</summary>
[HarmonyPatch(typeof(BEBehaviorMPRotor), nameof(BEBehaviorMPRotor.GetTorque))]
public static class WaterWheelTorqueSignPatch
{
    public static bool Prepare() => IngeniumModSystem.Config.freeFloatingWheels;

    /// <summary>Calls where the frame was swapped. This is NORMAL reversed-flow operation, not a
    /// defect signal: any wheel whose water pushes opposite its canonical frame swaps on every
    /// call. Diagnostic only.</summary>
    public static long ReversedFlowCalls;

    public static void Prefix(BEBehaviorMPRotor __instance, out BlockFacing? __state)
    {
        __state = null;

        // Windmills, the creative rotor and Millwright's rotors also execute this method. None are
        // driven by a directional fluid; their spin genuinely is mounting-determined. Untouched.
        if (__instance is not BEBehaviorMPWaterWheel wheel) return;

        // dir == 0 (no water, or the pre-first-CheckWater window) needs no case: flowRate is 0, so
        // TorqueFactor is 0 and the method returns 0 regardless of frame.
        if (WheelAxis.Dir(wheel) >= 0f) return;

        ref BlockFacing pd = ref WheelAxis.PropagationDir(__instance);
        if (pd == null) return;

        // Off-axis pd (possible pre-discovery): the frame test fails both ways, the swap would be
        // a silent no-op. Skip it so the counter stays honest.
        BlockFacing facing = WheelAxis.Facing(wheel);
        if (facing == null || (pd != facing && pd != facing.Opposite)) return;

        __state = pd;
        pd = pd.Opposite;
        ReversedFlowCalls++;
    }

    public static void Finalizer(BEBehaviorMPRotor __instance, BlockFacing? __state)
    {
        if (__state != null) WheelAxis.PropagationDir(__instance) = __state;
    }
}

/// <summary>Patch 3: render the plain shaft angle.
///
/// Vanilla's wheel getter returns base.AngleRad * dir. The base getter has already computed and
/// stored the true shaft angle in lastKnownAngleRad by the time this postfix runs, so returning
/// that field undoes the dir factor exactly, survives dir == 0 (where the vanilla multiply froze
/// the wheel at angle zero), and degrades to the last known pose when the network is null.
///
/// With Design D's physics, the shaft angle IS the with-water angle: the wheel and its axle
/// evaluate the same expression, and the network's signed speed carries the water's direction to
/// both. No per-wheel render factor exists anymore, which is precisely what makes the rigidity
/// unconditional.</summary>
[HarmonyPatch(typeof(BEBehaviorMPWaterWheel), "AngleRad", MethodType.Getter)]
public static class WaterWheelRenderPatch
{
    public static bool Prepare() => IngeniumModSystem.Config.freeFloatingWheels;

    public static void Postfix(BEBehaviorMPWaterWheel __instance, ref float __result)
    {
        __result = WheelAxis.LastKnownAngleRad(__instance);
    }
}
