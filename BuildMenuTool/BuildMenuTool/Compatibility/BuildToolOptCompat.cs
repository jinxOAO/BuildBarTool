using BepInEx.Bootstrap;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace BuildBarTool
{
    public static class BuildToolOptCompat
    {
        public const string MODGUID = "starfi5h.plugin.BuildToolOpt";

        internal static bool enabled;
        internal static bool hologramEnabled;

        public static void Compatible()
        {
            enabled = Chainloader.PluginInfos.TryGetValue(MODGUID, out _);
            hologramEnabled = false;
            if (!enabled) return;

            var harmony = new Harmony(BuildBarToolPlugin.GUID + ".Compatibility.BuildToolOpt");
            harmony.PatchAll(typeof(BuildToolOptCompat));
        }

        public static void Start()
        {
            hologramEnabled = BuildToolOpt.Plugin.EnableHologram;
        }
    }
}
