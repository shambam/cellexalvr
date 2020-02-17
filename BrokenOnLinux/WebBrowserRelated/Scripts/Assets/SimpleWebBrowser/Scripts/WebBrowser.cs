using System;
using UnityEngine;
using System.Collections;
using System.Text;
//using System.Diagnostics;
using MessageLibrary;
using UnityEngine.UI;
using System.Collections.Generic;
using CellexalVR.General;
using CellexalVR.Interaction;

namespace SimpleWebBrowser
{




    public class WebBrowser : MonoBehaviour
    {

        #region General

        [Header("General settings")] public int Width = 1024;

        public int Height = 768;

        public string MemoryFile = "MainSharedMem";

        public bool RandomMemoryFile = true;

        [Range(8000f, 9000f)] public int Port = 8885;

        public bool RandomPort = true;

        public string InitialURL = "http://www.google.com";

        public bool EnableWebRTC = false;

        [Multiline]
        public string JSInitializationCode = "";

        //public List<GameObject> AdditionalBrowserObjects

        #endregion



        [Header("UI settings")]
        [SerializeField]
        public BrowserUI mainUIPanel;

        public bool KeepUIVisible = false;

        public Camera MainCamera;

        [Header("Dialog settings")]
        [SerializeField]
        public Canvas DialogCanvas;
        [SerializeField]
        public Text DialogText;
        [SerializeField]
        public Button OkButton;
        [SerializeField]
        public Button YesButton;
        [SerializeField]
        public Button NoButton;
        [SerializeField]
        public InputField DialogPrompt;

        //dialog states - threading
        private bool _showDialog = false;
        private string _dialogMessage = "";
        private string _dialogPrompt = "";
        private DialogEventType _dialogEventType;
        //query - threading
        private bool _startQuery = false;
        private string _jsQueryString = "";

        //status - threading
        private bool _setUrl = false;
        private string _setUrlString = "";

        private SteamVR_TrackedObject rightController;
        private WebManager webManager;


        public ReferenceManager referenceManager;
        public GameObject browserPrefab;
        public SteamVR_Controller.Device device;
        private int layerMaskMenu;
        #region JS Query events

        public delegate void JSQuery(string query);

        public event JSQuery OnJSQuery;

        #endregion


        private Material _mainMaterial;





        private BrowserEngine _mainEngine;



        private bool _focused = false;


        private int posX = 0;
        private int posY = 0;


        //why Unity does not store the links in package?
        void InitPrefabLinks()
        {
            if (mainUIPanel == null)
                mainUIPanel = gameObject.transform.Find("MainUI").gameObject.GetComponent<BrowserUI>();
            if (DialogCanvas == null)
                DialogCanvas = gameObject.transform.Find("MessageBox").gameObject.GetComponent<Canvas>();
            if (DialogText == null)
                DialogText = DialogCanvas.transform.Find("MessageText").gameObject.GetComponent<Text>();
            if (OkButton == null)
                OkButton = DialogCanvas.transform.Find("OK").gameObject.GetComponent<Button>();
            if (YesButton == null)
                YesButton = DialogCanvas.transform.Find("Yes").gameObject.GetComponent<Button>();
            if (NoButton == null)
                NoButton = DialogCanvas.transform.Find("No").gameObject.GetComponent<Button>();
            if (DialogPrompt == null)
                DialogPrompt = DialogCanvas.transform.Find("Prompt").gameObject.GetComponent<InputField>();

        }

        //void Awake()
        //{
        //    _mainEngine = new BrowserEngine();

        //    if (RandomMemoryFile)
        //    {
        //        Guid memid = Guid.NewGuid();
        //        MemoryFile = memid.ToString();
        //    }
        //    if (RandomPort)
        //    {
        //        System.Random r = new System.Random();
        //        Port = 8000 + r.Next(1000);
        //    }



        //    _mainEngine.InitPlugin(Width, Height, MemoryFile, Port, InitialURL, EnableWebRTC);
        //    //run initialization
        //    if (JSInitializationCode.Trim() != "")
        //        _mainEngine.RunJSOnce(JSInitializationCode);
        //}

        // Use this for initialization
        void Start()
        {
            webManager = GetComponentInParent<WebManager>();
            _mainEngine = new BrowserEngine();

            if (RandomMemoryFile)
            {
                Guid memid = Guid.NewGuid();
                MemoryFile = memid.ToString();
            }
            if (RandomPort)
            {
                System.Random r = new System.Random();
                Port = 8000 + r.Next(1000);
            }



            _mainEngine.InitPlugin(Width, Height, MemoryFile, Port, InitialURL, EnableWebRTC);
            //run initialization
            if (JSInitializationCode.Trim() != "")
                _mainEngine.RunJSOnce(JSInitializationCode);

            InitPrefabLinks();
            mainUIPanel.InitPrefabLinks();
            if (referenceManager == null)
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
            rightController = referenceManager.rightController;
            device = SteamVR_Controller.Input((int)rightController.index);
            layerMaskMenu = 1 << LayerMask.NameToLayer("MenuLayer");
            if (MainCamera == null)
            {
                MainCamera = Camera.main;
                if (MainCamera == null)
                    Debug.LogError("Error: can't find main camera");
            }

            _mainMaterial = GetComponent<MeshRenderer>().material;
            _mainMaterial.SetTexture("_MainTex", _mainEngine.BrowserTexture);
            _mainMaterial.SetTextureScale("_MainTex", new Vector2(-1, 1));


            mainUIPanel.MainCanvas.worldCamera = MainCamera;





            // _mainInput = MainUrlInput.GetComponent<Input>();
            mainUIPanel.KeepUIVisible = KeepUIVisible;
            if (!KeepUIVisible)
                mainUIPanel.Hide();

            //attach dialogs and queries
            _mainEngine.OnJavaScriptDialog += _mainEngine_OnJavaScriptDialog;
            _mainEngine.OnJavaScriptQuery += _mainEngine_OnJavaScriptQuery;
            _mainEngine.OnPageLoaded += _mainEngine_OnPageLoaded;

            DialogCanvas.worldCamera = MainCamera;
            DialogCanvas.gameObject.SetActive(false);

        }

        private void _mainEngine_OnPageLoaded(string url)
        {
            _setUrl = true;
            _setUrlString = url;

        }

        //make it thread-safe
        private void _mainEngine_OnJavaScriptQuery(string message)
        {
            _jsQueryString = message;
            _startQuery = true;
        }

        public void RespondToJSQuery(string response)
        {
            _mainEngine.SendQueryResponse(response);
        }

        private void _mainEngine_OnJavaScriptDialog(string message, string prompt, DialogEventType type)
        {
            _showDialog = true;
            _dialogEventType = type;
            _dialogMessage = message;
            _dialogPrompt = prompt;

        }

        private void ShowDialog()
        {

            switch (_dialogEventType)
            {
                case DialogEventType.Alert:
                    {
                        DialogCanvas.gameObject.SetActive(true);
                        OkButton.gameObject.SetActive(true);
                        YesButton.gameObject.SetActive(false);
                        NoButton.gameObject.SetActive(false);
                        DialogPrompt.text = "";
                        DialogPrompt.gameObject.SetActive(false);
                        DialogText.text = _dialogMessage;
                        break;
                    }
                case DialogEventType.Confirm:
                    {
                        DialogCanvas.gameObject.SetActive(true);
                        OkButton.gameObject.SetActive(false);
                        YesButton.gameObject.SetActive(true);
                        NoButton.gameObject.SetActive(true);
                        DialogPrompt.text = "";
                        DialogPrompt.gameObject.SetActive(false);
                        DialogText.text = _dialogMessage;
                        break;
                    }
                case DialogEventType.Prompt:
                    {
                        DialogCanvas.gameObject.SetActive(true);
                        OkButton.gameObject.SetActive(false);
                        YesButton.gameObject.SetActive(true);
                        NoButton.gameObject.SetActive(true);
                        DialogPrompt.text = _dialogPrompt;
                        DialogPrompt.gameObject.SetActive(true);
                        DialogText.text = _dialogMessage;
                        break;
                    }
            }
            _showDialog = false;
        }

        #region UI

        public void OnNavigate()
        {
            // MainUrlInput.isFocused
            _mainEngine.SendNavigateEvent(mainUIPanel.UrlField.text, false, false);

        }

        public void OnNavigate(string url)
        {
            // MainUrlInput.isFocused
            try
            {
                _mainEngine.SendNavigateEvent(url, false, false);
            }
            catch (Exception e)
            {
                _mainEngine.SendNavigateEvent(url, false, false);
            }

        }

        public void RunJavaScript(string js)
        {
            _mainEngine.SendExecuteJSEvent(js);
        }

        public void GoBackForward(bool forward)
        {
            if (forward)
                _mainEngine.SendNavigateEvent("", false, true);
            else
                _mainEngine.SendNavigateEvent("", true, false);
        }

        #endregion

        #region Dialogs

        public void DialogResult(bool result)
        {
            DialogCanvas.gameObject.SetActive(false);
            _mainEngine.SendDialogResponse(result, DialogPrompt.text);

        }

        #endregion


        #region Events (3D)

        private void OnTriggerEnter(Collider other)
        {
            _focused = true;
            mainUIPanel.Show();
        }

        private void OnTriggerExit(Collider other)
        {
            _focused = false;
            mainUIPanel.Hide();
        }

        private void OnTriggerStay(Collider other)
        {
            if (_mainEngine.Initialized)
            {
                Vector2 pixelUV = GetScreenCoords();
                ProcessScrollInput((int)pixelUV.x, (int)pixelUV.y);
            }
        }

        void OnTriggerPressed()
        {
            if (_mainEngine.Initialized)
            {
                Vector2 pixelUV = GetScreenCoords();

                if (pixelUV.x > 0)
                {
                    SendMouseButtonEvent((int)pixelUV.x, (int)pixelUV.y, MouseButton.Left, MouseEventType.ButtonDown);

                }
            }

        }

        void OnTriggerUp()
        {
            if (_mainEngine.Initialized)
            {
                Vector2 pixelUV = GetScreenCoords();

                if (pixelUV.x > 0)
                {
                    SendMouseButtonEvent((int)pixelUV.x, (int)pixelUV.y, MouseButton.Left, MouseEventType.ButtonUp);
                }
            }
        }

        //void OnMouseOver()
        //{
        //    if (_mainEngine.Initialized)
        //    {
        //        Vector2 pixelUV = GetScreenCoords();

        //        if (pixelUV.x > 0)
        //        {
        //            int px = (int) pixelUV.x;
        //            int py = (int) pixelUV.y;

        //            ProcessScrollInput(px, py);

        //            if (posX != px || posY != py)
        //            {
        //                MouseMessage msg = new MouseMessage
        //                {
        //                    Type = MouseEventType.Move,
        //                    X = px,
        //                    Y = py,
        //                    GenericType = MessageLibrary.BrowserEventType.Mouse,
        //                    // Delta = e.Delta,
        //                    Button = MouseButton.None
        //                };

        //                if (Input.GetMouseButton(0))
        //                    msg.Button = MouseButton.Left;
        //                if (Input.GetMouseButton(1))
        //                    msg.Button = MouseButton.Right;
        //                if (Input.GetMouseButton(1))
        //                    msg.Button = MouseButton.Middle;

        //                posX = px;
        //                posY = py;
        //                _mainEngine.SendMouseEvent(msg);
        //            }

        //            //check other buttons...
        //            if (Input.GetMouseButtonDown(1))
        //                SendMouseButtonEvent(px, py, MouseButton.Right, MouseEventType.ButtonDown);
        //            if (Input.GetMouseButtonUp(1))
        //                SendMouseButtonEvent(px, py, MouseButton.Right, MouseEventType.ButtonUp);
        //            if (Input.GetMouseButtonDown(2))
        //                SendMouseButtonEvent(px, py, MouseButton.Middle, MouseEventType.ButtonDown);
        //            if (Input.GetMouseButtonUp(2))
        //                SendMouseButtonEvent(px, py, MouseButton.Middle, MouseEventType.ButtonUp);
        //        }
        //    }

        //    // Debug.Log(pixelUV);
        //}

        #endregion

        #region Helpers

        private Vector2 GetScreenCoords()
        {
            RaycastHit hit;
            if (!Physics.Raycast(referenceManager.rightLaser.transform.position, referenceManager.rightLaser.transform.forward, out hit, 10f))
                return new Vector2(-1f, -1f);

            //if (!hit.collider)
            //{
            //    //MainCamera.ScreenPointToRay(Input.mousePosition), out hit))
            //    print("no hit");
            //}
            Texture tex = _mainMaterial.mainTexture;


            Vector2 pixelUV = hit.textureCoord;
            pixelUV.x = (1 - pixelUV.x) * tex.width;
            pixelUV.y *= tex.height;
            return pixelUV;
        }

        private void SendMouseButtonEvent(int x, int y, MouseButton btn, MouseEventType type)
        {
            MouseMessage msg = new MouseMessage
            {
                Type = type,
                X = x,
                Y = y,
                GenericType = MessageLibrary.BrowserEventType.Mouse,
                // Delta = e.Delta,
                Button = btn
            };
            _mainEngine.SendMouseEvent(msg);
        }

        private void ProcessScrollInput(int px, int py)
        {
            Vector2 touchpad = (device.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0));
            //float scroll = Input.GetAxis("Mouse ScrollWheel");
            float scroll = touchpad.y * 0.01f;
            scroll = scroll * _mainEngine.BrowserTexture.height;

            int scInt = (int)scroll;

            if (scInt != 0)
            {
                MouseMessage msg = new MouseMessage
                {
                    Type = MouseEventType.Wheel,
                    X = px,
                    Y = py,
                    GenericType = MessageLibrary.BrowserEventType.Mouse,
                    Delta = scInt,
                    Button = MouseButton.None
                };

                if (Input.GetMouseButton(0))
                    msg.Button = MouseButton.Left;
                if (Input.GetMouseButton(1))
                    msg.Button = MouseButton.Right;
                if (Input.GetMouseButton(1))
                    msg.Button = MouseButton.Middle;

                _mainEngine.SendMouseEvent(msg);
            }
        }

        #endregion

        // Update is called once per frame
        void Update()
        {
            device = SteamVR_Controller.Input((int)rightController.index);
            //print("update in web browser " + device.Get());
            if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
            {
                OnTriggerPressed();
            }

            if (device.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
            {
                OnTriggerUp();
            }
            // If sockets get disconnected or similar error we need to recreate object.
            if (webManager.isVisible)
            {
                try
                {
                    _mainEngine.UpdateTexture();
                }
                catch (InvalidOperationException e)
                {
                    CellexalLog.Log("Browser could not update. Destroy object and re-initialize...");
                    var newBrowser = GameObject.Instantiate(browserPrefab, Vector3.zero, Quaternion.identity);
                    newBrowser.transform.parent = gameObject.transform.parent;
                    newBrowser.transform.localPosition = transform.localPosition;
                    newBrowser.transform.localScale = transform.localScale;
                    newBrowser.transform.localRotation = transform.localRotation;
                    //gameObject.AddComponent<WebBrowser>();
                    Destroy(gameObject);

                }
            }



            //Dialog
            if (_showDialog)
            {
                ShowDialog();
            }

            //Query
            if (_startQuery)
            {
                _startQuery = false;
                if (OnJSQuery != null)
                    OnJSQuery(_jsQueryString);

            }

            //Status
            if (_setUrl)
            {
                _setUrl = false;
                mainUIPanel.UrlField.text = _setUrlString;

            }



            if (_focused && !mainUIPanel.UrlField.isFocused) //keys
            {
                foreach (char c in Input.inputString)
                {

                    _mainEngine.SendCharEvent((int)c, KeyboardEventType.CharKey);
                }
                ProcessKeyEvents();
            }



        }

        #region Keys

        private void ProcessKeyEvents()
        {
            foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
            {
                CheckKey(k);
            }

        }

        private void CheckKey(KeyCode code)
        {
            if (Input.GetKeyDown(code))
                _mainEngine.SendCharEvent((int)code, KeyboardEventType.Down);
            if (Input.GetKeyUp(KeyCode.Backspace))
                _mainEngine.SendCharEvent((int)code, KeyboardEventType.Up);
        }

        #endregion

        void OnDisable()
        {
            _mainEngine.Shutdown();
        }


        public event BrowserEngine.PageLoaded OnPageLoaded;
    }
}