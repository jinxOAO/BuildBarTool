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

namespace BuildBarTool
{
    [BepInDependency(RebindBuildBarPlugin.MODGUID)]
    [BepInDependency(BuildBarToolPlugin.GUID)]
    [BepInPlugin(GUID, NAME, VERSION)]
    internal class RebindBuildBarCompat : BaseUnityPlugin
    {
        public const string GUID = "Gnimaerd.DSP.plugin.BuildBarTool_RebindBuildBarCompat";
        public const string NAME = "BuildBarTool_RebindBuildBarCompat";
        public const string VERSION = "0.1.0";


        internal static ManualLogSource logger;

        public static Vector2 pickerPos = new Vector2(-300, 238);

        public void Awake()
        {
            logger = base.Logger;
            Harmony.CreateAndPatchAll(typeof(RebindBuildBarCompat));
            BuildBarToolPlugin.RebindBuildBarCompatibility = true;
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
                BuildBarToolPlugin.forceShowAllButtons = true;
            else
                BuildBarToolPlugin.forceShowAllButtons = false;


            if (CustomKeyBindSystem.GetKeyBind("ClearBuildBar").keyValue)
            {
                for (int i = 1; i <= 10; i++)
                {
                    if (!BuildBarToolPlugin.childButtons[i].isPointerEnter) continue;
                    int buildIndex = buildMenu.currentCategory * 100 + i;

                    ConfigEntry<int> result = BuildBarToolPlugin.customBarBind.Bind("BuildBarBinds",
                        buildIndex.ToString(),
                        0,
                        "Cleared by player");
                    result.Value = 0;
                    BuildBarToolPlugin.protos[buildMenu.currentCategory, i] = null;
                    buildMenu.SetCurrentCategory(buildMenu.currentCategory);
                    VFAudio.Create("ui-click-0", null, Vector3.zero, true);
                    BuildBarToolPlugin.RefreshCategoryIfExtended(buildMenu.currentCategory);
                    return;
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildBarToolPlugin), "PostLoadData")]
        public static void LoadConfigFile()
        {
            for (int i = 0; i < 16; i++)
            {
                for (int j = 1; j < 11; j++)
                {
                    int buildIndex = i * 100 + j;
                    ItemProto proto = BuildBarToolPlugin.protos[i, j];

                    if (proto != null && proto.ID != 0)
                    {
                        ConfigEntry<int> result = BuildBarToolPlugin.customBarBind.Bind("BuildBarBinds",
                            buildIndex.ToString(),
                            proto.ID,
                            $"Item: {proto.Name.Translate()}");

                        if (result.Value == 0)
                        {
                            BuildBarToolPlugin.protos[i, j] = null;
                        }
                        else if (result.Value > 0 && LDB.items.Exist(result.Value) && result.Value != proto.ID)
                        {
                            BuildBarToolPlugin.protos[i, j] = LDB.items.Select(result.Value);
                        }
                        else if (result.Value < 0) // if unused, occupy
                        {
                            result.Value = proto.ID;
                        }
                    }
                    else
                    {
                        ConfigEntry<int> result = BuildBarToolPlugin.customBarBind.Bind("BuildBarBinds",
                            buildIndex.ToString(),
                            -1,
                            "Unused"); // unused items will set to -1 to wait for new mods adding new items to the slot

                        if (result.Value > 0 && LDB.items.Exist(result.Value))
                        {
                            BuildBarToolPlugin.protos[i, j] = LDB.items.Select(result.Value);
                            BuildBarToolPlugin.extendedCategories[i] = true;
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
                    BuildBarToolPlugin.extendedCategories[i] = false;
                    for (int j = 1; j < 13; j++)
                    {
                        if (BuildBarToolPlugin.protoIds[i, j] > 0)
                        {
                            ItemProto item = LDB.items.Select(BuildBarToolPlugin.protoIds[i, j]);
                            if (item != null)
                            {
                                BuildBarToolPlugin.protos[i, j] = item;
                                BuildBarToolPlugin.extendedCategories[i] = true;
                            }
                            else
                            {
                                BuildBarToolPlugin.protos[i, j] = null;
                            }
                        }
                        else
                        {
                            BuildBarToolPlugin.protos[i, j] = null;
                        }
                    }
                }
            }
            else
            {
                int i = RebindBuildBar.Patches.buildMenu.currentCategory;
                BuildBarToolPlugin.extendedCategories[i] = false;
                for (int j = 1; j < 13; j++)
                {
                    if (BuildBarToolPlugin.protoIds[i, j] > 0)
                    {
                        ItemProto item = LDB.items.Select(BuildBarToolPlugin.protoIds[i, j]);
                        if (item != null)
                        {
                            BuildBarToolPlugin.protos[i, j] = item;
                            BuildBarToolPlugin.extendedCategories[i] = true;
                        }
                        else
                        {
                            BuildBarToolPlugin.protos[i, j] = null;
                        }
                    }
                    else
                    {
                        BuildBarToolPlugin.protos[i, j] = null;
                    }
                }
            }
            SetConfigFile();
            BuildBarToolPlugin.customBarBind.Save();
        }

        /// <summary>
        /// use protos[] to set config file
        /// </summary>
        public static void SetConfigFile() // Used by Reset Method
        {
            for (int i = 0; i < BuildBarToolPlugin.extendedCategories.Length; i++)
            {
                BuildBarToolPlugin.extendedCategories[i] = false;
            }
            for (int i = 0; i < 16; i++)
            {
                if (i != RebindBuildBar.Patches.buildMenu.currentCategory && !CustomKeyBindSystem.GetKeyBind("ReassignBuildBar").keyValue) // if not reseting all category, not current category either, then skip
                    continue;
                for (int j = 1; j < 13; j++)
                {
                    int buildIndex = i * 100 + j;
                    if (BuildBarToolPlugin.protos[i, j] != null)
                    {
                        ItemProto item = BuildBarToolPlugin.protos[i, j];
                        ConfigEntry<int> result = BuildBarToolPlugin.customBarBind.Bind("BuildBarBinds",
                            buildIndex.ToString(),
                            item.ID,
                            $"Item: {item.Name.Translate()}");
                        result.Value = item.ID;
                        BuildBarToolPlugin.extendedCategories[i] = true;
                    }
                    else
                    {
                        ConfigEntry<int> result = BuildBarToolPlugin.customBarBind.Bind("BuildBarBinds",
                               buildIndex.ToString(),
                               -1,
                               "Unused");
                        result.Value = -1;
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
            if (CustomKeyBindSystem.GetKeyBind("ReassignBuildBar").keyValue && BuildBarToolPlugin.hotkeyActivateRow == 1) // block by return false
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
                                ConfigEntry<int> result = BuildBarToolPlugin.customBarBind.Bind("BuildBarBinds",
                                    buildIndex.ToString(),
                                    proto.ID,
                                    $"Item: {proto.Name.Translate()}");
                                result.Value = proto.ID;
                                BuildBarToolPlugin.protos[buildMenu.currentCategory, j] = proto;
                                BuildBarToolPlugin.extendedCategories[buildMenu.currentCategory] = true;
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
        [HarmonyPatch(typeof(BuildBarToolPlugin), "OnChildButtonClick")]
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
                        ConfigEntry<int> result = BuildBarToolPlugin.customBarBind.Bind("BuildBarBinds",
                            buildIndex.ToString(),
                            proto.ID,
                            $"Item: {proto.Name.Translate()}");
                        result.Value = proto.ID;
                        BuildBarToolPlugin.protos[buildMenu.currentCategory, index] = proto;
                        BuildBarToolPlugin.extendedCategories[buildMenu.currentCategory] = true;
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
