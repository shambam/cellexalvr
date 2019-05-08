﻿using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.DesktopUI;
using CellexalVR.Interaction;
using CellexalVR.Menu.SubMenus;
using CellexalVR.SceneObjects;
using CellexalVR.Tools;
using SQLiter;
using TMPro;
using UnityEngine;
using VRTK;
using SQLiter;
using CellexalVR.Tutorial;

namespace CellexalVR.General
{
    /// <summary>
    /// This class just holds a lot of references to other scripts and gameobjects so they won't clutter the inspector so much.
    /// </summary>
    public class ReferenceManager : MonoBehaviour
    {
        #region Controller things
        [Header("Controller things")]
        public SteamVR_TrackedObject rightController;
        public SteamVR_TrackedObject leftController;
        public ControllerModelSwitcher controllerModelSwitcher;
        //public GroupInfoDisplay groupInfoDisplay;
        //public StatusDisplay statusDisplay;
        //public StatusDisplay statusDisplayHUD;
        //public StatusDisplay statusDisplayFar;
        //public GameObject HUD;
        //public GameObject FarDisplay;
        public TextMeshProUGUI HUDFlashInfo;
        public TextMeshProUGUI HUDGroupInfo;
        public TextMeshProUGUI FarFlashInfo;
        public TextMeshProUGUI FarGroupInfo;
        public GameObject headset;
        public BoxCollider controllerMenuCollider;
        public LaserPointerController rightLaser;
        public VRTK_StraightPointerRenderer leftLaser;
        public LaserPointerController laserPointerController;

        #endregion

        #region Tools
        [Header("Tools")]
        //public SelectionToolHandler selectionToolHandler;
        public SelectionToolCollider selectionToolCollider;
        public GameObject deleteTool;
        public MinimizeTool minimizeTool;
        public GameObject helpMenu;
        public DrawTool drawTool;
        public GameObject webBrowser;

        #endregion

        #region Menu
        [Header("Menu")]
        public GameObject mainMenu;
        public ToggleArcsSubMenu arcsSubMenu;
        public AttributeSubMenu attributeSubMenu;
        public ColorByIndexMenu indexMenu;
        public GraphFromMarkersMenu createFromMarkerMenu;
        public SelectionFromPreviousMenu selectionFromPreviousMenu;
        public ColorByGeneMenu colorByGeneMenu;
        public FilterMenu filterMenu;
        public GameObject selectionMenu;
        public TextMesh currentFlashedGeneText;
        public GameObject frontButtons;
        public GameObject rightButtons;
        public GameObject backButtons;
        public GameObject leftButtons;
        public TextMesh frontDescription;
        public TextMesh rightDescription;
        public TextMesh backDescription;
        public TextMesh leftDescription;
        public MenuRotator menuRotator;
        public MinimizedObjectHandler minimizedObjectHandler;
        public MenuToggler menuToggler;

        #endregion

        #region Managers, generators and things
        [Header("Managers, generators and things")]
        public GraphManager graphManager;
        public CellManager cellManager;
        public SelectionManager selectionManager;
        public HeatmapGenerator heatmapGenerator;
        public NetworkGenerator networkGenerator;
        public GraphGenerator graphGenerator;
        public InputFolderGenerator inputFolderGenerator;
        public LoaderController loaderController;
        public ConfigManager configManager;
        public GameObject helperCylinder;
        public InputReader inputReader;
        public SQLite database;
        public LogManager logManager;
        public GameManager gameManager;
        public GameObject calculatorCluster;
        public ConsoleManager consoleManager;
        public TurnOffThoseLights turnOffThoseLights;
        public GameObject fpsCounter;
        public DemoManager demoManager;
        public NewGraphFromMarkers newGraphFromMarkers;
        public NotificationManager notificationManager;
        public TutorialManager tutorialManager;
        #endregion

        #region GeneKeyboard
        [Header("Keyboards")]
        public KeyboardHandler keyboardHandler;
        public KeyboardSwitch keyboardSwitch;
        public CorrelatedGenesList correlatedGenesList;
        public PreviousSearchesList previousSearchesList;
        public AutoCompleteList autoCompleteList;
        public KeyboardHandler folderKeyboard;
        public KeyboardHandler webBrowserKeyboard;

        #endregion

        #region SettingsMenu
        [Header("Settings Menu")]
        public SettingsMenu settingsMenu;
        public ColorPicker colorPicker;
        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Attempts to set all references using <see cref="GameObject.Find(string)"/> and <see cref="GameObject.GetComponent(string)"/>.
        /// </summary>
        public void AttemptSetReferences()
        {
            rightController = GameObject.Find("[CameraRig]/Controller (right)").GetComponent<SteamVR_TrackedObject>();
            leftController = GameObject.Find("[CameraRig]/Controller (left)").GetComponent<SteamVR_TrackedObject>();
            GameObject vrtkLeftController = GameObject.Find("[VRTK]/LeftController");
            controllerModelSwitcher = vrtkLeftController.GetComponent<ControllerModelSwitcher>();
            TextMeshProUGUI HUDFlashInfo;
            TextMeshProUGUI HUDGroupInfo;
            TextMeshProUGUI FarFlashInfo;
            TextMeshProUGUI FarGroupInfo;
            headset = GameObject.Find("[CameraRig]/Camera (head)/Camera (eye)");
            controllerMenuCollider = vrtkLeftController.GetComponent<BoxCollider>();
            rightLaser = vrtkLeftController.GetComponent<LaserPointerController>();
            leftLaser = vrtkLeftController.GetComponent<VRTK_StraightPointerRenderer>();
            laserPointerController = GameObject.Find("[VRTK]/RightController").GetComponent<LaserPointerController>();

            selectionToolCollider = rightController.GetComponentInChildren<SelectionToolCollider>(true);
            deleteTool = rightController.transform.Find("Delete Tool").gameObject;
            minimizeTool = rightController.GetComponentInChildren<MinimizeTool>(true);
            GameObject helpMenu;
            drawTool = rightController.GetComponentInChildren<DrawTool>();
            webBrowser = GameObject.Find("WebBrowser");

            mainMenu = GameObject.Find("MenuHolder/Main Menu");
            arcsSubMenu = mainMenu.GetComponentInChildren<ToggleArcsSubMenu>(true);
            attributeSubMenu = mainMenu.GetComponentInChildren<AttributeSubMenu>(true);
            indexMenu = mainMenu.GetComponentInChildren<ColorByIndexMenu>(true);
            createFromMarkerMenu = mainMenu.GetComponentInChildren<GraphFromMarkersMenu>(true);
            selectionFromPreviousMenu = mainMenu.GetComponentInChildren<SelectionFromPreviousMenu>(true);
            colorByGeneMenu = mainMenu.GetComponentInChildren<ColorByGeneMenu>(true);
            filterMenu = mainMenu.GetComponentInChildren<FilterMenu>(true);
            selectionMenu = GameObject.Find("MenuHolder/Main Menu/Selection Tool Menu");
            TextMesh currentFlashedGeneText;
            frontButtons = GameObject.Find("MenuHolder/Main Menu/Front Buttons");
            rightButtons = GameObject.Find("MenuHolder/Main Menu/Right Buttons");
            backButtons = GameObject.Find("MenuHolder/Main Menu/Back Buttons");
            leftButtons = GameObject.Find("MenuHolder/Main Menu/Left Buttons");
            frontDescription = frontButtons.transform.Find("Description Text Front Side").GetComponent<TextMesh>();
            rightDescription = rightButtons.transform.Find("Description Text Right Side").GetComponent<TextMesh>();
            backDescription = backButtons.transform.Find("Description Text Back Side").GetComponent<TextMesh>();
            leftDescription = leftButtons.transform.Find("Description Text Left Side").GetComponent<TextMesh>();
            menuRotator = mainMenu.GetComponent<MenuRotator>();
            minimizedObjectHandler = GameObject.Find("MenuHolder/Main Menu/Jail").GetComponent<MinimizedObjectHandler>();
            menuToggler = vrtkLeftController.GetComponent<MenuToggler>();

            GameObject managersParent = GameObject.Find("Managers");
            GameObject generatorsParent = GameObject.Find("Generators");
            graphManager = managersParent.GetComponentInChildren<GraphManager>();
            cellManager = managersParent.GetComponentInChildren<CellManager>();
            selectionManager = managersParent.GetComponentInChildren<SelectionManager>();
            heatmapGenerator = generatorsParent.GetComponentInChildren<HeatmapGenerator>();
            networkGenerator = generatorsParent.GetComponentInChildren<NetworkGenerator>();
            graphGenerator = generatorsParent.GetComponentInChildren<GraphGenerator>();
            inputFolderGenerator = generatorsParent.GetComponentInChildren<InputFolderGenerator>();
            loaderController = GameObject.Find("Tron_Loader").GetComponent<LoaderController>();
            GameObject inputreader = GameObject.Find("InputReader");
            configManager = inputreader.GetComponent<ConfigManager>();
            GameObject helperCylinder;
            inputReader = inputreader.GetComponent<InputReader>();
            database = GameObject.Find("SQLiter").GetComponent<SQLiter.SQLite>();
            logManager = inputreader.GetComponent<LogManager>();
            gameManager = managersParent.GetComponentInChildren<GameManager>();
            calculatorCluster = GameObject.Find("Calculator cluster");
            consoleManager = GameObject.Find("Console").GetComponent<ConsoleManager>();
            turnOffThoseLights = GameObject.Find("Light For Testing").GetComponent<TurnOffThoseLights>();
            fpsCounter = GameObject.Find("FPS canvas");
            DemoManager demoManager;
            newGraphFromMarkers = createFromMarkerMenu.GetComponent<NewGraphFromMarkers>();

            keyboardHandler = GameObject.Find("Keyboard Setup").GetComponent<KeyboardHandler>();
            keyboardSwitch = GameObject.Find("Keyboard Setup").GetComponent<KeyboardSwitch>();
            correlatedGenesList = GameObject.Find("Keyboard Setup/Correlated Genes List").GetComponent<CorrelatedGenesList>();
            previousSearchesList = GameObject.Find("Keyboard Setup/Previous Searches List").GetComponent<PreviousSearchesList>();
            autoCompleteList = GameObject.Find("Keyboard Setup").GetComponent<AutoCompleteList>();
            folderKeyboard = GameObject.Find("Tron_Loader/Folder Keyboard").GetComponent<KeyboardHandler>();
            webBrowserKeyboard = GameObject.Find("WebBrowser/Keyboard Setup").GetComponent<KeyboardHandler>();

            settingsMenu = GameObject.Find("Settings Menu").GetComponent<SettingsMenu>();
            colorPicker = settingsMenu.transform.Find("Color Picker/Content").GetComponent<ColorPicker>();
        }
#endif
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(ReferenceManager))]
    public class ReferenceManagerEditor : UnityEditor.Editor
    {
        private ReferenceManager instance;

        void OnEnable()
        {
            instance = (ReferenceManager)target;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Auto-populate references"))
            {
                instance.AttemptSetReferences();
            }
            DrawDefaultInspector();
        }
    }
#endif
}