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

/// <summary>Render half: makes the correction visible.
///
/// Without this the torque fix is invisible and a corrected wheel keeps spinning backwards while
/// quietly pulling its weight, which is a worse outcome than a visible fault because nobody can tell
/// a working fix from a broken one.
///
/// The rendered sense reduces to <c>s(facing) * sign(network.Speed)</c>: the <c>* dir</c> factor in
/// <c>AngleRad</c> cancels exactly against <c>IsRotationReversed(propagationDir)</c>, so contrary to
/// how it reads, <c>* dir</c> does NOT make the render follow the water. Every node on a network
/// shares <c>sign(network.Speed)</c>, so the only term that can differ between two wheels on one
/// shaft is <c>s(facing)</c>. Multiplying by the axis polarity cancels precisely that term.
///
/// A useful consequence, and the reason this half is worth having on its own: a wheel that visibly
/// spins against its network-mates is necessarily the one subtracting torque. The visual becomes a
/// reliable diagnostic with no tooltip needed.</summary>
[HarmonyPatch(typeof(BEBehaviorMPWaterWheel), "AngleRad", MethodType.Getter)]
public static class WaterWheelRenderSensePatch
{
    public static bool Prepare() => IngeniumModSystem.Config.freeFloatingWheels;

    public static void Postfix(BEBehaviorMPWaterWheel __instance, ref float __result)
    {
        BlockFacing facing = WheelAxis.Facing(__instance);
        if (facing == null || WheelAxis.Polarity(facing) >= 0) return;

        // Negating the angle reverses the rendered rotation. This is the same device vanilla already
        // uses: `* dir` produces negative angles routinely, so the renderer handles them.
        __result = -__result;
    }
}
