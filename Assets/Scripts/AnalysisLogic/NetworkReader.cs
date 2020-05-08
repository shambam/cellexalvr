using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.Extensions;
using CellexalVR.General;
using CellexalVR.Menu.Buttons;
using UnityEngine;

namespace CellexalVR.AnalysisLogic
{
    /// <summary>
    /// Class that handles the reading of correlation network input files.
    /// </summary>
    public class NetworkReader : MonoBehaviour
    {
        public ReferenceManager referenceManager;

        public IEnumerator ReadNetworkFilesCoroutine(int layoutSeed, string path, string selectionFile)
        {
            CellexalLog.Log("Started reading network files");
            CellexalEvents.ScriptRunning.Invoke();
            string networkDirectory = path; //CellexalUser.UserSpecificFolder + @"\Resources\Networks";
            if (!Directory.Exists(networkDirectory))
            {
                print(string.Format(
                    "No network directory found at {0}, make sure the network generating r script has executed properly.",
                    CellexalLog.FixFilePath(networkDirectory)));
                CellexalError.SpawnError("Error when generating networks",
                    string.Format(
                        "No network directory found at {0}, make sure the network generating r script has executed properly.",
                        CellexalLog.FixFilePath(networkDirectory)));
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
                CellexalError.SpawnError("Error when generating networks",
                    string.Format(
                        "No .cnt file found at {0}, make sure the network generating r script has executed properly by checking the r_log.txt in the output folder.",
                        CellexalLog.FixFilePath(networkDirectory)));
                CellexalEvents.CommandFinished.Invoke(false);
                yield break;
            }

            if (cntFilePaths.Length > 1)
            {
                CellexalError.SpawnError("Error when generating networks",
                    string.Format(
                        "More than one .cnt file found at {0}, make sure the network generating r script has executed properly by checking the r_log.txt in the output folder.",
                        CellexalLog.FixFilePath(networkDirectory)));
                CellexalEvents.CommandFinished.Invoke(false);
                yield break;
            }

            FileStream cntFileStream = new FileStream(cntFilePaths[0], FileMode.Open);
            StreamReader cntStreamReader = new StreamReader(cntFileStream);

            // make sure there is a .nwk file
            if (nwkFilePaths.Length == 0)
            {
                CellexalError.SpawnError("Error when generating networks",
                    string.Format(
                        "No .nwk file found at {0}, make sure the network generating r script has executed properly by checking the r_log.txt in the output folder.",
                        CellexalLog.FixFilePath(networkDirectory)));
                CellexalEvents.CommandFinished.Invoke(false);
                yield break;
            }

            FileStream nwkFileStream = new FileStream(nwkFilePaths[0], FileMode.Open);
            StreamReader nwkStreamReader = new StreamReader(nwkFileStream);
            // 1 MB = 1048576 B
            if (nwkFileStream.Length > 1048576)
            {
                CellexalError.SpawnError("Error when generating networks",
                    string.Format(".nwk file is larger than 1 MB. .nwk file size: {0} B", nwkFileStream.Length));
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
            NetworkHandler networkHandler = null;

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
                    graph = referenceManager.graphManager.FindGraph(graphName);
                    if (graph == null)
                    {
                        CellexalError.SpawnError("Error when generating networks",
                            string.Format(
                                "Could not find the graph named {0} when trying to create a convex hull, make sure there is a .mds and .hull file with the same name in the dataset.",
                                graphName));
                        CellexalEvents.CommandFinished.Invoke(false);
                        yield break;
                    }

                    StartCoroutine(graph.CreateGraphSkeleton(false));
                    while (!graph.convexHull.activeSelf)
                    {
                        yield return null;
                    }

                    var skeleton = graph.convexHull;
                    if (skeleton == null)
                    {
                        CellexalError.SpawnError("Error when generating networks",
                            string.Format(
                                "Could not create a convex hull for the graph named {0}, this could be because the convex hull file is incorrect",
                                graphName));
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

                    var networkHandlerName =
                        "NetworkHandler_" + graphName + "-" + (referenceManager.selectionManager.fileCreationCtr + 1);
                    GameObject existingHandler = GameObject.Find(networkHandlerName);
                    while (existingHandler != null)
                    {
                        networkHandlerName += "_Copy";
                        existingHandler = GameObject.Find(networkHandlerName);
                        yield return null;
                    }

                    networkHandler.name = networkHandlerName;
                }

                float x = float.Parse(words[0], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                float y = float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                float z = float.Parse(words[2], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                // the color is a hex string e.g. #FF0099
                UnityEngine.Color color = new UnityEngine.Color();
                string colorString = words[3];
                ColorUtility.TryParseHtmlString(colorString, out color);

                maxPosPcor[colorString] = 0f;
                minPosPcor[colorString] = float.MaxValue;
                maxNegPcor[colorString] = float.MinValue;
                minNegPcor[colorString] = 0f;
                Vector3 position = graph.ScaleCoordinates(new Vector3(x, y, z));
                int group = referenceManager.selectionToolCollider.GetColorIndex(colorString);
                NetworkCenter network =
                    referenceManager.networkGenerator.CreateNetworkCenter(networkHandler, group, position,
                        layoutSeed);
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
                float pcor = float.Parse(words[0], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);

                if (geneName1 == geneName2)
                {
                    CellexalError.SpawnError("Error in networkfiles",
                        "Gene \'" + geneName1 + "\' cannot be correlated to itself in file " + nwkFilePaths[0]);
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
                    NetworkNode newNode =
                        referenceManager.networkGenerator.CreateNetworkNode(geneName1, networks[color]);
                    nodes[node1] = newNode;
                }

                if (!nodes.ContainsKey(node2))
                {
                    NetworkNode newNode =
                        referenceManager.networkGenerator.CreateNetworkNode(geneName2, networks[color]);
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
            Array.Sort(keyPairs,
                (NetworkKeyPair x, NetworkKeyPair y) => x.key1.Equals(y.key2) ? 0 : x.key1.CompareTo(y.key1));

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
            CellexalLog.Log("Successfully created " + networks.Count + " networks with a total of " +
                            nodes.Values.Count + " nodes");
            CellexalEvents.CommandFinished.Invoke(true);
            CellexalEvents.ScriptFinished.Invoke();
            string sessionEntryName = networkDirectory + " from " + selectionFile;
            if (!referenceManager.sessionHistoryList.Contains(sessionEntryName, Definitions.HistoryEvent.NETWORK))
            {
                referenceManager.sessionHistoryList.AddEntry(sessionEntryName, Definitions.HistoryEvent.NETWORK,
                    layoutSeed);
            }

            networkHandler.CreateNetworkAnimation(graph.transform);
        }

        /// <summary>
        /// Helper struct for sorting network keys.
        /// </summary>
        public struct NetworkKeyPair
        {
            public string node1, node2, key1, key2;

            public NetworkKeyPair(string c, string n1, string n2, string k1, string k2)
            {
                key1 = k1;
                key2 = k2;
                node1 = n1;
                node2 = n2;
            }
        }
    }
}