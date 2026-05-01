using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ComputerysModdingUtilities;
using HarmonyLib;
using UnityEngine;

[assembly: StraftatMod(isVanillaCompatible: true)]

namespace TauntBinds;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    internal static Harmony harmony;

    private static ConfigEntry<KeyCode>[] tauntKeys = new ConfigEntry<KeyCode>[10];
    private static readonly KeyCode[] defaultKeys = [KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9];
    private static readonly float[] tauntDurations = [0.4f, 0.3f, 0.3f, 0.5f, 0.7f, 0.4f, 0.7f, 0.9f, 1f, 0.3f];

    private void Awake()
    {
        Log = Logger;
        harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        for (int i = 0; i < 10; i++)
        {
            tauntKeys[i] = Config.Bind("General", "Key for taunt #" + i, defaultKeys[i]);
        }
    }

    [HarmonyPatch(typeof(FirstPersonController))]
    internal static class FirstPersonControllerPatch
    {
        [HarmonyPatch("HandleTaunt")]
        [HarmonyPrefix]
        public static bool HandleTaunt(FirstPersonController __instance)
        {
            __instance.tauntTimer -= Time.deltaTime;
            if (__instance.tauntTimer > 0f) return false;

            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(tauntKeys[i].Value))
                {
                    __instance.AboubiPlayServer(i);
                    Settings.Instance.IncreaseTauntsAmount();
                    __instance.tauntTimer = tauntDurations[i];
                    break;
                }
            }
            return false;
        }
    }

    private void OnDestroy()
    {
        harmony.UnpatchSelf();
    }
}