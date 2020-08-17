using CellexalVR.Interaction;
using CellexalVR.Menu;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR.InteractionSystem;
using VRTK;

namespace CellexalVR.General
{
#if UNITY_EDITOR
    [ExecuteInEditMode]
    public class SceneBuilder : MonoBehaviour
    {
        public GameObject InputReader;
        private GameObject _InputReader;
        public GameObject CameraRig;
        private GameObject _CameraRig;
        public GameObject VRTK;
        private GameObject _VRTK;
        public GameObject Managers;
        private GameObject _Managers;
        public GameObject Generators;
        private GameObject _Generators;
        public GameObject SQLiter;
        private GameObject _SQLiter;
        public GameObject EventSystem;
        private GameObject _EventSystem;


        public GameObject Floor;
        private GameObject _Floor;
        //public GameObject Calculators;
        //private GameObject _Calculators;
        public GameObject LightForTesting;
        private GameObject _LightForTesting;
        public GameObject MenuHolder;
        private GameObject _MenuHolder;


        public GameObject Loader;
        private GameObject _Loader;
        public GameObject Keyboard;
        private GameObject _Keyboard;
        public GameObject WebBrowser;
        private GameObject _WebBrowser;
        public GameObject FilterCreator;
        private GameObject _FilterCreator;


        public GameObject SettingsMenu;
        private GameObject _SettingsMenu;
        public GameObject Console;
        private GameObject _Console;
        public GameObject FPSCanvas;
        private GameObject _FPSCanvas;
        public GameObject WaitingCanvas;
        private GameObject _WaitingCanvas;
        public GameObject SpectatorRig;
        private GameObject _SpectatorRig;
        public GameObject H5Reader;
        private GameObject _H5Reader;

        private List<GameObject> instances;
        private IEnumerator buildSceneEnumerator;
        private bool buildingScene = false;

        private SceneBuilder()
        {
            EditorApplication.update += Update;
            instances = new List<GameObject>();
        }

        private void Update()
        {
            if (!buildingScene)
            {
                EditorUtility.ClearProgressBar();
            }
            else if (gameObject.scene.IsValid() && buildSceneEnumerator != null)
            {
                if (!((WaitForSecondsRealtime)buildSceneEnumerator.Current).keepWaiting)
                    buildSceneEnumerator.MoveNext();
            }
        }

        /// <summary>
        /// Instantiates all gameobjects, runs all OnValidate() and builds all keyboards.
        /// </summary>
        public void BuildScene()
        {
            //if (Application.isPlaying)
            //{
            //    return;
            //}
            // instantiate missing gameobjects
            buildSceneEnumerator = BuildSceneCoroutine();
            buildSceneEnumerator.MoveNext();
        }

        private IEnumerator BuildSceneCoroutine()
        {
            buildingScene = true;
            EditorUtility.DisplayProgressBar("Building scene", "Instantiating objects", 0f);
            InstantiateSceneAsset(ref _InputReader, InputReader);
            yield return new WaitForSecondsRealtime(0.25f);
            EditorUtility.DisplayProgressBar("Building scene", "Instantiating objects", 0.1f);
            InstantiateSceneAsset(ref _CameraRig, CameraRig);
            _CameraRig.GetComponentInChildren<Player>().hands = new Hand[0];
            InstantiateSceneAsset(ref _VRTK, VRTK);
            InstantiateSceneAsset(ref _Managers, Managers);
            InstantiateSceneAsset(ref _Generators, Generators);
            InstantiateSceneAsset(ref _SQLiter, SQLiter);
            InstantiateSceneAsset(ref _EventSystem, EventSystem);
            InstantiateSceneAsset(ref _Floor, Floor);
            //InstantiateSceneAsset(ref _Calculators, Calculators);
            InstantiateSceneAsset(ref _LightForTesting, LightForTesting);
            InstantiateSceneAsset(ref _MenuHolder, MenuHolder);
            InstantiateSceneAsset(ref _Loader, Loader);
            InstantiateSceneAsset(ref _Keyboard, Keyboard);
            InstantiateSceneAsset(ref _WebBrowser, WebBrowser);
            InstantiateSceneAsset(ref _FilterCreator, FilterCreator);
            InstantiateSceneAsset(ref _SettingsMenu, SettingsMenu);
            InstantiateSceneAsset(ref _Console, Console);
            InstantiateSceneAsset(ref _FPSCanvas, FPSCanvas);
            InstantiateSceneAsset(ref _WaitingCanvas, WaitingCanvas);
            InstantiateSceneAsset(ref _SpectatorRig, SpectatorRig);
            InstantiateSceneAsset(ref _H5Reader, H5Reader);
            yield return new WaitForSecondsRealtime(0.25f);
            EditorUtility.DisplayProgressBar("Building scene", "Running OnValidate", 0.6f);

            // run onvalidate on everything
            foreach (var instance in instances)
            {
                var allChildren = instance.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var child in allChildren)
                {
                    if (child == null) continue;
                    // MonoBehaviour.SendMessage() does not work in inactive gameobjects, use reflection instead
                    if (child.GetType().GetMethod("OnValidate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance) != null)
                    {
                        child.Invoke("OnValidate", 0f);
                    }
                }
            }

            yield return new WaitForSecondsRealtime(0.25f);
            EditorUtility.DisplayProgressBar("Building scene", "Setting references", 0.7f);
            // set up missing references
            VRTK_SDKManager sdkmanager = _CameraRig.GetComponent<VRTK_SDKManager>();
            _CameraRig.GetComponentInChildren<Player>().hands = _CameraRig.GetComponentsInChildren<Hand>();

            // set up radial menu buttons
            //VRTK_RadialMenu leftRadialMenu = referenceManager.leftControllerScriptAlias.GetComponentInChildren<VRTK_RadialMenu>();
            //Undo.RecordObject(leftRadialMenu, "Radial menu events");
            //leftRadialMenu.buttons[1].OnClick = new UnityEngine.Events.UnityEvent();
            //leftRadialMenu.buttons[1].OnClick.AddListener(delegate { referenceManager.mainMenu.GetComponent<MenuRotator>().RotateLeft(1); });
            //leftRadialMenu.buttons[3].OnClick = new UnityEngine.Events.UnityEvent();
            //leftRadialMenu.buttons[3].OnClick.AddListener(delegate { referenceManager.mainMenu.GetComponent<MenuRotator>().RotateRight(1); });

            //VRTK_RadialMenu rightRadialMenu = referenceManager.rightControllerScriptAlias.GetComponentInChildren<VRTK_RadialMenu>();
            //Undo.RecordObject(rightRadialMenu, "Radial menu events");
            //ControllerModelSwitcher cms = referenceManager.leftController.GetComponent<ControllerModelSwitcher>();
            //SelectionToolCollider selectionToolCollider = referenceManager.selectionToolCollider;
            //rightRadialMenu.buttons[0].OnClick = new UnityEngine.Events.UnityEvent();
            //rightRadialMenu.buttons[0].OnClick.AddListener(delegate { cms.SwitchSelectionToolMesh(true); });
            //rightRadialMenu.buttons[1].OnClick = new UnityEngine.Events.UnityEvent();
            //rightRadialMenu.buttons[1].OnClick.AddListener(delegate { selectionToolCollider.ChangeColor(false); });
            //rightRadialMenu.buttons[2].OnClick = new UnityEngine.Events.UnityEvent();
            //rightRadialMenu.buttons[2].OnClick.AddListener(delegate { cms.SwitchSelectionToolMesh(false); });
            //rightRadialMenu.buttons[3].OnClick = new UnityEngine.Events.UnityEvent();
            //rightRadialMenu.buttons[3].OnClick.AddListener(delegate { selectionToolCollider.ChangeColor(true); });

            Undo.RecordObject(sdkmanager, "Set controller script alias");
            ReferenceManager referenceManager = _InputReader.GetComponent<ReferenceManager>();
            referenceManager.AttemptSetReferences();
            sdkmanager.scriptAliasLeftController = referenceManager.leftControllerScriptAlias;
            sdkmanager.scriptAliasRightController = referenceManager.rightControllerScriptAlias;

            //referenceManager.leftControllerScriptAlias.GetComponentInChildren<EventSetter>().BuildLeftRadialMenu();
            //referenceManager.rightControllerScriptAlias.GetComponentInChildren<EventSetter>().BuildRightRadialMenu();

            _Keyboard.GetComponent<GeneKeyboardHandler>().BuildKeyboard();
            _FilterCreator.GetComponentInChildren<FilterNameKeyboardHandler>(true).BuildKeyboard();
            // the BuildKeyboard() calls replaces the instantianted prefabs which breaks the fields... thus we have to GameObject.Find() them again
            _FilterCreator = GameObject.Find("Filter Creator");
            _FilterCreator.GetComponentInChildren<OperatorKeyboardHandler>(true).BuildKeyboard();
            _FilterCreator = GameObject.Find("Filter Creator");
            _FilterCreator.GetComponentInChildren<NumericalKeyboardHandler>(true).BuildKeyboard();
            _Loader.GetComponentInChildren<FolderKeyboardHandler>(true).BuildKeyboard();
            _WebBrowser.GetComponentInChildren<WebKeyboardHandler>(true).BuildKeyboard();

            AutoPopulateGameObjects();
            yield return new WaitForSecondsRealtime(0.1f);
            referenceManager = _InputReader.GetComponent<ReferenceManager>();
            referenceManager.AttemptSetReferences();
            EditorUtility.ClearProgressBar();
            buildingScene = false;
        }

        private void InstantiateSceneAsset(ref GameObject instance, GameObject prefab)
        {
            if (instance != null)
            {
                GameObject copy = instance;
                UnityEditor.EditorApplication.delayCall += () => { DestroyImmediate(copy.gameObject); };
                instances.Remove(instance);
                instance = null;
            }
            if (instance == null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab as GameObject) as GameObject;
                instance.name = prefab.name;
                instances.Add(instance);

            }
        }

        /// <summary>
        /// Removes all other gameobjects in the scene.
        /// </summary>
        public void RemoveInstances()
        {
            foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (gameObject != this.gameObject)
                {
                    UnityEditor.EditorApplication.delayCall += () => { DestroyImmediate(gameObject); };
                }
            }
            instances.Clear();
        }

        /// <summary>
        /// Attempts to set all fields in the inspector.
        /// </summary>
        public void AutoPopulateGameObjects()
        {
            InputReader = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/InputReader.prefab");
            CameraRig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/[VRTK]3.3.prefab");
            VRTK = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/[VRTK_Scripts].prefab");
            Managers = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Managers.prefab");
            Generators = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Generators.prefab");
            SQLiter = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/SQLiter.prefab");
            EventSystem = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/EventSystem.prefab");
            Floor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Floor.prefab");
            //Calculators = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Calculator cluster.prefab");
            LightForTesting = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Light For Testing.prefab");
            MenuHolder = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/MenuHolder.prefab");
            Loader = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/Tron_Loader.prefab");
            Keyboard = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Keyboards/Keyboard Setup.prefab");
            WebBrowser = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/WebBrowser.prefab");
            FilterCreator = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Filters/Filter Creator.prefab");
            SettingsMenu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DesktopUI/Settings Menu.prefab");
            Console = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DesktopUI/Console.prefab");
            FPSCanvas = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DesktopUI/FPS canvas.prefab");
            WaitingCanvas = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DesktopUI/ScreenCanvas.prefab");
            SpectatorRig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/SpectatorRig.prefab");
            H5Reader = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/H5Reader/H5ReaderAnnotatorManager.prefab");
        }

        [Obsolete]
        public void SaveAllPrefabs()
        {
            float progress = 0.0f;
            float progessPerInstance = 1f / instances.Count;
            foreach (var instance in instances)
            {
                EditorUtility.DisplayProgressBar("Saving prefabs", instance.name, progress);
                progress += progessPerInstance;
                PrefabUtility.ReplacePrefab(instance, PrefabUtility.GetCorrespondingObjectFromSource(instance), ReplacePrefabOptions.ConnectToPrefab);
            }
            EditorUtility.ClearProgressBar();
        }

        private void Start()
        {
            gameObject.SetActive(false);
        }

    }
    [UnityEditor.CustomEditor(typeof(SceneBuilder))]
    public class SceneBuilderEditor : UnityEditor.Editor
    {

        private SceneBuilder instance;

        void OnEnable()
        {
            instance = (SceneBuilder)target;
        }


        public override void OnInspectorGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Build scene"))
            {
                instance.BuildScene();
            }
            if (GUILayout.Button("Force build scene"))
            {
                instance.RemoveInstances();
                instance.BuildScene();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove objects"))
            {
                instance.RemoveInstances();
            }
            if (GUILayout.Button("Auto-populate gameobjects"))
            {
                instance.AutoPopulateGameObjects();
            }
            GUILayout.EndHorizontal();
            //if (GUILayout.Button("Save all prefabs"))
            //{
            //    instance.SaveAllPrefabs();
            //}

            DrawDefaultInspector();

        }

    }
#endif
}
