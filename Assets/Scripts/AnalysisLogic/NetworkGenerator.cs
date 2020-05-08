using CellexalVR.AnalysisObjects;
using CellexalVR.DesktopUI;
using CellexalVR.Extensions;
using CellexalVR.General;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CellexalVR.Menu.Buttons.Networks;
using UnityEditorInternal;
using UnityEngine;

namespace CellexalVR.AnalysisLogic
{
    /// <summary>
    /// This class starts the thread that generates the network files and then tells the inputreader to process them.
    /// </summary>
    public class NetworkGenerator : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public NetworkCenter networkCenterPrefab;
        public NetworkNode networkNodePrefab;
        public Material networkNodeDefaultMaterial;
        public Material networkLineDefaultMaterial;
        public List<NetworkHandler> networkList = new List<NetworkHandler>();
        public int selectionNr;
        public string networkMethod;

        public bool GeneratingNetworks { get; private set; }

        public Material[] LineMaterials;

        private GameObject calculatorCluster;

        //public SelectionToolHandler selectionToolHandler;
        public SelectionManager selectionManager;
        private InputReader inputReader;
        private GraphManager graphManager;

        private GameObject headset;

        //private StatusDisplay status;
        //private StatusDisplay statusDisplayHUD;
        //private StatusDisplay statusDisplayFar;
        private Thread t;

        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        public NetworkGenerator()
        {
            CellexalEvents.ConfigLoaded.AddListener(CreateLineMaterials);
        }

        private void Start()
        {
            selectionManager = referenceManager.selectionManager;
            inputReader = referenceManager.inputReader;
            graphManager = referenceManager.graphManager;
            headset = referenceManager.headset;

            //status = referenceManager.statusDisplay;
            //statusDisplayHUD = referenceManager.statusDisplayHUD;
            //statusDisplayFar = referenceManager.statusDisplayFar;
            //calculatorCluster = referenceManager.calculatorCluster;
        }

        /// <summary>
        /// Creates the materials used when drawing the lines between genes in networks.
        /// </summary>
        public void CreateLineMaterials()
        {
            int coloringMethod = CellexalConfig.Config.NetworkLineColoringMethod;
            if (coloringMethod == 0)
            {
                int numColors = CellexalConfig.Config.NumberOfNetworkLineColors;
                if (numColors < 4)
                {
                    CellexalLog.Log(
                        "WARNING: NumberOfNetworkLineColors in config file must be atleast 4 when NetworkLineColoringMethod is set to 0. Defaulting to 4.");
                    numColors = 4;
                }

                Color posHigh = CellexalConfig.Config.NetworkLineColorPositiveHigh;
                Color posLow = CellexalConfig.Config.NetworkLineColorPositiveLow;
                Color negLow = CellexalConfig.Config.NetworkLineColorNegativeLow;
                Color negHigh = CellexalConfig.Config.NetworkLineColorNegativeHigh;

                LineMaterials = new Material[numColors];

                var colors = Extensions.Extensions.InterpolateColors(negHigh, negLow, numColors / 2);

                for (int i = 0; i < numColors / 2; ++i)
                {
                    LineMaterials[i] = new Material(networkLineDefaultMaterial);
                    LineMaterials[i].color = colors[i];
                }

                colors = Extensions.Extensions.InterpolateColors(posLow, posHigh, numColors - numColors / 2);

                for (int i = numColors / 2, j = 0; i < numColors; ++i, ++j)
                {
                    LineMaterials[i] = new Material(networkLineDefaultMaterial);
                    LineMaterials[i].color = colors[j];
                }
            }
            else if (coloringMethod == 1)
            {
                int numColors = CellexalConfig.Config.NumberOfNetworkLineColors;
                if (numColors < 1)
                {
                    CellexalLog.Log(
                        "WARNING: NumberOfNetworkLineColors in config file must be atleast 1 when NetworkLineColoringMethod is set to 1. Defaulting to 1.");
                    numColors = 1;
                }

                List<Material> result = new List<Material>();
                // Create a cuboid in a 3D color spectrum and choose the colors
                // from the spectrum at (sort of) evenly distributed points in that cuboid.
                float spaceBetween = 1f / numColors;
                int sidex = numColors;
                int sidey = numColors / 6;
                int sidez = numColors / 6;
                if (sidey < 1)
                {
                    // if numcolors is too low the other for loops don't work because sidey = 0
                    for (int cubex = 0; cubex < sidex; ++cubex)
                    {
                        Material newMaterial = new Material(networkLineDefaultMaterial);
                        newMaterial.color = Color.HSVToRGB(1f - cubex * spaceBetween, 1f, 1f);
                        result.Add(newMaterial);
                    }
                }
                else
                {
                    for (int cubex = 0; cubex < sidex; ++cubex)
                    {
                        for (int cubey = 0; cubey < sidey; ++cubey)
                        {
                            for (int cubez = 0; cubez < sidez; ++cubez)
                            {
                                Material newMaterial = new Material(networkLineDefaultMaterial);
                                newMaterial.color = Color.HSVToRGB(1f - cubex * spaceBetween,
                                    1f - cubey * spaceBetween * 6, 1f - cubez * spaceBetween * 6);
                                result.Add(newMaterial);
                            }
                        }
                    }
                }

                LineMaterials = result.ToArray();
            }
        }


        [ConsoleCommand("networkGenerator", aliases: new string[] {"generatenetworks", "gn"})]
        public void GenerateNetworks()
        {
            var rand = new System.Random();
            var layoutSeed = rand.Next();
            GenerateNetworks(layoutSeed);
        }


        /// <summary>
        /// Generates networks based on the selectiontoolhandler's last selection.
        /// </summary>
        public void GenerateNetworks(int layoutSeed)
        {
            CellexalEvents.CreatingNetworks.Invoke();
            StartCoroutine(GenerateNetworksCoroutine(layoutSeed));
        }

        private IEnumerator GenerateNetworksCoroutine(int layoutSeed)
        {
            //int statusId = status.AddStatus("R script generating networks");
            //int statusIdHUD = statusDisplayHUD.AddStatus("R script generating networks");
            //int statusIdFar = statusDisplayFar.AddStatus("R script generating networks");
            GeneratingNetworks = true;
            //calculatorCluster.SetActive(true);
            referenceManager.floor.StartPulse();

            while (selectionManager.RObjectUpdating)
            {
                yield return null;
            }

            // generate the files containing the network information
            selectionNr = selectionManager.fileCreationCtr - 1;
            //string function = "make.cellexalvr.network";
            //string script = function + "(" + "cellexalObj, \"" + groupingFilePath + "\", \"" + networkResources + "\", method=\"" + networkMethod + "\")";
            string groupingFilePath =
                (CellexalUser.UserSpecificFolder + @"\selection" + selectionNr + ".txt").UnFixFilePath();
            string outputFilePath =
                (CellexalUser.UserSpecificFolder + @"\Resources\Networks" + selectionNr).UnFixFilePath();
            networkMethod = CellexalConfig.Config.NetworkAlgorithm;
            string args = CellexalUser.UserSpecificFolder.UnFixFilePath() + " " + groupingFilePath + " " +
                          outputFilePath + " " + networkMethod;
            string rScriptFilePath = (Application.streamingAssetsPath + @"\R\make_networks.R").FixFilePath();
            if (!Directory.Exists(outputFilePath))
            {
                CellexalLog.Log("Creating directory " + outputFilePath.FixFilePath());
                Directory.CreateDirectory(outputFilePath);
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

            CellexalLog.Log("Running R script " + rScriptFilePath + " with the arguments \"" + args);
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            t = new Thread(() => RScriptRunner.RunRScript(rScriptFilePath, args));
            t.Start();

            while (t.IsAlive || File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.R"))
            {
                yield return null;
            }

            // wait one frame for files to be created
            yield return null;
            stopwatch.Stop();
            CellexalLog.Log("Network R script finished in " + stopwatch.Elapsed.ToString());
            GeneratingNetworks = false;
            CellexalEvents.NetworkCreated.Invoke();
            CellexalEvents.ScriptFinished.Invoke();
            //if (!(referenceManager.heatmapGenerator.GeneratingHeatmaps && File.Exists(CellexalUser.UserSpecificFolder + "\\mainServer.input.R")))
            //{
            //    referenceManager.floor.StopPulse();
            //}

            inputReader.ReadNetworkFiles(layoutSeed, outputFilePath, groupingFilePath);
        }

        /// <summary>
        /// Helper method to create network nodes.
        /// </summary>
        /// <param name="geneName"> The name of the gene that the network node should represent. </param>
        /// <returns> Returns the newly created NetworkNode. </returns>
        public NetworkNode CreateNetworkNode(string geneName, NetworkCenter center)
        {
            NetworkNode newNode = Instantiate(networkNodePrefab);
            newNode.GetComponent<Renderer>().sharedMaterial = networkNodeDefaultMaterial;
            newNode.CameraToLookAt = headset.transform;
            newNode.SetReferenceManager(referenceManager);
            newNode.Label = geneName;
            newNode.Center = center;
            center.AddNode(newNode);
            return newNode;
        }

        /// <summary>
        /// Creates a new network center.
        /// </summary>
        /// <param name="handler"> The handler the center should be connected to. </param>
        /// <param name="name"> The name of the center. </param>
        /// <param name="position"> The position it should sit at. Should be from <see cref="Graph.ScaleCoordinates(float, float, float)"/>. </param>
        /// <returns> The new network center. </returns>
        public NetworkCenter CreateNetworkCenter(NetworkHandler handler, int group, Vector3 position,
            int layoutSeed)
        {
            NetworkCenter network = Instantiate(networkCenterPrefab);
            network.transform.parent = handler.gameObject.transform;
            network.transform.localPosition = position;
            network.referenceManager = referenceManager;
            handler.AddNetwork(network);
            network.Handler = handler;
            network.gameObject.name = "NetworkCenter_Group" + /*handler.name + */ group;
            network.selectionNr = selectionNr;
            network.Group = group;
            //graphManager.AddNetwork(handler);
            networkList.RemoveAll(item => item == null);
            if (!networkList.Contains(handler)) networkList.Add(handler);
            network.LayoutSeed = layoutSeed;


            return network;
        }

        /// <summary>
        /// Finds a networkhandler.
        /// </summary>
        /// <param name="networkName"> The name of the networkhandler </param>
        /// <returns> A reference to the networkhandler, or null if non was found.  </returns>
        public NetworkHandler FindNetworkHandler(string networkName)
        {
            foreach (NetworkHandler nh in networkList)
            {
                if (nh.name == networkName)
                {
                    return nh;
                }
            }

            return null;
        }

        /// <summary>
        /// Highlights a gene in all networks with a red circle.
        /// </summary>
        /// <param name="geneName">The gene to highlight.</param>
        public void HighLightGene(string geneName)
        {
            foreach (NetworkHandler nh in networkList)
            {
                nh.HighLightGene(geneName);
            }
        }
    }
}