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
    [BepInDependency(BuildMenuToolPlugin.GUID)]
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
            BuildMenuToolPlugin.RebindBuildBarCompatibility = true;
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
                BuildMenuToolPlugin.forceShowAllButtons = true;
            else
                BuildMenuToolPlugin.forceShowAllButtons = false;


            if (CustomKeyBindSystem.GetKeyBind("ClearBuildBar").keyValue)
            {
                for (int i = 1; i <= 10; i++)
                {
                    if (!BuildMenuToolPlugin.childButtons[i].isPointerEnter) continue;
                    int buildIndex = buildMenu.currentCategory * 100 + i;

                    ConfigEntry<int> result = BuildMenuToolPlugin.customBarBind.Bind("BuildBarBinds",
                        buildIndex.ToString(),
                        0,
                        "Cleared by player");
                    result.Value = 0;
                    BuildMenuToolPlugin.protos[buildMenu.currentCategory, i] = null;
                    buildMenu.SetCurrentCategory(buildMenu.currentCategory);
                    VFAudio.Create("ui-click-0", null, Vector3.zero, true);
                    BuildMenuToolPlugin.RefreshCategoryIfExtended(buildMenu.currentCategory);
                    return;
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildMenuToolPlugin), "PostLoadData")]
        public static void LoadConfigFile()
        {
            for (int i = 0; i < 16; i++)
            {
                for (int j = 1; j < 11; j++)
                {
                    int buildIndex = i * 100 + j;
                    ItemProto proto = BuildMenuToolPlugin.protos[i, j];

                    if (proto != null && proto.ID != 0)
                    {
                        ConfigEntry<int> result = BuildMenuToolPlugin.customBarBind.Bind("BuildBarBinds",
                            buildIndex.ToString(),
                            proto.ID,
                            $"Item: {proto.Name.Translate()}");

                        if (result.Value == 0)
                        {
                            BuildMenuToolPlugin.protos[i, j] = null;
                        }
                        else if (result.Value > 0 && LDB.items.Exist(result.Value) && result.Value != proto.ID)
                        {
                            BuildMenuToolPlugin.protos[i, j] = LDB.items.Select(result.Value);
                        }
                        else if (result.Value < 0) // if unused, occupy
                        {
                            result.Value = proto.ID;
                        }
                    }
                    else
                    {
                        ConfigEntry<int> result = BuildMenuToolPlugin.customBarBind.Bind("BuildBarBinds",
                            buildIndex.ToString(),
                            -1,
                            "Unused"); // unused items will set to -1 to wait for new mods adding new items to the slot

                        if (result.Value > 0 && LDB.items.Exist(result.Value))
                        {
                            BuildMenuToolPlugin.protos[i, j] = LDB.items.Select(result.Value);
                            BuildMenuToolPlugin.extendedCategories[i] = true;
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
                    BuildMenuToolPlugin.extendedCategories[i] = false;
                    for (int j = 1; j < 13; j++)
                    {
                        if (BuildMenuToolPlugin.protoIds[i, j] > 0)
                        {
                            ItemProto item = LDB.items.Select(BuildMenuToolPlugin.protoIds[i, j]);
                            if (item != null)
                            {
                                BuildMenuToolPlugin.protos[i, j] = item;
                                BuildMenuToolPlugin.extendedCategories[i] = true;
                            }
                            else
                            {
                                BuildMenuToolPlugin.protos[i, j] = null;
                            }
                        }
                        else
                        {
                            BuildMenuToolPlugin.protos[i, j] = null;
                        }
                    }
                }
            }
            else
            {
                int i = RebindBuildBar.Patches.buildMenu.currentCategory;
                BuildMenuToolPlugin.extendedCategories[i] = false;
                for (int j = 1; j < 13; j++)
                {
                    if (BuildMenuToolPlugin.protoIds[i, j] > 0)
                    {
                        ItemProto item = LDB.items.Select(BuildMenuToolPlugin.protoIds[i, j]);
                        if (item != null)
                        {
                            BuildMenuToolPlugin.protos[i, j] = item;
                            BuildMenuToolPlugin.extendedCategories[i] = true;
                        }
                        else
                        {
                            BuildMenuToolPlugin.protos[i, j] = null;
                        }
                    }
                    else
                    {
                        BuildMenuToolPlugin.protos[i, j] = null;
                    }
                }
            }
            SetConfigFile();
            BuildMenuToolPlugin.customBarBind.Save();
        }

        /// <summary>
        /// use protos[] to set config file
        /// </summary>
        public static void SetConfigFile() // Used by Reset Method
        {
            for (int i = 0; i < BuildMenuToolPlugin.extendedCategories.Length; i++)
            {
                BuildMenuToolPlugin.extendedCategories[i] = false;
            }
            for (int i = 0; i < 16; i++)
            {
                if (i != RebindBuildBar.Patches.buildMenu.currentCategory && !CustomKeyBindSystem.GetKeyBind("ReassignBuildBar").keyValue) // if not reseting all category, not current category either, then skip
                    continue;
                for (int j = 1; j < 13; j++)
                {
                    int buildIndex = i * 100 + j;
                    if (BuildMenuToolPlugin.protos[i, j] != null)
                    {
                        ItemProto item = BuildMenuToolPlugin.protos[i, j];
                        ConfigEntry<int> result = BuildMenuToolPlugin.customBarBind.Bind("BuildBarBinds",
                            buildIndex.ToString(),
                            item.ID,
                            $"Item: {item.Name.Translate()}");
                        result.Value = item.ID;
                        BuildMenuToolPlugin.extendedCategories[i] = true;
                    }
                    else
                    {
                        ConfigEntry<int> result = BuildMenuToolPlugin.customBarBind.Bind("BuildBarBinds",
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
            if (CustomKeyBindSystem.GetKeyBind("ReassignBuildBar").keyValue && BuildMenuToolPlugin.hotkeyActivateRow == 1) // block by return false
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
                                ConfigEntry<int> result = BuildMenuToolPlugin.customBarBind.Bind("BuildBarBinds",
                                    buildIndex.ToString(),
                                    proto.ID,
                                    $"Item: {proto.Name.Translate()}");
                                result.Value = proto.ID;
                                BuildMenuToolPlugin.protos[buildMenu.currentCategory, j] = proto;
                                BuildMenuToolPlugin.extendedCategories[buildMenu.currentCategory] = true;
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
        [HarmonyPatch(typeof(BuildMenuToolPlugin), "OnChildButtonClick")]
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
                        ConfigEntry<int> result = BuildMenuToolPlugin.customBarBind.Bind("BuildBarBinds",
                            buildIndex.ToString(),
                            proto.ID,
                            $"Item: {proto.Name.Translate()}");
                        result.Value = proto.ID;
                        BuildMenuToolPlugin.protos[buildMenu.currentCategory, index] = proto;
                        BuildMenuToolPlugin.extendedCategories[buildMenu.currentCategory] = true;
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
