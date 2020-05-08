using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using CellexalVR.General;
using CellexalVR.AnalysisLogic;

namespace CellexalVR.DesktopUI
{
    /// <summary>
    /// Controls the settings menu and its components.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        public GameObject settingsMenuGameObject;
        public ReferenceManager referenceManager;
        public GameObject unsavedChangesPrompt;
        public GameObject confirmQuitPrompt;
        public GameObject resetAllSettingsPrompt;
        public HinterController hinterController;
        //[Header("Menu items")]
        [Header("Username")]
        public TMPro.TMP_InputField usernameInputField;
        public TMPro.TMP_Text usernameText;
        [Header("Heatmap")]
        public TMPro.TMP_Dropdown heatmapColormapDropdown;
        public ColorPickerButton heatmapHighExpression;
        public ColorPickerButton heatmapMidExpression;
        public ColorPickerButton heatmapLowExpression;
        public Image heatmapGradient;
        public TMPro.TMP_InputField numberOfHeatmapColorsInputField;
        public TMPro.TMP_Dropdown heatmapAlgorithmDropdown;
        //public UnityEngine.UI.Dropdown heatmapAlgorithm;
        [Header("Graphs")]
        public TMPro.TMP_Dropdown graphColormapDropdown;
        public TMPro.TMP_Dropdown graphPointQualityDropdown;
        public TMPro.TMP_Dropdown graphPointSizeDropdown;
        public ColorPickerButton graphHighExpression;
        public ColorPickerButton graphMidExpression;
        public ColorPickerButton graphLowExpression;
        public Image graphGradient;
        public TMPro.TMP_InputField numberOfGraphColorsInputField;
        public ColorPickerButton graphDefaultColor;
        public ColorPickerButton graphZeroExpressionColor;
        public Toggle graphHightestExpressedMarker;
        [Header("Networks")]
        public TMPro.TMP_Dropdown networkAlgorithmDropdown;
        public TMPro.TMP_Dropdown networkLineColoringMethodDropdown;
        public ColorPickerButton networkLinePositiveHigh;
        public ColorPickerButton networkLinePositiveLow;
        public Image networkPositiveGradient;
        public ColorPickerButton networkLineNegativeHigh;
        public ColorPickerButton networkLineNegativeLow;
        public Image networkNegativeGradient;
        public TMPro.TMP_InputField networkNumberOfNetworkColors;
        public TMPro.TMP_InputField networkLineWidth;
        [Header("Selection")]
        public GameObject selectionColorGroup;
        public GameObject selectionColorButtonPrefab;
        public List<ColorPickerButton> selectionColorButtons;
        public GameObject addSelectionColorButton;
        [Header("Velocity")]
        public ColorPickerButton velocityHighColor;
        public ColorPickerButton velocityLowColor;
        [Header("Visual")]
        public TMPro.TMP_Dropdown skyboxDropdown;
        public Toggle notificationToggle;
        public ColorPickerButton skyboxTintColor;

        public Material[] skyboxes;

        public List<string> networkAlgorithms;
        public List<string> heatmapAlgorithms;
        public List<string> heatmapColormaps;
        public List<string> graphColormaps;
        public List<string> graphPointQualityModes;
        public List<string> graphPointSizeModes;
        public List<string> lineColouringMethods;

        private ColorPicker colorPicker;
        private Config beforeChanges;
        [HideInInspector]
        public bool unsavedChanges;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Awake()
        {
            CellexalEvents.ConfigLoaded.AddListener(SetValues);
            colorPicker = referenceManager.colorPicker;
            var skyboxOptions = new List<TMPro.TMP_Dropdown.OptionData>();
            foreach (Material mat in skyboxes)
            {
                skyboxOptions.Add(new TMPro.TMP_Dropdown.OptionData(mat.name));
            }
            skyboxDropdown.options = skyboxOptions;

            var networkMethods = new List<TMPro.TMP_Dropdown.OptionData>();
            foreach (string s in networkAlgorithms)
            {
                networkMethods.Add(new TMPro.TMP_Dropdown.OptionData(s));
            }
            networkAlgorithmDropdown.options = networkMethods;

            var hmColormaps = new List<TMPro.TMP_Dropdown.OptionData>();
            foreach (string s in heatmapColormaps)
            {
                hmColormaps.Add(new TMPro.TMP_Dropdown.OptionData(s));
            }
            heatmapColormapDropdown.options = hmColormaps;

            var gColormaps = new List<TMPro.TMP_Dropdown.OptionData>();
            foreach (string s in graphColormaps)
            {
                gColormaps.Add(new TMPro.TMP_Dropdown.OptionData(s));
            }
            graphColormapDropdown.options = gColormaps;

            var heatmapMethods = new List<TMPro.TMP_Dropdown.OptionData>();
            foreach (string s in heatmapAlgorithms)
            {
                heatmapMethods.Add(new TMPro.TMP_Dropdown.OptionData(s));
            }
            heatmapAlgorithmDropdown.options = heatmapMethods;

            var graphPointQualities = new List<TMPro.TMP_Dropdown.OptionData>();
            foreach (string s in graphPointQualityModes)
            {
                graphPointQualities.Add(new TMPro.TMP_Dropdown.OptionData(s));
            }
            graphPointQualityDropdown.options = graphPointQualities;

            var graphPointSizes = new List<TMPro.TMP_Dropdown.OptionData>();
            foreach (string s in graphPointSizeModes)
            {
                graphPointSizes.Add(new TMPro.TMP_Dropdown.OptionData(s));
            }
            graphPointSizeDropdown.options = graphPointSizes;

            var lineMethods = new List<TMPro.TMP_Dropdown.OptionData>();
            foreach (string s in lineColouringMethods)
            {
                lineMethods.Add(new TMPro.TMP_Dropdown.OptionData(s));
            }
            networkLineColoringMethodDropdown.options = lineMethods;

            selectionColorButtons = new List<ColorPickerButton>();

        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                bool newState = !settingsMenuGameObject.activeSelf;
                if (hinterController.hinter.activeSelf)
                {
                    hinterController.HideHinter();
                }
                if (!unsavedChanges)
                {
                    settingsMenuGameObject.SetActive(newState);
                    colorPicker.gameObject.SetActive(newState);
                }
                if (!newState && unsavedChanges)
                {
                    unsavedChangesPrompt.SetActive(true);
                    colorPicker.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Sets the values of the buttons in the menu to accurately depict what the current internal values are.
        /// </summary>
        private void SetValues()
        {
            usernameText.text = "Current user: " + CellexalUser.Username;
            heatmapHighExpression.Color = CellexalConfig.Config.HeatmapHighExpressionColor;
            heatmapMidExpression.Color = CellexalConfig.Config.HeatmapMidExpressionColor;
            heatmapLowExpression.Color = CellexalConfig.Config.HeatmapLowExpressionColor;
            numberOfHeatmapColorsInputField.text = "" + CellexalConfig.Config.NumberOfHeatmapColors;
            heatmapAlgorithmDropdown.value = heatmapAlgorithms.IndexOf(CellexalConfig.Config.HeatmapAlgorithm);
            graphPointQualityDropdown.value = graphPointQualityModes.IndexOf(CellexalConfig.Config.GraphPointQuality);
            graphPointSizeDropdown.value = graphPointSizeModes.IndexOf(CellexalConfig.Config.GraphPointSize);
            graphHighExpression.Color = CellexalConfig.Config.GraphHighExpressionColor;
            graphMidExpression.Color = CellexalConfig.Config.GraphMidExpressionColor;
            graphLowExpression.Color = CellexalConfig.Config.GraphLowExpressionColor;
            numberOfGraphColorsInputField.text = "" + CellexalConfig.Config.GraphNumberOfExpressionColors;
            graphDefaultColor.Color = CellexalConfig.Config.GraphDefaultColor;
            graphZeroExpressionColor.Color = CellexalConfig.Config.GraphZeroExpressionColor;
            graphHightestExpressedMarker.isOn = CellexalConfig.Config.GraphMostExpressedMarker;
            networkAlgorithmDropdown.value = networkAlgorithms.IndexOf(CellexalConfig.Config.NetworkAlgorithm);
            networkLineColoringMethodDropdown.value = CellexalConfig.Config.NetworkLineColoringMethod;
            networkLinePositiveHigh.Color = CellexalConfig.Config.NetworkLineColorPositiveHigh;
            networkLinePositiveLow.Color = CellexalConfig.Config.NetworkLineColorPositiveLow;
            networkLineNegativeHigh.Color = CellexalConfig.Config.NetworkLineColorNegativeHigh;
            networkLineNegativeLow.Color = CellexalConfig.Config.NetworkLineColorNegativeLow;
            networkNumberOfNetworkColors.text = "" + CellexalConfig.Config.NumberOfNetworkLineColors;
            networkLineWidth.text = "" + CellexalConfig.Config.NetworkLineWidth;
            //notificationToggle.isOn = CellexalConfig.Config.ShowNotifications;

            for (int i = 0; i < CellexalConfig.Config.SelectionToolColors.Length; ++i)
            {
                if (i < selectionColorButtons.Count)
                {
                    // there is already a button in the menu, change its color
                    selectionColorButtons[i].Color = CellexalConfig.Config.SelectionToolColors[i];
                }
                else if (i >= selectionColorButtons.Count)
                {
                    // no more buttons, create one
                    GameObject newButton = Instantiate(selectionColorButtonPrefab, selectionColorGroup.transform);
                    newButton.SetActive(true);
                    ColorPickerButton button = newButton.GetComponentInChildren<ColorPickerButton>();
                    button.Color = CellexalConfig.Config.SelectionToolColors[i];
                    button.selectionToolColorIndex = i;
                    addSelectionColorButton.transform.SetAsLastSibling();
                    selectionColorButtons.Add(button);
                }
            }
            if (CellexalConfig.Config.SelectionToolColors.Length < selectionColorButtons.Count)
            {
                int nrOfselectionColorButtons = selectionColorButtons.Count;
                for (int j = CellexalConfig.Config.SelectionToolColors.Length; j < nrOfselectionColorButtons; j++)
                {
                    RemoveSelectionColor(selectionColorButtons[selectionColorButtons.Count - 1].transform.parent.gameObject);
                }
                addSelectionColorButton.transform.SetAsLastSibling();
            }
            velocityHighColor.Color = CellexalConfig.Config.VelocityParticlesHighColor;
            velocityLowColor.Color = CellexalConfig.Config.VelocityParticlesLowColor;
            skyboxTintColor.Color = CellexalConfig.Config.SkyboxTintColor;



            SetNetworkColoringMethod();

            //LayoutRebuilder.MarkLayoutForRebuild((RectTransform)selectionColorGroup.transform);
            unsavedChanges = false;
            beforeChanges = new Config(CellexalConfig.Config);
        }

        private int TryParse(string s, int defaultValue)
        {
            if (s == null || s == "")
            {
                return defaultValue;
            }
            return int.Parse(s);
        }

        private float TryParse(string s, float defaultValue)
        {
            if (s == null || s == "")
            {
                return defaultValue;
            }
            return float.Parse(s, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
        }

        public void SetUser()
        {
            unsavedChanges = true;
            string name = usernameInputField.text;
            CellexalUser.Username = name;
            usernameText.text = "Current user: " + name;
        }

        public void SetNumberOfHeatmapColors()
        {
            unsavedChanges = true;
            int nColors = TryParse(numberOfHeatmapColorsInputField.text, 3);
            if (nColors < 3)
            {
                CellexalLog.Log("WARNING: Number of heatmap expression colors must be at least 3. Defaulting to 3.");
                nColors = 3;
            }
            numberOfHeatmapColorsInputField.text = "" + nColors;
            CellexalConfig.Config.NumberOfHeatmapColors = nColors;
            referenceManager.heatmapGenerator.InitColors();
            heatmapGradient.material.SetInt("_NColors", nColors);
        }

        public void SetHeatmapAlgorithm()
        {
            unsavedChanges = true;
            int val = heatmapAlgorithmDropdown.value;
            string algorithm = heatmapAlgorithmDropdown.options[val].text;
            CellexalConfig.Config.HeatmapAlgorithm = algorithm;
            referenceManager.heatmapGenerator.InitColors();
        }

        public void SetGraphPointQuality()
        {
            unsavedChanges = true;
            int val = graphPointQualityDropdown.value;
            string quality = graphPointQualityDropdown.options[val].text;
            CellexalConfig.Config.GraphPointQuality = quality;
            referenceManager.graphGenerator.UpdateMeshToUse();
            StartCoroutine(referenceManager.graphGenerator.RebuildGraphs());
            //referenceManager.heatmapGenerator.InitColors();
        }
        public void SetGraphPointSize()
        {
            unsavedChanges = true;
            int val = graphPointSizeDropdown.value;
            string size = graphPointSizeDropdown.options[val].text;
            CellexalConfig.Config.GraphPointSize = size;
            referenceManager.graphGenerator.UpdateMeshToUse();
            StartCoroutine(referenceManager.graphGenerator.RebuildGraphs());
            referenceManager.heatmapGenerator.InitColors();
        }

        public void SetNumberOfGraphColors()
        {
            unsavedChanges = true;
            int nColors = TryParse(numberOfGraphColorsInputField.text, 3);
            if (nColors < 3)
            {
                CellexalLog.Log("WARNING: Number of graph expression colors must be at least 3. Defaulting to 3.");
                nColors = 3;
            }
            numberOfGraphColorsInputField.text = "" + nColors;
            CellexalConfig.Config.GraphNumberOfExpressionColors = nColors;
            referenceManager.graphGenerator.CreateShaderColors();
            graphGradient.material.SetInt("_NColors", nColors);
        }

        public void SetNetworkAlgorithm()
        {
            unsavedChanges = true;
            int val = networkAlgorithmDropdown.value;
            string algorithm = networkAlgorithmDropdown.options[val].text;
            CellexalConfig.Config.NetworkAlgorithm = algorithm;
        }

        public void SetNetworkColoringMethod()
        {
            unsavedChanges = true;
            int newMethod = networkLineColoringMethodDropdown.value;
            CellexalConfig.Config.NetworkLineColoringMethod = newMethod;

            bool active = newMethod == 0;
            networkLinePositiveHigh.parentGroup.SetActive(active);
            networkLinePositiveLow.parentGroup.SetActive(active);
            networkLineNegativeHigh.parentGroup.SetActive(active);
            networkLineNegativeLow.parentGroup.SetActive(active);
            SetNumberOfNetworkColors();
        }

        public void SetNumberOfNetworkColors()
        {
            unsavedChanges = true;
            int nColors = TryParse(networkNumberOfNetworkColors.text, 1);
            if (CellexalConfig.Config.NetworkLineColoringMethod == 0 &&
                nColors < 4)
            {
                CellexalLog.Log("WARNING: Number of network line colors must be at least 4 when coloring method is set to by correlation. Defaulting to 4.");
                nColors = 4;
            }
            else if (CellexalConfig.Config.NetworkLineColoringMethod == 1 &&
                nColors < 1)
            {
                CellexalLog.Log("WARNING: Number of network line colors must be at least 1 when coloring method is set to random. Defaulting to 1.");
                nColors = 1;

            }
            networkNumberOfNetworkColors.text = "" + nColors;
            CellexalConfig.Config.NumberOfNetworkLineColors = nColors;
            referenceManager.networkGenerator.CreateLineMaterials();
            int halfColors = nColors / 2;
            networkPositiveGradient.material.SetInt("_NColors", halfColors);
            networkNegativeGradient.material.SetInt("_NColors", nColors - halfColors);
        }

        public void SetNetworkLineWidth()
        {
            unsavedChanges = true;
            float newValue = TryParse(networkLineWidth.text, 0.001f);
            if (newValue <= 0)
            {
                CellexalLog.Log("WARNING: Network line width may not be a negative value. Defaulting to 0.001.");
                newValue = 0.001f;
                networkLineWidth.text = "" + newValue;
            }
            CellexalConfig.Config.NetworkLineWidth = newValue;
        }

        public void SetSkyBox()
        {
            unsavedChanges = true;
            int selected = skyboxDropdown.value;
            RenderSettings.skybox = skyboxes[selected];
        }

        public void SetNotifications(bool active)
        {
            CellexalConfig.Config.ShowNotifications = active;
            referenceManager.notificationManager.active = active;
        }

        public void AddSelectionColor(bool update = true)
        {
            AddSelectionColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f), update);
        }

        public void AddSelectionColor(Color color, bool update = true)
        {
            unsavedChanges = true;
            GameObject newButton = Instantiate(selectionColorButtonPrefab, selectionColorGroup.transform);
            newButton.SetActive(true);
            ColorPickerButton colorPicker = newButton.GetComponentInChildren<ColorPickerButton>();
            colorPicker.Color = color;
            addSelectionColorButton.transform.SetAsLastSibling();
            selectionColorButtons.Add(colorPicker);
            if (update) UpdateSelectionToolColors();
        }

        public void RemoveSelectionColor(GameObject button)
        {
            unsavedChanges = true;
            selectionColorButtons.Remove(button.GetComponentInChildren<ColorPickerButton>());
            Destroy(button);
            UpdateSelectionToolColors();
        }


        public void UpdateSelectionToolColors()
        {
            Color[] colors = new Color[selectionColorButtons.Count];
            for (int i = 0; i < selectionColorButtons.Count; ++i)
            {
                selectionColorButtons[i].selectionToolColorIndex = i;
                colors[i] = selectionColorButtons[i].Color;
            }
            CellexalConfig.Config.SelectionToolColors = colors;
            referenceManager.selectionToolCollider.UpdateColors();
            referenceManager.attributeSubMenu.RecreateButtons();
            referenceManager.graphGenerator.CreateShaderColors();
        }

        public void SetGraphHighestExpressionMarker(bool active)
        {
            unsavedChanges = true;
            CellexalConfig.Config.GraphMostExpressedMarker = active;
        }

        public void UpdateVelocityColors()
        {
            unsavedChanges = true;
            foreach (AnalysisObjects.Graph g in referenceManager.velocityGenerator.ActiveGraphs)
            {
                g.velocityParticleEmitter.SetColors();
            }
        }

        public void ChangeMade()
        {
            unsavedChanges = true;
        }

        public void SaveAndClose()
        {
            CellexalLog.Log("Saved changes made in the settings menu.");
            unsavedChanges = false;
            if (referenceManager.multiuserMessageSender.multiplayer)
            {
                referenceManager.configManager.MultiUserSynchronise();
            }
            else
            {
                referenceManager.configManager.SaveConfigFile();
            }
            unsavedChangesPrompt.SetActive(false);
            settingsMenuGameObject.SetActive(false);
            colorPicker.gameObject.SetActive(false);
        }


        public void ShowResetAllSettings(bool show)
        {
            resetAllSettingsPrompt.SetActive(show);
        }

        public void ResetSettingsToFile()
        {
            referenceManager.configManager.ResetToDefault();
            resetAllSettingsPrompt.SetActive(false);
        }

        public void ResetSettingsToBefore()
        {
            CellexalLog.Log("Reset changes made in settings menu to what they were before.");
            CellexalConfig.Config = new Config(beforeChanges);
            SetValues();
            unsavedChangesPrompt.SetActive(false);
        }

        /// <summary>
        /// Spawns prompt before exiting.
        /// </summary>
        public void QuitButton()
        {
            confirmQuitPrompt.SetActive(true);
        }

        /// <summary>
        /// Quits the program.
        /// </summary>
        public void Quit()
        {
            CellexalLog.Log("Quit button pressed");
            CellexalLog.LogBacklog();
            // terminate server session
            referenceManager.inputReader.QuitServer();
            // Application.Quit() does not work in the unity editor, only in standalone builds.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

    }
}