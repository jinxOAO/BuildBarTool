using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;


namespace BuildBarTool { 
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(DeliverySlotsTweaksCompat.MODGUID, BepInDependency.DependencyFlags.SoftDependency)]

    public class CompatCheck : BaseUnityPlugin
    {
        public const string GUID = BuildBarToolPlugin.GUID + ".CheckPlugins";
        public const string NAME = BuildBarToolPlugin.NAME + ".CheckPlugins";
        public const string VERSION = BuildBarToolPlugin.VERSION;

        public void Awake()
        {
            DeliverySlotsTweaksCompat.Compatible();
        }

    }
}
