using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using CellexalVR.Spatial;
using UnityEngine;
using Valve.VR;

namespace CellexalVR.AnalysisLogic
{
    /// <summary>
    /// Class that handles the reading of MDS input files. Normally graph coordinates.
    /// </summary>
    public class MDSReader : MonoBehaviour
    {
        public ReferenceManager referenceManager;

        private readonly char[] separators = new char[] {' ', '\t', ','};
        private int nrOfLODGroups;


        /// <summary>
        /// Coroutine to create graphs.
        /// </summary>
        /// <param name="path"> The path to the folder where the files are. </param>
        /// <param name="mdsFiles"> The filenames. </param>
        /// <param name="type"></param>
        /// <param name="server"></param>
        public IEnumerator ReadMDSFiles(string path, string[] mdsFiles,
            GraphGenerator.GraphType type = GraphGenerator.GraphType.MDS, bool server = true)
        {
            if (!referenceManager.loaderController.loaderMovedDown)
            {
                referenceManager.loaderController.loaderMovedDown = true;
                referenceManager.loaderController.MoveLoader(new Vector3(0f, -2f, 0f), 2f);
            }

            nrOfLODGroups = CellexalConfig.Config.GraphPointQuality == "Standard" ? 2 : 1;

            //int statusId = status.AddStatus("Reading folder " + path);
            //int statusIdHUD = statusDisplayHUD.AddStatus("Reading folder " + path);
            //int statusIdFar = statusDisplayFar.AddStatus("Reading folder " + path);
            //  Read each .mds file
            //  The file format should be
            //  cell_id  axis_name1   axis_name2   axis_name3
            //  CELLNAME_1 X_COORD Y_COORD Z_COORD
            //  CELLNAME_2 X_COORD Y_COORD Z_COORD
            //  ...

            const float maximumDeltaTime = 0.05f; // 20 fps
            int maximumItemsPerFrame = CellexalConfig.Config.GraphLoadingCellsPerFrameStartCount;
            int totalNbrOfCells = 0;
            foreach (string file in mdsFiles)
            {
                print($"reading file: {file}");
                while (referenceManager.graphGenerator.isCreating)
                {
                    yield return null;
                }

                // // TODO: Make a more robust way of deciding if it should be loaded as a spatial graph.
                // if (file.Contains("slice"))
                // {
                //     StartCoroutine(ReadSpatialMDSFiles(file));
                //     referenceManager.graphGenerator.isCreating = true;
                //     continue;
                // }

                Graph combGraph = referenceManager.graphGenerator.CreateGraph(type);
                PointCloud pc = combGraph.GetComponent<PointCloud>();
                pc.Initialize(referenceManager.graphManager.Graphs.Count);
                // more_cells newGraph.GetComponent<GraphInteract>().isGrabbable = false;
                // file will be the full file name e.g C:\...\graph1.mds
                // good programming habits have left us with a nice mix of forward and backward slashes
                string[] regexResult = Regex.Split(file, @"[\\/]");
                string graphFileName = regexResult[regexResult.Length - 1];
                //combGraph.DirectoryName = regexResult[regexResult.Length - 2];
                referenceManager.graphManager.Graphs.Add(combGraph);
                switch (type)
                {
                    case GraphGenerator.GraphType.MDS:
                        combGraph.GraphName = graphFileName.Substring(0, graphFileName.Length - 4);
                        combGraph.FolderName = regexResult[regexResult.Length - 2];
                        referenceManager.graphManager.originalGraphs.Add(combGraph);
                        break;
                    case GraphGenerator.GraphType.FACS:
                    {
                        string graphName = "";
                        foreach (string s in referenceManager.newGraphFromMarkers.markers)
                        {
                            graphName += s + " - ";
                        }

                        combGraph.GraphNumber = referenceManager.inputReader.facsGraphCounter;
                        combGraph.GraphName = graphName;
                        combGraph.tag = "FacsGraph";
                        referenceManager.graphManager.facsGraphs.Add(combGraph);
                        break;
                    }
                    case GraphGenerator.GraphType.ATTRIBUTE:
                    case GraphGenerator.GraphType.BETWEEN:
                    case GraphGenerator.GraphType.SPATIAL:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }

                string[] axes = new string[3];
                string[] velo = new string[3];
                using (StreamReader mdsStreamReader = new StreamReader(file))
                {
                    //List<string> cellnames = new List<string>();
                    //List<float> xcoords = new List<float>();s
                    //List<float> ycoords = new List<float>();
                    //List<float> zcoords = new List<float>();
                    // first line is (if correct format) a header and the first word is cell_id (the name of the first column).
                    // If wrong and does not contain header read first line as a cell.
                    string header = mdsStreamReader.ReadLine();
                    if (header != null && header.Split(separators)[0].Equals("CellID"))
                    {
                        string[] columns = header.Split(separators).Skip(1).ToArray();
                        Array.Copy(columns, 0, axes, 0, 3);
                        if (columns.Length == 6)
                        {
                            Array.Copy(columns, 3, velo, 0, 3);
                            referenceManager.graphManager.velocityFiles.Add(file);
                            combGraph.hasVelocityInfo = true;
                        }
                    }
                    else if (header != null)
                    {
                        string[] words = header.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Length != 4 && words.Length != 7)
                        {
                            continue;
                        }

                        string cellName = words[0];
                        float x = float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                        float y = float.Parse(words[2], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                        float z = float.Parse(words[3], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                        Cell cell = referenceManager.cellManager.AddCell(cellName);
                        referenceManager.graphGenerator.AddGraphPoint(cell, x, y, z);
                        axes[0] = "x";
                        axes[1] = "y";
                        axes[2] = "z";
                    }

                    combGraph.axisNames = axes;
                    var itemsThisFrame = 0;
                    while (!mdsStreamReader.EndOfStream)
                    {
                        //  status.UpdateStatus(statusId, "Reading " + graphFileName + " (" + fileIndex + "/" + mdsFiles.Length + ") " + ((float)mdsStreamReader.BaseStream.Position / mdsStreamReader.BaseStream.Length) + "%");
                        //  statusDisplayHUD.UpdateStatus(statusIdHUD, "Reading " + graphFileName + " (" + fileIndex + "/" + mdsFiles.Length + ") " + ((float)mdsStreamReader.BaseStream.Position / mdsStreamReader.BaseStream.Length) + "%");
                        //  statusDisplayFar.UpdateStatus(statusIdFar, "Reading " + graphFileName + " (" + fileIndex + "/" + mdsFiles.Length + ") " + ((float)mdsStreamReader.BaseStream.Position / mdsStreamReader.BaseStream.Length) + "%");
                        //print(maximumItemsPerFrame);


                        for (int j = 0; j < maximumItemsPerFrame && !mdsStreamReader.EndOfStream; ++j)
                        {
                            string[] words = mdsStreamReader.ReadLine()
                                .Split(separators, StringSplitOptions.RemoveEmptyEntries);
                            if (words.Length != 4 && words.Length != 7)
                            {
                                continue;
                            }

                            string cellname = words[0];
                            float x = float.Parse(words[1],
                                System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                            float y = float.Parse(words[2],
                                System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                            float z = float.Parse(words[3],
                                System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                            Cell cell = referenceManager.cellManager.AddCell(cellname);
                            referenceManager.graphGenerator.AddGraphPoint(cell, x, y, z);
                            itemsThisFrame++;
                        }

                        totalNbrOfCells += itemsThisFrame;
                        // wait for end of frame
                        yield return null;

                        float lastFrame = Time.deltaTime;
                        if (lastFrame < maximumDeltaTime)
                        {
                            // we had some time over last frame
                            maximumItemsPerFrame += CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement;
                        }
                        else if (lastFrame > maximumDeltaTime && maximumItemsPerFrame >
                            CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement * 2)
                        {
                            // we took too much time last frame
                            maximumItemsPerFrame -= CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement;
                        }
                    }

                    // tell the graph that the info text is ready to be set
                    // more_cells newGraph.GetComponent<GraphInteract>().isGrabbable = true;
                    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();
                    // more_cells newGraph.CreateColliders();
                    stopwatch.Stop();
                    CellexalLog.Log("Created " + combGraph.GetComponents<BoxCollider>().Length + " colliders in " +
                                    stopwatch.Elapsed.ToString() + " for graph " + graphFileName);
                    //if (doLoad)
                    //{
                    //    graphManager.LoadPosition(newGraph, fileIndex);
                    //}
                    //mdsFileStream.Close();
                    mdsStreamReader.Close();
                    // if (debug)
                    //     newGraph.CreateConvexHull();
                }

                // If high quality mesh is used. Use LOD groups to swap to low q when further away.
                // Improves performance a lot when analysing larger graphs.
                int n = CellexalConfig.Config.GraphPointQuality == "Standard" ? 2 : 1;
                StartCoroutine(referenceManager.graphGenerator.SliceClusteringLOD(nrOfLODGroups));

                while (referenceManager.graphGenerator.isCreating)
                {
                    yield return null;
                }

                if (nrOfLODGroups > 1)
                {
                    combGraph.gameObject.AddComponent<LODGroup>();
                    referenceManager.graphGenerator.UpdateLODGroups(combGraph, nrOfLODGroups);
                }

                // Add axes in bottom corner of graph and scale points differently
                combGraph.SetInfoText();
                referenceManager.graphGenerator.AddAxes(combGraph, axes);

                //status.UpdateStatus(statusId, "Reading index.facs file");
                //statusDisplayHUD.UpdateStatus(statusIdHUD, "Reading index.facs file");
                //statusDisplayFar.UpdateStatus(statusIdFar, "Reading index.facs file");
                //status.RemoveStatus(statusId);
                //statusDisplayHUD.RemoveStatus(statusIdHUD);
                //statusDisplayFar.RemoveStatus(statusIdFar);
            }

            if (type.Equals(GraphGenerator.GraphType.MDS))
            {
                referenceManager.inputReader.attributeReader =
                    referenceManager.inputReader.gameObject.AddComponent<AttributeReader>();
                referenceManager.inputReader.attributeReader.referenceManager = referenceManager;
                StartCoroutine(referenceManager.inputReader.attributeReader.ReadAttributeFilesCoroutine(path));
                while (!referenceManager.inputReader.attributeFileRead)
                    yield return null;
                referenceManager.inputReader.ReadFacsFiles(path, totalNbrOfCells);
                referenceManager.inputReader.ReadFilterFiles(CellexalUser.UserSpecificFolder);
            }

            CellexalEvents.GraphsLoaded.Invoke();
        }


        /// <summary>
        /// For spatial data we want to have each slice as a separate graph to be able to interact with them individually.
        /// First the list of points is ordered by the z coordinate then for each z coordinate a graph is created. 
        /// </summary>
        /// <returns></returns>
        private IEnumerator ReadSpatialMDSFiles(string file)
        {
            if (!referenceManager.loaderController.loaderMovedDown)
            {
                referenceManager.loaderController.loaderMovedDown = true;
                referenceManager.loaderController.MoveLoader(new Vector3(0f, -2f, 0f), 2f);
            }

            // int nrOfLODGroups = CellexalConfig.Config.GraphPointQuality == "Standard" ? 2 : 1;
            List<Tuple<string, Vector3>> gps = new List<Tuple<string, Vector3>>();
            const float maximumDeltaTime = 0.05f; // 20 fps
            int maximumItemsPerFrame = CellexalConfig.Config.GraphLoadingCellsPerFrameStartCount;

            //string fullPath = Directory.GetCurrentDirectory() + "\\Data\\" + data + "\\tsne.mds";
            float prevCoord = float.NaN;
            while (referenceManager.graphGenerator.isCreating)
            {
                yield return null;
            }

            GameObject parent = GameObject.Instantiate(referenceManager.inputReader.spatialGraphPrefab);
            SpatialGraph sg = parent.GetComponent<SpatialGraph>();
            sg.gameObject.layer = LayerMask.NameToLayer("GraphLayer");
            referenceManager.graphManager.spatialGraphs.Add(sg);
            sg.referenceManager = referenceManager;

            int sliceNr = 0;
            using (StreamReader mdsStreamReader = new StreamReader(file))
            {
                int i = 0;
                string header = mdsStreamReader.ReadLine();
                while (!mdsStreamReader.EndOfStream)
                {
                    var itemsThisFrame = 0;
                    for (int j = 0; j < maximumItemsPerFrame && !mdsStreamReader.EndOfStream; ++j)
                    {
                        string[] words = mdsStreamReader.ReadLine()
                            ?.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                        if (words != null && (words.Length != 4 && words.Length != 7))
                        {
                            continue;
                        }

                        string cellName = words[0];
                        float x = float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                        float y = float.Parse(words[2], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                        float z = float.Parse(words[3], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                        Cell cell = referenceManager.cellManager.AddCell(cellName);
                        gps.Add(new Tuple<string, Vector3>(cellName, new Vector3(x, y, z)));
                        itemsThisFrame++;
                    }

                    i += itemsThisFrame;
                    // wait for end of frame
                    yield return null;

                    float lastFrame = Time.deltaTime;
                    if (lastFrame < maximumDeltaTime)
                    {
                        // we had some time over last frame
                        maximumItemsPerFrame += CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement;
                    }
                    else if (lastFrame > maximumDeltaTime && maximumItemsPerFrame >
                        CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement * 2)
                    {
                        // we took too much time last frame
                        maximumItemsPerFrame -= CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement;
                    }
                }
            }

            gps.Sort((x, y) => x.Item2.z.CompareTo(y.Item2.z));
            Vector3 maxCoords = new Vector3();
            Vector3 minCoords = new Vector3();
            maxCoords.x = gps.Max(v => (v.Item2.x));
            maxCoords.y = gps.Max(v => (v.Item2.y));
            maxCoords.z = gps.Max(v => (v.Item2.z));
            minCoords.x = gps.Min(v => (v.Item2.x));
            minCoords.y = gps.Min(v => (v.Item2.y));
            minCoords.z = gps.Min(v => (v.Item2.z));
            Graph combGraph = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL);
            yield return null;
            referenceManager.graphManager.Graphs.Add(combGraph);
            referenceManager.graphManager.originalGraphs.Add(combGraph);
            combGraph.gameObject.name = "Slice" + sliceNr;
            combGraph.GraphName = "Slice" + sliceNr;
            Transform transform1 = combGraph.transform;
            transform1.parent = parent.transform;
            transform1.localPosition = new Vector3(0, 0, 0);
            combGraph.lodGroups = nrOfLODGroups;
            combGraph.textures = new Texture2D[nrOfLODGroups];
            GraphSlice gs = combGraph.gameObject.AddComponent<Spatial.GraphSlice>();
            gs.referenceManager = referenceManager;
            yield return null;

            // const float sliceDist = 0.005f;
            Tuple<string, Vector3> gpTuple = gps[0];
            Cell c = referenceManager.cellManager.GetCell(gpTuple.Item1);

            Graph.GraphPoint gp = referenceManager.graphGenerator.AddGraphPoint(c, gpTuple.Item2.x,
                gpTuple.Item2.y,
                gpTuple.Item2.z);

            float currentCoord = gpTuple.Item2.z;
            for (int n = 1; n < gps.Count; n++)
            {
                gpTuple = gps[n];
                currentCoord = gpTuple.Item2.z;
                if (n == 1)
                {
                    prevCoord = currentCoord;
                }
                // when we reach new slice (new z coordinate) build the graph and then start adding to a new one.
                else if (Math.Abs(currentCoord - prevCoord) > 0.01f)
                {
                    for (int i = 0; i < nrOfLODGroups; i++)
                    {
                        referenceManager.graphGenerator.isCreating = true;
                        // referenceManager.graphGenerator.AddLODGroup(combGraph, i);
                        //
                        // combGraph.maxCoordValues = maxCoords;
                        // combGraph.minCoordValues = minCoords;
                        // referenceManager.graphGenerator.SliceClustering(lodGroup: i);
                        // while (referenceManager.graphGenerator.isCreating)
                        // {
                        //     yield return null;
                        // }

                        gs.zCoord = gp.WorldPosition.z;
                        combGraph.maxCoordValues = maxCoords;
                        combGraph.minCoordValues = minCoords;
                        StartCoroutine(referenceManager.graphGenerator.SliceClusteringLOD(nrOfLODGroups));

                        while (referenceManager.graphGenerator.isCreating)
                        {
                            yield return null;
                        }

                        gs.zCoord = gp.WorldPosition.z;

                        if (nrOfLODGroups > 1)
                        {
                            combGraph.gameObject.AddComponent<LODGroup>();
                            referenceManager.graphGenerator.UpdateLODGroups(combGraph);
                        }

                        combGraph = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL);
                        combGraph.lodGroups = nrOfLODGroups;
                        combGraph.textures = new Texture2D[nrOfLODGroups];
                        yield return null;
                        referenceManager.graphManager.Graphs.Add(combGraph);
                        referenceManager.graphManager.originalGraphs.Add(combGraph);
                        combGraph.transform.parent = parent.transform;
                        yield return null;
                        gs = combGraph.gameObject.AddComponent<GraphSlice>();
                        gs.referenceManager = referenceManager;
                        gs.sliceNr = ++sliceNr;
                        combGraph.transform.localPosition = new Vector3(0, 0, 0);
                        combGraph.GraphName = "Slice" + sliceNr;
                        combGraph.gameObject.name = "Slice" + sliceNr;
                    }
                }

                // last gp: finish the final slice
                else if (n == gps.Count - 1)
                {
                    c = referenceManager.cellManager.GetCell(gpTuple.Item1);
                    gp = referenceManager.graphGenerator.AddGraphPoint(c, gpTuple.Item2.x, gpTuple.Item2.y,
                        gpTuple.Item2.z);
                    for (int i = 0; i < nrOfLODGroups; i++)
                    {
                        referenceManager.graphGenerator.isCreating = true;
                        referenceManager.graphGenerator.AddLODGroup(combGraph, i);
                        combGraph.maxCoordValues = maxCoords;
                        combGraph.minCoordValues = minCoords;
                        referenceManager.graphGenerator.SliceClustering(lodGroup: i);
                        while (referenceManager.graphGenerator.isCreating)
                        {
                            yield return null;
                        }

                        gs.zCoord = gp.WorldPosition.z;

                        if (nrOfLODGroups > 1)
                        {
                            combGraph.gameObject.AddComponent<LODGroup>();
                            referenceManager.graphGenerator.UpdateLODGroups(combGraph);
                        }

                        combGraph.transform.localPosition = new Vector3(0, 0, 0);
                        continue;
                    }

                    c = referenceManager.cellManager.GetCell(gpTuple.Item1);
                    gp = referenceManager.graphGenerator.AddGraphPoint(c, gpTuple.Item2.x, gpTuple.Item2.y,
                        gpTuple.Item2.z);
                    prevCoord = currentCoord;
                }


                StartCoroutine(sg.AddSlices());
            }
        }
    }
}