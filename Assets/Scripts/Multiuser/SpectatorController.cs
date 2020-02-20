using CellexalVR.General;
using UnityEngine;

namespace CellexalVR.Multiuser
{
    /// <summary>
    /// Handles movement of the user when in spectator mode. Active e.g. if the user does not have a vr headset and wishes to use the keyboard instead.
    /// </summary>
    public class SpectatorController : MonoBehaviour
    {
        public float speed = 1f;
        public GameObject CtrlsCanvas;
        public GameObject TextCanvas;
        private GameObject settingsMenu;
        private GameObject console;

        public ReferenceManager referenceManager;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        void Start()
        {
            //foreach (Canvas c in settingsMenu.GetComponentsInChildren<Canvas>())
            //{
            //    c.renderMode = RenderMode.ScreenSpaceCamera;
            //    c.worldCamera = GetComponentInChildren<Camera>();
            //}
            //foreach (Canvas c in console.GetComponentsInChildren<Canvas>())
            //{
            //    c.renderMode = RenderMode.ScreenSpaceCamera;
            //    c.worldCamera = GetComponentInChildren<Camera>();
            //}

            settingsMenu = referenceManager.settingsMenu.gameObject;
            console = referenceManager.configManager.gameObject;

            if (!CrossSceneInformation.Spectator)
                gameObject.SetActive(false);
            //Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                CtrlsCanvas.SetActive(true);
                TextCanvas.SetActive(false);
            }

            if (Input.GetKeyUp(KeyCode.Tab))
            {
                CtrlsCanvas.SetActive(false);
                TextCanvas.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                speed /= 4;
            }

            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                speed = 1;
            }
            if (Input.GetKey(KeyCode.W))
            {
                transform.Translate(Vector3.forward * Time.deltaTime * speed);
            }

            if (Input.GetKey(KeyCode.S))
            {
                transform.Translate(Vector3.back * Time.deltaTime * speed);
            }

            if (Input.GetKey(KeyCode.A))
            {
                transform.Translate(Vector3.left * Time.deltaTime * speed);
            }

            if (Input.GetKey(KeyCode.D))
            {
                transform.Translate(Vector3.right * Time.deltaTime * speed);
            }

            if (Input.GetKey(KeyCode.E))
            {
                transform.Translate(Vector3.up * Time.deltaTime * speed);
            }

            if (Input.GetKey(KeyCode.Q))
            {
                transform.Translate(Vector3.down * Time.deltaTime * speed);
            }
        }
    }

}