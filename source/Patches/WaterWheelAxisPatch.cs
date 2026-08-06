using System;
using HarmonyLib;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace Ingenium.Patches;

/// <summary>
/// A water wheel should be a free floating gear. The water decides which way it turns.
///
/// THE DEFECT. A wheel's <c>side</c> variant fixes an axle AXIS, which is physical and must keep
/// mattering: a wheel whose axle lies along the current cannot be driven by it. But the variant also
/// fixes an axis POLARITY, and vanilla hangs three separate things off that polarity:
///
///   1. the sensing tangent, <c>num2 += (r_hat x n_hat) . pushVector</c> with <c>n_hat = facing.Normalf</c>
///   2. the torque sign, <c>num = (propagationDir == OutFacingForNetworkDiscovery) ? 1 : -1</c>,
///      and a water wheel's <c>OutFacingForNetworkDiscovery</c> IS its <c>facing</c>
///   3. the rendered spin, via <c>AngleRad => base.AngleRad * dir</c> and <c>IsRotationReversed</c>
///
/// Place the mirror variant and all three flip. Two wheels on one shaft, in one current, then
/// compute equal and opposite torque and cancel.
///
/// This is not an exotic build. <c>BlockWindmillRotor.GetFacingForPlacement</c> writes
/// <c>CodeWithVariant("side", val.Opposite.Code)</c> whenever a wheel is placed against an existing
/// mechanical connector, so attaching wheels to a shaft from both ends FORCES the mirror. Building
/// both wheels before the axle avoids it, which is why some banks work and others do not, and why
/// the difference looks like superstition.
///
/// THE FIX. Canonicalise the axis. Every <c>BlockFacing</c> axis has two members; one has a positive
/// normal. Treat that one as the reference. A wheel whose <c>facing</c> is the other member has its
/// whole frame inverted relative to the reference, so its torque sign must be inverted back.
///
/// WHY THIS PATCHES propagationDir RATHER THAN THE RESULT. Correcting the returned float would be a
/// one-liner and would be wrong. Line 86's <c>num</c> feeds <c>flag = num * speed &lt; 0</c>, the
/// back-driven test, and <c>flag</c> selects BOTH the resistance term and the magnitude:
///
/// <code>
/// resistance = flag ? Resistance * TorqueFactor * Min(0.8, |speed| * 400)
///                   : (num3 > 0 ? Resistance * Min(0.2, num3 * num3 * 80) : 0);
/// double val = flag ? capableSpeed : (capableSpeed - |speed|);
/// </code>
///
/// A mirrored wheel takes the back-driven branch, which returns the FULL <c>capableSpeed</c> rather
/// than the reduced forward value. Flipping only the sign afterward would leave that wheel
/// contributing roughly three times the torque it should at typical speeds, carrying the wrong drag
/// with it. Swapping <c>propagationDir</c> for the duration of the call instead makes the entire
/// method compute consistently: right branch, right magnitude, right resistance, right sign.
///
/// The swap is restored in the postfix, so nothing outside this call ever observes it and network
/// topology is untouched.
///
/// WHAT THIS DOES NOT BREAK. Genuine opposition still opposes. Two wheels in genuinely opposing
/// currents produce opposite <c>dir</c> against the SAME canonical axis, so they still fight, which
/// is correct. Only the spurious opposition, the kind caused purely by which mirror you placed, is
/// removed.
/// </summary>
public static class WheelAxis
{
    /// <summary>+1 if this facing is the positive-normal member of its axis, -1 otherwise. Derived
    /// from the normal rather than from a hardcoded list of facings, so it cannot drift out of step
    /// with the engine's own axis conventions.</summary>
    public static int Polarity(BlockFacing f)
    {
        Vec3i n = f.Normali;
        return (n.X + n.Y + n.Z) > 0 ? 1 : -1;
    }

    public static readonly AccessTools.FieldRef<BEBehaviorMPWaterWheel, BlockFacing> Facing =
        AccessTools.FieldRefAccess<BEBehaviorMPWaterWheel, BlockFacing>("facing");

    public static readonly AccessTools.FieldRef<BEBehaviorMPBase, BlockFacing> PropagationDir =
        AccessTools.FieldRefAccess<BEBehaviorMPBase, BlockFacing>("propagationDir");
}

/// <summary>Torque half: makes wheels on one shaft in one current add rather than cancel.</summary>
[HarmonyPatch(typeof(BEBehaviorMPRotor), nameof(BEBehaviorMPRotor.GetTorque))]
public static class WaterWheelTorqueSignPatch
{
    public static bool Prepare() => IngeniumModSystem.Config.freeFloatingWheels;

    /// <summary>Count of calls whose frame was corrected. Nonzero means at least one mirrored wheel
    /// exists somewhere in the world and was fighting its neighbours.</summary>
    public static long Corrected;

    public static void Prefix(BEBehaviorMPRotor __instance, out BlockFacing? __state)
    {
        __state = null;

        // Windmills, the creative rotor and Millwright's enhanced rotor all reach this method.
        // None of them are driven by a directional fluid, and their spin genuinely IS determined by
        // how they are mounted. Leave them entirely alone.
        if (__instance is not BEBehaviorMPWaterWheel wheel) return;

        BlockFacing facing = WheelAxis.Facing(wheel);
        if (facing == null || WheelAxis.Polarity(facing) >= 0) return;

        ref BlockFacing pd = ref WheelAxis.PropagationDir(__instance);
        if (pd == null) return;

        __state = pd;
        pd = pd.Opposite;
        Corrected++;
    }

    public static void Postfix(BEBehaviorMPRotor __instance, BlockFacing? __state)
    {
        if (__state == null) return;
        WheelAxis.PropagationDir(__instance) = __state;
    }
}

/// <summary>Render half: the water wheel double-counts its own direction.
///
/// Every other mechanical block renders <c>s(propagationDir) * networkAngle</c>. That is what keeps
/// a straight shaft coherent: <c>propagationDir</c> propagates along the run, so every node on it
/// agrees. The water wheel alone multiplies by <c>dir</c> on top:
///
/// <code>public override float AngleRad => base.AngleRad * dir;</code>
///
/// But <c>propagationDir</c> ALREADY encodes <c>dir</c>. <c>CheckWater</c> sets it to
/// <c>dir &lt; 0 ? facing.Opposite : facing</c>, so <c>s(propagationDir) = s(facing) * sign(dir)</c>
/// and the extra factor makes the wheel render <c>s(facing)</c> while the axle bolted through it
/// renders <c>s(facing) * sign(dir)</c>. Whenever <c>dir</c> is negative the wheel and its own axle
/// turn opposite ways on screen, in unmodified vanilla.
///
/// The first version of this patch negated one wheel of a mirrored pair so the two wheels agreed.
/// That fixed wheel to wheel and left wheel to axle broken, which is how the double-count was found:
/// the wheels lined up and the axles through them did not.
///
/// Cancelling the factor instead gives the wheel the same render rule as every other block, and all
/// three consistencies fall out at once. Wheel matches its axle, because both are now
/// <c>s(propagationDir)</c>. Mirrored wheels match each other, because <c>propagationDir</c> is
/// already identical for both: a pair in one current gets opposite <c>facing</c> AND opposite
/// <c>dir</c>, and the two inversions cancel inside <c>dir &lt; 0 ? facing.Opposite : facing</c>.
/// And the rotation follows the water, because <c>propagationDir</c> tracks <c>dir</c>, which is
/// derived from the flow.
///
/// Note this is a separate root from the torque half. Torque breaks on
/// <c>OutFacingForNetworkDiscovery</c>, which does differ between mirrored wheels. Render breaks on
/// the redundant <c>dir</c>. Two defects, two fixes, one symptom.</summary>
[HarmonyPatch(typeof(BEBehaviorMPWaterWheel), "AngleRad", MethodType.Getter)]
public static class WaterWheelRenderSensePatch
{
    public static bool Prepare() => IngeniumModSystem.Config.freeFloatingWheels;

    private static readonly AccessTools.FieldRef<BEBehaviorMPWaterWheel, float> Dir =
        AccessTools.FieldRefAccess<BEBehaviorMPWaterWheel, float>("dir");

    public static void Postfix(BEBehaviorMPWaterWheel __instance, ref float __result)
    {
        float dir = Dir(__instance);

        // Zero means the wheel has never resolved a flow direction. Leave vanilla's value alone
        // rather than zeroing the angle and freezing the model.
        if (dir == 0f) return;

        // dir is +1 or -1, so multiplying by it a second time cancels the factor and leaves
        // base.AngleRad, which is the rule every other mechanical block already follows.
        __result *= dir;
    }
}
