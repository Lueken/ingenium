using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace Ingenium.Patches;

/// <summary>
/// A water wheel destroys the rapids that power it.
///
/// WHAT VANILLA DOES. Every second, <c>BEBehaviorMPWaterWheel.CheckWater</c> samples a ring of cells
/// around the hub and then calls <c>ReplaceRapidWater</c> on up to four candidate positions. That
/// method looks harmless from its name. It is not:
///
/// <code>
/// string text = block.Code.FirstCodePart();                       // "rapidwater"
/// text = "water" + block.Code.Path.Substring(text.Length);        // "water" + "-e-7"
/// Block block2 = Api.World.GetBlock(new AssetLocation(domain, text));
/// Api.World.BlockAccessor.SetBlock(block2.Id, pos, 2);
/// </code>
///
/// It rebuilds the block code with the "rapidwater" prefix stripped, so <c>rapidwater-e-7</c>
/// becomes <c>water-e-7</c>. The guard <c>!behavior.multiplySpread</c> selects rapids and nothing
/// else, because <c>rapidwater.json</c> is the only blocktype in the entire asset tree that sets
/// <c>multiplySpread</c> false. Ordinary water hits the guard, is left alone, and still returns
/// true, which is what makes the caller's probe chain terminate.
///
/// WHY IT MATTERS. <c>getFlowSpeed</c> reads the static <c>flowspeed</c> block attribute and
/// compares it to <c>requiresMinFlowSpeed</c> (1.5). Ordinary water fails that test. So each
/// conversion permanently removes one of the cells that qualified the wheel as powered.
///
/// For a level-7 source the loss cannot heal. <c>CanSpreadIntoBlock</c> gates its whole flow branch
/// on <c>!IsLiquidSourceBlock</c> and otherwise falls to <c>block.LiquidLevel &lt; ourblock.LiquidLevel</c>,
/// and 7 is not less than 7; <c>TryLoweringLiquidLevel</c> refuses to drain a source. Worldgen
/// writes <c>rapidwater-still-7</c>, so worldgen rapids are exactly the case that cannot come back.
///
/// The observable symptom is a wheel whose "suitable power source blocks nearby" count drifts
/// downward over time, and several identically built wheels reading different numbers because each
/// is at a different point in eating its own supply. It also makes build ORDER matter: a wheel built
/// on existing rapids and a wheel that had rapids routed to it later end up in different worlds,
/// which nothing about a water wheel should cause.
///
/// THE FIX. Reproduce the boolean the caller consumes and skip the write. Control flow inside
/// <c>CheckWater</c> is preserved exactly, because nothing there reads the world state this method
/// changes; only <c>flowRate</c> matters, and that is computed before these calls.
///
/// SIDE. <c>CheckWater</c>'s tick listener is registered in <c>Initialize</c> with no side gate, so
/// this runs on client and server both. The patch must be universal or a client will locally predict
/// a conversion the server never performs.
///
/// Verified against VSSurvivalMod.dll from a live 1.22.5 server and a 1.22.3 client install. The
/// class is byte-identical between them.
/// </summary>
[HarmonyPatch(typeof(BEBehaviorMPWaterWheel), "ReplaceRapidWater")]
public static class WaterWheelRapidsPatch
{
    public static bool Prepare() => IngeniumModSystem.Config.preserveRapids;

    /// <summary>Returns false to skip the original entirely.</summary>
    public static bool Prefix(BlockPos pos, ref bool __result, BEBehaviorMPWaterWheel __instance)
    {
        // Fail open. If anything about the world is not readable, let vanilla run rather than
        // silently changing the caller's control flow.
        var accessor = __instance?.Api?.World?.BlockAccessor;
        if (accessor == null || pos == null) return true;

        // Layer 2 is the fluid layer, the same one the original reads and writes.
        Block block = accessor.GetBlock(pos, 2);
        if (block == null) return true;

        // Exactly the original's return condition, minus the SetBlock in its inner branch.
        __result = block.GetBehavior<BlockBehaviorFiniteSpreadingLiquid>() != null
                   && block.LiquidCode == "water";
        return false;
    }
}
