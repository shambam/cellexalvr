using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Valve.VR;
using CellexalVR.DesktopUI;
using CellexalVR.Extensions;

namespace CellexalVR.General
{
    /// <summary>
    /// This static class represents a log that can be written to.
    /// </summary>
    public static class CellexalLog
    {
        public static ConsoleManager consoleManager;

        private static string logDirectory;
        private static string logFilePath = "";
        private static int maxNrOfLogFiles = 5;
        public static string LogFilePath
        {
            get { return logFilePath; }
            private set { logFilePath = value; }
        }
        private static List<string> logThisLater = new List<string>();

        public static void InitNewLog()
        {
            // File names can't have colons so we only use hyphens
            var now = DateTime.Now;
            var time = now.ToString("yyyy-MM-dd-HH-mm-ss");

            logDirectory = (Directory.GetCurrentDirectory() + "/Output/" + CellexalUser.Username + "/Logs/").FixFilePath();
            if (!Directory.Exists(logDirectory))
            {
                logThisLater.Add("\tCreated directory " + logDirectory);
                Directory.CreateDirectory(logDirectory);
            }

            // To clean up some older log files.
            string[] files = Directory.GetFiles(logDirectory);
            int nrOfFilesToDelete = files.Length - maxNrOfLogFiles;
            for (int i = 0; i < nrOfFilesToDelete; i++)
            {
                File.Delete(files[i]);
            }

            LogFilePath = logDirectory + "\\cellexal-log-" + time + ".txt";
            // this will most likely always happen
            if (!File.Exists(LogFilePath))
            {
                logThisLater.Add("\tCreated file " + LogFilePath);
                File.Create(LogFilePath).Dispose();
            }

            string nicerTime = now.ToString("yyyy-MM-dd HH:mm:ss");
            Log("Welcome to CellexalVR " + Application.version,
                "Running on Unity " + Application.unityVersion,
                "BuildGUID: " + Application.buildGUID,
                "Logfile created at " + nicerTime);

            Log("\nSome system information:",
                "\tOS: " + SystemInfo.operatingSystem,
                "\tCPU: " + SystemInfo.processorType,
                "\tProcessor count: " + SystemInfo.processorCount,
                "\tGPU: " + SystemInfo.graphicsDeviceName,
                "\tRAM size: " + SystemInfo.systemMemorySize);

            if (logThisLater.Count > 0)
            {
                Log("The following was generated before the log file existed:");
                LogBacklog();
                Log("\tEnd of what was generated before the log file existed.");
                logThisLater.Clear();
            }

            CellexalEvents.LogInitialized.Invoke();
        }

        /// <summary>
        /// Writes everything that has been accumulated to the log file.
        /// </summary>
        public static void LogBacklog()
        {
            if (logFilePath == "")
            {
                return;
            }
            using (StreamWriter logWriter = new StreamWriter(new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.None)))
            {
                foreach (string s in logThisLater)
                {
                    logWriter.WriteLine(s);
                }
                logThisLater.Clear();
                logWriter.Flush();
            }
        }

        /// <summary>
        /// Writes to the log. This method will append a linebreak at the end of the written line.
        /// </summary>
        /// <param name="message"> The string that should be written to the log. </param>
        public static void Log(string message)
        {
            if (consoleManager)
            {
                consoleManager.AppendOutput(message);
            }

            logThisLater.Add(message);
        }

        /// <summary>
        /// Logs multiple messages. This method will append a linebreak between each message.
        /// </summary>
        /// <param name="message"> The messages that should be written to the log. </param>
        public static void Log(params string[] message)
        {
            foreach (string s in message)
            {
                Log(s);
            }
        }

        /// <summary>
        /// Replaces all forward and backward slashes with whatever is the correct directory seperator character on this system.
        /// </summary>
        /// <param name="path"> A file path with a weird mix of forward and backward slashes. </param>
        /// <returns> A file path without a weird mix of forward and backward slashes. </returns>
        public static string FixFilePath(string path)
        {
            char directorySeparatorChar = Path.DirectorySeparatorChar;
            path = path.Replace('/', directorySeparatorChar);
            path = path.Replace('\\', directorySeparatorChar);
            return path;
        }

        /// <summary>
        /// Closes the old log and opens a new log.
        /// </summary>
        public static void UsernameChanged()
        {
            if (LogFilePath != "")
            {
                Log("Changing user to " + CellexalUser.Username,
                    "Goodbye.");
            }
            InitNewLog();
        }
    }

    /// <summary>
    /// This class subscribes to some events that is of intrest to the log.
    /// </summary>
    public class LogManager : MonoBehaviour
    {
        public ReferenceManager referenceManager;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Awake()
        {
            CellexalLog.InitNewLog();
            CellexalEvents.UsernameChanged.AddListener(CellexalLog.UsernameChanged);
            //CellexalEvents.GraphsLoaded.AddListener(CellexalLog.InitNewLog);

            string outputDirectory = Directory.GetCurrentDirectory() + "\\Output";

            CellexalLog.consoleManager = referenceManager.consoleManager;
            if (!Directory.Exists(outputDirectory))
            {
                CellexalLog.Log("Created directory " + CellexalLog.FixFilePath(outputDirectory));
                Directory.CreateDirectory(outputDirectory);
            }
        }

        #region Events

        private void OnEnable()
        {
            SteamVR_Events.RenderModelLoaded.Listen(OnRenderModelLoaded);
            SteamVR_Events.DeviceConnected.Listen(OnDeviceConnected);
            InvokeRepeating("LogBacklog", 10f, 10f);
        }

        private void LogBacklog()
        {
            CellexalLog.LogBacklog();
        }

        private void OnDisable()
        {
            SteamVR_Events.RenderModelLoaded.Remove(OnRenderModelLoaded);
            SteamVR_Events.DeviceConnected.Remove(OnDeviceConnected);
            CellexalLog.LogBacklog();
            CancelInvoke();
        }

        private void OnRenderModelLoaded(SteamVR_RenderModel model, bool success)
        {
            if (success)
                CellexalLog.Log("Render model successfully loaded");
            else
                CellexalLog.Log("ERROR: Render model not successfully loaded");
        }

        private void OnDeviceConnected(int index, bool connected)
        {
            var error = ETrackedPropertyError.TrackedProp_Success;
            var result = new System.Text.StringBuilder(64);
            OpenVR.System.GetStringTrackedDeviceProperty((uint)index, ETrackedDeviceProperty.Prop_RenderModelName_String, result, 64, ref error);

            if (connected)
                CellexalLog.Log("Device " + result.ToString() + " with index " + index + " connected");
            else
                CellexalLog.Log("Device " + result.ToString() + " with index " + index + " disconnected");
        }

        private void OnApplicationQuit()
        {
            string nicerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            CellexalLog.Log("Application quit on " + nicerTime);
            CellexalLog.Log("Goodbye.");
            CellexalLog.LogBacklog();
        }

        #endregion

    }
}