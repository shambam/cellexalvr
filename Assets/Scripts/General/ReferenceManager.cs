using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.DesktopUI;
using CellexalVR.Filters;
using CellexalVR.Interaction;
using CellexalVR.Menu;
using CellexalVR.Menu.SubMenus;
using CellexalVR.Multiuser;
using CellexalVR.SceneObjects;
using CellexalVR.Tools;
using CellexalVR.Tutorial;
using SQLiter;
using UnityEditor;
using UnityEngine;
using VRTK;
using CellexalVR.AnalysisLogic.H5reader;
using UnityEngine.Serialization;

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
        public GameObject rightControllerScriptAlias;
        public GameObject leftControllerScriptAlias;
        public ControllerModelSwitcher controllerModelSwitcher;
        //public GroupInfoDisplay groupInfoDisplay;
        //public StatusDisplay statusDisplay;
        //public StatusDisplay statusDisplayHUD;
        //public StatusDisplay statusDisplayFar;
        //public GameObject HUD;
        //public GameObject FarDisplay;
        //public TextMeshProUGUI HUDFlashInfo;
        //public TextMeshProUGUI HUDGroupInfo;
        //public TextMeshProUGUI FarFlashInfo;
        //public TextMeshProUGUI FarGroupInfo;
        public GameObject headset;
        public BoxCollider controllerMenuCollider;
        public VRTK_StraightPointerRenderer rightLaser;
        public VRTK_StraightPointerRenderer leftLaser;
        public LaserPointerController laserPointerController;

        #endregion

        #region Tools
        [Header("Tools")]
        //public SelectionToolHandler selectionToolHandler;
        public SelectionToolCollider selectionToolCollider;
        public GameObject deleteTool;
        public MinimizeTool minimizeTool;
        //public GameObject helpMenu;
        public DrawTool drawTool;
        public GameObject webBrowser;
        public CaptureScreenshot screenshotCamera;
        public GameObject teleportLaser;

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
        public VelocitySubMenu velocitySubMenu;
        public GameObject selectionMenu;
        public FlybyMenu flybyMenu;
        //public TextMesh currentFlashedGeneText;
        public GameObject frontButtons;
        public GameObject rightButtons;
        public GameObject backButtons;
        public GameObject leftButtons;
        //public TextMesh frontDescription;
        //public TextMesh rightDescription;
        //public TextMesh backDescription;
        //public TextMesh leftDescription;
        public MenuRotator menuRotator;
        public MinimizedObjectHandler minimizedObjectHandler;
        public MenuToggler menuToggler;
        #endregion

        #region Managers, generators and things
        [Header("Managers, generators and things")]
        public GraphManager graphManager;
        public CellManager cellManager;
        public LineBundler lineBundler;
        public SelectionManager selectionManager;
        public AnnotationManager annotationManager;
        public HeatmapGenerator heatmapGenerator;
        public NetworkGenerator networkGenerator;
        public GraphGenerator graphGenerator;
        public LegendManager legendManager;
        public InputFolderGenerator inputFolderGenerator;
        public LoaderController loaderController;
        public ConfigManager configManager;
        //public GameObject helperCylinder;
        public InputReader inputReader;
        public ReportReader reportReader;
        public SQLite database;
        public LogManager logManager;
        public MultiuserMessageSender multiuserMessageSender;
        //public GameObject calculatorCluster;
        public ConsoleManager consoleManager;
        public TurnOffThoseLights turnOffThoseLights;
        public GameObject fpsCounter;
        //public DemoManager demoManager;
        public NewGraphFromMarkers newGraphFromMarkers;
        public NotificationManager notificationManager;
        public TutorialManager tutorialManager;
        public ScreenCanvas screenCanvas;
        //public GameObject helpVideoPlayer;
        public PlayVideo helpVideoManager;
        public VelocityGenerator velocityGenerator;
        public ConvexHullGenerator convexHullGenerator;
        public FilterManager filterManager;
        public ReportManager reportManager;
        public Floor floor;

        //h5reader annotator
        public H5ReaderAnnotatorScriptManager h5ReaderAnnotatorScriptManager;

        #endregion

        #region Keyboard
        [Header("Keyboards")]
        public KeyboardHandler geneKeyboard;
        public KeyboardSwitch keyboardSwitch;
        public CorrelatedGenesList correlatedGenesList;
        public PreviousSearchesList previousSearchesList;
        public AutoCompleteList autoCompleteList;
        public ColoringOptionsList coloringOptionsList;
        public KeyboardHandler folderKeyboard;
        public KeyboardHandler webBrowserKeyboard;
        public SessionHistoryList sessionHistoryList;

        #endregion

        #region Filters
        [Header("Filters")]
        public GameObject filterBlockBoard;
        public KeyboardHandler filterNameKeyboard;
        public KeyboardHandler filterOperatorKeyboard;
        public KeyboardHandler filterValueKeyboard;
        public AutoCompleteList filterNameKeyboardAutoCompleteList;
        public CullingFilterManager cullingFilterManager;

        #endregion

        #region SettingsMenu
        [Header("Settings Menu")]
        public SettingsMenu settingsMenu;
        public ColorPicker colorPicker;
        #endregion

        #region Multi-user
        public GameObject spectatorRig;
        public GameObject VRRig;

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Attempts to set all references using <see cref="GameObject.Find(string)"/> and <see cref="GameObject.GetComponent(string)"/>.
        /// </summary>
        public void AttemptSetReferences()
        {
            // sorry about this monstrosity
            Undo.RecordObject(this, "ReferenceManager Auto-populate");

            h5ReaderAnnotatorScriptManager = GameObject.Find("H5ReaderTestObjectManager").GetComponent<H5ReaderAnnotatorScriptManager>();
            rightController = GameObject.Find("[VRTK]3.3/SDK setup/[CameraRig]/Controller (right)").GetComponent<SteamVR_TrackedObject>();
            leftController = GameObject.Find("[VRTK]3.3/SDK setup/[CameraRig]/Controller (left)").GetComponent<SteamVR_TrackedObject>();
            rightControllerScriptAlias = GameObject.Find("[VRTK_Scripts]/RightControllerScriptAlias");
            leftControllerScriptAlias = GameObject.Find("[VRTK_Scripts]/LeftControllerScriptAlias");
            controllerModelSwitcher = leftController.GetComponent<ControllerModelSwitcher>();
            //TextMeshProUGUI HUDFlashInfo;
            //TextMeshProUGUI HUDGroupInfo;
            //TextMeshProUGUI FarFlashInfo;
            //TextMeshProUGUI FarGroupInfo;

            headset = GameObject.Find("[VRTK]3.3/SDK setup/[CameraRig]/Camera (head)/Camera (eye)");
            controllerMenuCollider = leftController.GetComponent<BoxCollider>();
            rightLaser = rightControllerScriptAlias.GetComponent<VRTK_StraightPointerRenderer>();
            leftLaser = leftControllerScriptAlias.GetComponent<VRTK_StraightPointerRenderer>();
            laserPointerController = rightControllerScriptAlias.GetComponent<LaserPointerController>();

            selectionToolCollider = rightController.GetComponentInChildren<SelectionToolCollider>(true);
            deleteTool = rightController.transform.Find("Delete Tool").gameObject;
            minimizeTool = rightController.GetComponentInChildren<MinimizeTool>(true);
            //GameObject helpMenu;
            drawTool = rightController.GetComponentInChildren<DrawTool>();
            webBrowser = GameObject.Find("WebBrowser");
            screenshotCamera = headset.GetComponentInChildren<CaptureScreenshot>(true);
            teleportLaser = leftControllerScriptAlias.GetComponentInChildren<VRTK_BezierPointerRenderer>(true).gameObject;

            mainMenu = GameObject.Find("MenuHolder/Main Menu");
            arcsSubMenu = mainMenu.GetComponentInChildren<ToggleArcsSubMenu>(true);
            attributeSubMenu = mainMenu.GetComponentInChildren<AttributeSubMenu>(true);
            indexMenu = mainMenu.GetComponentInChildren<ColorByIndexMenu>(true);
            createFromMarkerMenu = mainMenu.GetComponentInChildren<GraphFromMarkersMenu>(true);
            selectionFromPreviousMenu = mainMenu.GetComponentInChildren<SelectionFromPreviousMenu>(true);
            colorByGeneMenu = mainMenu.GetComponentInChildren<ColorByGeneMenu>(true);
            filterMenu = mainMenu.GetComponentInChildren<FilterMenu>(true);
            velocitySubMenu = mainMenu.GetComponentInChildren<VelocitySubMenu>(true);
            selectionMenu = GameObject.Find("MenuHolder/Main Menu/Selection Tool Menu");
            flybyMenu = mainMenu.GetComponentInChildren<FlybyMenu>();
            frontButtons = GameObject.Find("MenuHolder/Main Menu/Front Buttons");
            rightButtons = GameObject.Find("MenuHolder/Main Menu/Right Buttons");
            backButtons = GameObject.Find("MenuHolder/Main Menu/Back Buttons");
            leftButtons = GameObject.Find("MenuHolder/Main Menu/Left Buttons");
            //frontDescription = frontButtons.transform.Find("Description Text Front Side").GetComponent<TextMesh>();
            //rightDescription = rightButtons.transform.Find("Description Text Right Side").GetComponent<TextMesh>();
            //backDescription = backButtons.transform.Find("Description Text Back Side").GetComponent<TextMesh>();
            //leftDescription = leftButtons.transform.Find("Description Text Left Side").GetComponent<TextMesh>();
            menuRotator = mainMenu.GetComponent<MenuRotator>();
            minimizedObjectHandler = GameObject.Find("MenuHolder/Main Menu/Jail").GetComponent<MinimizedObjectHandler>();
            menuToggler = leftController.GetComponent<MenuToggler>();

            GameObject managersParent = GameObject.Find("Managers");
            GameObject generatorsParent = GameObject.Find("Generators");
            graphManager = managersParent.GetComponentInChildren<GraphManager>();
            cellManager = managersParent.GetComponentInChildren<CellManager>();
            lineBundler = managersParent.GetComponentInChildren<LineBundler>();
            selectionManager = managersParent.GetComponentInChildren<SelectionManager>();
            annotationManager = managersParent.GetComponentInChildren<AnnotationManager>();
            heatmapGenerator = generatorsParent.GetComponentInChildren<HeatmapGenerator>();
            networkGenerator = generatorsParent.GetComponentInChildren<NetworkGenerator>();
            graphGenerator = generatorsParent.GetComponentInChildren<GraphGenerator>();
            legendManager = managersParent.GetComponentInChildren<LegendManager>();
            cullingFilterManager = managersParent.GetComponentInChildren<CullingFilterManager>();
            inputFolderGenerator = generatorsParent.GetComponentInChildren<InputFolderGenerator>();
            loaderController = GameObject.Find("Tron_Loader").GetComponent<LoaderController>();
            GameObject inputreader = GameObject.Find("InputReader");
            configManager = inputreader.GetComponent<ConfigManager>();
            //GameObject helperCylinder;
            inputReader = inputreader.GetComponent<InputReader>();
            database = GameObject.Find("SQLiter").GetComponent<SQLiter.SQLite>();
            logManager = inputreader.GetComponent<LogManager>();
            multiuserMessageSender = managersParent.GetComponentInChildren<MultiuserMessageSender>();
            //calculatorCluster = GameObject.Find("Calculator cluster");
            consoleManager = GameObject.Find("Console").GetComponent<ConsoleManager>();
            turnOffThoseLights = GameObject.Find("Light For Testing").GetComponent<TurnOffThoseLights>();
            fpsCounter = GameObject.Find("FPS canvas");
            //DemoManager demoManager;
            newGraphFromMarkers = createFromMarkerMenu.GetComponent<NewGraphFromMarkers>();
            notificationManager = managersParent.GetComponentInChildren<NotificationManager>();
            tutorialManager = managersParent.GetComponentInChildren<TutorialManager>();
            screenCanvas = GameObject.Find("ScreenCanvas").GetComponent<ScreenCanvas>();
            helpVideoManager = leftController.GetComponentInChildren<PlayVideo>(true);
            velocityGenerator = generatorsParent.GetComponentInChildren<VelocityGenerator>(true);
            convexHullGenerator = generatorsParent.GetComponentInChildren<ConvexHullGenerator>(true);
            filterManager = managersParent.GetComponentInChildren<FilterManager>(true);
            reportManager = managersParent.GetComponentInChildren<ReportManager>(true);
            reportReader = reportManager.GetComponent<ReportReader>();
            floor = GameObject.Find("Floor").GetComponent<Floor>();

            geneKeyboard = GameObject.Find("Keyboard Setup").GetComponent<KeyboardHandler>();
            keyboardSwitch = GameObject.Find("Keyboard Setup").GetComponent<KeyboardSwitch>();
            correlatedGenesList = GameObject.Find("Keyboard Setup/Correlated Genes List").GetComponent<CorrelatedGenesList>();
            previousSearchesList = GameObject.Find("Keyboard Setup/Previous Searches List").GetComponent<PreviousSearchesList>();
            autoCompleteList = GameObject.Find("Keyboard Setup").GetComponent<AutoCompleteList>();
            coloringOptionsList = GameObject.Find("Keyboard Setup/Coloring Options List").GetComponent<ColoringOptionsList>();
            folderKeyboard = GameObject.Find("Tron_Loader/Folder Keyboard").GetComponent<KeyboardHandler>();
            webBrowserKeyboard = GameObject.Find("WebBrowser/Web Keyboard").GetComponent<KeyboardHandler>();
            sessionHistoryList = geneKeyboard.GetComponentInChildren<SessionHistoryList>();

            GameObject filterCreator = GameObject.Find("Filter Creator");
            filterBlockBoard = filterCreator.transform.Find("Filter Block Board").gameObject;
            GameObject keyboardParent = filterBlockBoard.transform.Find("Keyboards").gameObject;
            filterNameKeyboard = keyboardParent.GetComponentInChildren<FilterNameKeyboardHandler>(true);
            filterOperatorKeyboard = keyboardParent.GetComponentInChildren<OperatorKeyboardHandler>(true);
            filterValueKeyboard = keyboardParent.GetComponentInChildren<NumericalKeyboardHandler>(true);
            filterNameKeyboardAutoCompleteList = filterNameKeyboard.gameObject.GetComponentInChildren<AutoCompleteList>(true);

            settingsMenu = GameObject.Find("Settings Menu").GetComponent<SettingsMenu>();
            colorPicker = settingsMenu.transform.Find("Color Picker/Content").GetComponent<ColorPicker>();

            spectatorRig = GameObject.Find("SpectatorRig");
            VRRig = GameObject.Find("[VRTK]3.3");

            UnityEditor.PrefabUtility.ApplyPrefabInstance(gameObject, UnityEditor.InteractionMode.AutomatedAction);
            UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, "Assets/Prefabs/Environment/InputReader.prefab", UnityEditor.InteractionMode.AutomatedAction);
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
                serializedObject.Update();
                serializedObject.ApplyModifiedProperties();
            }
            DrawDefaultInspector();
        }

        public void SaveFields()
        {
            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }

    }
#endif
}
