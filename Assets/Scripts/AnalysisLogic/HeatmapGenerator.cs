using CellexalVR.AnalysisObjects;
using CellexalVR.DesktopUI;
using CellexalVR.Extensions;
using CellexalVR.General;
using CellexalVR.Menu.Buttons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace CellexalVR.AnalysisLogic
{
    /// <summary>
    /// A generator for heatmaps. Creates the colors that are later used when generating heatmaps
    /// </summary>
    public class HeatmapGenerator : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public GameObject heatmapPrefab;
        public Texture2D heatmapTexture;
        public int selectionNr;
        public int heatmapsCreated = 0;
        public bool GeneratingHeatmaps { get; private set; }

        public SolidBrush[] expressionColors;


        //private GameObject calculatorCluster;
        public SelectionManager selectionManager;
        private ArrayList data;
        private Thread t;
        private SteamVR_Controller.Device device;
        private Vector3 heatmapPosition;
        private List<Heatmap> heatmapList = new List<Heatmap>();
        private string statsMethod;
        private System.Drawing.Font geneFont;
        private int nrOfGenes = 250;

        public UnityEngine.Color HighlightMarkerColor { get; private set; }
        public UnityEngine.Color ConfirmMarkerColor { get; private set; }

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        [ConsoleCommand("heatmapGenerator", folder: "Output", aliases: new string[] {"loadheatmapfile", "lhf"})]
        public void LoadHeatmap(string heatmapName, string fromSelectionFile = "")
        {
            string[] words = heatmapName.Split(Path.DirectorySeparatorChar);
            string heatmapNameLastPart = words[words.Length - 1];
            string directory = (CellexalUser.UserSpecificFolder + "/Heatmap/" + heatmapNameLastPart).FixFilePath();
            CellexalLog.Log("Loading old heatmap from file " + directory);
            // the important parts in the GenerateHeatmapRoutine to            build the heatmap ? !
            Heatmap heatmap = Instantiate(heatmapPrefab).GetComponent<Heatmap>();

            List<Graph.GraphPoint> selection = referenceManager.inputReader.ReadSelectionFile(fromSelectionFile, false);
            heatmap.Init();
            heatmap.transform.parent = transform;
            heatmap.transform.localPosition = heatmapPosition;
            heatmap.selectionNr = selectionNr;
            heatmapList.Add(heatmap);
            heatmap.directory = directory;
            heatmap.selectionFile = fromSelectionFile;
            BuildTexture(selection, heatmapName, heatmap);
            heatmapNameLastPart = heatmapNameLastPart.Replace(".txt", "");
            GameObject existingHeatmap = GameObject.Find(heatmapNameLastPart);
            while (existingHeatmap != null)
            {
                heatmapNameLastPart += "_Copy";
                existingHeatmap = GameObject.Find(heatmapNameLastPart);
            }

            heatmap.gameObject.name = heatmapNameLastPart;
            heatmap.highlightQuad.GetComponent<Renderer>().material.color = HighlightMarkerColor;
            heatmap.confirmQuad.GetComponent<Renderer>().material.color = ConfirmMarkerColor;
        }

        void Awake()
        {
            t = null;
            heatmapPosition = heatmapPrefab.transform.position;
            selectionManager = referenceManager.selectionManager;
            //status = referenceManager.statusDisplay;
            //statusDisplayHUD = referenceManager.statusDisplayHUD;
            //statusDisplayFar = referenceManager.statusDisplayFar;
            //calculatorCluster = referenceManager.calculatorCluster;
            //calculatorCluster.SetActive(false);
            GeneratingHeatmaps = false;
            CellexalEvents.ConfigLoaded.AddListener(InitColors);
        }

        /// <summary>
        /// Initializes <see cref="expressionColors"/> with the colors in the config file.
        /// </summary>
        public void InitColors()
        {
            int numberOfExpressionColors = CellexalConfig.Config.NumberOfHeatmapColors;
            expressionColors = new SolidBrush[numberOfExpressionColors];
            UnityEngine.Color low = CellexalConfig.Config.HeatmapLowExpressionColor;
            UnityEngine.Color mid = CellexalConfig.Config.HeatmapMidExpressionColor;
            UnityEngine.Color high = CellexalConfig.Config.HeatmapHighExpressionColor;
            //print(low + " " + mid + " " + high);

            int dividerLowMid = numberOfExpressionColors / 2;
            if (dividerLowMid == 0)
                dividerLowMid = 1;
            float lowMidDeltaR = (mid.r * mid.r - low.r * low.r) / dividerLowMid;
            float lowMidDeltaG = (mid.g * mid.g - low.g * low.g) / dividerLowMid;
            float lowMidDeltaB = (mid.b * mid.b - low.b * low.b) / dividerLowMid;

            int dividerMidHigh = numberOfExpressionColors - dividerLowMid - 1;
            if (dividerMidHigh == 0)
                dividerMidHigh = 1;
            float midHighDeltaR = (high.r * high.r - mid.r * mid.r) / dividerMidHigh;
            float midHighDeltaG = (high.g * high.g - mid.g * mid.g) / dividerMidHigh;
            float midHighDeltaB = (high.b * high.b - mid.b * mid.b) / dividerMidHigh;
            //print(midHighDeltaR + " " + midHighDeltaG + " " + midHighDeltaB);

            for (int i = 0; i < numberOfExpressionColors / 2 + 1; ++i)
            {
                float r = low.r * low.r + lowMidDeltaR * i;
                float g = low.g * low.g + lowMidDeltaG * i;
                float b = low.b * low.b + lowMidDeltaB * i;
                if (r < 0) r = 0;
                if (g < 0) g = 0;
                if (b < 0) b = 0;
                expressionColors[i] = new SolidBrush(System.Drawing.Color.FromArgb((int) (Mathf.Sqrt(r) * 255),
                    (int) (Mathf.Sqrt(g) * 255), (int) (Mathf.Sqrt(b) * 255)));
            }

            for (int i = numberOfExpressionColors / 2 + 1, j = 1; i < numberOfExpressionColors; ++i, ++j)
            {
                float r = mid.r * mid.r + midHighDeltaR * j;
                float g = mid.g * mid.g + midHighDeltaG * j;
                float b = mid.b * mid.b + midHighDeltaB * j;
                if (r < 0) r = 0;
                if (g < 0) g = 0;
                if (b < 0) b = 0;
                expressionColors[i] = new SolidBrush(System.Drawing.Color.FromArgb((int) (Mathf.Sqrt(r) * 255),
                    (int) (Mathf.Sqrt(g) * 255), (int) (Mathf.Sqrt(b) * 255)));
            }

            HighlightMarkerColor = CellexalConfig.Config.HeatmapHighlightMarkerColor;
            ConfirmMarkerColor = CellexalConfig.Config.HeatmapConfirmMarkerColor;
        }

        /// <summary>
        /// Removes all heatmaps.
        /// </summary>
        internal void DeleteHeatmaps()
        {
            foreach (Heatmap h in heatmapList)
            {
                if (h != null)
                {
                    Destroy(h.gameObject);
                }
            }

            heatmapList.Clear();
        }

        [ConsoleCommand("heatmapGenerator", aliases: new string[] {"generateheatmap", "gh"})]
        public void CreateHeatmap()
        {
            statsMethod = CellexalConfig.Config.HeatmapAlgorithm;
            CellexalLog.Log("Creating heatmap");
            CellexalEvents.CreatingHeatmap.Invoke();
            string heatmapName = "heatmap_" + System.DateTime.Now.ToString("HH-mm-ss");
            StartCoroutine(GenerateHeatmapRoutine(heatmapName));
        }

        /// <summary>
        /// Creates a new heatmap using the last confirmed selection.
        /// </summary>
        /// <param name="heatmapName">If created via multiplayer. Name it the same as on other client.</param>
        public void CreateHeatmap(string heatmapName = "")
        {
            // if (selectionManager.groupCount < 2)
            // {
            //     CellexalLog.Log("Could not create heatmap. Not enough groups selected to create a heatmap");
            //     CellexalError.SpawnError("Could not create heatmap", "Not enough groups selected to create a heatmap");
            //     return;
            // }

            // name the heatmap "heatmap_X". Where X is some number.
            if (heatmapName.Equals(string.Empty))
            {
                heatmapName = "heatmap_" + System.DateTime.Now.ToString("HH-mm-ss");
                referenceManager.multiuserMessageSender.SendMessageCreateHeatmap(heatmapName);
            }

            statsMethod = CellexalConfig.Config.HeatmapAlgorithm;
            CellexalLog.Log("Creating heatmap");
            CellexalEvents.CreatingHeatmap.Invoke();
            StartCoroutine(GenerateHeatmapRoutine(heatmapName));
        }

        /// <summary>
        /// Removes one heatmap.
        /// </summary>
        /// <param name="heatmapName">The heatmap's name.</param>
        public void DeleteHeatmap(string heatmapName)
        {
            Heatmap heatmap = FindHeatmap(heatmapName);
            heatmapList.Remove(heatmap);
            heatmap.DeleteHeatmap();
        }

        /// <summary>
        /// Finds a heatmap.
        /// </summary>
        /// <param name="heatmapName">The heatmap's name.</param>
        /// <returns>The heatmap, or null if no heatmap by that name exists.</returns>
        public Heatmap FindHeatmap(string heatmapName)
        {
            return heatmapList.Find((Heatmap h) => h != null ? heatmapName == h.name : false);
        }

        /// <summary>
        /// Highlights gene in genelist in all the heatmaps if it is there.
        /// </summary>
        /// <param name="geneName">Name of the gene to be highlighted.</param>
        public void HighLightGene(string geneName)
        {
            foreach (Heatmap hm in heatmapList)
            {
                hm.HighLightGene(geneName);
            }
        }


        /// <summary>
        /// Coroutine for creating a heatmap.
        /// </summary>
        private IEnumerator GenerateHeatmapRoutine(string heatmapName)
        {
            GeneratingHeatmaps = true;
            // Show calculators and floor pulse
            //calculatorCluster.SetActive(true);
            referenceManager.floor.StartPulse();

            // Check if more than one cell is selected
            while (selectionManager.RObjectUpdating)
            {
                yield return null;
            }

            List<Graph.GraphPoint> selection = selectionManager.GetLastSelection();

            if (selection.Count < 1)
            {
                CellexalLog.Log("can not create heatmap with less than 1 graphpoints, aborting");
                if (!(referenceManager.networkGenerator.GeneratingNetworks &&
                      File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.R")))
                {
                    referenceManager.floor.StopPulse();
                }

                //if (!referenceManager.networkGenerator.GeneratingNetworks)
                //    referenceManager.calculatorCluster.SetActive(false);
                referenceManager.notificationManager.SpawnNotification("Heatmap generation failed.");
                yield break;
            }

            //string function = "make.cellexalvr.heatmap.list";
            string objectPath = (CellexalUser.UserSpecificFolder + "\\cellexalObj.RData").UnFixFilePath();
            string groupingFilepath =
                (CellexalUser.UserSpecificFolder + "\\selection" + (selectionManager.fileCreationCtr - 1) + ".txt")
                .UnFixFilePath();
            string topGenesNr = nrOfGenes.ToString();
            string heatmapDirectory = (CellexalUser.UserSpecificFolder + @"\Heatmap").UnFixFilePath();
            string outputFilePath = (heatmapDirectory + @"\\" + heatmapName + ".txt");
            string statsMethod = CellexalConfig.Config.HeatmapAlgorithm;
            //string args = "cellexalObj" + ", \"" + groupingFilepath + "\", " + topGenesNr + ", \"" + outputFilePath + "\", \"" + statsMethod + "\"";
            string args = CellexalUser.UserSpecificFolder + " " + groupingFilepath + " " + topGenesNr + " " +
                          outputFilePath + " " + statsMethod;

            string rScriptFilePath = (Application.streamingAssetsPath + @"\R\make_heatmap.R").FixFilePath();

            //string script = function + "(" + args + ")";

            if (!Directory.Exists(heatmapDirectory))
            {
                CellexalLog.Log("Creating directory " + heatmapDirectory.FixFilePath());
                Directory.CreateDirectory(heatmapDirectory);
            }

            bool rServerReady = File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.pid") &&
                                !File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.R") &&
                                !File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.lock");
            while (!rServerReady || !RScriptRunner.serverIdle)
            {
                rServerReady = File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.pid") &&
                               !File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.R") &&
                               !File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.lock");
                yield return null;
            }

            //t = new Thread(() => RScriptRunner.RunScript(script));
            t = new Thread(() => RScriptRunner.RunRScript(rScriptFilePath, args));
            t.Start();
            CellexalLog.Log("Running R function " + rScriptFilePath + " with the arguments: " + args);
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            while (t.IsAlive || File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.R"))
            {
                yield return null;
            }

            stopwatch.Stop();
            CellexalLog.Log("Heatmap R script finished in " + stopwatch.Elapsed.ToString());

            GeneratingHeatmaps = false;

            var heatmap = Instantiate(heatmapPrefab).GetComponent<Heatmap>();

            heatmap.Init();
            heatmap.transform.parent = transform;
            heatmap.transform.localPosition = heatmapPosition;
            heatmap.selectionNr = selectionNr;
            heatmap.selectionFile =
                (CellexalUser.UserSpecificFolder + "/selection" + selectionNr + ".txt").FixFilePath();
            heatmapList.Add(heatmap);
            heatmap.directory = outputFilePath;
            BuildTexture(selection, outputFilePath, heatmap);
            GameObject existingHeatmap = GameObject.Find(heatmapName);
            while (existingHeatmap != null)
            {
                heatmapName += "_Copy";
                existingHeatmap = GameObject.Find(heatmapName);
                yield return null;
            }

            heatmap.gameObject.name = heatmapName; //"heatmap_" + heatmapsCreated;
            heatmap.highlightQuad.GetComponent<Renderer>().material.color = HighlightMarkerColor;
            heatmap.confirmQuad.GetComponent<Renderer>().material.color = ConfirmMarkerColor;
            CellexalEvents.CommandFinished.Invoke(true);
        }


        #region BuildTexture Methods

        /// <summary>
        /// Builds the heatmap texture.
        /// </summary>
        /// <param name="selection">An array containing the <see cref="GraphPoint"/> that are in the heatmap</param>
        /// <param name="filepath">A path to the file containing the gene names</param>
        public void BuildTexture(List<Graph.GraphPoint> selection, string filepath, Heatmap heatmap)
        {
            if (selection == null)
            {
                CellexalLog.Log("WARNING: No selection to build texture from.");
                return;
            }

            heatmap.selection = selection;
            if (heatmap.buildingTexture)
            {
                CellexalLog.Log("WARNING: Not building heatmap texture because it is already building");
                return;
            }

            gameObject.SetActive(true);
            heatmap.GetComponent<Collider>().enabled = false;
            heatmap.cells = new Cell[selection.Count];
            heatmap.attributeWidths = new List<Tuple<int, float, int>>();
            heatmap.cellAttributes = new List<Tuple<Cell, int>>();
            heatmap.attributeColors = new Dictionary<int, UnityEngine.Color>();
            heatmap.groupWidths = new List<Tuple<int, float, int>>();
            heatmap.groupingColors = new Dictionary<int, UnityEngine.Color>();
            float cellWidth = (float) heatmap.heatmapWidth / selection.Count;
            int lastGroup = -1;
            int groupWidth = 0;
            heatmap.attributeWidth = 0;
            heatmap.lastAttribute = -1;
            // read the cells and their groups
            for (int i = 0; i < selection.Count; ++i)
            {
                Graph.GraphPoint graphpoint = selection[i];
                int group = graphpoint.Group;
                var cell = referenceManager.cellManager.GetCell(graphpoint.Label);
                heatmap.cells[i] = cell;
                var attributes = cell.Attributes;
                heatmap.AddAttributeWidth(attributes, cellWidth, cell);
                heatmap.groupingColors[group] = graphpoint.GetColor();
                if (lastGroup == -1)
                {
                    lastGroup = group;
                }

                // used for saving the widths of the groups later
                if (group != lastGroup)
                {
                    heatmap.groupWidths.Add(new Tuple<int, float, int>(lastGroup, groupWidth * cellWidth,
                        (int) groupWidth));
                    groupWidth = 0;
                    lastGroup = group;
                }

                groupWidth++;
            }

            // add the last group as well
            heatmap.groupWidths.Add(new Tuple<int, float, int>(lastGroup, groupWidth * cellWidth, groupWidth));
            heatmap.attributeWidths.Add(new Tuple<int, float, int>(heatmap.lastAttribute,
                heatmap.attributeWidth * cellWidth, heatmap.attributeWidth));
            if (heatmap.genes == null || heatmap.genes.Length == 0)
            {
                if (!File.Exists(filepath))
                {
                    CellexalLog.Log("Failed to create heatmap. R script did not return gene list file " + filepath);
                    CellexalError.SpawnError("Failed to create heatmap",
                        "R script did not return gene list file " + filepath);
                    if (!(referenceManager.networkGenerator.GeneratingNetworks &&
                          File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.R")))
                    {
                        referenceManager.floor.StopPulse();
                    }
                    Destroy(heatmap.gameObject);

                    return;
                }

                StreamReader streamReader = new StreamReader(filepath);
                int numberOfGenes = int.Parse(streamReader.ReadLine());
                heatmap.genes = new string[numberOfGenes];
                int i = 0;
                while (!streamReader.EndOfStream)
                {
                    heatmap.genes[i] = streamReader.ReadLine();
                    i++;
                }

                streamReader.Close();
            }

            heatmap.orderedByAttribute = false;
            //try
            //{
            StartCoroutine(BuildTextureCoroutine(heatmap));
            //}
            //catch (Exception e)
            //{
            //    CellexalLog.Log("Failed to create heatmap - " + e.StackTrace);
            //    CellexalError.SpawnError("Failed to create heatmap", "Read full stacktrace in cellexal log");
            //}
        }

        /// <summary>
        /// Builds a heatmap texture based on already known groupwidths and attribute widths.
        /// </summary>
        /// <param name="heatmap">The heatmap to attach the texture to.</param>
        public void BuildTexture(Heatmap heatmap)
        {
            if (heatmap.buildingTexture)
            {
                CellexalLog.Log("WARNING: Not building heatmap texture because it is already building");
                return;
            }

            heatmap.GetComponent<Collider>().enabled = false;
            // merge groups
            List<Tuple<int, float, int>> groupWidths = heatmap.groupWidths;
            for (int i = 0; i < groupWidths.Count - 1; ++i)
            {
                // if two groups with the same color are now beside eachother, merge them
                if (groupWidths[i].Item1 == groupWidths[i + 1].Item1)
                {
                    Tuple<int, float, int> oldTuple1 = groupWidths[i];
                    Tuple<int, float, int> oldTuple2 = groupWidths[i + 1];
                    float newWidthCoords = oldTuple1.Item2 + oldTuple2.Item2;
                    int newWidthCells = oldTuple1.Item3 + oldTuple2.Item3;
                    groupWidths[i] = new Tuple<int, float, int>(oldTuple1.Item1, newWidthCoords, newWidthCells);
                    groupWidths.RemoveAt(i + 1);
                }
            }

            StartCoroutine(BuildTextureCoroutine(heatmap));
        }

        /// <summary>
        /// Builds this heatmaps texture using the supplied cells and genes.
        /// </summary>
        /// <param name="newCells">The cells that the heatmap should contain.</param>
        /// <param name="newGenes">The genes that the heatmap should contain.</param>
        /// <param name="newGroupWidths">The grouping information.</param>
        public void BuildTexture(Cell[] newCells, string[] newGenes, List<Tuple<int, float, int>> newGroupWidths,
            Heatmap heatmap)
        {
            if (heatmap.buildingTexture)
            {
                CellexalLog.Log("WARNING: Not building heatmap texture because it is already building");
                return;
            }

            heatmap.cells = newCells;
            heatmap.genes = newGenes;
            heatmap.groupWidths = newGroupWidths;
            heatmap.UpdateAttributeWidhts();
            StartCoroutine(BuildTextureCoroutine(heatmap));
        }

        /// <summary>
        /// Coroutine that starts a thread and generates the heatmap.
        /// </summary>
        /// <param name="heatmap"></param>
        public IEnumerator BuildTextureCoroutine(Heatmap heatmap)
        {
            heatmap.buildingTexture = true;
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            CellexalLog.Log("Started building a heatmap texture");
            referenceManager.floor.StartPulse();

            foreach (var button in heatmap.GetComponentsInChildren<CellexalButton>())
            {
                button.SetButtonActivated(false);
            }

            //SQLiter.SQLite database = referenceManager.database;
            //SQLiter.SQLite db = Instantiate(SQLiter.SQLite);
            //{
            //    referenceManager = referenceManager
            //};
            SQLiter.SQLite db = heatmap.gameObject.AddComponent<SQLiter.SQLite>();
            db.referenceManager = referenceManager;
            db.InitDatabase(heatmap.directory + ".sqlite3");

            yield return StartCoroutine(db.ValidateDatabaseCoroutine());
            if (!db._databaseOK)
            {
                // something went wrong in the r script
                CellexalError.SpawnError("Could not generate heatmap",
                    "R script did not return a valid gene database file, check R log for more information");
                CellexalLog.Log("Could not generate heatmap, R script did not return a valid gene database file");
                Destroy(heatmap.gameObject);
                referenceManager.floor.StopPulse();
                yield break;
            }

            heatmap.bitmap = new Bitmap(heatmap.bitmapWidth, heatmap.bitmapHeight);

            //xcoord = heatmapX;

            while (db.QueryRunning)
            {
                yield return null;
            }

            db.QueryGenesIds(heatmap.genes);
            while (db.QueryRunning)
            {
                yield return null;
            }

            ArrayList result = db._result;
            Dictionary<string, string> geneIds = new Dictionary<string, string>(result.Count);
            foreach (Tuple<string, string> t in result)
            {
                // keys are names, values are ids
                geneIds[t.Item1] = t.Item2;
            }

            Dictionary<string, int> genePositions = new Dictionary<string, int>(heatmap.genes.Length);
            for (int i = 0; i < heatmap.genes.Length; ++i)
            {
                // gene names are keys, positions are values
                genePositions[heatmap.genes[i]] = i;
            }

            Dictionary<string, int> cellsPosition = new Dictionary<string, int>(heatmap.cells.Length);

            for (int i = 0; i < heatmap.cells.Length; ++i)
            {
                cellsPosition[heatmap.cells[i].Label] = i;
            }

            while (db.QueryRunning)
            {
                yield return null;
            }

            db.QueryGenesInCells(heatmap.genes, heatmap.cells.Select((c) => c.Label).ToArray());
            while (db.QueryRunning)
            {
                yield return null;
            }

            result = db._result;
            string heatmapDirectory = Directory.GetCurrentDirectory() + @"\Output\Images";
            if (!Directory.Exists(heatmapDirectory))
            {
                Directory.CreateDirectory(heatmapDirectory);
            }

            string heatmapFilePath = heatmapDirectory + "\\heatmap_temp.png";


            CellexalLog.Log("Reading " + result.Count + " results from database");
            //CreateHeatmapPNG(heatmap, result, genePositions, cellsPosition, geneIds, heatmapFilePath);
            Thread thread = new Thread(() =>
                CreateHeatmapPNG(heatmap, result, genePositions, cellsPosition, geneIds, heatmapFilePath));
            thread.Start();
            while (thread.IsAlive)
            {
                yield return null;
            }

            // the thread is now done and the heatmap has been painted
            // copy the bitmap data over to a unity texture
            // using a memorystream here seemed like a better alternative but made the standalone crash
            // these yields makes the loading a little bit smoother, but still cuts a few frames.

            var texture = Instantiate(heatmapTexture) as Texture2D;
            yield return null;
            texture.LoadImage(File.ReadAllBytes(heatmapFilePath));
            heatmap.texture = texture;
            yield return null;
            heatmap.GetComponent<Renderer>().material.SetTexture("_MainTex", texture);
            yield return null;

            heatmap.GetComponent<Collider>().enabled = true;

            //if (!referenceManager.networkGenerator.GeneratingNetworks)
            //    referenceManager.calculatorCluster.SetActive(false);

            foreach (var button in GetComponentsInChildren<CellexalButton>())
            {
                button.SetButtonActivated(true);
            }

            stopwatch.Stop();
            CellexalLog.Log("Finished building a heatmap texture in " + stopwatch.Elapsed.ToString());
            heatmap.buildingTexture = false;
            heatmap.createAnim = true;

            CellexalEvents.HeatmapCreated.Invoke();
            CellexalEvents.ScriptFinished.Invoke();
            //if (!(referenceManager.networkGenerator.GeneratingNetworks && File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.R")))
            //{
            //    referenceManager.floor.StopPulse();
            //}
            referenceManager.notificationManager.SpawnNotification("Heatmap finished.");
            string sessionEntryName = heatmap.directory + " from " + heatmap.selectionFile;
            if (!referenceManager.sessionHistoryList.Contains(sessionEntryName, Definitions.HistoryEvent.HEATMAP))
            {
                referenceManager.sessionHistoryList.AddEntry(heatmap.directory + " from " + heatmap.selectionFile,
                    Definitions.HistoryEvent.HEATMAP);
            }
        }

        /// <summary>
        /// Helper method that is run as a thread in <see cref="BuildTextureCoroutine(List{Tuple{int, float, int}}, List{Tuple{int, float, int}}, Heatmap)"/>.
        /// </summary>
        public void CreateHeatmapPNG(Heatmap heatmap, ArrayList result, Dictionary<string, int> genePositions,
            Dictionary<string, int> cellsPosition,
            Dictionary<string, string> geneIds, string heatmapFilePath)
        {
            var attributeWidths = heatmap.attributeWidths;
            var groupWidths = heatmap.groupWidths;
            //System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(heatmap.bitmap);
            System.Drawing.Graphics graphics = DrawHeatmap(heatmap, System.Drawing.Color.FromArgb(0, 0, 0, 0),
                System.Drawing.Color.White);

            float xcoord = heatmap.heatmapX;
            float ycoord = heatmap.heatmapY;
            float xcoordInc = (float) heatmap.heatmapWidth / heatmap.cells.Length;
            float ycoordInc = (float) heatmap.heatmapHeight / heatmap.genes.Length;
            System.Drawing.SolidBrush[] heatmapBrushes = expressionColors;
            graphics.FillRectangle(Brushes.Black, heatmap.heatmapX, heatmap.heatmapY, heatmap.heatmapWidth,
                heatmap.heatmapHeight);
            Tuple<string, float> tuple;
            List<Tuple<string, float>> expressions = new List<Tuple<string, float>>();
            for (int i = 1; i < result.Count;)
            {
                // new gene
                string genename = ((Tuple<string, float>) result[i]).Item1;

                ycoord = heatmap.heatmapY + genePositions[genename] * ycoordInc;
                // the arraylist should contain the gene id and that gene's highest expression before all the expressions
                i += 1;
                expressions.Clear();
                while (i < result.Count)
                {
                    tuple = (Tuple<string, float>) result[i];
                    i++;
                    if (geneIds.ContainsKey(tuple.Item1))
                        break;
                    expressions.Add(tuple);
                }

                expressions.Sort((Tuple<string, float> t1, Tuple<string, float> t2) => t1.Item2.CompareTo(t2.Item2));
                float binsize = (float) expressions.Count / (CellexalConfig.Config.NumberOfHeatmapColors - 1);
                int expressionIndex = 0;

                for (int j = 0; j < CellexalConfig.Config.NumberOfHeatmapColors; ++j)
                {
                    int nextLimit = (int) (binsize * (j + 1));
                    for (; expressionIndex < nextLimit && expressionIndex < expressions.Count; ++expressionIndex)
                    {
                        expressions[expressionIndex] = new Tuple<string, float>(expressions[expressionIndex].Item1, j);
                    }
                }

                for (int j = 0; j < expressions.Count; ++j)
                {
                    string cellName = expressions[j].Item1;
                    float expression = expressions[j].Item2;
                    xcoord = heatmap.heatmapX + cellsPosition[cellName] * xcoordInc;
                    graphics.FillRectangle(heatmapBrushes[(int) expression], xcoord, ycoord, xcoordInc, ycoordInc);
                }

                //if (expression == highestExpression)
                //{
                //    graphics.FillRectangle(heatmapBrushes[heatmapBrushes.Length - 1], xcoord, ycoord, xcoordInc, ycoordInc);
                //}
                //else
                //{
                //graphics.FillRectangle(heatmapBrushes[(int)((expression - lowestExpression) / (highestExpression - lowestExpression) * numberOfExpressionColors)], xcoord, ycoord, xcoordInc, ycoordInc);
                //}
                //}
            }

            heatmap.bitmap.Save(heatmapFilePath, ImageFormat.Png);
            graphics.Dispose();
        }

        /// <summary>
        /// If the heatmap is to be saved for logging purposes (when the user presses save heatmap)
        /// the background color should be white and text colour black for easier reading on a computer screen.
        /// So only the genelist and widths are redrawn.
        /// </summary>
        public void SavePNGtoDisk(Heatmap heatmap, string heatmapFilePath)
        {
            System.Drawing.Graphics graphics =
                DrawHeatmap(heatmap, System.Drawing.Color.White, System.Drawing.Color.Black);

            heatmap.bitmap.Save(heatmapFilePath, ImageFormat.Png);
            graphics.Dispose();
        }

        /// <summary>
        /// Helper function to draw the parts of the heatmap that are incommon for both logging and in-session heatmaps.
        /// </summary>
        private System.Drawing.Graphics DrawHeatmap(Heatmap heatmap, System.Drawing.Color backgroundColor,
            System.Drawing.Color textColor)
        {
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(heatmap.bitmap);

            // get the grouping colors
            Dictionary<int, SolidBrush> groupBrushes = new Dictionary<int, SolidBrush>();
            foreach (var entry in heatmap.groupingColors)
            {
                UnityEngine.Color unitycolor = entry.Value;

                groupBrushes[entry.Key] = new SolidBrush(System.Drawing.Color.FromArgb((int) (unitycolor.r * 255),
                    (int) (unitycolor.g * 255), (int) (unitycolor.b * 255)));
            }

            // get the attribute colors
            Dictionary<int, SolidBrush> attributeBrushes = new Dictionary<int, SolidBrush>();
            foreach (var entry in heatmap.attributeColors)
            {
                UnityEngine.Color unitycolor = entry.Value;
                attributeBrushes[entry.Key] = new SolidBrush(System.Drawing.Color.FromArgb((int) (unitycolor.r * 255),
                    (int) (unitycolor.g * 255), (int) (unitycolor.b * 255)));
            }
            // draw a background
            //graphics.Clear(System.Drawing.Color.FromArgb(0, 0, 0, 0));

            SolidBrush backgroundBrush = new SolidBrush(backgroundColor);
            graphics.FillRectangle(backgroundBrush, 0, 0, heatmap.heatmapX, heatmap.bitmapHeight);
            graphics.FillRectangle(backgroundBrush, 0, 0, heatmap.bitmapWidth, heatmap.heatmapY);
            graphics.FillRectangle(backgroundBrush, heatmap.geneListX, 0, heatmap.geneListWidth, heatmap.bitmapHeight);
            graphics.FillRectangle(backgroundBrush, 0, (heatmap.bitmapWidth - heatmap.heatmapY), heatmap.bitmapWidth,
                heatmap.bitmapHeight);

            float xcoord = heatmap.heatmapX;
            float ycoord = heatmap.heatmapY;
            float xcoordInc = (float) heatmap.heatmapWidth / heatmap.cells.Length;
            float ycoordInc = (float) heatmap.heatmapHeight / heatmap.genes.Length;

            // draw the attribute bar
            for (int i = 0; i < heatmap.attributeWidths.Count; ++i)
            {
                int attributeNbr = heatmap.attributeWidths[i].Item1;
                float attributeWidth = heatmap.attributeWidths[i].Item2;
                graphics.FillRectangle(attributeBrushes[attributeNbr], xcoord, heatmap.attributeBarY, attributeWidth,
                    heatmap.attributeBarHeight);
                xcoord += attributeWidth;
            }

            xcoord = heatmap.heatmapX;
            ycoord = heatmap.heatmapY;
            // draw the grouping bar
            for (int i = 0; i < heatmap.groupWidths.Count; ++i)
            {
                int groupNbr = heatmap.groupWidths[i].Item1;
                float groupWidth = heatmap.groupWidths[i].Item2;
                graphics.FillRectangle(groupBrushes[groupNbr], xcoord, heatmap.groupBarY, groupWidth,
                    heatmap.groupBarHeight);
                xcoord += groupWidth;
            }

            ycoord = heatmap.heatmapY - 4;
            float fontSize;
            if (heatmap.genes.Length > 120)
            {
                fontSize = 8f;
            }
            else
            {
                fontSize = 20f;
            }

            geneFont = new System.Drawing.Font(FontFamily.GenericMonospace, fontSize, System.Drawing.FontStyle.Regular);
            SolidBrush textBrush = new SolidBrush(textColor);
            // draw all the gene names
            for (int i = 0; i < heatmap.genes.Length; ++i)
            {
                string geneName = heatmap.genes[i];

                graphics.DrawString(geneName, geneFont, textBrush, heatmap.geneListX, ycoord);
                ycoord += ycoordInc;
            }

            return graphics;
        }

        #endregion

        public void AddHeatmapToList(Heatmap heatmap)
        {
            heatmapList.Add(heatmap);
            heatmap.selectionNr = selectionNr;
        }
    }
}