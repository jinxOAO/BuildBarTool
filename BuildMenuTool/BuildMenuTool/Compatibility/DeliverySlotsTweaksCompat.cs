using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBarTool
{
    public static class DeliverySlotsTweaksCompat
    {
        public const string MODGUID = "starfi5h.plugin.DeliverySlotsTweaks";

        internal static bool enabled;

        public static void Compatible()
        {
            enabled = Chainloader.PluginInfos.TryGetValue(MODGUID, out _);

            if (!enabled) return;

            var harmony = new Harmony(BuildBarToolPlugin.GUID + ".Compatibility.DeliverySlotsTweaksCompat");
            harmony.PatchAll(typeof(DeliverySlotsTweaksCompat));
            //BuildMenuToolPlugin.logger.LogInfo("DeliverySlotsTweaks Compatibility Compatible finish.");
        }
    }
}
