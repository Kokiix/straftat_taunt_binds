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

[BepInPlugin("TauntBinds", "Taunt Binds", "1.1.0")]
[BepInDependency("dimolade.dimolade.InfTaunt", BepInDependency.DependencyFlags.SoftDependency)]
public class TauntBindPlugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    private static Harmony _harmony;

    private static ConfigEntry<KeyCode>[] _tauntKeys = new ConfigEntry<KeyCode>[10];
    private static readonly KeyCode[] _defaultKeys = [KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9];
    private static bool _infTaunt = false;

    private void Awake()
    {
        Log = Logger;
        for (int i = 0; i < 10; i++)
        {
            _tauntKeys[i] = Config.Bind("General", "Key for taunt #" + i, _defaultKeys[i]);
        }

        // Inftaunt
        _infTaunt = Config.Bind("General", "Infinite Taunt", false, "Remove cooldown on taunting. Having the InfTaunt mod enabled will automatically set this to true.").Value;
        if (Chainloader.PluginInfos.ContainsKey("dimolade.dimolade.InfTaunt"))
        {
            _infTaunt = true;
            // Other mod has a prefix that totally rewrites the function so it can't be compatible
            Harmony.UnpatchID("dimolade.harmony.InfTaunt");
        }

        _harmony = new Harmony("TauntBinds");
        _harmony.PatchAll();
    }

    private void OnDestroy()
    {
        _harmony.UnpatchSelf();
    }

    [HarmonyPatch(typeof(FirstPersonController), "HandleTaunt")]
    public static class TauntBindPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var tauntTimer = AccessTools.Field(typeof(FirstPersonController), "tauntTimer");

            var matcher = new CodeMatcher(instructions);

            // Remove tauntTimer <= 0 check
            if (_infTaunt)
                matcher.MatchForward(useEnd: false,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, tauntTimer),
                new CodeMatch(OpCodes.Ldc_R4),
                new CodeMatch(OpCodes.Ble_Un),
                new CodeMatch(OpCodes.Ret))
                .RemoveInstructions(5);

            // Do once for taunt input, then again for cooldown increment
            for (int i = 0; i < 2; i++)
            {
                // Set keybinds #1-9
                for (int j = 1; j < 10; j++)
                {
                    matcher.MatchForward(useEnd: false, new CodeMatch(OpCodes.Ldc_I4_S));
                    matcher.SetOperandAndAdvance((int)_tauntKeys[j].Value);
                }
                // Then #0
                matcher.MatchForward(useEnd: false, new CodeMatch(OpCodes.Ldc_I4_S));
                matcher.SetOperandAndAdvance((int)_tauntKeys[0].Value);
            }

            return matcher.InstructionEnumeration();
        }
    }
}

[HarmonyPatch(typeof(FirstPersonController), "RpcLogic___AboubiPlayObservers_3316948804")]
public static class RemoveTauntReceiveLimiter
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
        .Start()
        .RemoveInstructions(10)
        .InstructionEnumeration();
    }
}