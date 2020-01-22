using CellexalVR.AnalysisLogic;
using CellexalVR.General;
using CellexalVR.SceneObjects;
using System;
using System.Collections;
using System.IO;
using System.Threading;
using UnityEngine;

namespace CellexalVR.Tools
{

    /// <summary>
    /// This class takes screenshots of what the user sees in the virtual environment.
    /// </summary>
    public class CaptureScreenshot : MonoBehaviour
    {
        public SteamVR_TrackedObject rightController;
        public ScreenCanvas screenCanvas;
        public GameObject panel;

        private SteamVR_Controller.Device device;
        private float fadeTime = 0.7f;
        private float elapsedTime = 0.0f;
        private float colorAlpha;
        private int screenshotCounter;
        public ReferenceManager referenceManager;
        //private string directory = Directory.GetCurrentDirectory() + Path.PathSeparator + "Output\";

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        void Start()
        {
            device = SteamVR_Controller.Input((int)rightController.index);

            screenCanvas = referenceManager.screenCanvas;
        }

        void Update()
        {
            device = SteamVR_Controller.Input((int)rightController.index);
            //Vector2 touchpad = (device.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0));
            if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
            {
                StartCoroutine(Capture());
            }

        }

        /// <summary>
        /// Method to capture screenshot. Disables the UI canvas so it does not show up on the image.
        /// </summary>
        /// <returns></returns>
        IEnumerator Capture()
        {
            string screenshotImageDirectory = CellexalUser.UserSpecificFolder;
            if (!Directory.Exists(screenshotImageDirectory))
            {
                Directory.CreateDirectory(screenshotImageDirectory);
                CellexalLog.Log("Created directory " + screenshotImageDirectory);
            }

            screenshotImageDirectory += Path.PathSeparator + "Screenshots";
            if (!Directory.Exists(screenshotImageDirectory))
            {
                Directory.CreateDirectory(screenshotImageDirectory);
                CellexalLog.Log("Created directory " + screenshotImageDirectory);
            }

            string screenshotImageFilePath = screenshotImageDirectory + Path.PathSeparator + name + "_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png";

            panel.SetActive(false);
            ScreenCapture.CaptureScreenshot(screenshotImageFilePath);
            CellexalLog.Log("Screenshot taken!");

            screenCanvas.FadeAnimation(1f);
            yield return new WaitForSeconds(0.5f);
#if try_this_later
            string logScreenShotRScriptPath = (Application.streamingAssetsPath + @"\log_screenshot.R");
            Thread t = new Thread(() => RScriptRunner.RunRScript(logScreenShotRScriptPath, screenshotImageFilePath));
            t.Start();
#endif
            panel.SetActive(true);
            screenshotCounter++;
        }

        /// <summary>
        /// Calls R logging function to save screenshot for session report.
        /// </summary

        IEnumerator LogScreenshot(string screenshotImageFilePath)
        {
            string args = screenshotImageFilePath;
            string rScriptFilePath = Application.streamingAssetsPath + @"\R\screenshot_report.R";
            bool rServerReady = File.Exists(CellexalUser.UserSpecificFolder + Path.PathSeparator + "mainServer.pid") &&
                    !File.Exists(CellexalUser.UserSpecificFolder + Path.PathSeparator + "mainServer.input.R") &&
                    !File.Exists(CellexalUser.UserSpecificFolder + Path.PathSeparator + "mainServer.input.lock");
            while (!rServerReady || !RScriptRunner.serverIdle)
            {
                rServerReady = File.Exists(CellexalUser.UserSpecificFolder + Path.PathSeparator + "mainServer.pid") &&
                                !File.Exists(CellexalUser.UserSpecificFolder + Path.PathSeparator + "mainServer.input.R") &&
                                !File.Exists(CellexalUser.UserSpecificFolder + Path.PathSeparator + "mainServer.input.lock");
                yield return null;
            }
            //t = new Thread(() => RScriptRunner.RunScript(script));
            Thread t = new Thread(() => RScriptRunner.RunRScript(rScriptFilePath, args));
            t.Start();
            CellexalLog.Log("Running R function " + rScriptFilePath + " with the arguments: " + args);
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            while (t.IsAlive || File.Exists(CellexalUser.UserSpecificFolder + Path.PathSeparator + "mainServer.input.R"))
            {
                yield return null;
            }

            stopwatch.Stop();
            CellexalLog.Log("R log script finished in " + stopwatch.Elapsed.ToString());
        }

    }
}
