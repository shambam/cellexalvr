using UnityEngine;
using System.Collections;
using System.IO;
using System.Linq;
using SQLiter;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using TMPro;
using System.Threading;
using CellexalVR.Menu.SubMenus;
using CellexalVR.Menu.Buttons;
using CellexalVR.General;
using CellexalVR.AnalysisObjects;
using CellexalVR.DesktopUI;
using CellexalVR.Extensions;
using CellexalVR.SceneObjects;
using CellexalVR.Interaction;
using System.Drawing;
using System.Drawing.Imaging;
using CellexalVR.Multiuser;
using System.Diagnostics;
using CellexalVR.Spatial;

namespace CellexalVR.AnalysisLogic
{
    /// <summary>
    /// A class for reading data files and creating objects in the virtual environment.
    /// 
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public NetworkCenter networkPrefab;
        public GameObject SpatialGraphPrefab;

        public TextMeshPro graphName;

        public GameObject lineprefab;

        private char[] separators = new char[] { ' ', '\t' };

        private GraphManager graphManager;
        private CellManager cellManager;
        private LoaderController loaderController;
        private SQLite database;
        //private SelectionToolHandler selectionToolHandler;
        private SelectionManager selectionManager;
        private AttributeSubMenu attributeSubMenu;
        private ColorByIndexMenu indexMenu;
        private GraphFromMarkersMenu createFromMarkerMenu;
        private GameObject headset;
        //private StatusDisplay status;
        //private StatusDisplay statusDisplayHUD;
        //private StatusDisplay statusDisplayFar;
        private MultiuserMessageSender multiuserMessageSender;
        private NetworkGenerator networkGenerator;
        private GraphGenerator graphGenerator;
        private string currentPath;
        private int facsGraphCounter;
        private bool attributeFileRead = false;

        private Bitmap image1;

        [Tooltip("Automatically loads the Bertie dataset")]
        public bool debug = false;

        //Flag for loading previous sessions
        public bool doLoad = false;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        private void Start()
        {
            multiuserMessageSender = referenceManager.multiuserMessageSender;
            graphManager = referenceManager.graphManager;
            cellManager = referenceManager.cellManager;
            loaderController = referenceManager.loaderController;
            database = referenceManager.database;
            //selectionToolHandler = referenceManager.selectionToolHandler;
            selectionManager = referenceManager.selectionManager;
            attributeSubMenu = referenceManager.attributeSubMenu;
            indexMenu = referenceManager.indexMenu;
            createFromMarkerMenu = referenceManager.createFromMarkerMenu;
            //headset = referenceManager.headset;
            if (CrossSceneInformation.Spectator)
            {
                headset = referenceManager.spectatorRig;
                referenceManager.headset = headset;
            }
            else
            {
                headset = referenceManager.headset;
            }
            //status = referenceManager.statusDisplay;
            //statusDisplayHUD = referenceManager.statusDisplayHUD;
            //statusDisplayFar = referenceManager.statusDisplayFar;
            networkGenerator = referenceManager.networkGenerator;
            graphGenerator = referenceManager.graphGenerator;
            currentPath = "";
            facsGraphCounter = 0;

            RScriptRunner.SetReferenceManager(referenceManager);
            CellexalEvents.UsernameChanged.AddListener(LoadPreviousGroupings);
        }



        [ConsoleCommand("inputReader", folder: "Data", aliases: new string[] { "readfolder", "rf" })]
        public void ReadFolderConsole(string path)
        {
            referenceManager.multiuserMessageSender.SendMessageReadFolder(path);
            ReadFolder(path);
        }
        /// <summary>
        /// Reads one folder of data and creates the graphs described by the data.
        /// </summary>
        /// <param name="path"> The path to the folder. </param>
        //[ConsoleCommand("inputReader", folder: "Data", aliases: new string[] { "readfolder", "rf" })]
        public void ReadFolder(string path)
        {
            UpdateSelectionToolHandler();
            if (PhotonNetwork.isMasterClient)
            {
                referenceManager.configManager.MultiUserSynchronise();
            }
            attributeFileRead = false;
            // multiple_exp if (currentPath.Length > 0)
            // multiple_exp {
            // multiple_exp     currentPath += "+" + path;
            // multiple_exp }
            currentPath = path;
            string workingDirectory = Directory.GetCurrentDirectory();
            string fullPath = workingDirectory + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar  + path;
            CellexalLog.Log("Started reading the data folder at " + CellexalLog.FixFilePath(fullPath));
            CellexalUser.DataSourceFolder = currentPath;
            //LoadPreviousGroupings();
            database.InitDatabase(fullPath + Path.DirectorySeparatorChar + "database.sqlite");
            if (Directory.Exists(workingDirectory + Path.DirectorySeparatorChar + "Output"))
            {
                if (File.Exists(workingDirectory + Path.DirectorySeparatorChar + "Output\\r_log.txt"))
                {
                    CellexalLog.Log("Deleting old r log file");
                    File.Delete(workingDirectory + Path.DirectorySeparatorChar + "Output\\r_log.txt");
                }
            }
            selectionManager.DataDir = fullPath;
            if (!debug)
            {
                // clear the network folder
                string networkDirectory = (CellexalUser.UserSpecificFolder + Path.DirectorySeparatorChar + "Resources\\Networks").FixFilePath();
                if (!Directory.Exists(networkDirectory))
                {
                    CellexalLog.Log("Creating directory " + CellexalLog.FixFilePath(networkDirectory));
                    Directory.CreateDirectory(networkDirectory);
                }
                string[] networkFilesList = Directory.GetFiles(networkDirectory, "*");
                CellexalLog.Log("Deleting " + networkFilesList.Length + " files in " + CellexalLog.FixFilePath(networkDirectory));
                foreach (string f in networkFilesList)
                {
                    File.Delete(f);
                }
            }
            string[] mdsFiles;
            if (CrossSceneInformation.Tutorial)
            {
                mdsFiles = Directory.GetFiles(fullPath, "DDRTree.mds");
            }
            else
            {
                mdsFiles = Directory.GetFiles(fullPath, "*.mds");
            }
            if (mdsFiles.Length == 0)
            {
                CellexalError.SpawnError("Empty dataset", "The loaded dataset did not contain any .mds files. Make sure you have placed the dataset files in the correct folder.");
                throw new System.InvalidOperationException("Empty dataset");
            }
            CellexalLog.Log("Reading " + mdsFiles.Length + " .mds files");
            StartCoroutine(ReadMDSFiles(fullPath, mdsFiles));
            graphGenerator.isCreating = true;
            referenceManager.selectionManager.LoadAnnotationTags();
            //string[] spatialMds = Directory.GetFiles(fullPath, "*.spatialmds");
            //StartCoroutine(ReadSpatialMDSFiles(fullPath, spatialMds));
        }

        void UpdateSelectionToolHandler()
        {
            referenceManager.heatmapGenerator.selectionManager = referenceManager.selectionManager;
            referenceManager.networkGenerator.selectionManager = referenceManager.selectionManager;
            referenceManager.graphManager.selectionManager = referenceManager.selectionManager;
        }


        /// <summary>
        /// Reads a facs marker file.
        /// </summary>
        /// <param name="path">The path to the directory</param>
        /// <param name="file">The path to the file.</param>
        public void ReadGraphFromMarkerFile(string path, string file)
        {
            facsGraphCounter++;

            StartCoroutine(ReadMDSFiles(path, new string[] { file }, GraphGenerator.GraphType.FACS, false));
        }

        /// <summary>
        /// Coroutine to create graphs.
        /// </summary>
        /// <param name="path"> The path to the folder where the files are. </param>
        /// <param name="mdsFiles"> The filenames. </param>
        IEnumerator ReadMDSFiles(string path, string[] mdsFiles, GraphGenerator.GraphType type = GraphGenerator.GraphType.MDS, bool server = true)
        {

            if (!loaderController.loaderMovedDown)
            {
                loaderController.loaderMovedDown = true;
                loaderController.MoveLoader(new Vector3(0f, -2f, 0f), 2f);
            }

            //int statusId = status.AddStatus("Reading folder " + path);
            //int statusIdHUD = statusDisplayHUD.AddStatus("Reading folder " + path);
            //int statusIdFar = statusDisplayFar.AddStatus("Reading folder " + path);
            int fileIndex = 0;
            //  Read each .mds file
            //  The file format should be
            //  cell_id  axis_name1   axis_name2   axis_name3
            //  CELLNAME_1 X_COORD Y_COORD Z_COORD
            //  CELLNAME_2 X_COORD Y_COORD Z_COORD
            //  ...

            float maximumDeltaTime = 0.05f; // 20 fps
            int maximumItemsPerFrame = CellexalConfig.Config.GraphLoadingCellsPerFrameStartCount;
            int itemsThisFrame = 0;
            int totalNbrOfCells = 0;
            foreach (string file in mdsFiles)
            {
                print(file);
                while (graphGenerator.isCreating)
                {
                    yield return null;
                }
                if (file.Contains("slice"))
                {
                    StartCoroutine(ReadSpatialMDSFiles(file));
                    graphGenerator.isCreating = true;
                    continue;
                }
                Graph combGraph = graphGenerator.CreateGraph(type);
                // more_cells newGraph.GetComponent<GraphInteract>().isGrabbable = false;
                // file will be the full file name e.g C:\...\graph1.mds
                // good programming habits have left us with a nice mix of forward and backward slashes
                string[] regexResult = Regex.Split(file, @"[\\/]");
                string graphFileName = regexResult[regexResult.Length - 1];
                //combGraph.DirectoryName = regexResult[regexResult.Length - 2];
                graphManager.Graphs.Add(combGraph);
                if (type.Equals(GraphGenerator.GraphType.MDS))
                {
                    combGraph.GraphName = graphFileName.Substring(0, graphFileName.Length - 4);
                    combGraph.FolderName = regexResult[regexResult.Length - 2];
                    graphManager.originalGraphs.Add(combGraph);
                }
                else if (type.Equals(GraphGenerator.GraphType.FACS))
                {
                    string name = "";
                    foreach (string s in referenceManager.newGraphFromMarkers.markers)
                    {
                        name += s + " - ";
                    }
                    combGraph.GraphNumber = facsGraphCounter;
                    combGraph.GraphName = name;
                    combGraph.tag = "FacsGraph";
                    graphManager.facsGraphs.Add(combGraph);
                }
                //combGraph.gameObject.name = combGraph.GraphName;
                //FileStream mdsFileStream = new FileStream(file, FileMode.Open);

                //image1 = new Bitmap(400, 400);
                //System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(image1);
                //int i, j;
                string[] axes = new string[3];
                string[] velo = new string[3];
                using (StreamReader mdsStreamReader = new StreamReader(file))
                {
                    //List<string> cellnames = new List<string>();
                    //List<float> xcoords = new List<float>();
                    //List<float> ycoords = new List<float>();
                    //List<float> zcoords = new List<float>();
                    int i = 0;
                    // first line is (if correct format) a header and the first word is cell_id (the name of the first column).
                    // If wrong and does not contain header read first line as a cell.
                    string header = mdsStreamReader.ReadLine();
                    if (header.Split(null)[0].Equals("CellID"))
                    {
                        string[] columns = header.Split(null).Skip(1).ToArray();
                        Array.Copy(columns, 0, axes, 0, 3);
                        if (columns.Length == 6)
                        {
                            Array.Copy(columns, 3, velo, 0, 3);
                            graphManager.velocityFiles.Add(file);
                            combGraph.hasVelocityInfo = true;
                        }
                    }
                    else
                    {
                        string[] words = header.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Length != 4 && words.Length != 7)
                        {
                            print(words.Length);
                            continue;
                        }
                        string cellname = words[0];
                        float x = float.Parse(words[1]);
                        float y = float.Parse(words[2]);
                        float z = float.Parse(words[3]);
                        Cell cell = cellManager.AddCell(cellname);
                        graphGenerator.AddGraphPoint(cell, x, y, z);
                        axes[0] = "x";
                        axes[1] = "y";
                        axes[2] = "z";
                    }
                    combGraph.axisNames = axes;
                    while (!mdsStreamReader.EndOfStream)
                    {
                        itemsThisFrame = 0;
                        //  status.UpdateStatus(statusId, "Reading " + graphFileName + " (" + fileIndex + "/" + mdsFiles.Length + ") " + ((float)mdsStreamReader.BaseStream.Position / mdsStreamReader.BaseStream.Length) + "%");
                        //  statusDisplayHUD.UpdateStatus(statusIdHUD, "Reading " + graphFileName + " (" + fileIndex + "/" + mdsFiles.Length + ") " + ((float)mdsStreamReader.BaseStream.Position / mdsStreamReader.BaseStream.Length) + "%");
                        //  statusDisplayFar.UpdateStatus(statusIdFar, "Reading " + graphFileName + " (" + fileIndex + "/" + mdsFiles.Length + ") " + ((float)mdsStreamReader.BaseStream.Position / mdsStreamReader.BaseStream.Length) + "%");
                        //print(maximumItemsPerFrame);


                        for (int j = 0; j < maximumItemsPerFrame && !mdsStreamReader.EndOfStream; ++j)
                        {
                            string[] words = mdsStreamReader.ReadLine().Split(separators, StringSplitOptions.RemoveEmptyEntries);
                            if (words.Length != 4 && words.Length != 7)
                            {
                                print(words.Length);
                                continue;
                            }
                            string cellname = words[0];
                            float x = float.Parse(words[1]);
                            float y = float.Parse(words[2]);
                            float z = float.Parse(words[3]);

                            Cell cell = cellManager.AddCell(cellname);
                            graphGenerator.AddGraphPoint(cell, x, y, z);
                            itemsThisFrame++;
                        }

                        i += itemsThisFrame;
                        totalNbrOfCells += itemsThisFrame;
                        // wait for end of frame
                        yield return null;

                        float lastFrame = Time.deltaTime;
                        if (lastFrame < maximumDeltaTime)
                        {
                            // we had some time over last frame
                            maximumItemsPerFrame += CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement;
                        }
                        else if (lastFrame > maximumDeltaTime && maximumItemsPerFrame > CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement * 2)
                        {
                            // we took too much time last frame
                            maximumItemsPerFrame -= CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement;
                        }
                    }

                    fileIndex++;
                    // tell the graph that the info text is ready to be set
                    // more_cells newGraph.GetComponent<GraphInteract>().isGrabbable = true;
                    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();
                    // more_cells newGraph.CreateColliders();
                    stopwatch.Stop();
                    CellexalLog.Log("Created " + combGraph.GetComponents<BoxCollider>().Length + " colliders in " + stopwatch.Elapsed.ToString() + " for graph " + graphFileName);
                    //if (doLoad)
                    //{
                    //    graphManager.LoadPosition(newGraph, fileIndex);
                    //}
                    //mdsFileStream.Close();
                    mdsStreamReader.Close();
                    // if (debug)
                    //     newGraph.CreateConvexHull();

                }

                // Add axes in bottom corner of graph and scale points differently
                graphGenerator.SliceClustering();
                graphGenerator.AddAxes(combGraph, axes);
                combGraph.SetInfoText();

                CellexalLog.Log("Successfully read graph from " + graphFileName + " reading ~" + maximumItemsPerFrame + " lines every frame");
                //combinedGraphGenerator.isCreating = false;
            }

            //newGraph.transform.Translate(Vector3.up * 5);


            //}
            if (type.Equals(GraphGenerator.GraphType.MDS))
            {
                StartCoroutine(ReadAttributeFilesCoroutine(path));
                while (!attributeFileRead)
                    yield return null;
                ReadFacsFiles(path, totalNbrOfCells);
                ReadFilterFiles(CellexalUser.UserSpecificFolder);
            }

            //loaderController.loaderMovedDown = true;


            //status.UpdateStatus(statusId, "Reading index.facs file");
            //statusDisplayHUD.UpdateStatus(statusIdHUD, "Reading index.facs file");
            //statusDisplayFar.UpdateStatus(statusIdFar, "Reading index.facs file");
            //flashGenesMenu.CreateTabs(path);
            //status.RemoveStatus(statusId);
            //statusDisplayHUD.RemoveStatus(statusIdHUD);
            //statusDisplayFar.RemoveStatus(statusIdFar);

            if (server)
            {
                StartCoroutine(StartServer("main"));
                //StartCoroutine(StartServer("gene"));
            }

            while (graphGenerator.isCreating)
            {
                yield return null;
            }
            CellexalEvents.GraphsLoaded.Invoke();

        }

        /// <summary>
        /// For spatial data we want to have each slice as a separate graph to be able to interact with them individually.
        /// First the list of points is ordered by the z coordinate then for each z coordinate a graph is created. 
        /// </summary>
        /// <returns></returns>
        public IEnumerator ReadSpatialMDSFiles(string file)
        {
            if (!loaderController.loaderMovedDown)
            {
                loaderController.loaderMovedDown = true;
                loaderController.MoveLoader(new Vector3(0f, -2f, 0f), 2f);
            }
            List<Tuple<string, Vector3>> gps = new List<Tuple<string, Vector3>>();
            float maximumDeltaTime = 0.05f; // 20 fps
            int maximumItemsPerFrame = CellexalConfig.Config.GraphLoadingCellsPerFrameStartCount;
            int itemsThisFrame = 0;
            int totalNbrOfCells = 0;
            //string fullPath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar  + data + Path.DirectorySeparatorChar + "tsne.mds";
            float prevCoord = float.NaN;
            while (graphGenerator.isCreating)
            {
                yield return null;
            }
            float currentCoord;
            GameObject parent = GameObject.Instantiate(SpatialGraphPrefab);
            SpatialGraph sg = parent.GetComponent<SpatialGraph>();
            sg.referenceManager = referenceManager;
            float posIncr = 0;
            int sliceNr = 0;
            using (StreamReader mdsStreamReader = new StreamReader(file))
            {
                int i = 0;
                string header = mdsStreamReader.ReadLine();
                while (!mdsStreamReader.EndOfStream)
                {
                    itemsThisFrame = 0;
                    for (int j = 0; j < maximumItemsPerFrame && !mdsStreamReader.EndOfStream; ++j)
                    {
                        string[] words = mdsStreamReader.ReadLine().Split(separators, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Length != 4 && words.Length != 7)
                        {
                            print(words.Length);
                            continue;
                        }
                        string cellname = words[0];
                        float x = float.Parse(words[1]);
                        float y = float.Parse(words[2]);
                        float z = float.Parse(words[3]);
                        gps.Add(new Tuple<string, Vector3>(cellname, new Vector3(x, y, z)));
                        itemsThisFrame++;
                    }

                    i += itemsThisFrame;
                    totalNbrOfCells += itemsThisFrame;
                    // wait for end of frame
                    yield return null;

                    float lastFrame = Time.deltaTime;
                    if (lastFrame < maximumDeltaTime)
                    {
                        // we had some time over last frame
                        maximumItemsPerFrame += CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement;
                    }
                    else if (lastFrame > maximumDeltaTime && maximumItemsPerFrame > CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement * 2)
                    {
                        // we took too much time last frame
                        maximumItemsPerFrame -= CellexalConfig.Config.GraphLoadingCellsPerFrameIncrement;
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

                Graph combGraph = graphGenerator.CreateGraph(GraphGenerator.GraphType.MDS);
                yield return null;
                graphManager.Graphs.Add(combGraph);
                graphManager.originalGraphs.Add(combGraph);
                combGraph.gameObject.name = "Slice" + sliceNr;
                combGraph.GraphName = "Slice" + sliceNr;
                combGraph.transform.parent = parent.transform;
                combGraph.transform.localPosition = new Vector3(0, 0, 0);
                var gs = combGraph.gameObject.AddComponent<Spatial.GraphSlice>();
                yield return null;
                float sliceDist = 0.005f;
                posIncr += sliceDist;
                var gpTuple = gps[0];
                Cell cell = referenceManager.cellManager.AddCell(gpTuple.Item1);
                var gp = graphGenerator.AddGraphPoint(cell, gpTuple.Item2.x, gpTuple.Item2.y, gpTuple.Item2.z);
                for (int n = 1; n < gps.Count; n++)
                {
                    gpTuple = gps[n];
                    currentCoord = gpTuple.Item2.z;
                    if (n == 1)
                    {
                        prevCoord = currentCoord;
                    }
                    // when we reach new slize (new z coord) build the graph and then start adding to a new one.
                    else if (currentCoord != prevCoord)
                    {
                        combGraph.maxCoordValues = maxCoords;
                        combGraph.minCoordValues = minCoords;
                        graphGenerator.SliceClustering();
                        while (graphGenerator.isCreating)
                        {
                            yield return null;
                        }
                        gs.zCoord = gp.WorldPosition.z;
                        //gs.AddReplacement();
                        combGraph = graphGenerator.CreateGraph(GraphGenerator.GraphType.MDS);
                        yield return null;
                        graphManager.Graphs.Add(combGraph);
                        graphManager.originalGraphs.Add(combGraph);
                        combGraph.transform.parent = parent.transform;
                        yield return null;
                        gs = combGraph.gameObject.AddComponent<Spatial.GraphSlice>();
                        gs.sliceNr = ++sliceNr;
                        gs.zCoord = posIncr;
                        posIncr += sliceDist;
                        combGraph.transform.localPosition = new Vector3(0, 0, 0);
                        combGraph.GraphName = "Slice" + sliceNr;
                        combGraph.gameObject.name = "Slice" + sliceNr;
                    }
                    else if (n == gps.Count - 1)
                    {
                        combGraph.maxCoordValues = maxCoords;
                        combGraph.minCoordValues = minCoords;
                        graphGenerator.SliceClustering();
                        while (graphGenerator.isCreating)
                        {
                            yield return null;
                        }
                        gs.zCoord = posIncr;
                        //gs.AddReplacement();
                        continue;
                    }
                    cell = referenceManager.cellManager.AddCell(gpTuple.Item1);
                    gp = graphGenerator.AddGraphPoint(cell, gpTuple.Item2.x, gpTuple.Item2.y, gpTuple.Item2.z);
                    prevCoord = currentCoord;
                }
                StartCoroutine(sg.AddSlices());
            }
            //graphGenerator.CreateMesh();
        }

        /// <summary>
        /// Reads an attribute file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        public IEnumerator ReadAttributeFilesCoroutine(string path)
        {
            // Read the each .meta.cell file
            // The file format should be
            //              TYPE_1  TYPE_2  ...
            //  CELLNAME_1  [0,1]   [0,1]
            //  CELLNAME_2  [0,1]   [0,1]
            // ...
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            string[] metacellfiles = Directory.GetFiles(path, "*.meta.cell");
            foreach (string metacellfile in metacellfiles)
            {
                FileStream metacellFileStream = new FileStream(metacellfile, FileMode.Open);
                StreamReader metacellStreamReader = new StreamReader(metacellFileStream);

                // first line is a header line
                string header = metacellStreamReader.ReadLine();
                string[] attributeTypes = header.Split(null);
                string[] actualAttributeTypes = new string[attributeTypes.Length - 1];
                for (int i = 1; i < attributeTypes.Length; ++i)
                {
                    //if (attributeTypes[i].Length > 10)
                    //{
                    //    attributeTypes[i] = attributeTypes[i].Substring(0, 10);
                    //}
                    actualAttributeTypes[i - 1] = attributeTypes[i];
                    //print(attributeTypes[i]);
                }
                int yieldCount = 0;
                while (!metacellStreamReader.EndOfStream)
                {
                    string line = metacellStreamReader.ReadLine();
                    if (line == "")
                        continue;

                    string[] words = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    string cellname = words[0];
                    for (int j = 1; j < words.Length; ++j)
                    {
                        if (words[j] == "1")
                            cellManager.AddAttribute(cellname, attributeTypes[j], (j - 1) % CellexalConfig.Config.SelectionToolColors.Length);
                    }
                    yieldCount++;
                    if (yieldCount % 500 == 0)
                        yield return null;

                }
                metacellStreamReader.Close();
                metacellFileStream.Close();
                attributeSubMenu.CreateButtons(actualAttributeTypes);
                cellManager.Attributes = actualAttributeTypes;
                if (cellManager.Attributes.Length > CellexalConfig.Config.SelectionToolColors.Length)
                {
                    CellexalError.SpawnError("Attributes", "The number of attributes are higher than the number of colours in your config." +
                        " Consider adding more colours in the settings menu (under Selection Colours)");
                }
            }
            stopwatch.Stop();
            attributeFileRead = true;
            CellexalLog.Log("read attributes in " + stopwatch.Elapsed.ToString());
        }

        private void ReadFilterFiles(string path)
        {
            string[] files = Directory.GetFiles(path, "*.fil");
            referenceManager.filterMenu.CreateButtons(files);
        }

        // public void DrawSomeLines(SQLite database)
        // {
        //     StartCoroutine(DrawSomeLinesCoroutine(database));
        // }

        // public IEnumerator DrawSomeLinesCoroutine(SQLite database)
        // {
        //     var line = Instantiate(lineprefab);
        //     Vector3[] pos = new Vector3[database._result.Count];
        //     Tuple<string, float>[] exprs = new Tuple<string, float>[database._result.Count];
        //     for (int i = 0; i < database._result.Count; ++i)
        //     {
        //         exprs[i] = (Tuple<string, float>)(database._result[i]);
        //     }
        //
        //     Array.Sort(exprs, (Tuple<string, float> x, Tuple<string, float> y) => (y.Item2.CompareTo(x.Item2)));
        //
        //
        //     var lineRenderer = line.GetComponent<LineRenderer>();
        //     lineRenderer.startColor = Color.red;
        //     lineRenderer.endColor = Color.blue;
        //     for (int i = 0; i < exprs.Length; ++i)
        //     {
        //
        //         lineRenderer.positionCount = i + 1;
        //         lineRenderer.SetPosition(i, graphManager.FindGraphPoint("DDRTree", exprs[i].Item1).transform.position);
        //     }
        //
        //     yield return null;
        // }

        /// <summary>
        /// Start the R session that will run in the background. 
        /// </summary>
        /// <param name="serverType">If you are running several sessions give a serverType name that works as a prefix so the 
        /// R session knows which file to look for.</param>
        /// <returns></returns>
        private IEnumerator StartServer(string serverType)
        {
            Process currentProcess = Process.GetCurrentProcess();
            int pid = currentProcess.Id;
            string rScriptFilePath = Application.streamingAssetsPath + @"\R\start_server.R";
            string serverName = CellexalUser.UserSpecificFolder + Path.DirectorySeparatorChar + serverType + "Server";
            string dataSourceFolder = Directory.GetCurrentDirectory() + @"\Data\" + CellexalUser.DataSourceFolder;
            string args = serverName + " " + dataSourceFolder + " " + CellexalUser.UserSpecificFolder + " " + pid;

            CellexalLog.Log("Running start server script at " + rScriptFilePath + " with the arguments " + args);
            string value = null;
            Thread t = new Thread(
            () =>
            {
                value = RScriptRunner.RunFromCmd(rScriptFilePath, args, true);
            });
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            t.Start();
            while (!File.Exists(serverName + ".pid"))
            {
                if (value != null && !value.Equals(string.Empty))
                {
                    CellexalError.SpawnError("Failed to start R Server", "Make sure you have set the correct R path in the launcher menu");
                    yield break;
                }
                yield return null;
            }

            stopwatch.Stop();
            CellexalLog.Log("Start Server finished in " + stopwatch.Elapsed.ToString());
            referenceManager.notificationManager.SpawnNotification(serverType + " R Server Session Initiated.");
            StartCoroutine(referenceManager.reportManager.LogStart());

        }

        /// <summary>
        /// To clean up server files after termination. Can be called if the user wants to start a new session (e.g. when loading a new dataset) or when exiting the program. 
        /// </summary>
        public void QuitServer()
        {
            File.Delete(CellexalUser.UserSpecificFolder + Path.DirectorySeparatorChar + "mainServer.pid");
            File.Delete(CellexalUser.UserSpecificFolder + Path.DirectorySeparatorChar + "mainServer.input.lock");
            File.Delete(CellexalUser.UserSpecificFolder + Path.DirectorySeparatorChar + "mainServer.input.R");
            //File.Delete(CellexalUser.UserSpecificFolder + Path.DirectorySeparatorChar + "geneServer.pid");
            CellexalLog.Log("Stopped Server");
        }





        /// <summary>
        /// Reads a file containing lists of genes that should be flashed.
        /// </summary>
        /// <param name="path"> The path to the file. </param>
        /// <returns> An array of categories. Each has its name at index zero, and the rest of each array is filled with the content of the category. </returns>
        public string[][] ReadFlashingGenesFiles(string path)
        {
            FileStream fileStream = new FileStream(path, FileMode.Open);
            StreamReader streamReader = new StreamReader(fileStream);
            // The file format should be
            // CATEGORY_1, CATEGORY 2  ...
            // GENE_11 ,   GENE_21
            // GENE_12 ,   GENE_22
            // ...
            // All categories and genes should be comma seperated.

            string header = streamReader.ReadLine();
            string[] words = header.Split(',');
            List<string>[] genes = new List<string>[words.Length];
            for (int i = 0; i < words.Length; ++i)
            {
                // put the gene category names at the first index of each list.
                genes[i] = new List<string>();
                genes[i].Add(words[i].Trim());
            }

            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                if (line.Length == 0)
                    continue;
                words = line.Split(',');
                for (int j = 0; j < words.Length; ++j)
                {
                    string gene = words[j].Trim();
                    if (gene != string.Empty)
                    {
                        genes[j].Add(gene.ToLower());
                    }
                }
            }

            string[][] result = new string[genes.Length][];
            for (int i = 0; i < words.Length; ++i)
            {
                result[i] = genes[i].ToArray();
            }

            streamReader.Close();
            fileStream.Close();
            return result;
        }

        /// <summary>
        /// Reads the index.facs file.
        /// </summary>
        private void ReadFacsFiles(string path, int nbrOfCells)
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            string fullpath = path + Path.DirectorySeparatorChar + "index.facs";

            if (!File.Exists(fullpath))
            {
                print("File " + fullpath + " not found");
                CellexalLog.Log(".facs file not found");
                return;
            }

            FileStream fileStream = new FileStream(fullpath, FileMode.Open);
            StreamReader streamReader = new StreamReader(fileStream);

            // The file format should be:
            //             TYPE_1  TYPE_2 ...
            // CELLNAME_1  VALUE   VALUE  
            // CELLNAME_2  VALUE   VALUE
            // ...

            string headerline = streamReader.ReadLine();
            if (headerline == null)
            {
                // empty file
                CellexalLog.Log("Empty index.facs file");
                return;
            }
            string[] header = headerline.Split(new string[] { "\t", " " }, StringSplitOptions.RemoveEmptyEntries);
            float[] min = new float[header.Length];
            float[] max = new float[header.Length];
            string[] values = new string[header.Length + 1];
            int i = 0;
            for (; i < min.Length; ++i)
            {
                min[i] = float.MaxValue;
                max[i] = float.MinValue;
            }

            // string[] cellnames = new string[nbrOfCells];
            // float[,] values = new float[nbrOfCells, header.Length];

            // read the file, calculate the min and max values and save all values
            for (i = 0; !streamReader.EndOfStream; ++i)
            {
                string line = streamReader.ReadLine();
                SplitValues(line, ref values, separators);
                string cellName = values[0];
                for (int j = 0; j < values.Length - 1; ++j)
                {

                    float value = float.Parse(values[j + 1]);
                    if (value < min[j])
                        min[j] = value;
                    if (value > max[j])
                        max[j] = value;

                }
                for (int j = 0; j < values.Length - 1; ++j)
                {
                    // normalize to the range [0, 29]
                    cellManager.AddFacs(cellName, header[j], float.Parse(values[j + 1]));
                    cellManager.AddFacsValue(cellName, header[j], values[j + 1]);
                    //print(values[j + 1]);
                }
            }
            // set the min and max values
            for (i = 0; i < header.Length; ++i)
            {
                cellManager.FacsRanges[header[i].ToLower()] = new Tuple<float, float>(min[i], max[i]);
            }

            streamReader.Close();
            fileStream.Close();
            indexMenu.CreateButtons(header);
            createFromMarkerMenu.CreateButtons(header);
            cellManager.Facs = header;
            CellexalLog.Log("Successfully read " + CellexalLog.FixFilePath(fullpath));

            stopwatch.Stop();
            return;
        }

        private void SplitValues(string line, ref string[] values, char[] seperators)
        {
            int charIndex = 0;
            for (int i = 0; i < values.Length; ++i)
            {
                int nextSeperator = line.IndexOfAny(seperators, charIndex);
                if (nextSeperator >= 0)
                {
                    values[i] = line.Substring(charIndex, nextSeperator - charIndex);
                }
                else
                {
                    values[i] = line.Substring(charIndex);
                }
                charIndex = nextSeperator + 1;
            }
        }

        /// <summary>
        /// Helper struct for sorting network keys.
        /// </summary>
        public struct NetworkKeyPair
        {
            public string color, node1, node2, key1, key2;

            public NetworkKeyPair(string c, string n1, string n2, string k1, string k2)
            {
                color = c;
                key1 = k1;
                key2 = k2;
                node1 = n1;
                node2 = n2;
            }
        }

        /// <summary>
        /// Reads the files containg networks.
        /// </summary>
        public void ReadNetworkFiles(int layoutSeed)
        {
            StartCoroutine(ReadNetworkFilesCoroutine(layoutSeed));
        }
        private IEnumerator ReadNetworkFilesCoroutine(int layoutSeed)
        {
            CellexalLog.Log("Started reading network files");
            CellexalEvents.ScriptRunning.Invoke();
            string networkDirectory = CellexalUser.UserSpecificFolder + @"\Resources\Networks";
            if (!Directory.Exists(networkDirectory))
            {
                print(string.Format("No network directory found at {0}, make sure the network generating r script has executed properly.", CellexalLog.FixFilePath(networkDirectory)));
                CellexalError.SpawnError("Error when generating networks", string.Format("No network directory found at {0}, make sure the network generating r script has executed properly.", CellexalLog.FixFilePath(networkDirectory)));
                CellexalEvents.CommandFinished.Invoke(false);
                yield break;
            }
            string[] cntFilePaths = Directory.GetFiles(networkDirectory, "*.cnt");
            string[] nwkFilePaths = Directory.GetFiles(networkDirectory, "*.nwk");

            // make sure there is a .cnt file
            if (cntFilePaths.Length == 0)
            {
                //status.ShowStatusForTime("No .cnt file found. This dataset probably does not have a correct database", 10f, UnityEngine.Color.red);
                //statusDisplayHUD.ShowStatusForTime("No .cnt file found. This dataset probably does not have a correct database", 10f, UnityEngine.Color.red);
                //statusDisplayFar.ShowStatusForTime("No .cnt file found. This dataset probably does not have a correct database", 10f, UnityEngine.Color.red);
                CellexalError.SpawnError("Error when generating networks", string.Format("No .cnt file found at {0}, make sure the network generating r script has executed properly by checking the r_log.txt in the output folder.", CellexalLog.FixFilePath(networkDirectory)));
                CellexalEvents.CommandFinished.Invoke(false);
                yield break;
            }

            if (cntFilePaths.Length > 1)
            {
                CellexalError.SpawnError("Error when generating networks", string.Format("More than one .cnt file found at {0}, make sure the network generating r script has executed properly by checking the r_log.txt in the output folder.", CellexalLog.FixFilePath(networkDirectory)));
                CellexalEvents.CommandFinished.Invoke(false);
                yield break;
            }

            FileStream cntFileStream = new FileStream(cntFilePaths[0], FileMode.Open);
            StreamReader cntStreamReader = new StreamReader(cntFileStream);

            // make sure there is a .nwk file
            if (nwkFilePaths.Length == 0)
            {
                CellexalError.SpawnError("Error when generating networks", string.Format("No .nwk file found at {0}, make sure the network generating r script has executed properly by checking the r_log.txt in the output folder.", CellexalLog.FixFilePath(networkDirectory)));
                CellexalEvents.CommandFinished.Invoke(false);
                yield break;
            }
            FileStream nwkFileStream = new FileStream(nwkFilePaths[0], FileMode.Open);
            StreamReader nwkStreamReader = new StreamReader(nwkFileStream);
            // 1 MB = 1048576 B
            if (nwkFileStream.Length > 1048576)
            {
                CellexalError.SpawnError("Error when generating networks", string.Format(".nwk file is larger than 1 MB. .nwk file size: {0} B", nwkFileStream.Length));
                nwkStreamReader.Close();
                nwkFileStream.Close();
                CellexalEvents.CommandFinished.Invoke(false);
                yield break;
            }


            // Read the .cnt file
            // The file format should be
            //  X_COORD Y_COORD Z_COORD KEY GRAPHNAME
            //  X_COORD Y_COORD Z_COORD KEY GRAPHNAME
            // ...
            // KEY is simply a hex rgb color code
            // GRAPHNAME is the name of the file (and graph) that the network was made from

            // read the graph's name and create a skeleton
            bool firstLine = true;
            Dictionary<string, NetworkCenter> networks = new Dictionary<string, NetworkCenter>();
            // these variables are set when the first line is read
            Graph graph = null;
            GameObject skeleton = null;
            NetworkHandler networkHandler = null;
            string networkHandlerName = null;

            Dictionary<string, float> maxNegPcor = new Dictionary<string, float>();
            Dictionary<string, float> minNegPcor = new Dictionary<string, float>();
            Dictionary<string, float> maxPosPcor = new Dictionary<string, float>();
            Dictionary<string, float> minPosPcor = new Dictionary<string, float>();

            while (!cntStreamReader.EndOfStream)
            {
                string line = cntStreamReader.ReadLine();
                if (line == "")
                    continue;
                string[] words = line.Split(null);

                if (firstLine)
                {
                    firstLine = false;
                    string graphName = words[words.Length - 1];
                    graph = graphManager.FindGraph(graphName);
                    if (graph == null)
                    {
                        CellexalError.SpawnError("Error when generating networks", string.Format("Could not find the graph named {0} when trying to create a convex hull, make sure there is a .mds and .hull file with the same name in the dataset.", graphName));
                        CellexalEvents.CommandFinished.Invoke(false);
                        yield break;
                    }

                    StartCoroutine(graph.CreateGraphSkeleton(false));
                    while (!graph.convexHull.activeSelf)
                    {
                        yield return null;
                    }

                    skeleton = graph.convexHull;
                    if (skeleton == null)
                    {
                        CellexalError.SpawnError("Error when generating networks", string.Format("Could not create a convex hull for the graph named {0}, this could be because the convex hull file is incorrect", graphName));
                        CellexalEvents.CommandFinished.Invoke(false);
                        yield break;
                    }
                    CellexalLog.Log("Successfully created convex hull of " + graphName);
                    networkHandler = skeleton.GetComponent<NetworkHandler>();
                    foreach (BoxCollider graphCollider in graph.GetComponents<BoxCollider>())
                    {
                        BoxCollider newCollider = networkHandler.gameObject.AddComponent<BoxCollider>();
                        newCollider.center = graphCollider.center;
                        newCollider.size = graphCollider.size;
                    }
                    networkHandlerName = "NetworkHandler_" + graphName + "-" + (selectionManager.fileCreationCtr + 1);
                    networkHandler.name = networkHandlerName;
                }
                float x = float.Parse(words[0]);
                float y = float.Parse(words[1]);
                float z = float.Parse(words[2]);
                // the color is a hex string e.g. #FF0099
                UnityEngine.Color color = new UnityEngine.Color();
                string colorString = words[3];
                ColorUtility.TryParseHtmlString(colorString, out color);

                maxPosPcor[colorString] = 0f;
                minPosPcor[colorString] = float.MaxValue;
                maxNegPcor[colorString] = float.MinValue;
                minNegPcor[colorString] = 0f;
                Vector3 position = graph.ScaleCoordinates(new Vector3(x, y, z));
                NetworkCenter network = networkGenerator.CreateNetworkCenter(networkHandler, colorString, position, layoutSeed);
                foreach (Renderer r in network.GetComponentsInChildren<Renderer>())
                {
                    if (r.gameObject.GetComponent<CellexalButton>() == null)
                    {
                        r.material.color = color;
                    }
                }
                networks[colorString] = network;
            }

            CellexalLog.Log("Successfully read .cnt file");

            // Read the .nwk file
            // The file format should be
            //  PCOR    NODE_1  NODE_2  PVAL    QVAL    PROB    GRPS[I] KEY_1   KEY_2
            //  VALUE   STRING  STRING  VALUE   VALUE   VALUE   HEX_RGB STRING  STRING
            //  VALUE   STRING  STRING  VALUE   VALUE   VALUE   HEX_RGB STRING  STRING
            //  ...
            // We only care about NODE_1, NODE_2, GRPS[I], KEY_1 and KEY_2
            // NODE_1 and NODE_2 are two genenames that should be linked together.
            // GRPS[I] is the network the two genes are in. A gene can be in multiple networks.
            // KEY_1 is the two genenames concatenated together as NODE_1 + NODE_2
            // KEY_2 is the two genenames concatenated together as NODE_2 + NODE_1

            CellexalLog.Log("Reading .nwk file with " + nwkFileStream.Length + " bytes");
            Dictionary<string, NetworkNode> nodes = new Dictionary<string, NetworkNode>(1024);
            List<NetworkKeyPair> tmp = new List<NetworkKeyPair>();
            // skip the first line as it is a header
            nwkStreamReader.ReadLine();

            while (!nwkStreamReader.EndOfStream)
            {
                string line = nwkStreamReader.ReadLine();
                if (line == "")
                    continue;
                if (line[0] == '#')
                    continue;
                string[] words = line.Split(null);
                string color = words[6];
                string geneName1 = words[1];
                string node1 = geneName1 + color;
                string geneName2 = words[2];
                string node2 = geneName2 + color;
                string key1 = words[7];
                string key2 = words[8];
                float pcor = float.Parse(words[0]);

                if (geneName1 == geneName2)
                {
                    CellexalError.SpawnError("Error in networkfiles", "Gene \'" + geneName1 + "\' cannot be correlated to itself in file " + nwkFilePaths[0]);
                    cntStreamReader.Close();
                    cntFileStream.Close();
                    nwkStreamReader.Close();
                    nwkFileStream.Close();
                    CellexalEvents.CommandFinished.Invoke(false);
                    yield break;
                }

                if (pcor < 0)
                {
                    if (pcor < minNegPcor[color])
                        minNegPcor[color] = pcor;
                    if (pcor > maxNegPcor[color])
                        maxNegPcor[color] = pcor;
                }
                else
                {
                    if (pcor < minPosPcor[color])
                        minPosPcor[color] = pcor;
                    if (pcor > maxPosPcor[color])
                        maxPosPcor[color] = pcor;
                }
                // add the nodes if they don't already exist
                if (!nodes.ContainsKey(node1))
                {
                    NetworkNode newNode = networkGenerator.CreateNetworkNode(geneName1, networks[color]);
                    nodes[node1] = newNode;
                }

                if (!nodes.ContainsKey(node2))
                {
                    NetworkNode newNode = networkGenerator.CreateNetworkNode(geneName2, networks[color]);
                    nodes[node2] = newNode;
                }

                Transform parentNetwork = networks[words[6]].transform;
                nodes[node1].transform.parent = parentNetwork;
                nodes[node2].transform.parent = parentNetwork;

                // add a bidirectional connection
                nodes[node1].AddNeighbour(nodes[node2], pcor);
                // add the keypair
                tmp.Add(new NetworkKeyPair(color, node1, node2, key1, key2));

            }
            nwkStreamReader.Close();
            nwkFileStream.Close();
            CellexalLog.Log("Successfully read .nwk file");
            NetworkKeyPair[] keyPairs = new NetworkKeyPair[tmp.Count];
            tmp.CopyTo(keyPairs);
            // sort the array of keypairs
            // if two keypairs are equal (they both contain the same key), they should be next to each other in the list, otherwise sort based on key1
            Array.Sort(keyPairs, (NetworkKeyPair x, NetworkKeyPair y) => x.key1.Equals(y.key2) ? 0 : x.key1.CompareTo(y.key1));

            //yield return null;
            networkHandler.CalculateLayoutOnAllNetworks();

            // wait for all networks to finish their layout
            while (networkHandler.layoutApplied != networks.Count)
                yield return null;

            foreach (var network in networks)
            {
                network.Value.MaxPosPcor = maxPosPcor[network.Key];
                network.Value.MinPosPcor = minPosPcor[network.Key];
                network.Value.MaxNegPcor = maxNegPcor[network.Key];
                network.Value.MinNegPcor = minNegPcor[network.Key];
            }

            foreach (var node in nodes.Values)
            {
                node.ColorEdges();
            }
            yield return null;
            // give all nodes in the networks edges
            networkHandler.CreateArcs(ref keyPairs, ref nodes);


            cntStreamReader.Close();
            cntFileStream.Close();
            nwkStreamReader.Close();
            nwkFileStream.Close();
            CellexalLog.Log("Successfully created " + networks.Count + " networks with a total of " + nodes.Values.Count + " nodes");
            CellexalEvents.CommandFinished.Invoke(true);
            CellexalEvents.ScriptFinished.Invoke();
            networkHandler.CreateNetworkAnimation(graph.transform);
        }

        /// <summary>
        /// Read all the user.group files which cointains the grouping information from previous sessions.
        /// </summary>
        public void LoadPreviousGroupings()
        {
            string dataFolder = CellexalUser.UserSpecificFolder;
            string groupingsInfoFile = dataFolder + Path.DirectorySeparatorChar + "groupings_info.txt";
            CellexalLog.Log("Started reading the previous groupings files");
            //print(groupingsInfoFile);
            if (!File.Exists(groupingsInfoFile))
            {
                CellexalLog.Log("WARNING: No groupings info file found at " + CellexalLog.FixFilePath(groupingsInfoFile));
                return;
            }
            FileStream fileStream = new FileStream(groupingsInfoFile, FileMode.Open);
            StreamReader streamReader = new StreamReader(fileStream);
            // skip the header
            List<string> groupingNames = new List<string>();
            List<int> fileLengths = new List<int>();
            string line = "";
            string[] words = null;
            while (!streamReader.EndOfStream)
            {
                line = streamReader.ReadLine();
                if (line == "") continue;
                words = line.Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // set the grouping's name to [the grouping's number]\n[number of colors in grouping]\n[number of cells in groupings]
                string groupingName = words[0];
                int indexOfLastDot = groupingName.LastIndexOf(".");
                if (indexOfLastDot == -1)
                {
                    CellexalLog.Log("WARNING: Could not find \'.\' in \"" + words[0] + "\"");
                    indexOfLastDot = groupingName.Length - 1;
                }
                //string groupingNumber = groupingName.Substring(indexOfLastDot, groupingName.Length - indexOfLastDot);
                groupingNames.Add(groupingName + "\n" + words[1] + "\n" + words[2]);
                fileLengths.Add(int.Parse(words[2]));
            }
            streamReader.Close();
            fileStream.Close();

            CellexalLog.Log("Reading " + groupingNames.Count + " files");
            // initialize the arrays
            string[][] cellNames = new string[groupingNames.Count][];
            int[][] groups = new int[groupingNames.Count][];
            string[] graphNames = new string[groupingNames.Count];
            Dictionary<int, UnityEngine.Color>[] groupingColors = new Dictionary<int, UnityEngine.Color>[groupingNames.Count];
            for (int i = 0; i < cellNames.Length; ++i)
            {
                cellNames[i] = new string[fileLengths[i]];
                groups[i] = new int[fileLengths[i]];
            }
            for (int i = 0; i < groupingNames.Count; ++i)
            {
                groupingColors[i] = new Dictionary<int, UnityEngine.Color>();
            }
            words = null;
            string[] files = Directory.GetFiles(dataFolder, "*.cgr");
            for (int i = 0; i < fileLengths.Count; ++i)
            {
                string file = files[i];
                fileStream = new FileStream(file, FileMode.Open);
                streamReader = new StreamReader(fileStream);

                for (int j = 0; j < fileLengths[i]; ++j)
                {
                    line = streamReader.ReadLine();
                    print(line);
                    words = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    cellNames[i][j] = words[0];

                    try
                    {
                        int group = int.Parse(words[3]);
                        groups[i][j] = group;
                        UnityEngine.Color groupColor;
                        ColorUtility.TryParseHtmlString(words[1], out groupColor);
                        groupingColors[i][group] = groupColor;
                    }
                    catch (FormatException e)
                    {
                        foreach (string s in words)
                        {
                            print(s);
                        }
                        print(words[3] + " " + file + " " + j + "\n" + e.StackTrace);
                        streamReader.Close();
                        fileStream.Close();
                        return;
                    }

                }
                graphNames[i] = words[2];
                streamReader.Close();
                fileStream.Close();
            }
            referenceManager.selectionFromPreviousMenu.SelectionFromPreviousButton(graphNames, groupingNames.ToArray(), cellNames, groups, groupingColors);
            CellexalLog.Log("Successfully read " + groupingNames.Count + " files");
        }

        [ConsoleCommand("inputReader", folder: "Output" , aliases: new string[] { "readselectionfile", "rsf" })]
        public void ReadPreviousSelection(string selectionFile)
        {
            selectionFile = "Output" + Path.DirectorySeparatorChar  + selectionFile;
            CellexalLog.Log("Loading old selection from file " + selectionFile);

            RegisterOldGroup(selectionFile);

        }


        [ConsoleCommand("inputReader", aliases: new string[] { "selectfromprevious", "sfp" })]
        public void ReadAndSelectPreviousSelection(int index)
        {
            string dataFolder = CellexalUser.UserSpecificFolder;
            string[] files = Directory.GetFiles(dataFolder, "selection*.txt");
            if (files.Length == 0)
            {
                CellexalLog.Log("No previous selections found.");
                CellexalEvents.CommandFinished.Invoke(false);
                return;
            }
            else if (index < 0 || index >= files.Length)
            {
                CellexalLog.Log(string.Format("Index \'{0}\' is not within the range [0, {1}] when reading previous selection files.", index, files.Length - 1));
                CellexalEvents.CommandFinished.Invoke(false);
                return;
            }
            RegisterOldGroup(files[index]);
        }

        public void RegisterOldGroup(string file)
        {

            FileStream fileStream = new FileStream(file, FileMode.Open);
            StreamReader streamReader = new StreamReader(fileStream);
            var selectionManager = referenceManager.selectionManager;
            GraphManager graphManager = referenceManager.graphManager;
            int numPointsAdded = 0;
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                string[] words = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                int group = 0;
                UnityEngine.Color groupColor;

                try
                {
                    group = int.Parse(words[3]);
                    ColorUtility.TryParseHtmlString(words[1], out groupColor);
                }
                catch (FormatException)
                {
                    CellexalLog.Log(string.Format("Bad color on line {0} in file {1}.", numPointsAdded + 1, file));
                    streamReader.Close();
                    fileStream.Close();
                    CellexalEvents.CommandFinished.Invoke(false);
                    return;
                }
                selectionManager.AddGraphpointToSelection(graphManager.FindGraphPoint(words[2], words[0]), group, false, groupColor);
                numPointsAdded++;
            }
            CellexalLog.Log(string.Format("Added {0} points to selection", numPointsAdded));
            CellexalEvents.CommandFinished.Invoke(true);
            streamReader.Close();
            fileStream.Close();
            selectionManager.DumpAnnotatedSelectionToTextFile();
        }
        //[ConsoleCommand("inputReader", aliases: new string[] { "selectfromprevious", "sfp" })]
        //public void ReadAndSelectPreviousSelection(int index)
        //{
        //    string dataFolder = CellexalUser.UserSpecificFolder;
        //    string[] files = Directory.GetFiles(dataFolder, "selection*.txt");
        //    if (files.Length == 0)
        //    {
        //        CellexalLog.Log("No previous selections found.");
        //        CellexalEvents.CommandFinished.Invoke(false);
        //        return;
        //    }
        //    else if (index < 0 || index >= files.Length)
        //    {
        //        CellexalLog.Log(string.Format("Index \'{0}\' is not within the range [0, {1}] when reading previous selection files.", index, files.Length - 1));
        //        CellexalEvents.CommandFinished.Invoke(false);
        //        return;
        //    }

        //    FileStream fileStream = new FileStream(files[index], FileMode.Open);
        //    StreamReader streamReader = new StreamReader(fileStream);
        //    var selectionManager = referenceManager.selectionManager;
        //    GraphManager graphManager = referenceManager.graphManager;
        //    int numPointsAdded = 0;
        //    while (!streamReader.EndOfStream)
        //    {
        //        string line = streamReader.ReadLine();
        //        string[] words = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        //        int group = 0;
        //        UnityEngine.Color groupColor;

        //        try
        //        {
        //            group = int.Parse(words[3]);
        //            ColorUtility.TryParseHtmlString(words[1], out groupColor);
        //        }
        //        catch (FormatException)
        //        {
        //            CellexalLog.Log(string.Format("Bad color on line {0} in file {1}.", numPointsAdded + 1, files[index]));
        //            streamReader.Close();
        //            fileStream.Close();
        //            CellexalEvents.CommandFinished.Invoke(false);
        //            return;
        //        }
        //        selectionManager.AddGraphpointToSelection(graphManager.FindGraphPoint(words[2], words[0]), group, false, groupColor);
        //        numPointsAdded++;
        //    }
        //    CellexalLog.Log(string.Format("Added {0} points to selection", numPointsAdded));
        //    CellexalEvents.CommandFinished.Invoke(true);
        //    streamReader.Close();
        //    fileStream.Close();
        //}

        /// <summary>
        /// Sorts a list of gene expressions based on the mean expression level.
        /// </summary>
        /// <param name="genes"> An array of genes to be sorted. This array will be reordered but the elements won't be modified. </param>
        /// <param name="expressions"> An array of arrays containing the expressions.</param>
        /// <remarks> The expressions parameter should be set up as follows: the first dimension should be the genes and the second the cells, i.e. looking for a certain expression is done via <code>expressions[geneIndex][cellIndex]</code></remarks>
        public void SortGenesMeanExpression(ref string[] genes, ref int[][] expressions)
        {
            StringFloatPair[] pairList = new StringFloatPair[genes.Length];

            for (int i = 0; i < expressions.Length; ++i)
            {
                float meanExpressions = 0;
                for (int j = 0; j < expressions[i].Length; ++j)
                {
                    meanExpressions += expressions[i][j];
                }
                pairList[i] = new StringFloatPair(genes[i], meanExpressions + expressions[i].Length);
            }
            // sort based on mean expressions, will also sort the expression matrix accordingly
            Array.Sort(pairList, expressions);
            for (int i = 0; i < pairList.Length; ++i)
            {
                genes[i] = pairList[i].s;
            }
        }

        /// <summary>
        /// Helper class to sort a list of genes based on mean gene expression.
        /// </summary>
        private class StringFloatPair : IComparable<StringFloatPair>
        {
            public string s;
            public float f;

            public StringFloatPair(string s, float f)
            {
                this.s = s;
                this.f = f;
            }

            public int CompareTo(StringFloatPair other)
            {
                return f.CompareTo(other.f);
            }
        }



    }
}