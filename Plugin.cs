using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using ComputerysModdingUtilities;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;

[assembly: StraftatMod(isVanillaCompatible: true)]

namespace TauntBinds;

[BepInPlugin("TauntBinds", "Taunt Binds", "1.0.0")]
[BepInDependency("dimolade.dimolade.InfTaunt", BepInDependency.DependencyFlags.SoftDependency)]
public class TauntBindPlugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    private static Harmony _harmony;

    private static ConfigEntry<KeyCode>[] _tauntKeys = new ConfigEntry<KeyCode>[10];
    private static readonly KeyCode[] _defaultKeys = [KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9];
    private static readonly float[] _tauntCooldowns = [0.4f, 0.3f, 0.3f, 0.5f, 0.7f, 0.4f, 0.7f, 0.9f, 1f, 0.3f];
    private static bool _infTaunt = false;

    private void Awake()
    {
        Log = Logger;
        for (int i = 0; i < 10; i++)
        {
            _tauntKeys[i] = Config.Bind("General", "Key for taunt #" + i, _defaultKeys[i]);
        }

        // Inftaunt
        _infTaunt = Config.Bind("General", "Infinite Taunt", false, "Remove cooldown on taunting").Value;
        if (Chainloader.PluginInfos.ContainsKey("dimolade.dimolade.InfTaunt"))
        {
            _infTaunt = true;
            // Other mod has a prefix that just totally rewrites the function so it can't be compatible
            Harmony.UnpatchID("dimolade.harmony.InfTaunt");
        }

        // debug
        HarmonyFileLog.Enabled = true;

        _harmony = new Harmony("TauntBinds");
        _harmony.PatchAll();
    }

    private void OnDestroy()
    {
        _harmony.UnpatchSelf();
    }

    [HarmonyPatch(typeof(FirstPersonController), "HandleTaunt")]
    public static class FirstPersonControllerPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // var handleTaunt = AccessTools.Method(typeof(FirstPersonControllerPatch), nameof(HandleTaunt));

            var matcher = new CodeMatcher(instructions);
            for (int i = 1; i < 10; i++)
            {
                matcher.MatchForward(useEnd: false, new CodeMatch(OpCodes.Ldc_I4_S));
                matcher.SetOperandAndAdvance((int)_tauntKeys[i].Value);
            }
            matcher.MatchForward(useEnd: false, new CodeMatch(OpCodes.Ldc_I4_S));
            matcher.SetOperandAndAdvance((int)_tauntKeys[0].Value);
            return matcher.InstructionEnumeration();
        }
    }
}