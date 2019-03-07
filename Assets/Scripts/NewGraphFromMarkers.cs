﻿using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Select 3 markers and create new graph with these as axes.
/// </summary>
public class NewGraphFromMarkers : MonoBehaviour
{
    public ReferenceManager referenceManager;
    public List<string> markers;

    private string filePath;
    private bool useSelection;

    private void Start()
    {
        markers = new List<string>();
        CellexalEvents.SelectionConfirmed.AddListener(SwitchMode);
        CellexalEvents.GraphsReset.AddListener(SwitchMode);
    }

    private void Update()
    {
        
    }

    private void SwitchMode()
    {
        useSelection = !useSelection;
    }

    [ConsoleCommand("newGraphFromMarkers", "newGraphFromMarkers", "ngfm")]
    public void CreateMarkerGraph(bool selection=false)
    {
        if (referenceManager.selectionToolHandler.GetLastSelection().Count > 0)
        {
            DumpSelectionToTextFile(referenceManager.selectionToolHandler.GetLastSelection(), markers[0], markers[1], markers[2]);
        }
        else
        {
            List<CombinedGraph.CombinedGraphPoint> cellList = new List<CombinedGraph.CombinedGraphPoint>(referenceManager.graphManager.Graphs[0].points.Values);
            DumpSelectionToTextFile(cellList, markers[0], markers[1], markers[2]);
        }
        string[] files = new string[1];
        files[0] = filePath;
        referenceManager.inputReader.ReadCoordinates(CellexalUser.UserSpecificFolder, files);
        markers.Clear();
        foreach(AddMarkerButton b in GetComponentsInChildren<AddMarkerButton>())
        {
            b.activeOutline.SetActive(false);
        }
    }

    /// <summary>
    /// Dumping the selection with ids and facs values on the same format as the mds files so it can be read in by the inputreader.
    /// </summary>
    /// <param name="points">The points to save to the file. Either a selection or all the points in the graph.</param>
    public void DumpSelectionToTextFile(List<CombinedGraph.CombinedGraphPoint> points, string first_marker,
                                        string second_marker, string third_marker)
    {
        // print(new System.Diagnostics.StackTrace());
        this.filePath = CellexalUser.UserSpecificFolder + "\\" + first_marker + "_" + second_marker + "_" + third_marker + ".txt";
        HashSet<string> previousLines = new HashSet<string>();
        using (StreamWriter file = new StreamWriter(filePath))
        {
            CellexalLog.Log("Dumping selection data to " + CellexalLog.FixFilePath(filePath));
            CellexalLog.Log("\tSelection consists of  " + points.Count + " points");
            string header = "\t" + first_marker + "\t" + second_marker + "\t" + third_marker;
            file.WriteLine(header);
            for (int i = 0; i < points.Count; i++)
            {
                string label = points[i].Label;
                Cell cell = referenceManager.cellManager.GetCell(label);
                // Add returns true if it was actually added,
                // false if it was already there
                // Duplicate lines (coordinates) causes trouble when creating the meshes...
                string currentLine = cell.FacsValue[first_marker.ToLower()] + "\t" + cell.FacsValue[second_marker.ToLower()] 
                                        + "\t" + cell.FacsValue[third_marker.ToLower()];
                if (previousLines.Add(currentLine))
                {
                    file.Write(label);
                    file.Write("\t");
                    file.WriteLine(currentLine);
                }
            }
            file.Flush();
            file.Close();
        }
    }
}

