using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ComputerysModdingUtilities;
using HarmonyLib;
using UnityEngine;

[assembly: StraftatMod(isVanillaCompatible: true)]

namespace TauntBinds;

[BepInPlugin("com.koki.tauntbinds", "Taunt Binds", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    internal static Harmony harmony;

    private static ConfigEntry<KeyCode>[] _tauntKeys = new ConfigEntry<KeyCode>[10];
    private static readonly KeyCode[] _defaultKeys = [KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9];
    private static readonly float[] _tauntDurations = [0.4f, 0.3f, 0.3f, 0.5f, 0.7f, 0.4f, 0.7f, 0.9f, 1f, 0.3f];

    private void Awake()
    {
        Log = Logger;
        harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        for (int i = 0; i < 10; i++)
        {
            _tauntKeys[i] = Config.Bind("General", "Key for taunt #" + i, _defaultKeys[i]);
        }
    }

    [HarmonyPatch(typeof(FirstPersonController), "HandleTaunt")]
    internal static class FirstPersonControllerPatch
    {
        public static bool Prefix(FirstPersonController __instance)
        {
            __instance.tauntTimer -= Time.deltaTime;
            if (__instance.tauntTimer > 0f) return false;

            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(_tauntKeys[i].Value))
                {
                    __instance.AboubiPlayServer(i);
                    Settings.Instance.IncreaseTauntsAmount();
                    __instance.tauntTimer = _tauntDurations[i];
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