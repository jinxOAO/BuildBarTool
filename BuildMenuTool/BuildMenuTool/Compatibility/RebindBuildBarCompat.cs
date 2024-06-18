using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using CommonAPI.Systems.ModLocalization;
using CommonAPI.Systems;
using CommonAPI;
using HarmonyLib;
using xiaoye97;
using RebindBuildBar;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using System.Security.Permissions;
using System.Reflection;

namespace BuildMenuTool
{
    [BepInDependency(RebindBuildBarPlugin.MODGUID)]
    [BepInDependency(BuildMenuTool.GUID)]
    [BepInPlugin(GUID, NAME, VERSION)]
    internal class RebindBuildBarCompat : BaseUnityPlugin
    {
        public const string GUID = "Gnimaerd.DSP.plugin.BuildMenuTool_RebindBuildBarCompat";
        public const string NAME = "BuildMenuTool_RebindBuildBarCompat";
        public const string VERSION = "0.1.0";


        internal static ManualLogSource logger;

        public static Vector2 pickerPos = new Vector2(-300, 238);

        public void Awake()
        {
            logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(RebindBuildBarCompat));
            BuildMenuTool.RebindBuildBarCompatibility = true;
        }

        public void Start()
        {
        }

        public void Update()
        {
            UIBuildMenu buildMenu = RebindBuildBar.Patches.buildMenu;
            if (buildMenu == null || !buildMenu.childGroup.gameObject.activeSelf) return;
            if (buildMenu.currentCategory < 1 || buildMenu.currentCategory >= 9) return;

            // when holding clear key or reassign key, show all
            if (CustomKeyBindSystem.GetKeyBind("ReassignBuildBar").keyValue)
                BuildMenuTool.forceShowAllButtons = true;
            else
                BuildMenuTool.forceShowAllButtons = false;


            if (CustomKeyBindSystem.GetKeyBind("ClearBuildBar").keyValue)
            {
                for (int i = 1; i <= 10; i++)
                {
                    if (!BuildMenuTool.childButtons[i].isPointerEnter) continue;
                    int buildIndex = buildMenu.currentCategory * 100 + i;

                    ConfigEntry<int> result = BuildMenuTool.customBarBind.Bind("BuildBarBinds",
                        buildIndex.ToString(),
                        0,
                        "Cleared by player");
                    result.Value = 0;
                    BuildMenuTool.protos[buildMenu.currentCategory, i] = null;
                    buildMenu.SetCurrentCategory(buildMenu.currentCategory);
                    VFAudio.Create("ui-click-0", null, Vector3.zero, true);
                    return;
                }
            }

        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildMenuTool), "PostLoadData")]
        public static void LoadConfigFile()
        {
            for (int i = 0; i < 16; i++)
            {
                for (int j = 1; j < 11; j++)
                {
                    int buildIndex = i * 100 + j;
                    ItemProto proto = BuildMenuTool.protos[i, j];

                    if (proto != null && proto.ID != 0)
                    {
                        ConfigEntry<int> result = BuildMenuTool.customBarBind.Bind("BuildBarBinds",
                            buildIndex.ToString(),
                            proto.ID,
                            $"Item: {proto.Name.Translate()}");

                        if (result.Value == 0)
                        {
                            BuildMenuTool.protos[i, j] = null;
                        }
                        else if (result.Value > 0 && LDB.items.Exist(result.Value) && result.Value != proto.ID)
                        {
                            BuildMenuTool.protos[i, j] = LDB.items.Select(result.Value);
                        }
                    }
                    else
                    {
                        ConfigEntry<int> result = BuildMenuTool.customBarBind.Bind("BuildBarBinds",
                            buildIndex.ToString(),
                            0,
                            "Unused");

                        if (result.Value > 0 && LDB.items.Exist(result.Value))
                        {
                            BuildMenuTool.protos[i, j] = LDB.items.Select(result.Value);
                        }
                    }
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(RebindBuildBar.Patches), "ResetBuildBarItems")]
        public static void ReloadOneCategoryPostfix(bool heldCtrl)
        {
            if (heldCtrl)
            {
                for (int i = 1; i < 16; i++)
                {
                    BuildMenuTool.extendedCategories[i] = false;
                    for (int j = 1; j < 13; j++)
                    {
                        if (BuildMenuTool.protoIds[i, j] > 0)
                        {
                            ItemProto item = LDB.items.Select(BuildMenuTool.protoIds[i, j]);
                            if (item != null)
                            {
                                BuildMenuTool.protos[i, j] = item;
                                BuildMenuTool.extendedCategories[i] = true;
                            }
                            else
                            {
                                BuildMenuTool.protos[i, j] = null;
                            }
                        }
                        else
                        {
                            BuildMenuTool.protos[i, j] = null;
                        }
                    }
                }
            }
            else
            {
                int i = RebindBuildBar.Patches.buildMenu.currentCategory;
                BuildMenuTool.extendedCategories[i] = false;
                for (int j = 1; j < 13; j++)
                {
                    if (BuildMenuTool.protoIds[i, j] > 0)
                    {
                        ItemProto item = LDB.items.Select(BuildMenuTool.protoIds[i, j]);
                        if (item != null)
                        {
                            BuildMenuTool.protos[i, j] = item;
                            BuildMenuTool.extendedCategories[i] = true;
                        }
                        else
                        {
                            BuildMenuTool.protos[i, j] = null;
                        }
                    }
                    else
                    {
                        BuildMenuTool.protos[i, j] = null;
                    }
                }
            }
            SetConfigFile();
            BuildMenuTool.customBarBind.Save();
        }

        /// <summary>
        /// use protos[] to set config file
        /// </summary>
        public static void SetConfigFile()
        {
            for (int i = 0; i < BuildMenuTool.extendedCategories.Length; i++)
            {
                BuildMenuTool.extendedCategories[i] = false;
            }
            for (int i = 0; i < 16; i++)
            {
                for (int j = 1; j < 13; j++)
                {
                    int buildIndex = i * 100 + j;
                    if (BuildMenuTool.protos[i, j] != null)
                    {
                        ItemProto item = BuildMenuTool.protos[i, j];
                        ConfigEntry<int> result = BuildMenuTool.customBarBind.Bind("BuildBarBinds",
                            buildIndex.ToString(),
                            item.ID,
                            $"Item: {item.Name.Translate()}");
                        result.Value = item.ID;
                        BuildMenuTool.extendedCategories[i] = true;
                    }
                    else
                    {
                        ConfigEntry<int> result = BuildMenuTool.customBarBind.Bind("BuildBarBinds",
                               buildIndex.ToString(),
                               0,
                               "Unused");
                        result.Value = 0;
                    }
                }
            }
        }

        /// <summary>
        /// block the ori rebindBuildBar mod's update when use F1-F10 to rebind, if the hotkey is shown in tier 2 row. And execute tier2 rebind logic.
        /// </summary>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(RebindBuildBar.Patches), "Update")]
        public static bool RebindBuildBarPatchesUpdatePrefix()
        {
            UIBuildMenu buildMenu = RebindBuildBar.Patches.buildMenu;
            if (buildMenu == null || !buildMenu.childGroup.gameObject.activeSelf) return false;
            if (buildMenu.currentCategory < 1 || buildMenu.currentCategory >= 9) return false;
            if (CustomKeyBindSystem.GetKeyBind("ReassignBuildBar").keyValue && BuildMenuTool.hotkeyActivateRow == 1) // block by return false
            {
                for (int j = 1; j <= 10; j++)
                {
                    if (Input.GetKeyDown(KeyCode.F1 + (j - 1)) && VFInput.inScreen && !VFInput.inputing)
                    {
                        int buildIndex = buildMenu.currentCategory * 100 + j;

                        UIItemPickerExtension.Popup(pickerPos, proto =>
                        {
                            if (proto != null && proto.ID != 0)
                            {
                                ConfigEntry<int> result = BuildMenuTool.customBarBind.Bind("BuildBarBinds",
                                    buildIndex.ToString(),
                                    proto.ID,
                                    $"Item: {proto.Name.Translate()}");
                                result.Value = proto.ID;
                                BuildMenuTool.protos[buildMenu.currentCategory, j] = proto;
                                buildMenu.SetCurrentCategory(buildMenu.currentCategory);
                                VFAudio.Create("ui-click-0", null, Vector3.zero, true);
                            }
                        }, true, proto => proto.ModelIndex != 0 && proto.CanBuild);
                        UIRoot.instance.uiGame.itemPicker.OnTypeButtonClick(2);
                        return false;
                    }
                }
                return false;
            }
            return true;
        }

        // reset patch

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildMenuTool), "OnChildButtonClick")]
        public static bool OnChildButtonClickPrefix(int index)
        {
            UIBuildMenu buildMenu = RebindBuildBar.Patches.buildMenu;
            if (buildMenu == null || !buildMenu.childGroup.gameObject.activeSelf) return false;
            if (buildMenu.currentCategory < 1 || buildMenu.currentCategory >= 9) return false;


            if (CustomKeyBindSystem.GetKeyBind("ReassignBuildBar").keyValue)
            {
                int buildIndex = buildMenu.currentCategory * 100 + index;

                UIItemPickerExtension.Popup(pickerPos, proto =>
                {
                    if (proto != null && proto.ID != 0)
                    {
                        ConfigEntry<int> result = BuildMenuTool.customBarBind.Bind("BuildBarBinds",
                            buildIndex.ToString(),
                            proto.ID,
                            $"Item: {proto.Name.Translate()}");
                        result.Value = proto.ID;
                        BuildMenuTool.protos[buildMenu.currentCategory, index] = proto;
                        buildMenu.SetCurrentCategory(buildMenu.currentCategory);
                        VFAudio.Create("ui-click-0", null, Vector3.zero, true);
                    }
                }, true, proto => proto.ModelIndex != 0 && proto.CanBuild);
                UIRoot.instance.uiGame.itemPicker.OnTypeButtonClick(2);
                return false;
            }
            return true;
        }
    }
}
