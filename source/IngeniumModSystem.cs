using HarmonyLib;
using Vintagestory.API.Common;

[assembly: ModInfo("Ingenium", "ingenium",
    Authors = new string[] { "Venah" },
    Description = "Mechanical power, corrected.",
    Version = "0.2.1")]

namespace Ingenium;

/// <summary>
/// Ingenium fixes defects in Vintage Story's mechanical power simulation. It depends on nothing but
/// the base game, deliberately: every fix here is a candidate for upstream, and a fix that requires
/// a third-party mod is not one the developers can take.
///
/// Scope discipline. This mod corrects behaviour that is inconsistent with itself. It does not
/// rebalance, it does not add content, and it does not gate anything on player skill. Where vanilla
/// is merely not to somebody's taste, Ingenium leaves it alone.
///
/// Every patch states its evidence in its own file: the decompiled line it corrects, what the
/// original does, and what breaks without the fix.
/// </summary>
public class IngeniumModSystem : ModSystem
{
    public const string HarmonyId = "com.venah.ingenium";

    private Harmony? harmony;
    public static IngeniumConfig Config { get; private set; } = new();

    public override void StartPre(ICoreAPI api)
    {
        // Config before patching, so a disabled fix is never applied rather than applied and
        // branched out of. Fails open: a missing or malformed file yields defaults.
        try
        {
            Config = api.LoadModConfig<IngeniumConfig>("ingenium.json") ?? new IngeniumConfig();
        }
        catch (System.Exception e)
        {
            api.Logger.Warning("[Ingenium] Config unreadable, using defaults: " + e.Message);
            Config = new IngeniumConfig();
        }
        api.StoreModConfig(Config, "ingenium.json");
    }

    public override void Start(ICoreAPI api)
    {
        harmony = new Harmony(HarmonyId);

        // Every target here is non-public vanilla internals. If a future game build renames one,
        // PatchAll throws and the whole mod fails to load, which is a hostile way to tell a server
        // owner that one fix went stale. Degrade loudly instead: the game keeps its own behaviour
        // and the log says exactly which patch could not attach.
        try
        {
            harmony.PatchAll();
        }
        catch (System.Exception e)
        {
            api.Logger.Error("[Ingenium] A patch failed to attach and Ingenium is running with reduced or no effect. "
                + "This usually means the game changed a method Ingenium hooks. Details: " + e.Message);
        }

        int n = 0;
        foreach (var m in harmony.GetPatchedMethods()) { api.Logger.Notification("[Ingenium] Patched: " + m.DeclaringType?.Name + "." + m.Name); n++; }

        if (n == 0 && Config.preserveRapids)
            api.Logger.Warning($"[Ingenium] {Mod.Info.Version} loaded but patched NOTHING. Expected ReplaceRapidWater at minimum.");
        else
            api.Logger.Notification($"[Ingenium] {Mod.Info.Version} active, {n} method(s) patched. preserveRapids={Config.preserveRapids} freeFloatingWheels={Config.freeFloatingWheels} debugLogging={Config.debugLogging}");

        // Periodic totals, server side only. A fix that removes a world write has no visible
        // artifact, so the running count is the evidence that it is doing anything at all.
        if (api.Side == EnumAppSide.Server && Config.debugLogging)
        {
            api.Event.RegisterGameTickListener(_ =>
            {
                long n2 = Patches.WaterWheelRapidsPatch.Preserved;
                if (n2 > lastReported)
                {
                    api.Logger.Notification($"[Ingenium] rapids preserved: {n2} total (+{n2 - lastReported} since last report)");
                    lastReported = n2;
                }

                // Nonzero means a mirrored wheel exists and was fighting its neighbours. Reported
                // once rather than per tick, because this fires on every GetTorque call for every
                // affected wheel and would otherwise be thousands of lines a minute.
                long n3 = Patches.WaterWheelTorqueSignPatch.Corrected;
                if (n3 > 0 && !reportedAxisCorrection)
                {
                    reportedAxisCorrection = true;
                    api.Logger.Notification("[Ingenium] axis correction is active: at least one water wheel was built on the "
                        + "mirrored side variant and was subtracting torque from its network. It now adds.");
                }
            }, 60000);
        }
    }

    private long lastReported;
    private bool reportedAxisCorrection;

    public override void Dispose()
    {
        harmony?.UnpatchAll(HarmonyId);
        harmony = null;
    }
}

/// <summary>Every fix ships behind its own switch. A correctness fix should still be something a
/// server owner can turn off without uninstalling the mod, because the fix that misbehaves on
/// somebody else's pack is the one nobody can diagnose.</summary>
public class IngeniumConfig
{
    /// <summary>Stop water wheels from converting the rapids that power them into ordinary water.
    /// See WaterWheelRapidsPatch for what the base game does and why it matters.</summary>
    public bool preserveRapids { get; set; } = true;

    /// <summary>Make a water wheel's spin depend on the water rather than on which mirror variant
    /// happened to be placed. Wheels on one shaft in one current then add instead of cancelling, and
    /// they turn the way the water pushes them. Genuinely opposing currents still oppose.
    /// See WaterWheelAxisPatch.</summary>
    public bool freeFloatingWheels { get; set; } = true;

    /// <summary>Verbose logging, on by default. These fixes remove world writes rather than adding
    /// anything visible, so without a record of what did not happen there is no way to tell a
    /// working fix from a fix that never attached.</summary>
    public bool debugLogging { get; set; } = true;
}
