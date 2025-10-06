using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace GGTemps.Patches
{
    public class Menu : MonoBehaviour
    {
        public static bool IsPatched { get; private set; }

        internal static void ApplyHarmonyPatches()
        {
            if (!IsPatched)
            {
                if (instance == null)
                {
                    instance = new HarmonyLib.Harmony(GGTemps.PluginInfo.GUID);
                }
                try
                {
                    instance.PatchAll(Assembly.GetExecutingAssembly());
                }
                catch (System.Exception ex)
                {
                    MelonLogger.LogError($"Failed To Patch: {ex}");
                }
                IsPatched = true;
            }
        }

        internal static void RemoveHarmonyPatches()
        {
            if (instance != null && IsPatched)
            {
                IsPatched = false;
            }
        }

        private static HarmonyLib.Harmony instance;
    }
}
