using System.Collections.Generic;
using TMPro;
using Unfoundry;
using UnityEngine.UI;
using UnityEngine;
using System.IO;
using TinyJSON;
using System;
using System.Linq;

namespace PlanIt
{
    internal class PlannerFrame : IEscapeCloseable
    {
        private GameObject _frame = null;
        private GameObject _tabsPanel = null;
        private GameObject _planList = null;
        private GameObject _planContainer = null;
        private GameObject _planContent = null;
        private GameObject _outputList = null;
        private GameObject _inputList = null;
        private TMP_InputField _newPlanInput;
        private List<UIBuilder.GenericUpdateDelegate> _guiUpdaters = new List<UIBuilder.GenericUpdateDelegate>();

        private GameObject[] _conveyorOptionButtons = new GameObject[3];
        private GameObject[] _assemblerOptionButtons = new GameObject[3];
        private GameObject[] _crusherOptionButtons = new GameObject[2];
        private GameObject[] _smelterOptionButtons = new GameObject[2];
        private GameObject[] _metallurgyOptionButtons = new GameObject[3];
        private Toggle _allowUnresearchedToggle;

        private string _currentPlanPath = string.Empty;
        private PlanData _currentPlan = default;

        private string _planFolder = string.Empty;

        public bool IsOpen => _frame != null && _frame.activeSelf;

        public ItemSelectFrame _itemSelectFrame;

        public PlannerFrame(string planFolder)
        {
            _planFolder = planFolder;
            _itemSelectFrame = new ItemSelectFrame();
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

            _currentPlanPath = filePath;

            _currentPlan = PlanData.Load(filePath);

            _planContainer.transform.DestroyAllChildren();

            UIBuilder.BeginWith(_planContainer)
                .Element("Options Row")
                    .Layout()
                        .FlexibleHeight(0)
                    .Done
                    .AutoSize(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize)
                    .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 4, TextAnchor.MiddleLeft, false, true, true, false, false, false, false)
                    .Element_IconButton("Conveyor 1 Button", "conveyor_i")
                        .Keep(out _conveyorOptionButtons[0])
                        .SetOnClick(() => { _currentPlan.conveyorTier = 0; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Conveyor I")
                    .Done
                    .Element_IconButton("Conveyor 2 Button", "conveyor_iii")
                        .Keep(out _conveyorOptionButtons[1])
                        .SetOnClick(() => { _currentPlan.conveyorTier = 1; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Conveyor II")
                    .Done
                    .Element_IconButton("Conveyor 3 Button", "conveyor_iii")
                        .Keep(out _conveyorOptionButtons[2])
                        .SetOnClick(() => { _currentPlan.conveyorTier = 2; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Conveyor III")
                    .Done
                    .Element("Spacer")
                        .Layout()
                            .MinWidth(16)
                            .PreferredWidth(16)
                            .FlexibleWidth(0)
                        .Done
                    .Done
                    .Element_IconButton("Assembler 1 Button", "assembler_i")
                        .Keep(out _assemblerOptionButtons[0])
                        .SetOnClick(() => { _currentPlan.assemblerTier = 0; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Assembler I")
                    .Done
                    .Element_IconButton("Assembler 2 Button", "assembler_ii")
                        .Keep(out _assemblerOptionButtons[1])
                        .SetOnClick(() => { _currentPlan.assemblerTier = 1; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Assembler II")
                    .Done
                    .Element_IconButton("Assembler 3 Button", "assembler_iii")
                        .Keep(out _assemblerOptionButtons[2])
                        .SetOnClick(() => { _currentPlan.assemblerTier = 2; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Assembler III")
                    .Done
                    .Element("Spacer")
                        .Layout()
                            .MinWidth(16)
                            .PreferredWidth(16)
                            .FlexibleWidth(0)
                        .Done
                    .Done
                    .Element_IconButton("Crusher 1 Button", "ore_crusher")
                        .Keep(out _crusherOptionButtons[0])
                        .SetOnClick(() => { _currentPlan.crusherTier = 0; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Crusher I")
                    .Done
                    .Element_IconButton("Crusher 2 Button", "ore_crusher_ii")
                        .Keep(out _crusherOptionButtons[1])
                        .SetOnClick(() => { _currentPlan.crusherTier = 1; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Crusher II")
                    .Done
                    .Element("Spacer")
                        .Layout()
                            .MinWidth(16)
                            .PreferredWidth(16)
                            .FlexibleWidth(0)
                        .Done
                    .Done
                    .Element_IconButton("Smelter 1 Button", "smelter_small")
                        .Keep(out _smelterOptionButtons[0])
                        .SetOnClick(() => { _currentPlan.useAdvancedSmelter = false; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Smelter If Possible")
                    .Done
                    .Element_IconButton("Smelter 2 Button", "smelter")
                        .Keep(out _smelterOptionButtons[1])
                        .SetOnClick(() => { _currentPlan.useAdvancedSmelter = true; UpdateOptionButtons(); })
                        .Component_Tooltip("Use Advanced Smelter")
                    .Done
                    .Element("Spacer")
                        .Layout()
                            .MinWidth(16)
                            .PreferredWidth(16)
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
                            .MinWidth(16)
                            .PreferredWidth(16)
                            .FlexibleWidth(0)
                        .Done
                    .Done
                    .Element_Toggle("Allow Unresearched Toggle", false, 30.0f, isOn =>
                    {
                        _currentPlan.allowUnresearched = isOn; UpdateOptionButtons();
                    })
                        .Keep(out _allowUnresearchedToggle)
                        .Component_Tooltip("Allow using unresearched recipes.")
                    .Done
                    .Element_Label("Allow Unresearched Label", "Allow Unresearched", 180)
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
                .Element("Outputs Row")
                    .Layout()
                        .FlexibleHeight(0)
                    .Done
                    .AutoSize(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize)
                    .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 4, TextAnchor.MiddleLeft, false, true, true, false, false, false, false)
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
                        .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.MiddleLeft, false, true, true, false, false, false, false)
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
                    .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 4, TextAnchor.MiddleLeft, false, true, true, false, false, false, false)
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
                        .SetHorizontalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, false, true, false, false)
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
                        .SetVerticalLayout(new RectOffset(0, 0, 0, 0), 2.0f, TextAnchor.UpperLeft, false, true, true, true, false, false, false)
                        .AutoSize(ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize)
                        .Done;
                })
                    .Layout()
                        .FlexibleHeight(1)
                    .Done
                .Done
                .End();

            UpdateOptionButtons();
            UpdateOutputs();
            UpdateInputs();
        }

        private void AddOutput()
        {
            _itemSelectFrame.Show(itemElement =>
            {
                foreach (var output in _currentPlan.outputs) if (output == itemElement.fullIdentifier) return;
                foreach (var input in _currentPlan.inputs) if (input == itemElement.fullIdentifier) return;

                _currentPlan.outputs.Add(itemElement.fullIdentifier);
                _currentPlan.outputAmounts.Add(0.0f);
                SavePlan();
                UpdateOutputs();
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
            });
        }

        private void RemoveLastOutput()
        {
            if (_currentPlan.outputs.Count == 0) return;
            _currentPlan.outputs.RemoveAt(_currentPlan.outputs.Count - 1);
            _currentPlan.outputAmounts.RemoveAt(_currentPlan.outputAmounts.Count - 1);
            SavePlan();
            UpdateOutputs();
            AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIButtonClick);
        }

        private void RemoveLastInput()
        {
            if (_currentPlan.inputs.Count == 0) return;
            _currentPlan.inputs.RemoveAt(_currentPlan.inputs.Count - 1);
            SavePlan();
            UpdateInputs();
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
            AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIButtonClick);
        }

        private void RemoveInput(string fullIdentifier)
        {
            if (_currentPlan.inputs.Count == 0) return;
            if (!_currentPlan.inputs.Remove(fullIdentifier)) return;
            SavePlan();
            UpdateInputs();
            AudioManager.playUISoundEffect(ResourceDB.resourceLinker.audioClip_UIButtonClick);
        }

        private void SetOutputAmount(int outputIndex, float outputAmount)
        {
            if (outputIndex < 0 || outputIndex >= _currentPlan.outputAmounts.Count) return;

            _currentPlan.outputAmounts[outputIndex] = outputAmount;
            SavePlan();
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
            UpdateOptionButtons(_currentPlan.assemblerTier, _assemblerOptionButtons);
            UpdateOptionButtons(_currentPlan.crusherTier, _crusherOptionButtons);
            UpdateOptionButtons(_currentPlan.useAdvancedSmelter ? 1 : 0, _smelterOptionButtons);
            UpdateOptionButtons(_currentPlan.metallurgyTier, _metallurgyOptionButtons);

            _allowUnresearchedToggle.SetIsOnWithoutNotify(_currentPlan.allowUnresearched);

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
            UpdateSolution();

            if (_currentPlan.outputs.Count != _currentPlan.outputAmounts.Count)
            {
                Plugin.log.LogWarning($"Output count ({_currentPlan.outputs.Count}) does not match output amount count ({_currentPlan.outputAmounts.Count})");
                return;
            }

            _outputList.transform.DestroyAllChildren();
            var builder = UIBuilder.BeginWith(_outputList);
            for (int i = 0; i < _currentPlan.outputs.Count; i++)
            {
                string output = _currentPlan.outputs[i];
                float outputAmount = _currentPlan.outputAmounts[i];
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
                            TMP_InputField.ContentType.DecimalNumber,
                            value =>
                            {
                                try
                                {
                                    SetOutputAmount(outputIndex, Convert.ToSingle(value, System.Globalization.CultureInfo.InvariantCulture));
                                }
                                catch
                                {
                                    SetOutputAmount(outputIndex, 0.0f);
                                }
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
        }

        private void UpdateInputs()
        {
            UpdateSolution();

            _inputList.transform.DestroyAllChildren();
            var builder = UIBuilder.BeginWith(_inputList);
            foreach (var input in _currentPlan.inputs)
            {
                var inputElement = ItemElementTemplate.Get(input);
                if (inputElement.isValid)
                {
                    builder.Element_IconButton($"Input Button - {inputElement.name}", inputElement.icon)
                        .Component_Tooltip(inputElement.name)
                        .SetOnClick(() => { RemoveInput(inputElement.fullIdentifier); });
                }
            }
        }

        private ItemElementTemplate[] _allItemElements = null;
        private void UpdateSolution()
        {
            var solver = new Solver();
            solver.FindSubGraphs();

            var targets = new Dictionary<ItemElementTemplate, float>();
            var ignore = new HashSet<ulong>();

            for (int outputIndex = 0; outputIndex < _currentPlan.outputs.Count; outputIndex++)
            {
                var outputElement = ItemElementTemplate.Get(_currentPlan.outputs[outputIndex]);
                if (outputElement.isValid)
                {
                    targets[outputElement] = _currentPlan.outputAmounts[outputIndex];
                }
            }

            var result = solver.Solve(targets, ignore);
            result.Dump();
        }

        public void iec_triggerFrameClose()
        {
            Hide();
        }
    }
}
