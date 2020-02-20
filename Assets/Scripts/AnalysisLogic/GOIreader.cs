using UnityEngine;
using System.Collections;
using CellexalVR.General;
using CellexalVR.DesktopUI;
using System;
using System.IO;

namespace CellexalVR.AnalysisLogic
{
    public class GOIreader : MonoBehaviour
    {
        private ReferenceManager referenceManager;

        // Use this for initialization
        void Start()
        {
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
        }

        [ConsoleCommand("goiReader", folder: "Data", aliases: new string[] { "loadGOIheatmaps", "GOIs" })]
        public void ReadPreviousSelection(string selectionPath)
        {
            selectionPath = "Data" + "/" + selectionPath;

            referenceManager.inputReader.RegisterOldGroup(selectionPath + "/GOIs_selection.txt");

            // iterate over all heatmap files
            string[] dirs = Directory.GetFiles(selectionPath, "GOIS_slice_*.txt");
            CellexalLog.Log("The number of files heatmaps to be loaded:" + dirs.Length.ToString());
            foreach (string dir in dirs)
            {
                referenceManager.heatmapGenerator.LoadHeatmap( "../" + dir);
            }
            

            //RegisterOldGroup(selectionFile);

        }

    }
}
