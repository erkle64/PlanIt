using C3.ModKit;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unfoundry;
using UnityEngine;
using UnityEngine.UI;

namespace PlanIt
{
    [UnfoundryMod(GUID)]
    public class Plugin : UnfoundryPlugin
    {
        public const string
            MODNAME = "PlanIt",
            AUTHOR = "erkle64",
            GUID = AUTHOR + "." + MODNAME,
            VERSION = "0.1.0";

        public static LogSource log;

        public static TypedConfigEntry<KeyCode> configOpenPlannerKey;

        private GameObject _plannerFrame = null;
        private GameObject _plannerTabsPanel = null;
        private GameObject _planList = null;
        private TMP_InputField _newPlanInput;
        private List<UIBuilder.GenericUpdateDelegate> _guiUpdaters = new List<UIBuilder.GenericUpdateDelegate>();

        private string _planFolder = string.Empty;

        public Plugin()
        {
            log = new LogSource(MODNAME);

            new Config(GUID)
                .Group("Input",
                    "Key Codes: Backspace, Tab, Clear, Return, Pause, Escape, Space, Exclaim,",
                    "DoubleQuote, Hash, Dollar, Percent, Ampersand, Quote, LeftParen, RightParen,",
                    "Asterisk, Plus, Comma, Minus, Period, Slash,",
                    "Alpha0, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,",
                    "Colon, Semicolon, Less, Equals, Greater, Question, At,",
                    "LeftBracket, Backslash, RightBracket, Caret, Underscore, BackQuote,",
                    "A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,",
                    "LeftCurlyBracket, Pipe, RightCurlyBracket, Tilde, Delete,",
                    "Keypad0, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,",
                    "KeypadPeriod, KeypadDivide, KeypadMultiply, KeypadMinus, KeypadPlus, KeypadEnter, KeypadEquals,",
                    "UpArrow, DownArrow, RightArrow, LeftArrow, Insert, Home, End, PageUp, PageDown,",
                    "F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15,",
                    "Numlock, CapsLock, ScrollLock,",
                    "RightShift, LeftShift, RightControl, LeftControl, RightAlt, LeftAlt, RightApple, RightApple,",
                    "LeftCommand, LeftCommand, LeftWindows, RightWindows, AltGr,",
                    "Help, Print, SysReq, Break, Menu,",
                    "Mouse0, Mouse1, Mouse2, Mouse3, Mouse4, Mouse5, Mouse6")
                    .Entry(out configOpenPlannerKey, "openPlannerKey", KeyCode.KeypadDivide, "Keyboard shortcut for open the planner.")
                .EndGroup()
                .Load()
                .Save();
        }

        public override void Load(Mod mod)
        {
            _planFolder = Path.Combine(Application.persistentDataPath, MODNAME.ToLower());
            if (!Directory.Exists(_planFolder)) Directory.CreateDirectory(_planFolder);

            log.Log($"Loading {MODNAME}");

            CommonEvents.OnUpdate += Update;
        }

        public void Update()
        {
            if (Input.GetKeyUp(configOpenPlannerKey.Get()) && InputHelpers.IsKeyboardInputAllowed)
            {
                TogglePlannerFrame();
            }
        }

        private void ProcessUpdaters()
        {
            foreach (var update in _guiUpdaters) update();
        }

        private void TogglePlannerFrame()
        {
            if (_plannerFrame == null || !_plannerFrame.activeSelf)
            {
                ShowPlannerFrame();
            }
            else
            {
                HidePlannerFrame();
            }
        }

        private void ShowPlannerFrame()
        {
            if (_plannerFrame == null)
            {
                UIBuilder.BeginWith(GameRoot.getDefaultCanvas())
                    .Element_Panel("PlannerFrame", "corner_cut_outline", new Color(0.133f, 0.133f, 0.133f, 1.0f), new Vector4(13, 10, 8, 13))
                        .Keep(out _plannerFrame)
                        .SetRectTransform(20.0f, 20.0f, -20.0f, -20.0f, 0.5f, 0.5f, 0.0f, 0.0f, 1.0f, 1.0f)
                        .Element_Header("HeaderBar", "corner_cut_outline", new Color(0.0f, 0.6f, 1.0f, 1.0f), new Vector4(13, 3, 8, 13))
                            .SetRectTransform(0.0f, -60.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 1.0f, 1.0f, 1.0f)
                            .Layout()
                                .MinWidth(200)
                                .MinHeight(60)
                            .Done
                            .Element("Heading")
                                .SetRectTransform(0.0f, 0.0f, -60.0f, 0.0f, 0.0f, 0.5f, 0.0f, 0.0f, 1.0f, 1.0f)
                                .Component_Text("PlanIt - Planner", "OpenSansSemibold SDF", 34.0f, Color.white)
                            .Done
                            .Element_Button("Button Close", "corner_cut_fully_inset", Color.white, new Vector4(13.0f, 1.0f, 4.0f, 13.0f))
                                .SetOnClick(HidePlannerFrame)
                                .SetRectTransform(-60.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.5f, 1.0f, 0.0f, 1.0f, 1.0f)
                                .SetTransitionColors(new Color(1.0f, 1.0f, 1.0f, 1.0f), new Color(1.0f, 0.25f, 0.0f, 1.0f), new Color(1.0f, 0.0f, 0.0f, 1.0f), new Color(1.0f, 0.25f, 0.0f, 1.0f), new Color(0.5f, 0.5f, 0.5f, 1.0f), 1.0f, 0.1f)
                                .Element("Image")
                                    .SetRectTransform(5.0f, 5.0f, -5.0f, -5.0f, 0.5f, 0.5f, 0.0f, 0.0f, 1.0f, 1.0f)
                                    .Component_Image("cross", Color.white, Image.Type.Sliced, Vector4.zero)
                                .Done
                            .Done
                        .Done
                        .Element("Content")
                            .SetRectTransform(0.0f, 0.0f, 0.0f, -60.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.0f, 1.0f)
                            .SetHorizontalLayout(new RectOffset(10, 10, 10, 16), 4.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, true)
                            .Element("Plan List Container")
                                .Layout()
                                    .PreferredWidth(360)
                                    .FlexibleWidth(0)
                                    .FlexibleHeight(1)
                                .Done
                                .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 8.0f, TextAnchor.UpperLeft, false, true, true, false, false, true, false)
                                .Element_ScrollBox("Plan List", planListBuilder =>
                                {
                                    planListBuilder = planListBuilder
                                        .Keep(out _planList)
                                        .SetVerticalLayout(new RectOffset(4, 4, 4, 4), 2.0f, TextAnchor.UpperLeft, false, false, true, false, false, false, false)
                                        .AutoSize(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize)
                                    .Done;
                                })
                                    .Layout()
                                        .MinWidth(360)
                                        .PreferredWidth(360)
                                        .FlexibleHeight(1)
                                    .Done
                                .Done
                                .Element("New Plan Row")
                                    .Layout()
                                        .MinHeight(40)
                                        .PreferredHeight(40)
                                        .FlexibleHeight(0)
                                    .Done
                                    .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 8.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, true)
                                    .Element_InputField("New Plan Input", "")
                                        .Layout()
                                            .PreferredWidth(292)
                                            .FlexibleWidth(1)
                                        .Done
                                        .Keep(out _newPlanInput)
                                        .WithComponent<TMP_InputField>(input =>
                                        {
                                            input.onValueChanged.AddListener((_) => ProcessUpdaters());
                                        })
                                    .Done
                                    .Element_TextButton("New Plan Button", "New")
                                        .Layout()
                                            .MinWidth(60)
                                            .PreferredWidth(60)
                                            .FlexibleWidth(0)
                                        .Done
                                        .Updater<Button>(_guiUpdaters, () => !string.IsNullOrWhiteSpace(_newPlanInput?.text))
                                    .Done
                                .Done
                            .Done
                            .Element("Divider")
                                .Component_Image("solid_square_white", Color.grey, Image.Type.Sliced, Vector4.zero)
                                .Layout()
                                    .MinWidth(1)
                                    .PreferredWidth(1)
                                    .FlexibleWidth(0)
                                    .FlexibleHeight(1)
                                .Done
                            .Done
                            .Element("Main Container")
                                .Layout()
                                    .FlexibleWidth(1)
                                    .FlexibleHeight(1)
                                .Done
                            .Done
                        .Done
                    .Done
                .End();

                ProcessUpdaters();
                FillPlanList();
                AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIOpen);
                GlobalStateManager.addCursorRequirement();
            }
            else if (!_plannerFrame.gameObject.activeSelf)
            {
                _plannerFrame.gameObject.SetActive(true);
                ProcessUpdaters();
                FillPlanList();
                AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIOpen);
                GlobalStateManager.addCursorRequirement();
            }
        }

        private void HidePlannerFrame()
        {
            if (_plannerFrame != null && _plannerFrame.gameObject.activeSelf)
            {
                _plannerFrame.SetActive(false);
                AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIClose);
                GlobalStateManager.removeCursorRequirement();
            }
        }

        private void FillPlanList()
        {
            DestroyAllTransformChildren(_planList.transform);

            var builder = UIBuilder.BeginWith(_planList);

            foreach (var filePath in Directory.GetFiles(_planFolder, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(filePath);
                builder = builder.Element_TextButton_AutoSize($"Plan {name}", name)
                        .Layout()
                            .FlexibleWidth(1)
                        .Done
                        .SetOnClick(() =>
                        {
                            ProcessUpdaters();
                        })
                    .Done;
            }

            builder.End();
        }

        private static void DestroyAllTransformChildren(Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}


