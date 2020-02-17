using UnityEngine;
using System.IO;
using System;
using CellexalVR.General;

namespace CellexalVR.DesktopUI
{
    /// <summary>
    /// Represents the buttons that open a folder or
    /// </summary>
    public class OpenFolderButton : MonoBehaviour
    {
        public string path;

        public void OpenFolder()
        {
            string currentDir = Directory.GetCurrentDirectory();
            string fullPath = currentDir + Path.PathSeparator + path;
            if (path == "$LOG")
            {
                CellexalLog.LogBacklog();
                fullPath = CellexalLog.LogFilePath;
            }
            else if (path == "$UNITY_LOG")
            {
#if UNITY_EDITOR
                fullPath = Path.Combine(Environment.GetEnvironmentVariable("AppData"), "..", "Local", "Unity", "Editor", "Player.log");
#else
                fullPath = Path.Combine(Environment.GetEnvironmentVariable("AppData"), "..", "LocalLow", Application.companyName, Application.productName, "Player.log");
#endif
            }
            System.Diagnostics.Process.Start(fullPath);
        }
    }
}