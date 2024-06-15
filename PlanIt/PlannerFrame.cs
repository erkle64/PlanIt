using System.Collections.Generic;
using TMPro;
using Unfoundry;
using UnityEngine.UI;
using UnityEngine;
using System.IO;
using TinyJSON;
using System;
using System.Reflection.Emit;
using System.Diagnostics;

namespace PlanIt
{
    internal class PlannerFrame : IEscapeCloseable
    {
        private GameObject _frame = null;
        private TextMeshProUGUI _heading = null;
        private GameObject _tabsPanel = null;
        private GameObject _planList = null;
        private GameObject _planContainer = null;
        private GameObject _planContent = null;
        private GameObject _outputList = null;
        private GameObject _inputList = null;
        private GameObject _extraOutputList = null;
        private GameObject _extraInputList = null;
        private TMP_InputField _renamePlanInput;
        private GameObject _editPlanRow;
        private TMP_InputField _newPlanInput;
        private List<UIBuilder.GenericUpdateDelegate> _guiUpdaters = new List<UIBuilder.GenericUpdateDelegate>();

        private GameObject[] _conveyorOptionButtons = new GameObject[3];
        private GameObject[] _metallurgyOptionButtons = new GameObject[3];
        private GameObject[] _salesOptionButtons = new GameObject[2];
        private GameObject[] _cementOptionButtons = new GameObject[2];
        private Toggle _allowUnresearchedToggle;

        private string _currentPlanPath = string.Empty;
        private PlanData _currentPlan = default;

        private string _planFolder = string.Empty;

        private Sprite _borderSprite;
        private Sprite _arrowLeftSprite;
        private Sprite _arrowRightSprite;
        private Sprite _plusSprite;
        private Sprite _renameSprite;
        private Sprite _deleteSprite;

        private ItemElementTemplate[] _allItemElements = null;

        private TextMeshProUGUI _blastFurnaceTowerCounterLabel;
        private TextMeshProUGUI _stoveTowerCounterLabel;
        private TextMeshProUGUI _airVentVentCounterLabel;
        private Dictionary<ItemElementTemplate, TextMeshProUGUI> _inputLabels = new Dictionary<ItemElementTemplate, TextMeshProUGUI>();

        public bool IsOpen => _frame != null && _frame.activeSelf;

        public ItemSelectFrame _itemSelectFrame;

        public PlannerFrame(string planFolder, Dictionary<string, UnityEngine.Object> assets)
        {
            _planFolder = planFolder;
            _itemSelectFrame = new ItemSelectFrame();

            _borderSprite = assets.LoadAsset<Sprite>("planit_border");
            _arrowLeftSprite = assets.LoadAsset<Sprite>("planit_arrow_left");
            _arrowRightSprite = assets.LoadAsset<Sprite>("planit_arrow_right");
            _plusSprite = assets.LoadAsset<Sprite>("planit_plus");
            _renameSprite = assets.LoadAsset<Sprite>("planit_rename");
            _deleteSprite = assets.LoadAsset<Sprite>("planit_delete");
        }

        public void Show()
        {
            if (_frame == null)
            {
                UIBuilder.BeginWith(GameRoot.getDefaultCanvas())
                    .Element_Panel("PlannerFrame", "corner_cut_outline", new Color(0.133f, 0.133f, 0.133f, 1.0f), new Vector4(13, 10, 8, 13))
                        .Keep(out _frame)
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
                                .Keep(out _heading)
                            .Done
                            .Element_Button("Button Close", "corner_cut_fully_inset", Color.white, new Vector4(13.0f, 1.0f, 4.0f, 13.0f))
                                .SetOnClick(Hide)
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
                            .SetHorizontalLayout(new RectOffset(10, 10, 10, 16), 4.0f, TextAnchor.UpperLeft, false, true, true, false, true, false, false)
                            .Element("Plan List Container")
                                .Layout()
                                    .PreferredWidth(360)
                                    .FlexibleWidth(0)
                                    .FlexibleHeight(1)
                                .Done
                                .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 8.0f, TextAnchor.UpperLeft, false, true, true, true, false, false, false)
                                .Element_ScrollBox("Plan List", planListBuilder =>
                                {
                                    planListBuilder = planListBuilder
                                        .Keep(out _planList)
                                        .SetVerticalLayout(new RectOffset(4, 4, 4, 4), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                                        .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                                    .Done;
                                })
                                    .Layout()
                                        .MinWidth(360)
                                        .PreferredWidth(360)
                                        .FlexibleHeight(1)
                                    .Done
                                .Done
                                .Element("Edit Plan Row")
                                    .Keep(out _editPlanRow)
                                    .Layout()
                                        .MinHeight(40)
                                        .PreferredHeight(40)
                                        .FlexibleHeight(0)
                                    .Done
                                    .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 8.0f, TextAnchor.UpperLeft, false, true, true, false, true, false, false)
                                    .Element_InputField("Rename Plan Input", "")
                                        .Layout()
                                            .FlexibleWidth(1)
                                        .Done
                                        .Keep(out _renamePlanInput)
                                        .WithComponent<TMP_InputField>(input =>
                                        {
                                            input.onValueChanged.AddListener((_) => ProcessUpdaters());
                                        })
                                    .Done
                                    .Element_IconButton("Rename Plan Button", _renameSprite, 30, 30)
                                        .Layout()
                                            .MinWidth(40)
                                            .PreferredWidth(40)
                                            .FlexibleWidth(0)
                                        .Done
                                        .Updater<Button>(_guiUpdaters, () => !string.IsNullOrWhiteSpace(_renamePlanInput?.text))
                                        .SetOnClick(OnClickRenamePlan)
                                        .Component_Tooltip("Rename Plan")
                                    .Done
                                    .Element_IconButton("Rename Plan Button", _deleteSprite, 30, 30)
                                        .Layout()
                                            .MinWidth(40)
                                            .PreferredWidth(40)
                                            .FlexibleWidth(0)
                                        .Done
                                        .Updater<Button>(_guiUpdaters, () => !string.IsNullOrWhiteSpace(_renamePlanInput?.text))
                                        .SetOnClick(OnClickDeletePlan)
                                        .Component_Tooltip("Delete Plan")
                                    .Done
                                .Done
                                .Element("New Plan Row")
                                    .Layout()
                                        .MinHeight(40)
                                        .PreferredHeight(40)
                                        .FlexibleHeight(0)
                                    .Done
                                    .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 8.0f, TextAnchor.UpperLeft, false, true, true, false, true, false, false)
                                    .Element_InputField("New Plan Input", "")
                                        .Layout()
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
                                        .SetOnClick(OnClickNewPlan)
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
                                .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 4.0f, TextAnchor.UpperLeft, false, true, true, true, false, false, false)
                                .Keep(out _planContainer)
                            .Done
                        .Done
                    .Done
                .End();

                _editPlanRow.SetActive(false);

                ProcessUpdaters();
                FillPlanList();
                AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIOpen);
                GlobalStateManager.addCursorRequirement();
                GlobalStateManager.registerEscapeCloseable(this);
            }
            else if (!_frame.gameObject.activeSelf)
            {
                _frame.gameObject.SetActive(true);
                ProcessUpdaters();
                FillPlanList();
                AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIOpen);
                GlobalStateManager.addCursorRequirement();
                GlobalStateManager.registerEscapeCloseable(this);
            }
        }

        public void Hide()
        {
            if (_frame != null && _frame.gameObject.activeSelf)
            {
                _frame.SetActive(false);
                AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIClose);
                GlobalStateManager.removeCursorRequirement();
                GlobalStateManager.deRegisterEscapeCloseable(this);
            }
        }

        private void ProcessUpdaters()
        {
            foreach (var update in _guiUpdaters) update();
        }

        private void FillPlanList()
        {
            _planList.transform.DestroyAllChildren();

            var builder = UIBuilder.BeginWith(_planList);

            foreach (var filePath in Directory.GetFiles(_planFolder, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(filePath);
                builder = builder.Element_TextButton_AutoSize($"Plan {name}", name)
                        .SetOnClick(() =>
                        {
                            LoadPlan(filePath);
                            AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIButtonClick);
                        })
                    .Done;
            }

            builder.End();

            ProcessUpdaters();
        }

        private void OnClickRenamePlan()
        {
            if (string.IsNullOrEmpty(_currentPlanPath)) return;

            var name = PathHelpers.MakeValidFileName(_renamePlanInput?.text ?? "");
            if (string.IsNullOrEmpty(name)) return;

            var filePath = Path.Combine(_planFolder, $"{name}.json");
            if (File.Exists(filePath))
            {
                ConfirmationFrame.Show($"Overwrite '{name}'?", "Confirm", () =>
                {
                    RenamePlan(_currentPlanPath, filePath);
                });
            }
            else
            {
                RenamePlan(_currentPlanPath, filePath);
            }
        }

        private void OnClickDeletePlan()
        {
            if (string.IsNullOrEmpty(_currentPlanPath)) return;

            ConfirmationFrame.Show($"Delete '{Path.GetFileNameWithoutExtension(_currentPlanPath)}'?", "Confirm", () =>
            {
                DeletePlan(_currentPlanPath);
            });
        }

        private void OnClickNewPlan()
        {
            var name = PathHelpers.MakeValidFileName(_newPlanInput?.text ?? "");
            if (string.IsNullOrEmpty(name)) return;

            var filePath = Path.Combine(_planFolder, $"{name}.json");
            if (File.Exists(filePath))
            {
                ConfirmationFrame.Show($"Overwrite '{name}'?", "Confirm", () =>
                {
                    CreateNewPlan(filePath);
                });
            }
            else
            {
                CreateNewPlan(filePath);
            }
        }

        private void RenamePlan(string currentPlanPath, string filePath)
        {
            try
            {
                if (File.Exists(currentPlanPath)) File.Move(currentPlanPath, filePath);
            }
            catch { }

            FillPlanList();
            LoadPlan(filePath);
        }

        private void DeletePlan(string currentPlanPath)
        {
            try
            {
                if (File.Exists(currentPlanPath)) File.Delete(currentPlanPath);
            }
            catch { }

            FillPlanList();
            _planContainer.transform.DestroyAllChildren();
            _heading.text = "PlanIt - Planner";
            _editPlanRow.SetActive(false);
        }

        private void CreateNewPlan(string filePath)
        {
            var newPlan = PlanData.Create();
            var json = JSON.Dump(newPlan, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            File.WriteAllText(filePath, json);
            FillPlanList();
        }

        private void LoadPlan(string filePath)
        {
            if (_planContainer == null || !File.Exists(filePath)) return;

            var name = Path.GetFileNameWithoutExtension(filePath);

            _editPlanRow.SetActive(true);
            _renamePlanInput.text = name;

            _heading.text = $"PlanIt - {name}";

            ItemElementRecipe.Init(_currentPlan.blastFurnaceTowers, _currentPlan.stoveTowers, _currentPlan.airVentVents);

            _currentPlanPath = filePath;

            _currentPlan = PlanData.Load(filePath);
            _currentPlan.blastFurnaceTowers = Mathf.Clamp(_currentPlan.blastFurnaceTowers, 1, ItemElementRecipe.BlastFurnaceMaxTowers);
            _currentPlan.stoveTowers = Mathf.Clamp(_currentPlan.stoveTowers, ItemElementRecipe.StoveMinTowers, ItemElementRecipe.StoveMaxTowers);
            _currentPlan.airVentVents = Mathf.Clamp(_currentPlan.airVentVents, ItemElementRecipe.AirVentMinVents, ItemElementRecipe.AirVentMaxVents);

            _planContainer.transform.DestroyAllChildren();

            UIBuilder.BeginWith(_planContainer)
                .Element("Options Row")
                    .Layout()
                        .FlexibleHeight(0)
                    .Done
                    .AutoSize(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize)
                    .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 4, TextAnchor.MiddleLeft, false, true, true, false, false, false, false)
                    .Do(conveyorBuilder =>
                    {
                        _conveyorOptionButtons = new GameObject[ItemElementRecipe.ConveyorSpeeds.Count];
                        var conveyorIndex = 0;
                        foreach (var conveyorSpeed in ItemElementRecipe.ConveyorSpeeds)
                        {
                            var (conveyor, speed) = conveyorSpeed;
                            var conveyorIndexToSet = conveyorIndex;
                            conveyorBuilder = conveyorBuilder
                                .Element_IconButton($"{conveyor.name} Button", conveyor.icon)
                                    .Keep(out _conveyorOptionButtons[conveyorIndex])
                                    .SetOnClick(() => { _currentPlan.conveyorTier = conveyorIndexToSet; UpdateOptionButtons(); })
                                    .Component_Tooltip($"Use {conveyor.name}\nSpeed: {Mathf.RoundToInt((float)speed)}/m")
                                .Done;
                            conveyorIndex++;
                        }
                    })
                    .Element("Spacer")
                        .Layout()
                            .MinWidth(12)
                            .PreferredWidth(12)
                            .FlexibleWidth(0)
                        .Done
                    .Done
                    .Element_IconButton("Metallurgy 1 Button", "ore_rubble_xenoferrite")
                        .Keep(out _metallurgyOptionButtons[0])
                        .SetOnClick(() => { _currentPlan.metallurgyTier = 0; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Metallurgy Tier 1")
                    .Done
                    .Element_IconButton("Metallurgy 2 Button", "ore_xenoferrite")
                        .Keep(out _metallurgyOptionButtons[1])
                        .SetOnClick(() => { _currentPlan.metallurgyTier = 1; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Metallurgy Tier 2")
                    .Done
                    .Element_IconButton("Metallurgy 3 Button", "molten_xf")
                        .Keep(out _metallurgyOptionButtons[2])
                        .SetOnClick(() => { _currentPlan.metallurgyTier = 2; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Metallurgy Tier 3")
                    .Done
                    .Element("Spacer")
                        .Layout()
                            .MinWidth(12)
                            .PreferredWidth(12)
                            .FlexibleWidth(0)
                        .Done
                    .Done
                    .Element_IconButton("Drone Button", "maintenance_drone")
                        .Keep(out _salesOptionButtons[0])
                        .SetOnClick(() => { _currentPlan.salesTier = 0; UpdateOptionButtons(); })
                        .Component_Tooltip("Sell Maintenance Drones")
                    .Done
                    .Element_IconButton("Robot Button", "robot01")
                        .Keep(out _salesOptionButtons[1])
                        .SetOnClick(() => { _currentPlan.salesTier = 1; UpdateOptionButtons(); })
                        .Component_Tooltip("Sell Service Robots")
                    .Done
                    .Element("Spacer")
                        .Layout()
                            .MinWidth(12)
                            .PreferredWidth(12)
                            .FlexibleWidth(0)
                        .Done
                    .Done
                    .Element_IconButton("Mineral Rock Button", "mineral_rock")
                        .Keep(out _cementOptionButtons[0])
                        .SetOnClick(() => { _currentPlan.cementTier = 0; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Mineral Rock for Cement")
                    .Done
                    .Element_IconButton("Slag Button", "slag_reprocessing")
                        .Keep(out _cementOptionButtons[1])
                        .SetOnClick(() => { _currentPlan.cementTier = 1; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Slag for Cement")
                    .Done
                    //.Element("Spacer")
                    //    .Layout()
                    //        .MinWidth(12)
                    //        .PreferredWidth(12)
                    //        .FlexibleWidth(0)
                    //    .Done
                    //.Done
                    //.Element_Toggle("Allow Unresearched Toggle", false, 30.0f, isOn =>
                    //{
                    //    _currentPlan.allowUnresearched = isOn; UpdateOptionButtons();
                    //})
                    //    .Keep(out _allowUnresearchedToggle)
                    //    .Component_Tooltip("Allow using unresearched recipes.")
                    //.Done
                    //.Element_Label("Allow Unresearched Label", "Allow Unresearched", 180)
                    //    .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                    //.Done
                    .Element("Spacer")
                        .Layout()
                            .MinWidth(12)
                            .PreferredWidth(12)
                            .FlexibleWidth(0)
                        .Done
                    .Done
                    .Element("Blast Furnace Icon")
                        .Component_Image(ResourceDB.getIcon("blast_furnace_module"), Color.white, Image.Type.Sliced, Vector4.zero)
                        .Component_Tooltip("Blast Furnace Tower Count")
                        .Layout()
                            .MinWidth(58)
                            .PreferredWidth(58)
                            .FlexibleWidth(0)
                            .MinHeight(58)
                            .PreferredHeight(58)
                            .FlexibleHeight(0)
                        .Done
                    .Done
                    .Element("Blast Furnace Tower Counter")
                        .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                        .Element_Label("Blast Furnace Tower Counter Label", "1", 50)
                            .Keep(out _blastFurnaceTowerCounterLabel)
                        .Done
                        .Element("Blast Furnace Tower Counter Buttons")
                            .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                            .Element_IconButton("Blast Furnace Button Increase", _arrowLeftSprite, 20, 20)
                                .SetOnClick(() => {
                                    if (_currentPlan.blastFurnaceTowers > 1)
                                    {
                                        _currentPlan.blastFurnaceTowers--;
                                        UpdateOptionButtons();
                                    }
                                })
                            .Done
                            .Element_IconButton("Blast Furnace Button Increase", _arrowRightSprite, 20, 20)
                                .SetOnClick(() => {
                                    if (_currentPlan.blastFurnaceTowers < ItemElementRecipe.BlastFurnaceMaxTowers)
                                    {
                                        _currentPlan.blastFurnaceTowers++;
                                        UpdateOptionButtons();
                                    }
                                })
                            .Done
                        .Done
                    .Done
                    .Element("Spacer")
                        .Layout()
                            .MinWidth(12)
                            .PreferredWidth(12)
                            .FlexibleWidth(0)
                        .Done
                    .Done
                    .Element("Stove Icon")
                        .Component_Image(ResourceDB.getIcon("hot_air_stove_base"), Color.white, Image.Type.Sliced, Vector4.zero)
                        .Component_Tooltip("Hot Air Stove Tower Count")
                        .Layout()
                            .MinWidth(58)
                            .PreferredWidth(58)
                            .FlexibleWidth(0)
                            .MinHeight(58)
                            .PreferredHeight(58)
                            .FlexibleHeight(0)
                        .Done
                    .Done
                    .Element("Stove Tower Counter")
                        .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                        .Element_Label("Stove Tower Counter Label", "1", 50)
                            .Keep(out _stoveTowerCounterLabel)
                        .Done
                        .Element("Stove Tower Counter Buttons")
                            .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                            .Element_IconButton("Stove Button Increase", _arrowLeftSprite, 20, 20)
                                .SetOnClick(() => {
                                    if (_currentPlan.stoveTowers > ItemElementRecipe.StoveMinTowers)
                                    {
                                        _currentPlan.stoveTowers--;
                                        UpdateOptionButtons();
                                    }
                                })
                            .Done
                            .Element_IconButton("Stove Button Increase", _arrowRightSprite, 20, 20)
                                .SetOnClick(() => {
                                    if (_currentPlan.stoveTowers < ItemElementRecipe.StoveMaxTowers)
                                    {
                                        _currentPlan.stoveTowers++;
                                        UpdateOptionButtons();
                                    }
                                })
                            .Done
                        .Done
                    .Done
                    .Element("Spacer")
                        .Layout()
                            .MinWidth(12)
                            .PreferredWidth(12)
                            .FlexibleWidth(0)
                        .Done
                    .Done
                    .Element("Air Vent Icon")
                        .Component_Image(ResourceDB.getIcon("hot_air_stove_base"), Color.white, Image.Type.Sliced, Vector4.zero)
                        .Component_Tooltip("Air Vent Count")
                        .Layout()
                            .MinWidth(58)
                            .PreferredWidth(58)
                            .FlexibleWidth(0)
                            .MinHeight(58)
                            .PreferredHeight(58)
                            .FlexibleHeight(0)
                        .Done
                    .Done
                    .Element("Air Vent Tower Counter")
                        .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                        .Element_Label("Air Vent Tower Counter Label", "1", 50)
                            .Keep(out _airVentVentCounterLabel)
                        .Done
                        .Element("Air Vent Tower Counter Buttons")
                            .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                            .Element_IconButton("Air Vent Button Increase", _arrowLeftSprite, 20, 20)
                                .SetOnClick(() => {
                                    if (_currentPlan.airVentVents > ItemElementRecipe.AirVentMinVents)
                                    {
                                        _currentPlan.airVentVents--;
                                        UpdateOptionButtons();
                                    }
                                })
                            .Done
                            .Element_IconButton("Air Vent Button Increase", _arrowRightSprite, 20, 20)
                                .SetOnClick(() => {
                                    if (_currentPlan.airVentVents < ItemElementRecipe.AirVentMaxVents)
                                    {
                                        _currentPlan.airVentVents++;
                                        UpdateOptionButtons();
                                    }
                                })
                            .Done
                        .Done
                    .Done
                .Done
                .Element("Divider")
                    .Component_Image("solid_square_white", Color.grey, Image.Type.Sliced, Vector4.zero)
                    .Layout()
                        .MinHeight(1)
                        .PreferredHeight(1)
                        .FlexibleHeight(0)
                        .FlexibleWidth(1)
                    .Done
                .Done
                .Element("Outputs Row")
                    .Layout()
                        .FlexibleHeight(0)
                    .Done
                    .AutoSize(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize)
                    .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 4, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                    .Element_Label("Outputs Label", "Outputs:", 100)
                    .Done
                    .Element("Outputs Add-Remove Buttons")
                        .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, true, false, false, false)
                        .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                        .Layout()
                            .FlexibleWidth(0)
                        .Done
                        .Element_ImageButton("Add Output Button", "icon_plus")
                            .Layout()
                                .MinWidth(30)
                                .PreferredWidth(30)
                                .FlexibleWidth(0)
                                .MinHeight(30)
                                .PreferredHeight(30)
                                .FlexibleHeight(0)
                            .Done
                            .Component_Tooltip("Add Output")
                            .SetOnClick(AddOutput)
                        .Done
                        .Element_ImageButton("Remove Output Button", "icon_minus")
                            .Layout()
                                .MinWidth(30)
                                .PreferredWidth(30)
                                .FlexibleWidth(0)
                                .MinHeight(30)
                                .PreferredHeight(30)
                                .FlexibleHeight(0)
                            .Done
                            .Component_Tooltip("Remove Last Output")
                            .SetOnClick(RemoveLastOutput)
                        .Done
                    .Done
                    .Element("Outputs List")
                        .Keep(out _outputList)
                        .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                        .AutoSize(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize)
                        .Layout()
                            .FlexibleWidth(1)
                        .Done
                    .Done
                .Done
                .Element("Divider")
                    .Component_Image("solid_square_white", Color.grey, Image.Type.Sliced, Vector4.zero)
                    .Layout()
                        .MinHeight(1)
                        .PreferredHeight(1)
                        .FlexibleHeight(0)
                        .FlexibleWidth(1)
                    .Done
                .Done
                .Element("Inputs Row")
                    .Layout()
                        .FlexibleHeight(0)
                    .Done
                    .AutoSize(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize)
                    .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 4, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                    .Element_Label("Inputs Label", "Inputs:", 100)
                    .Done
                    .Element("Inputs Add-Remove Buttons")
                        .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, true, false, false, false)
                        .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                        .Layout()
                            .FlexibleWidth(0)
                        .Done
                        .Element_ImageButton("Add Input Button", "icon_plus")
                            .Layout()
                                .MinWidth(30)
                                .PreferredWidth(30)
                                .FlexibleWidth(0)
                                .MinHeight(30)
                                .PreferredHeight(30)
                                .FlexibleHeight(0)
                            .Done
                            .Component_Tooltip("Add Input")
                            .SetOnClick(AddInput)
                        .Done
                        .Element_ImageButton("Remove Input Button", "icon_minus")
                            .Layout()
                                .MinWidth(30)
                                .PreferredWidth(30)
                                .FlexibleWidth(0)
                                .MinHeight(30)
                                .PreferredHeight(30)
                                .FlexibleHeight(0)
                            .Done
                            .Component_Tooltip("Remove Last Input")
                            .SetOnClick(RemoveLastInput)
                        .Done
                    .Done
                    .Element("Inputs List")
                        .Keep(out _inputList)
                        .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                        .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                    .Done
                .Done
                .Element("Divider")
                    .Component_Image("solid_square_white", Color.grey, Image.Type.Sliced, Vector4.zero)
                    .Layout()
                        .MinHeight(1)
                        .PreferredHeight(1)
                        .FlexibleHeight(0)
                        .FlexibleWidth(1)
                    .Done
                .Done
                .Element_ScrollBox("Plan Content Row", contentBuilder =>
                {
                    contentBuilder = contentBuilder
                        .Keep(out _planContent)
                        .SetVerticalLayout(new RectOffset(4, 4, 4, 4), 4.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                        .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                        .Done;
                })
                    .WithComponent<ScrollRect>(scrollRect => scrollRect.scrollSensitivity = 30.0f)
                    .Layout()
                        .FlexibleHeight(1)
                    .Done
                .Done
                .End();

            UpdateOutputs();
            UpdateInputs();
            UpdateOptionButtons();
        }

        private void AddOutput()
        {
            _itemSelectFrame.Show(itemElement =>
            {
                foreach (var output in _currentPlan.outputs) if (output == itemElement.fullIdentifier) return;
                foreach (var input in _currentPlan.inputs) if (input == itemElement.fullIdentifier) return;

                _currentPlan.outputs.Add(itemElement.fullIdentifier);
                _currentPlan.outputAmounts.Add(0);
                SavePlan();
                UpdateOutputs();
                UpdateSolution();
            });
        }

        private void AddInput()
        {
            _itemSelectFrame.Show(itemElement =>
            {
                foreach (var output in _currentPlan.outputs) if (output == itemElement.fullIdentifier) return;
                foreach (var input in _currentPlan.inputs) if (input == itemElement.fullIdentifier) return;

                _currentPlan.inputs.Add(itemElement.fullIdentifier);
                SavePlan();
                UpdateInputs();
                UpdateSolution();
            });
        }

        private void RemoveLastOutput()
        {
            if (_currentPlan.outputs.Count == 0) return;
            _currentPlan.outputs.RemoveAt(_currentPlan.outputs.Count - 1);
            _currentPlan.outputAmounts.RemoveAt(_currentPlan.outputAmounts.Count - 1);
            SavePlan();
            UpdateOutputs();
            UpdateSolution();
            AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIButtonClick);
        }

        private void RemoveLastInput()
        {
            if (_currentPlan.inputs.Count == 0) return;
            _currentPlan.inputs.RemoveAt(_currentPlan.inputs.Count - 1);
            SavePlan();
            UpdateInputs();
            UpdateSolution();
            AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIButtonClick);
        }

        private void RemoveOutput(string fullIdentifier)
        {
            if (_currentPlan.outputs.Count == 0) return;
            var index = _currentPlan.outputs.IndexOf(fullIdentifier);
            if (index < 0) return;
            _currentPlan.outputs.RemoveAt(index);
            _currentPlan.outputAmounts.RemoveAt(index);
            SavePlan();
            UpdateOutputs();
            UpdateSolution();
            AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIButtonClick);
        }

        private void RemoveInput(string fullIdentifier)
        {
            if (_currentPlan.inputs.Count == 0) return;
            if (!_currentPlan.inputs.Remove(fullIdentifier)) return;
            SavePlan();
            UpdateInputs();
            UpdateSolution();
            AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIButtonClick);
        }

        private void ToggleInput(string fullIdentifier)
        {
            if (_currentPlan.inputs.Contains(fullIdentifier))
            {
                _currentPlan.inputs.Remove(fullIdentifier);
            }
            else if (!_currentPlan.outputs.Contains(fullIdentifier))
            {
                _currentPlan.inputs.Add(fullIdentifier);
            }

            SavePlan();
            UpdateInputs();
            UpdateSolution();
            AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIButtonClick);
        }

        private void SetOutputAmount(int outputIndex, double outputAmount)
        {
            if (outputIndex < 0 || outputIndex >= _currentPlan.outputAmounts.Count) return;

            _currentPlan.outputAmounts[outputIndex] = outputAmount;
            SavePlan();
            UpdateSolution();
        }

        private void SavePlan()
        {
            if (string.IsNullOrEmpty(_currentPlanPath)) return;

            try
            {
                _currentPlan.Save(_currentPlanPath);
            }
            catch { }
        }

        private void UpdateOptionButtons()
        {
            UpdateSolution();

            UpdateOptionButtons(_currentPlan.conveyorTier, _conveyorOptionButtons);
            UpdateOptionButtons(_currentPlan.metallurgyTier, _metallurgyOptionButtons);
            UpdateOptionButtons(_currentPlan.salesTier, _salesOptionButtons);
            UpdateOptionButtons(_currentPlan.cementTier, _cementOptionButtons);

            //_allowUnresearchedToggle.SetIsOnWithoutNotify(_currentPlan.allowUnresearched);

            _blastFurnaceTowerCounterLabel.text = _currentPlan.blastFurnaceTowers.ToString();
            _stoveTowerCounterLabel.text = _currentPlan.stoveTowers.ToString();
            _airVentVentCounterLabel.text = _currentPlan.airVentVents.ToString();

            SavePlan();
        }

        private void UpdateOptionButtons(int tier, GameObject[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].GetComponent<Button>().interactable = i != tier;
            }
        }

        private void UpdateOutputs()
        {
            if (_currentPlan.outputs.Count != _currentPlan.outputAmounts.Count)
            {
                Plugin.log.LogWarning($"Output count ({_currentPlan.outputs.Count}) does not match output amount count ({_currentPlan.outputAmounts.Count})");
                return;
            }

            _outputList.transform.DestroyAllChildren();
            var builder = UIBuilder.BeginWith(_outputList);
            for (int i = 0; i < _currentPlan.outputs.Count; i++)
            {
                var output = _currentPlan.outputs[i];
                var outputAmount = _currentPlan.outputAmounts[i];
                var outputElement = ItemElementTemplate.Get(output);
                if (outputElement.isValid)
                {
                    var outputIndex = i;
                    builder = builder.Element($"Output Button Wrapper - {outputElement.name}")
                        .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                        .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                        .Element_IconButton($"Output Button - {outputElement.name}", outputElement.icon, 48, 48)
                            .Component_Tooltip(outputElement.name)
                            .SetOnClick(() => { RemoveOutput(outputElement.fullIdentifier); })
                            .Layout()
                                .MinWidth(58)
                                .PreferredWidth(58)
                                .FlexibleWidth(0)
                                .MinHeight(58)
                                .PreferredHeight(58)
                                .FlexibleHeight(0)
                            .Done
                        .Done
                        .Element_InputField(
                            $"Output Amount - {outputElement.name}",
                            Convert.ToString(outputAmount, System.Globalization.CultureInfo.InvariantCulture),
                            TMP_InputField.ContentType.DecimalNumber)
                            .WithComponent<TMP_InputField>(textField =>
                            {
                                textField.onEndEdit.AddListener(value =>
                                {
                                    var amount = 0.0;
                                    try
                                    {
                                        amount = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                                    }
                                    catch { }
                                    SetOutputAmount(outputIndex, amount);
                                });
                                textField.onSubmit.AddListener(value =>
                                {
                                    var amount = 0.0;
                                    try
                                    {
                                        amount = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                                    }
                                    catch { }
                                    SetOutputAmount(outputIndex, amount);
                                });
                            })
                            .Layout()
                                .MinWidth(58)
                                .PreferredWidth(58)
                                .FlexibleWidth(0)
                                .MinHeight(14)
                                .PreferredHeight(14)
                                .FlexibleHeight(0)
                            .Done
                        .Done
                    .Done;
                }
            }

            builder = builder
                .Element("Extra Outputs List")
                    .Keep(out _extraOutputList)
                    .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                    .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                    .Layout()
                        .FlexibleWidth(0)
                    .Done
                .Done;
        }

        private void UpdateInputs()
        {
            _inputLabels.Clear();
            _inputList.transform.DestroyAllChildren();
            var builder = UIBuilder.BeginWith(_inputList);
            foreach (var input in _currentPlan.inputs)
            {
                var inputElement = ItemElementTemplate.Get(input);
                if (inputElement.isValid)
                {
                    TextMeshProUGUI label = null;
                    builder = builder
                        .Element($"Input Button Wrapper - {inputElement.name}")
                            .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                            .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                            .Element_IconButton($"Input Button - {inputElement.name}", inputElement.icon, 48, 48)
                                .Component_Tooltip(inputElement.name)
                                .SetOnClick(() => { RemoveInput(inputElement.fullIdentifier); })
                            .Done
                            .Element_Label($"Input Label - {inputElement.name}", "", 58)
                                .Keep(out label)
                                .WithComponent<TextMeshProUGUI>(text => { text.fontSize = 12.0f; text.alignment = TextAlignmentOptions.Center; })
                            .Done
                        .Done;
                    _inputLabels[inputElement] = label;
                }
            }

            builder = builder
                .Element("Extra Inputs List")
                    .Keep(out _extraInputList)
                    .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                    .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                    .Layout()
                        .FlexibleWidth(0)
                    .Done
                .Done;
        }

        private static readonly string[][] _metallurgyTierRecipes = new string[][]
        {
            new string[]
            {
                "CR:_base_xf_plates_t1",
                "CR:_base_technum_rods_t1",
                "CR:_base_steel_t1"
            },
            new string[]
            {
                "CR:_base_ore_xenoferrite",
                "CR:_base_ore_technum",
                "CR:_base_xf_plates_t2",
                "CR:_base_technum_rods_t2",
                "CR:_base_steel_t2"
            },
            new string[]
            {
                "CR:_base_ore_xenoferrite",
                "CR:_base_ore_technum",
                "BFM:_base_bfm_te",
                "BFM:_base_bfm_xf",
                "CR:_base_xf_plates_t3",
                "CR:_base_technum_rods_t3",
                "CR:_base_steel_t2"
            }
        };

        private void UpdateSolution()
        {
            ItemElementRecipe.Init(_currentPlan.blastFurnaceTowers, _currentPlan.stoveTowers, _currentPlan.airVentVents);

            var disabledRecipes = new HashSet<ulong>();
            for (int metallurgyTier = 0; metallurgyTier < _metallurgyTierRecipes.Length; metallurgyTier++)
            {
                if (metallurgyTier == _currentPlan.metallurgyTier) continue;
                foreach (var recipeIdentifier in _metallurgyTierRecipes[metallurgyTier])
                {
                    var itemElementRecipe = ItemElementRecipe.Get(recipeIdentifier);
                    if (itemElementRecipe == null)
                    {
                        Plugin.log.LogWarning($"Invalid recipe {recipeIdentifier}");
                        continue;
                    }
                    disabledRecipes.Add(itemElementRecipe.id);
                }
            }
            foreach (var recipeIdentifier in _metallurgyTierRecipes[_currentPlan.metallurgyTier])
            {
                var itemElementRecipe = ItemElementRecipe.Get(recipeIdentifier);
                if (itemElementRecipe == null)
                {
                    Plugin.log.LogWarning($"Invalid recipe {recipeIdentifier}");
                    continue;
                }
                disabledRecipes.Remove(itemElementRecipe.id);
            }

            if (_currentPlan.salesTier == 0)
            {
                disabledRecipes.Add(ItemElementRecipe.Get("RC:sales_base_robot_01").id);
            }
            else
            {
                disabledRecipes.Add(ItemElementRecipe.Get("RC:sales_base_maintenance_drone_i").id);
            }

            if (_currentPlan.cementTier == 0)
            {
                disabledRecipes.Add(ItemElementRecipe.Get("CR:_base_cement_reprocessed").id);
            }
            else
            {
                disabledRecipes.Add(ItemElementRecipe.Get("CR:_base_cement").id);
            }

            var sw = new Stopwatch();
            sw.Restart();
            sw.Start();
            var solver = new Solver(disabledRecipes);
            solver.FindSubGraphs();
            sw.Stop();
            Plugin.log.Log($"FindSubGraphs: {sw.ElapsedMilliseconds}ms");

            var targets = new Dictionary<ItemElementTemplate, double>();
            var ignore = new HashSet<ItemElementTemplate>();
            foreach (var item in _currentPlan.inputs)
            {
                var itemElement = ItemElementTemplate.Get(item);
                if (itemElement.isValid)
                {
                    ignore.Add(itemElement);
                }
            }

            for (int outputIndex = 0; outputIndex < _currentPlan.outputs.Count; outputIndex++)
            {
                var outputElement = ItemElementTemplate.Get(_currentPlan.outputs[outputIndex]);
                if (outputElement.isValid)
                {
                    targets[outputElement] = _currentPlan.outputAmounts[outputIndex];
                }
            }

            sw.Restart();
            sw.Start();
            var result = solver.Solve(targets, ignore);
            sw.Stop();
            Plugin.log.Log($"Solve: {sw.ElapsedMilliseconds}ms");

            //result.Dump();

            sw.Restart();
            sw.Start();
            var (conveyor, conveyorSpeed) = ItemElementRecipe.ConveyorSpeeds[Mathf.Clamp(_currentPlan.conveyorTier, 0, ItemElementRecipe.ConveyorSpeeds.Count - 1)];
            _planContent.transform.DestroyAllChildren();
            var builder = UIBuilder.BeginWith(_planContent);
            var inputAmounts = new Dictionary<ItemElementTemplate, double>();
            foreach (var recipeAmount in result.recipeAmounts)
            {
                var recipe = ItemElementRecipe.Get(recipeAmount.Key);
                foreach (var input in recipe.inputs)
                {
                    var itemElement = input.itemElement;
                    if (itemElement.isValid)
                    {
                        var amount = 0.0;
                        if (inputAmounts.TryGetValue(itemElement, out var _amount))
                        {
                            amount = _amount;
                        }
                        inputAmounts[itemElement] = amount + recipeAmount.Value * input.amount;
                    }
                }
                if (recipe.inputs.Length > 0)
                {
                    foreach (var output in recipe.outputs)
                    {
                        var itemElement = output.itemElement;
                        if (itemElement.isValid)
                        {
                            var amount = 0.0;
                            if (inputAmounts.TryGetValue(itemElement, out var _amount))
                            {
                                amount = _amount;
                            }
                            inputAmounts[itemElement] = amount - recipeAmount.Value * output.amount;
                        }
                    }
                }

                builder = builder
                    .Element($"Recipe - {recipe.name}")
                        .SetHorizontalLayout(new RectOffset(6, 6, 6, 6), 4, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                        .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                        .Component_Image(_borderSprite, new Color(1.0f, 1.0f, 1.0f, 0.5f), Image.Type.Sliced, new Vector4(8, 8, 8, 8));

                foreach (var output in recipe.outputs)
                {
                    var itemElement = output.itemElement;
                    builder = builder
                        .Element("Output Wrapper")
                            .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperCenter, false, true, true, false, false, false, false)
                            .Layout()
                                .MinWidth(58)
                                .PreferredWidth(58)
                                .FlexibleWidth(0)
                            .Done
                            .AutoSize(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize)
                            .Element_IconButton($"Output - {itemElement.name}", itemElement.icon, 48, 48)
                                .Component_Tooltip(itemElement.name)
                                .SetOnClick(() => { ToggleInput(itemElement.fullIdentifier); })
                            .Done
                            .Element_Label($"Amount - {itemElement.name}", $"{Math.Max(0.01, output.amount * recipeAmount.Value):0.##}", 58)
                                .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                                .WithComponent<TextMeshProUGUI>(text => {
                                    text.fontSize = 12.0f;
                                    text.alignment = TextAlignmentOptions.Center;
                                    text.enableAutoSizing = true;
                                    text.fontSizeMax = 12.0f;
                                    text.fontSizeMin = 6.0f;
                                })
                            .Done
                            .Do(beltAmountBuild => {
                                if (itemElement.isItem)
                                {
                                    beltAmountBuild = beltAmountBuild
                                        .Element("Belt Amount Wrapper")
                                            .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                                            .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                                            .Layout()
                                                .FlexibleWidth(0)
                                            .Done
                                            .Element("Belt Icon")
                                                .Component_Image(conveyor.icon, Color.white, Image.Type.Simple)
                                                .Layout()
                                                    .MinWidth(16)
                                                    .PreferredWidth(16)
                                                    .FlexibleWidth(0)
                                                    .MinHeight(16)
                                                    .PreferredHeight(16)
                                                    .FlexibleHeight(0)
                                                .Done
                                            .Done
                                            .Element($"Belt Amount - {itemElement.name}")
                                                .Component_Text($"{Math.Max(0.01, (double)(output.amount * recipeAmount.Value / conveyorSpeed)):0.##}", "OpenSansSemibold SDF", 12.0f, Color.white, TextAlignmentOptions.MidlineLeft)
                                                .WithComponent<TextMeshProUGUI>(text => {
                                                    text.fontSize = 12.0f;
                                                    text.alignment = TextAlignmentOptions.Center;
                                                    text.enableAutoSizing = true;
                                                    text.fontSizeMax = 12.0f;
                                                    text.fontSizeMin = 6.0f;
                                                })
                                                .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.Unconstrained)
                                                .Layout()
                                                    .MinWidth(16)
                                                    .FlexibleWidth(0)
                                                    .MinHeight(16)
                                                    .PreferredHeight(16)
                                                    .FlexibleHeight(0)
                                                .Done
                                            .Done
                                        .Done;
                                }
                            })
                        .Done;
                }

                builder = builder
                    .Element("Gap")
                        .Layout()
                            .MinWidth(24)
                            .PreferredWidth(24)
                            .FlexibleWidth(0)
                            .MinHeight(24)
                            .PreferredHeight(24)
                            .FlexibleHeight(0)
                        .Done
                        .Element("Arrow")
                            .SetRectTransform(0, -16, 0, -16, 0, 1, 0, 1, 0, 1)
                            .SetSizeDelta(24, 24)
                            .Component_Image(_arrowLeftSprite, Color.white, Image.Type.Simple)
                        .Done
                    .Done;

                foreach (var producer in recipe.producers)
                {
                    var producerAmount = recipeAmount.Value * recipe.time / (producer.speed * 60.0);
                    builder = builder
                        .Element("Producer Wrapper")
                            .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                            .AutoSize(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize)
                            .Layout()
                                .MinWidth(58)
                                .PreferredWidth(58)
                                .FlexibleWidth(0)
                            .Done
                            .Element_IconButton($"Input - {producer.name}", producer.icon, 48, 48)
                                .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                                .Component_Tooltip(producer.name)
                            .Done
                            .Element_Label($"Amount - {producer.name}", $"{Math.Max(0.01, (double)producerAmount):0.##}", 58)
                                .WithComponent<TextMeshProUGUI>(text => {
                                    text.fontSize = 12.0f;
                                    text.alignment = TextAlignmentOptions.Center;
                                    text.enableAutoSizing = true;
                                    text.fontSizeMax = 12.0f;
                                    text.fontSizeMin = 6.0f;
                                })
                            .Done
                            .Do(powerBuilder => {
                                if (producer.powerUsage > 0.0 && producerAmount > 0.0)
                                {
                                    var power = (double)(producer.powerUsage * producerAmount);
                                    string powerText;
                                    if (power >= 10000000000.0)
                                    {
                                        powerText = $"{power / 1000000000.0:0.#}TW";
                                    }
                                    else if (power >= 10000000.0)
                                    {
                                        powerText = $"{power / 1000000.0:0.#}GW";
                                    }
                                    else if (power >= 10000.0)
                                    {
                                        powerText = $"{power / 1000.0:0.#}MW";
                                    }
                                    else
                                    {
                                        powerText = $"{Mathf.RoundToInt((float)power)}KW";
                                    }

                                    powerBuilder = powerBuilder
                                        .Element_Label($"Power - {producer.name}", powerText, 58)
                                            .WithComponent<TextMeshProUGUI>(text => {
                                                text.fontSize = 12.0f;
                                                text.alignment = TextAlignmentOptions.Center;
                                                text.enableAutoSizing = true;
                                                text.fontSizeMax = 12.0f;
                                                text.fontSizeMin = 6.0f;
                                            })
                                        .Done;
                                }
                            })
                        .Done;
                }

                if (recipe.inputs.Length > 0)
                {
                    builder = builder
                        .Element("Gap")
                            .Layout()
                                .MinWidth(24)
                                .PreferredWidth(24)
                                .FlexibleWidth(0)
                                .MinHeight(24)
                                .PreferredHeight(24)
                                .FlexibleHeight(0)
                            .Done
                            .Element("Arrow")
                                .SetRectTransform(0, -16, 0, -16, 0, 1, 0, 1, 0, 1)
                                .SetSizeDelta(24, 24)
                                .Component_Image(_arrowLeftSprite, Color.white, Image.Type.Simple)
                            .Done
                        .Done;

                    foreach (var input in recipe.inputs)
                    {
                        var itemElement = input.itemElement;
                        builder = builder
                            .Element("Input Wrapper")
                                .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperCenter, false, true, true, false, false, false, false)
                                .AutoSize(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize)
                                .Layout()
                                    .MinWidth(58)
                                    .PreferredWidth(58)
                                    .FlexibleWidth(0)
                                .Done
                                .Element_IconButton($"Input - {itemElement.name}", itemElement.icon, 48, 48)
                                    .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                                    .Component_Tooltip(itemElement.name)
                                    .SetOnClick(() => { ToggleInput(itemElement.fullIdentifier); })
                                .Done
                                .Element_Label($"Amount - {itemElement.name}", $"{Math.Max(0.01, (double)(input.amount * recipeAmount.Value)):0.##}", 58)
                                    .WithComponent<TextMeshProUGUI>(text =>
                                    {
                                        text.fontSize = 12.0f;
                                        text.alignment = TextAlignmentOptions.Center;
                                        text.enableAutoSizing = true;
                                        text.fontSizeMax = 12.0f;
                                        text.fontSizeMin = 6.0f;
                                    })
                                .Done
                                .Do(beltAmountBuild => {
                                    if (itemElement.isItem)
                                    {
                                        beltAmountBuild = beltAmountBuild
                                            .Element("Belt Amount Wrapper")
                                                .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                                                .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                                                .Layout()
                                                    .FlexibleWidth(0)
                                                .Done
                                                .Element("Belt Icon")
                                                    .Component_Image(conveyor.icon, Color.white, Image.Type.Simple)
                                                    .Layout()
                                                        .MinWidth(16)
                                                        .PreferredWidth(16)
                                                        .FlexibleWidth(0)
                                                        .MinHeight(16)
                                                        .PreferredHeight(16)
                                                        .FlexibleHeight(0)
                                                    .Done
                                                .Done
                                                .Element($"Belt Amount - {itemElement.name}")
                                                    .Component_Text($"{Math.Max(0.01, (double)(input.amount * recipeAmount.Value / conveyorSpeed)):0.##}", "OpenSansSemibold SDF", 12.0f, Color.white, TextAlignmentOptions.MidlineLeft)
                                                    .WithComponent<TextMeshProUGUI>(text => {
                                                        text.fontSize = 12.0f;
                                                        text.alignment = TextAlignmentOptions.Center;
                                                        text.enableAutoSizing = true;
                                                        text.fontSizeMax = 12.0f;
                                                        text.fontSizeMin = 6.0f;
                                                    })
                                                    .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.Unconstrained)
                                                    .Layout()
                                                        .MinWidth(16)
                                                        .FlexibleWidth(0)
                                                        .MinHeight(16)
                                                        .PreferredHeight(16)
                                                        .FlexibleHeight(0)
                                                    .Done
                                                .Done
                                            .Done;
                                    }
                                })
                            .Done;
                    }
                }

                builder = builder.Done;
            }
            builder.End();
            sw.Stop();
            Plugin.log.Log($"Update UI: {sw.ElapsedMilliseconds}ms");

            sw.Restart();
            sw.Start();
            foreach (var inputLabel in _inputLabels.Values) inputLabel.text = string.Empty;
            _extraInputList.transform.DestroyAllChildren();
            builder = UIBuilder.BeginWith(_extraInputList);
            foreach (var input in inputAmounts)
            {
                if (_inputLabels.TryGetValue(input.Key, out var inputLabel))
                {
                    inputLabel.text = $"{Math.Max(0.01, (double)input.Value):0.##}";
                }
                else if (input.Value > 0.001)
                {
                    if (_extraInputList.transform.childCount == 0)
                    {
                        builder = builder
                            .Element("Gap")
                                .Layout()
                                    .MinWidth(24)
                                    .PreferredWidth(24)
                                    .FlexibleWidth(0)
                                    .MinHeight(24)
                                    .PreferredHeight(24)
                                    .FlexibleHeight(0)
                                .Done
                                .Element("Arrow")
                                    .SetRectTransform(0, -16, 0, -16, 0, 1, 0, 1, 0, 1)
                                    .SetSizeDelta(24, 24)
                                    .Component_Image(_plusSprite, Color.white, Image.Type.Simple)
                                .Done
                            .Done;
                    }

                    var inputElement = input.Key;
                    builder = builder
                        .Element($"Input Button Wrapper - {inputElement.name}")
                            .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                            .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                            .Element_IconButton($"Input Button - {inputElement.name}", inputElement.icon, 48, 48)
                                .Component_Tooltip(inputElement.name)
                                .SetOnClick(() => { ToggleInput(inputElement.fullIdentifier); })
                            .Done
                            .Element_Label($"Input Label - {inputElement.name}", $"{Math.Max(0.01, (double)input.Value):0.##}", 58)
                                .WithComponent<TextMeshProUGUI>(text => { text.fontSize = 12.0f; text.alignment = TextAlignmentOptions.Center; })
                            .Done
                        .Done;
                }
            }
            builder.End();

            _extraOutputList.transform.DestroyAllChildren();
            builder = UIBuilder.BeginWith(_extraOutputList);
            foreach (var output in result.wasteAmounts)
            {
                if (output.Value > 0.001)
                {
                    if (_extraOutputList.transform.childCount == 0)
                    {
                        builder = builder
                            .Element("Gap")
                                .Layout()
                                    .MinWidth(24)
                                    .PreferredWidth(24)
                                    .FlexibleWidth(0)
                                    .MinHeight(24)
                                    .PreferredHeight(24)
                                    .FlexibleHeight(0)
                                .Done
                                .Element("Arrow")
                                    .SetRectTransform(0, -16, 0, -16, 0, 1, 0, 1, 0, 1)
                                    .SetSizeDelta(24, 24)
                                    .Component_Image(_plusSprite, Color.white, Image.Type.Simple)
                                .Done
                            .Done;
                    }

                    var outputElement = output.Key;
                    builder = builder
                        .Element($"Output Button Wrapper - {outputElement.name}")
                            .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, false, false, false)
                            .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                            .Element_IconButton($"Output Button - {outputElement.name}", outputElement.icon, 48, 48)
                                .Component_Tooltip(outputElement.name)
                            .Done
                            .Element_Label($"Output Label - {outputElement.name}", $"{Math.Max(0.01, (double)output.Value):0.##}", 58)
                                .WithComponent<TextMeshProUGUI>(text => { text.fontSize = 12.0f; text.alignment = TextAlignmentOptions.Center; })
                            .Done
                        .Done;
                }
            }
            builder.End();
            sw.Stop();
            Plugin.log.Log($"Update UI 2: {sw.ElapsedMilliseconds}ms");
        }

        public void iec_triggerFrameClose()
        {
            Hide();
        }
    }
}
