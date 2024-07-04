using CommonAPI.Systems.ModLocalization;
using CommonAPI.Systems;
using CommonAPI;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xiaoye97;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BepInEx.Logging;
using xiaoye97.Patches;

namespace BuildBarTool
{
    [BepInDependency(LDBToolPlugin.MODGUID)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(TabSystem), nameof(LocalizationModule))]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class BuildBarToolPlugin : BaseUnityPlugin
    {
        public const string GUID = "Gnimaerd.DSP.plugin.BuildBarTool";
        public const string NAME = "BuildBarTool";
        public const string VERSION = "1.0.0";
        internal static bool developerMode = true;

        public static ConfigFile customBarBind = new ConfigFile($"{Paths.ConfigPath}/RebindBuildBar/CustomBarBindTier2.cfg", true);

        internal static ManualLogSource logger;

        public static List<GameObject> childButtonObjs = new List<GameObject>();
        public static List<UIButton> childButtons = new List<UIButton>();
        public static List<Image> childIcons = new List<Image>();
        public static List<Text> childNumTexts = new List<Text>();
        public static List<Text> childTips = new List<Text>();
        public static int[,] protoIds = new int[16, 13]; // int[category, buildBarIndex]
        public static ItemProto[,] protos = new ItemProto[16, 13]; // int[category, buildBarIndex]
        public static bool[] extendedCategories = new bool[16]; // category that is extended to 2 lines will be set to true
        public static List<CanvasGroup> childCanvasGroups = new List<CanvasGroup>();
        public static List<Text> childHotkeyText = new List<Text>();  // F1-F12快捷键文本
        public static List<Text> oriChildHotkeyText = new List<Text>();  // 原始按钮的F1-F12快捷键文本
        public static GameObject switchHotkeyRowText;

        //public static int categoryBias = 13;
        public static int hotkeyActivateRow = 0;

        public static float secondLevelAlpha = 1f;
        public static int dblClickIndex = 0;
        public static double dblClickTime = 0;


        // compatible fields
        public static bool forceShowAllButtons = false; // if holding rebind key, force to show all buttons
        public static bool GenesisCompatibility = false;
        public static bool RebindBuildBarCompatibility = false;

        public static Color textColor = new Color(0.3821f, 0.8455f, 1f, 0.7843f);
        public static Color lockedTextColor = new Color(0.8f, 0, 0, 0.5f);

        public static Color normalColor = new Color(0.588f, 0.588f, 0.588f, 0.7f);
        public static Color disabledColor = new Color(0.2f, 0.15f, 0.15f, 0.7f);
        public static Color disabledColor2 = new Color(0.316f, 0.281f, 0.281f, 0.7f);

        // 在设置 locked 的建筑的icon颜色时，正常会调用UIBuildMenu的_OnUpdate(的postpatch)设置为此mod中的disableColor（记为颜色A）后，立即由该icon的父级gameobj的UIButton的LateUpdate，修改该UIButton.transition[1].target（也就是这个icon的Image(Graphic)）的color，根据Transition的damp(0.3)设置颜色为原颜色A和transition的normalColor按照damp的过渡色，这个是实际显示的颜色。但是，按住ctrl（准备rebind的绑定按键，在此mod里会导致所有框全部强制显示）切换category时，发现LateUpdate不会每帧执行，奇怪。因此颜色不会从A改变为过渡色，出现了不一致。不按住Ctrl切换category却不会阻止LateUpdate。
        // 找到了直接原因：上述UIButton的updating在这种情况下是false，但是导致它是false的原因未知。原本游戏中第一行按键的updating属性是true的原因很可能是UIBuildMenu.UpdateUXPanel修改的(它被_OnUpdate调用)。但是奇怪的是为什么我不按Ctrl的时候，第二行UIButton的updating会被设置为true？谁设置的？推测是UIButton.OnEnabled()
        // 尽管没有找到上述最后那个疑问的答案，但是貌似找到了解决此问题的办法。此外，RebindBuildBar原本的mod也会出现此bug，但仅仅会在开始游戏（不确定是开始游戏还是载入存档的）第一次打开建造栏时出现，后续无论怎么切换都不会出现问题。

        public static string lockedText = "";

        public void Awake()
        {
            BBTProtos.AddLocalizationProtos();
            logger = base.Logger;
            InitStaticData();
            Harmony.CreateAndPatchAll(typeof(BuildBarToolPlugin));
        }

        public void Start()
        {
            InitAll();
        }

        public void Update()
        {

        }

        public static void InitAll()
        {
            InitUI();
        }

        public static void InitStaticData()
        {
            for (int i = 0; i <extendedCategories.Length; i++)
            {
                extendedCategories[i] = false;
            }
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                    protoIds[i, j] = 0;
                }
            }
        }

        public static void InitUI()
        {
            // 修改底部栏位高度大小、修正文本倾斜度，修正沙盒模式额外面板的位置
            GameObject mainBg = GameObject.Find("UI Root/Overlay Canvas/In Game/Function Panel/bg-trans");
            GameObject sandboxBg = GameObject.Find("UI Root/Overlay Canvas/In Game/Function Panel/bg-trans/sandbox-btn");
            GameObject sandboxTitle = GameObject.Find("UI Root/Overlay Canvas/In Game/Function Panel/bg-trans/sandbox-btn/title");

            float oriXM = mainBg.GetComponent<RectTransform>().sizeDelta.x;
            mainBg.GetComponent<RectTransform>().sizeDelta = new Vector2(oriXM, 228);
            float oriXSB = sandboxBg.GetComponent<RectTransform>().sizeDelta.x;
            sandboxBg.GetComponent<RectTransform>().sizeDelta = new Vector2(oriXSB, 228);
            sandboxTitle.transform.rotation = Quaternion.AngleAxis(77, new Vector3(0, 0, 1));
            sandboxTitle.transform.localPosition = new Vector3(81, -50, 0);

            // 新增按钮
            GameObject oriButtonObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Function Panel/Build Menu/child-group/button-1");
            Transform parent = GameObject.Find("UI Root/Overlay Canvas/In Game/Function Panel/Build Menu/child-group").transform;
            UIButton oriUIButton = oriButtonObj.GetComponent<UIButton>();
            float oriX1 = oriButtonObj.transform.localPosition.x;
            float oriY1 = oriButtonObj.transform.localPosition.y;

            for (int i = 0; i < 11; i++)
            {
                //UIButton uibtn = GameObject.Instantiate(oriUIButton, oriUIButton.transform.parent);
                //var oldbutton = uibtn.gameObject.GetComponent<Button>();
                //var btn = uibtn.gameObject.GetComponent<UIButton>();
                //if (btn != null && oldbutton != null)
                //{
                //    UnityEngine.Object.DestroyImmediate(oldbutton);
                //    btn.button = uibtn.gameObject.AddComponent<Button>();
                //}
                //btn.button.onClick.AddListener(()=> { OnChildButtonClick(1); });
                //btn.gameObject.transform.localPosition = new Vector3(oriX1 + i * 52, oriY1 + 60, 0);
                //btn.gameObject.name = $"button-up-{i + 1}"; // 这个也是可用的，是巨佬原本的方法

                // 感谢巨佬Awbugl的帮忙
                GameObject buildBtnObj = GameObject.Instantiate(oriButtonObj, parent);
                buildBtnObj.name = $"button-up-{i}";
                buildBtnObj.transform.localPosition = new Vector3(oriX1 + i * 52 - 52, oriY1 + 60, 0);
                buildBtnObj.AddComponent<CanvasGroup>();
                GameObject.DestroyImmediate(buildBtnObj.GetComponent<Button>()); // 必须先DestoryButton
                buildBtnObj.AddComponent<Button>(); // 再Add新的
                //buildBtnObj.GetComponent<Button>().onClick.RemoveAllListeners(); // 这个此处无效，用这个办法会无法移除原本的点击功能，必须用上面先Destory后Add的方法。
                string ii = i.ToString();
                buildBtnObj.GetComponent<Button>().onClick.AddListener(() => { OnChildButtonClick(Convert.ToInt32(ii)); }); // 为什么这里直接传入i会导致所有按钮都是i=9的状态，但是转换一下string就行了？！

                UIButton uiBtn = buildBtnObj.GetComponent<UIButton>();
                uiBtn.button = buildBtnObj.GetComponent<Button>();

                childButtonObjs.Add(buildBtnObj);
                childButtons.Add(uiBtn);
                childIcons.Add(buildBtnObj.transform.Find("icon").GetComponent<Image>());
                childNumTexts.Add(buildBtnObj.transform.Find("count").GetComponent<Text>());
                childCanvasGroups.Add(buildBtnObj.GetComponent<CanvasGroup>());
                childHotkeyText.Add(buildBtnObj.transform.Find("text").GetComponent<Text>());
            }
            for (int i = 0; i < 10; i++)
            {
                oriChildHotkeyText.Add(GameObject.Find($"UI Root/Overlay Canvas/In Game/Function Panel/Build Menu/child-group/button-{i + 1}/text").GetComponent<Text>());
            }

            // 切换快捷键在哪行的提示文本
            GameObject oriTextObj = oriButtonObj.transform.Find("text").gameObject;
            switchHotkeyRowText = GameObject.Instantiate(oriTextObj, parent);
            switchHotkeyRowText.name = "switch-note-text";
            switchHotkeyRowText.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 160);
            switchHotkeyRowText.GetComponent<Text>().text = "切换快捷键".Translate();
            switchHotkeyRowText.GetComponent<Text>().fontSize = 14;
            int YPos = 155;
            if (RebindBuildBarCompatibility)
                YPos = 180;
            switchHotkeyRowText.transform.localPosition = new Vector3(200, YPos, 0);
            if (GenesisCompatibility)
            {
                switchHotkeyRowText.transform.localPosition = new Vector3(190, YPos, 0);
            }
            childCanvasGroups.Add(switchHotkeyRowText.AddComponent<CanvasGroup>());
        }

        // 在LDBTool完成postLoad之后进行
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload_Patch), "VFPreloadPostPatch")]
        public static void PostLoadData()
        {
            // load from data by BuildBar.Bind()
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                    if (protoIds[i, j] > 0)
                    {
                        ItemProto item = LDB.items.Select(protoIds[i, j]);
                        if (item != null)
                        {
                            protos[i, j] = item;
                            logger.LogInfo(string.Format("Set build bar at {0},{1} (tier 2) ID:{2} name:{3}", new object[]
                            {
                                i,
                                j,
                                item.ID,
                                item.Name.Translate()
                            }));
                            extendedCategories[i] = true;
                        }
                        else
                        {
                            logger.LogWarning(string.Format("Set Build Bar Fail because item ID:{0} is null.", protoIds[i, j]));
                        }
                    }
                }
            }

            // load from data in itemProto.BuildIndex
            LoadFromBuildIndex();
            // init text translate
            lockedText = "gmLockedItemText".Translate();
        }

        /// <summary>
        /// When UIBuildMenu StaticLoad, load the buildindex data from itemProto.BuildIndex
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIBuildMenu), "StaticLoad")]
        public static bool StaticLoadPrefix()
        {
            if (!UIBuildMenu.staticLoaded)
            { 
                LoadFromBuildIndex();
            }
            return true;
        }


        public static void LoadFromBuildIndex()
        {
            for (int i = 0; i < LDB.items.dataArray.Length; i++)
            {
                int buildIndex = LDB.items.dataArray[i].BuildIndex;
                if (buildIndex > 0)
                {
                    int num = buildIndex / 100;
                    int num2 = buildIndex % 100 - 20; // c21-c30, c = category
                    if (num <= 15 && num2 >= 0 && num2 <= 12)
                    {
                        protoIds[num, num2] = LDB.items.dataArray[i].ID;
                        protos[num, num2] = LDB.items.dataArray[i];
                        extendedCategories[num] = true;
                        logger.LogInfo(string.Format("Set build bar at {0},{1} (tier 2) from ItemProto.BuildIndex, ID:{2} name:{3}", new object[]
                        {
                            num,
                            num2,
                            LDB.items.dataArray[i].ID,
                            LDB.items.dataArray[i].Name.Translate()
                        }));
                    }
                }
            }
        }




        /// <summary>
        /// 将点击某类时，整个面板起来得更高，可以容纳两行建造按钮
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIFunctionPanel), "_OnUpdate")]
        public static void UIBuildMenuOnUpdatePostPatch(ref UIFunctionPanel __instance)
        {

            var _this = __instance;
            bool guideComplete = GameMain.data.guideComplete;
            float oriPosWanted = 0f;
            _this.posWanted = -60; // 对应游戏的0的显示效果
            if (guideComplete)
            {
                if (_this.buildMenu.active)
                {
                    if (extendedCategories[_this.buildMenu.currentCategory] || forceShowAllButtons)
                    {
                        bool unlocked = forceShowAllButtons;
                        if (UIRoot.instance?.uiGame?.buildMenu?.showButtonsAnyways != null)
                            unlocked = unlocked || UIRoot.instance.uiGame.buildMenu.showButtonsAnyways;
                        // show the row2 only if at least one item in this category in row2 is unlocked. or when holding rebind key (showAllButtons is true), or sandbox mode show all buttons.
                        GameHistoryData history = GameMain.data.history;
                        int category = _this.buildMenu.currentCategory;
                        for (int i = 0; i < 12; i++)
                        {
                            ItemProto item = protos[category, i];
                            int itemId = item != null ? item.ID : 0;
                            if(itemId > 0 && history.ItemUnlocked(itemId))
                            {
                                unlocked = true;
                                break;
                            }
                        }
                        if (unlocked)
                            _this.posWanted = 0f;
                        else
                        {
                            _this.posWanted = -135f;
                            oriPosWanted = -75f;
                        }
                    }
                    else
                    {
                        _this.posWanted = -135f;
                        oriPosWanted = -75f;
                    }
                    if (_this.buildMenu.isDismantleMode)
                    {
                        _this.posWanted = -75f;
                    }
                    if (_this.buildMenu.isUpgradeMode)
                    {
                        _this.posWanted = -75f;
                    }
                }
                else if (_this.sandboxMenu.active)
                {
                    if (_this.sandboxMenu.childGroup.activeSelf)
                    {
                        _this.posWanted = -60f;
                    }
                    else
                    {
                        _this.posWanted = -135f;
                        oriPosWanted = -75f;
                    }
                    if (_this.sandboxMenu.isRemovalMode)
                    {
                        _this.posWanted = -75f;
                    }
                }
                else if (_this.mainPlayer.controller.actionBuild.blueprintMode > EBlueprintMode.None)
                {
                    _this.posWanted = -193f;
                    oriPosWanted = -133f;
                }
                else
                {
                    _this.posWanted = -195f;
                    oriPosWanted = -135f;
                }
            }
            else
            {
                _this.posWanted = -195f;
                oriPosWanted = -135f;
            }

            //下面这段用于抵消原本函数中进行了Lerp.Tween的效果：
            float target = oriPosWanted;
            float now = _this.pos;
            float speed = 18f;
            if (!((double)Mathf.Abs(target - now) < 1E-05))
            {
                float deltaTime = Time.deltaTime;
                float t;
                if (deltaTime > 0.01f)
                {
                    float f = 1f - speed * 0.01f;
                    t = 1f - Mathf.Pow(f, deltaTime * 100f);
                }
                else
                {
                    t = speed * deltaTime;
                }
                _this.pos = (_this.pos - t * target) / (1 - t);
            }
            // 然后再执行本应仅执行的Tween
            _this.pos = Lerp.Tween(_this.pos, _this.posWanted, 18f);

            _this.sandboxTitleObject.SetActive(_this.pos > -150f);
            _this.sandboxIconObject.SetActive(_this.pos <= -150f);
            _this.bgTrans.anchoredPosition = new Vector2(_this.bgTrans.anchoredPosition.x, _this.pos);
            _this.sandboxRect.anchoredPosition = new Vector2(Mathf.Clamp(-_this.pos * 2f - 320f, -50f, 10f), 0f);

            // 以下是为了在从category不为双行的，但是展开状态切换成双行的时候，让第二行的图标能渐变出现，而非在底部背景未完全展开到需要高度之前就立刻完全显示出来
            if (_this.pos > -10)
            {
                float targetAlpha = Mathf.Clamp01(secondLevelAlpha + 0.02f);
                if (secondLevelAlpha != targetAlpha)
                {
                    secondLevelAlpha = targetAlpha;
                    for (int i = 0; i < childCanvasGroups.Count; i++) // i==10是提示文本的canvasGroup，不是按键
                    {
                        childCanvasGroups[i].alpha = secondLevelAlpha;
                    }
                }
            }
            else
            {
                secondLevelAlpha = 0;
                for (int i = 0; i < childCanvasGroups.Count; i++)
                {
                    childCanvasGroups[i].alpha = secondLevelAlpha;
                }
            }
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(UIButton), "OnEnable")]
        //public static void testenable(ref UIButton __instance)
        //{
        //    logger.LogInfo($"{__instance.gameObject.name} onenable");
        //}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIBuildMenu), "SetCurrentCategory")]
        public static void UIBuildMenuSetCurrentCategoryPostPatch(ref UIBuildMenu __instance, int category)
        {
            var _this = __instance;
            if (_this.player != null)
            {
                int num = _this.currentCategory;
                _this.currentCategory = category;
                if (num != _this.currentCategory)
                {
                    _this.player.controller.actionPick.pickMode = false;
                }
                GameHistoryData history = GameMain.history;

                if (extendedCategories[category] || RebindBuildBarCompatibility)
                {
                    StorageComponent package = _this.player.package;
                    for (int i = 1; i < 11; i++)
                    {
                        if (childButtons[i] != null)
                        {
                            if (protos[category, i] != null && (protos[category, i].IsEntity || protos[category, i].BuildMode == 4))
                            {
                                int id = protos[category, i].ID;
                                if (history.ItemUnlocked(id) || RebindBuildBarCompatibility)
                                {
                                    int num2 = package.GetItemCount(protos[category, i].ID);
                                    if(DeliverySlotsTweaksCompat.enabled)
                                    {
                                        num2 += _this.player.deliveryPackage.GetItemCount(id);
                                    }
                                    if (protos[category, i].ID == _this.player.inhandItemId)
                                    {
                                        num2 += _this.player.inhandItemCount;
                                    }
                                    childIcons[i].enabled = true;
                                    childButtons[i].OnEnable(); // 执行以将UIButton.updating设置为true来防止颜色不一致问题
                                    childIcons[i].color = normalColor;
                                    childButtons[i].transitions[1].normalColor = normalColor;
                                    childIcons[i].sprite = protos[category, i].iconSprite;
                                    StringBuilderUtility.WriteKMG(_this.strb, 5, (long)num2, false);
                                    childNumTexts[i].text = ((num2 > 0) ? _this.strb.ToString().Trim() : "");
                                    childNumTexts[i].color = textColor;
                                    childButtons[i].button.interactable = true;
                                    //childTips[i].color = _this.tipTextColor;
                                    if(!history.ItemUnlocked(id))
                                    {
                                        childNumTexts[i].text = lockedText;
                                        childNumTexts[i].color = lockedTextColor;
                                        childIcons[i].color = disabledColor2;
                                        childButtons[i].transitions[1].normalColor = disabledColor2;
                                    }
                                    childButtonObjs[i].SetActive(true);

                                }
                                else
                                {
                                    childButtonObjs[i].SetActive(false);
                                    childIcons[i].enabled = false;
                                    childIcons[i].sprite = null;
                                    StringBuilderUtility.WriteKMG(_this.strb, 5, 0L, false);
                                    childNumTexts[i].text = "";
                                }
                            }
                            else
                            {
                                childButtonObjs[i].SetActive(false);
                                childIcons[i].enabled = false;
                                childIcons[i].sprite = null;
                                StringBuilderUtility.WriteKMG(_this.strb, 5, 0L, false);
                                childNumTexts[i].text = "";
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 1; i < 11; i++)
                    {
                        childButtons[i].gameObject.SetActive(false);
                        childIcons[i].sprite = null;
                        StringBuilderUtility.WriteKMG(_this.strb, 5, 0L, false);
                        childNumTexts[i].text = "";
                    }
                }
            }
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIBuildMenu), "_OnUpdate")]
        public static bool UIBuildMenuOnUpdatePrePatch(ref UIBuildMenu __instance)
        {
            bool oriFlag = VFInput.inputing;
            if (extendedCategories[__instance.currentCategory] && hotkeyActivateRow == 1) // 如果是第二行快捷键状态，通过让VFInput.inputing = true 拦截可能在原方法内触发的第一行的OnChildButtonClick
            {
                VFInput.inputing = true;
            }
            // 但如果按键是1234567等等就得不拦截，还原回其状态
            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + (i - 1)))
                {
                    VFInput.inputing = oriFlag;
                    break;
                }
            }
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.X))
            {
                VFInput.inputing = oriFlag;
            }
            return true;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIBuildMenu), "_OnUpdate")]
        public static void UIBuildMenuOnUpdatePostPatch(ref UIBuildMenu __instance)
        {
            var _this = __instance;
            int category = _this.currentCategory;
            if (extendedCategories[category] || forceShowAllButtons)
            {
                GameHistoryData history = GameMain.history;
                StorageComponent package = _this.player.package;

                // 快捷键
                VFInput.inputing = (EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null);
                if (_this.childGroup.gameObject.activeSelf && hotkeyActivateRow == 1)
                {
                    for (int j = 1; j <= 10; j++)
                    {
                        if (Input.GetKeyDown(KeyCode.F1 + (j - 1)) && VFInput.inScreen && VFInput.readyToBuild && !VFInput.inputing)
                        {
                            _this.isKeyDownCallingAudio = true;
                            OnChildButtonClick(j);
                            VFAudio.Create("ui-click-0", null, Vector3.zero, true, 0, -1, -1L);
                        }
                    }
                }

                int num2 = 1;
                while (num2 < 12 && num2 < childNumTexts.Count)
                {
                    if (protos[category, num2] != null)
                    {
                        int id2 = protos[category, num2].ID;
                        bool unlocked = history.ItemUnlocked(id2) || _this.showButtonsAnyways;
                        if (unlocked || (RebindBuildBarCompatibility && forceShowAllButtons))
                        {
                            childIcons[num2].enabled = true;
                            childIcons[num2].color = normalColor;
                            childButtons[num2].transitions[1].normalColor = normalColor;
                            childButtons[num2].OnEnable(); // 执行以将UIButton.updating设置为true来防止颜色不一致问题
                            childButtons[num2].tips.itemId = id2;
                            childButtons[num2].tips.itemInc = 0;
                            childButtons[num2].tips.itemCount = 0;
                            childButtons[num2].tips.corner = 8;
                            childButtons[num2].tips.delay = 0.2f;
                            childButtons[num2].tips.type = UIButton.ItemTipType.Other;
                            int num3 = package.GetItemCount(id2);
                            bool flag2 = _this.player.inhandItemId == id2;
                            if (flag2)
                            {
                                num3 += _this.player.inhandItemCount;
                            }
                            if(DeliverySlotsTweaksCompat.enabled)
                            {
                                num3 += _this.player.deliveryPackage.GetItemCount(id2);
                            }
                            StringBuilderUtility.WriteKMG(_this.strb, 5, (long)num3, false);
                            childNumTexts[num2].text = ((num3 > 0) ? _this.strb.ToString().Trim() : "");
                            childNumTexts[num2].color = textColor;
                            childButtons[num2].button.interactable = true;
                            if (childIcons[num2].sprite == null && protos[category, num2] != null)
                            {
                                childIcons[num2].sprite = protos[category, num2].iconSprite;
                            }
                            //childTips[num2].color = _this.tipTextColor;
                            childButtons[num2].highlighted = flag2;

                            if(!unlocked)
                            {
                                childNumTexts[num2].text = lockedText;
                                childNumTexts[num2].color = lockedTextColor;
                                childIcons[num2].color = disabledColor2;
                                childButtons[num2].transitions[1].normalColor = disabledColor2; // 防止闪烁情况发生，阻止颜色折中效果，将两端颜色设为一致，防止帧前后（因为UIButton的Update导致）颜色跳变
                            }
                            childButtons[num2].button.gameObject.SetActive(true);
                        }
                        else
                        {
                            childButtons[num2].tips.itemId = 0;
                            childButtons[num2].tips.itemInc = 0;
                            childButtons[num2].tips.itemCount = 0;
                            childButtons[num2].tips.type = UIButton.ItemTipType.Other;
                            childButtons[num2].button.interactable = false;
                            childButtonObjs[num2].SetActive(false);
                            //childButtons[num2].button.gameObject.SetActive(false);
                        }
                    }
                    else if (forceShowAllButtons)
                    {
                        childIcons[num2].enabled = false;
                        childButtons[num2].button.interactable = true;
                        childButtons[num2].tips.itemId = 0;
                        childButtons[num2].button.gameObject.SetActive(true);
                    }
                    else
                    {
                        childButtons[num2].button.gameObject.SetActive(false);
                    }
                    num2++;
                }
                if (Input.GetKeyDown(KeyCode.CapsLock))
                {
                    hotkeyActivateRow = (hotkeyActivateRow + 1) % 2;
                }
                RefreshHotKeyRow();
            }
            else // 设置F1
            {
                RefreshHotKeyRow(true);
            }
        }


        public static void OnChildButtonClick(int index)
        {
            UIBuildMenu _this = UIRoot.instance.uiGame.buildMenu;
            int category = _this.currentCategory;
            if (category < 0 || category > protos.Length)
            {
                return;
            }
            bool flag = false;
            if (_this.player == null)
            {
                return;
            }
            if (protos[category, index] == null)
            {
                return;
            }
            int id = protos[category, index].ID;
            if (!_this.showButtonsAnyways && !GameMain.history.ItemUnlocked(id))
            {
                return;
            }
            if (index == dblClickIndex && GameMain.gameTime - dblClickTime < 0.33000001311302185)
            {
                UIRoot.instance.uiGame.FocusOnReplicate(id);
                dblClickTime = 0.0;
                dblClickIndex = 0;
                flag = true;
            }
            dblClickIndex = index;
            dblClickTime = GameMain.gameTime;
            for (int i = 0; i < _this.randRemindTips.Length; i++)
            {
                if (_this.randRemindTips[i] != null && _this.randRemindTips[i].active)
                {
                    int featureId = _this.randRemindTips[i].featureId;
                    int num = featureId / 100;
                    int num2 = featureId % 100;
                    if (category == num && index == num2)
                    {
                        _this.randRemindTips[i]._Close();
                    }
                }
            }
            int itemCount = _this.player.package.GetItemCount(id);
            if(DeliverySlotsTweaksCompat.enabled)
            {
                itemCount += _this.player.deliveryPackage.GetItemCount(id);
            }
            if (itemCount <= 0 && (category != 9 || index != 1))
            {
                if (!flag)
                {
                    UIRealtimeTip.Popup("双击打开合成器".Translate(), true, 2);
                }
                return;
            }
            if (_this.player.inhandItemId == id)
            {
                _this.player.SetHandItems(0, 0, 0);
            }
            else if (itemCount > 0 || (category == 9 && index == 1))
            {
                _this.player.SetHandItems(id, 0, 0);
            }
            else
            {
                _this.player.SetHandItems(0, 0, 0);
            }
            if (_this.isKeyDownCallingAudio)
            {
                VFAudio.Create("build-menu-child", null, Vector3.zero, false, 0, -1, -1L).Play();
                _this.isKeyDownCallingAudio = false;
            }
        }


        public static void RefreshHotKeyRow(bool force0 = false)
        {
            bool flag0 = hotkeyActivateRow == 0;
            bool flag1 = hotkeyActivateRow == 1;
            if (force0)
            {
                flag0 = true;
                flag1 = false;
            }
            for (int i = 0; i < 10; i++)
            {
                oriChildHotkeyText[i].text = flag0 ? $"F{i + 1}" : "";
            }
            for (int i = 0; i < 11; i++)
            {
                childHotkeyText[i].text = flag1 ? $"F{i}" : "";
            }

        }

        public static void RefreshCategoryIfExtended(int category)
        {
            if(category <0 || category > extendedCategories.Length)
            {
                return;
            }
            extendedCategories[category] = false;

            for (int i = 0; i < 13; i++)
            {
                if (protos[category, i] != null)
                {
                    extendedCategories[category] = true;
                    return;
                }
            }
        }

    }
}
