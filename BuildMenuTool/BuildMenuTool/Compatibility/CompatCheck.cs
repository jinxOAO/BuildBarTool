using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;

namespace BuildBarTool { 
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(DeliverySlotsTweaksCompat.MODGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(BuildToolOptCompat.MODGUID, BepInDependency.DependencyFlags.SoftDependency)]

    public class CompatCheck : BaseUnityPlugin
    {
        public const string GUID = BuildBarToolPlugin.GUID + ".CheckPlugins";
        public const string NAME = BuildBarToolPlugin.NAME + ".CheckPlugins";
        public const string VERSION = BuildBarToolPlugin.VERSION;

        public static ManualLogSource Log;

        public void Awake()
        {
            DeliverySlotsTweaksCompat.Compatible();
            BuildToolOptCompat.Compatible();
        }

        public void Start()
        {
            Log = base.Logger;
            try
            {
                if (BuildToolOptCompat.enabled)
                    BuildToolOptCompat.Start();
            }
            catch (Exception e)
            {
                Log.LogWarning("Error in BuildBarTool.CompatCheck Start()");
                Log.LogWarning(e);
            }
        }
    }
}
