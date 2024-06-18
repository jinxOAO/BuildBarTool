using BepInEx.Logging;
using CommonAPI;
using HarmonyLib.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using xiaoye97;

namespace BuildMenuTool
{
    public static class BuildBar
    {
        public static bool Bind(int category, int index, int itemId, int tier = 2)
        {
            if(category >= 1 && category < 12)
            {
                if (index >= 1 && index <= 10)
                {
                    if (tier == 1)
                    {
                        LDBTool.SetBuildBar(category, index, itemId);
                    }
                    else if (tier == 2)
                    {
                        if (BuildMenuTool.protoIds[category, index] > 0)
                        {
                            BuildMenuTool.logger.LogWarning(string.Format("Bind Build Bar Fail (item ID:{0}). Build bar [{1}, tier2, {2}] is already bound with another item ID:{3}.", new object[]
                            {
                                itemId,
                                category,
                                index,
                                BuildMenuTool.protoIds[category, index]
                            }));
                            return false;
                        }
                        else
                            BuildMenuTool.protoIds[category, index] = itemId;
                    }
                    else
                    {
                        BuildMenuTool.logger.LogWarning("Bind Build Bar Fail. tier must be 1 or 2.");
                        return false;
                    }
                }
                else
                {
                    BuildMenuTool.logger.LogWarning("Bind Build Bar Fail. index must be between 1 and 10.");
                    return false;
                }
            }
            else
            {
                BuildMenuTool.logger.LogWarning("Bind Build Bar Fail. category must be between 1 and 12.");
                return false;
            }
            return true;
        }

        public static void BindTier1(int category, int index, int itemId)
        {
            Bind(category, index, itemId, 1);
        }

        public static void BindTier2(int category, int index, int itemId)
        {
            Bind(category, index, itemId, 2);
        }

        public static void BindBuildBar(this ItemProto proto, int buildIndex, int tier)
        {
            Bind(buildIndex / 100, buildIndex % 100, proto.ID ,tier);
            proto.BuildIndex = 0;
        }

        public static void SetToTier2BuildBar(this ItemProto proto)
        {
            Bind(proto.BuildIndex/100, proto.BuildIndex % 100, proto.ID, 2);
            proto.BuildIndex = 0;
        }
    }
}
