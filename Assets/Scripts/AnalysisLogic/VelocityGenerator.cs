using CellexalVR.AnalysisObjects;
using CellexalVR.DesktopUI;
using CellexalVR.General;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CellexalVR.AnalysisLogic.H5reader;

namespace CellexalVR.AnalysisLogic
{
    public class VelocityGenerator : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public List<Graph> ActiveGraphs { get; set; }
        public GameObject particleSystemPrefab;
        public Material arrowMaterial;
        public Material standardMaterial;

        private float frequency = 1f;
        private float speed = 8f;
        private float threshold = 0f;
        private bool constantEmitOverTime = true;
        private bool graphPointsToggled = false;
        private bool useGraphPointColors = false;
        private bool useArrowParticle = true;

        private string particleSystemGameObjectName = "Velocity Particle System";
        private void Start()
        {
            ActiveGraphs = new List<Graph>();
        }
        private void OnValidate()
        {
            if (gameObject.scene.IsValid())
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            }
        }

        public string[] VelocityFiles()
        {
            return referenceManager.graphManager.velocityFiles.ToArray();
        }

        /// <summary>
        /// Reads a velocity file and starts the particle system that visualises the velcoity information.
        /// </summary>
        /// <param name="path">The path to the .mds file that contains the velocity information.</param>
        [ConsoleCommand("velocityGenerator", folder: "Data", aliases: new string[] { "readvelocityfile", "rvf" })]
        public void ReadVelocityFile(string path)
        {
            ReadVelocityFile(path, "");
        }

        /// <summary>
        /// Reads a velocity file and starts the particle system that visualises the velcoity information for a subgraph.
        /// </summary>
        /// <param name="path">The path to the .mds file that contains the velocity information.</param>
        /// <param name="subGraphName">The name of the subgraph.</param>
        public void ReadVelocityFile(string path, string subGraphName)
        {
            print(path);
            //summertwerk
            if (referenceManager.inputReader.h5readers.Count > 0)
            {
                StartCoroutine(ReadVelocityParticleSystemFromHDF5(referenceManager.inputReader.h5readers.First().Value ,path, subGraphName));
            }
            else
                StartCoroutine(ReadVelocityParticleSystem(path, subGraphName));

        }

        private IEnumerator ReadVelocityParticleSystem(string path, string subGraphName = "")
        {

            while (referenceManager.graphGenerator.isCreating)
            {
                yield return null;
            }

            path = Directory.GetCurrentDirectory() + "\\Data\\" + CellexalUser.DataSourceFolder + "\\" +
                              path + ".mds";

            CellexalLog.Log("Started reading velocity file " + path);

            Graph graph;
            Graph originalGraph;
            int lastSlashIndex = path.LastIndexOfAny(new char[] { '/', '\\' });
            int lastDotIndex = path.LastIndexOf('.');
            string graphName = path.Substring(lastSlashIndex + 1, lastDotIndex - lastSlashIndex - 1);
            originalGraph = referenceManager.graphManager.FindGraph(graphName);
            //print(path);
            //print(lastSlashIndex);
            //print(lastDotIndex);

            if (subGraphName != string.Empty)
            {
                graph = referenceManager.graphManager.FindGraph(subGraphName);
            }
            else
            {
                graph = originalGraph;
            }

            int counter = 0;

            //print(graphName + " - " + graph.GraphName);
            Dictionary<Graph.GraphPoint, Vector3> velocities = new Dictionary<Graph.GraphPoint, Vector3>(graph.points.Count);
            using (FileStream stream = new FileStream(path, FileMode.Open))
            using (StreamReader reader = new StreamReader(stream))
            {
                string header = reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] words = line.Split(null);
                    string cellName = words[0];


                    Graph.GraphPoint point = graph.FindGraphPoint(cellName);

                    if (point == null)
                    {
                        continue;
                    }
                    float xfrom = float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                    float yfrom = float.Parse(words[2], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                    float zfrom = float.Parse(words[3], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                    float xto = float.Parse(words[4], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                    float yto = float.Parse(words[5], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                    float zto = float.Parse(words[6], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);

                    Vector3 from = originalGraph.ScaleCoordinates(new Vector3(xfrom, yfrom, zfrom));
                    Vector3 to = originalGraph.ScaleCoordinates(new Vector3(xto, yto, zto));
                    Vector3 diff = to - from;

                    if (counter<3)
                    {
                        UnityEngine.Debug.Log("(" + diff.x + ", " + diff.y + ", " + diff.z + ")");
                        //UnityEngine.Debug.Log((new Vector3(xto, yto, zto) - new Vector3(xfrom, yfrom, zfrom)) *1000);
                        counter++;
                    }

                    velocities[point] = diff / 5f;
                }

                GameObject particleSystemGameObject = Instantiate(particleSystemPrefab, graph.transform);
                particleSystemGameObject.name = particleSystemGameObjectName;
                VelocityParticleEmitter emitter = particleSystemGameObject.GetComponent<VelocityParticleEmitter>();
                emitter.referenceManager = referenceManager;
                emitter.arrowParticleMaterial = arrowMaterial;
                emitter.circleParticleMaterial = standardMaterial;
                emitter.graph = graph;
                emitter.ArrowEmitRate = 1f / frequency;
                emitter.Velocities = velocities;
                emitter.Threshold = threshold;
                emitter.Speed = speed;
                emitter.UseGraphPointColors = useGraphPointColors;
                emitter.UseArrowParticle = useArrowParticle;
                graph.velocityParticleEmitter = emitter;
                if (ActiveGraphs.Count > 0 && ActiveGraphs[0].graphPointsInactive)
                {
                    graph.ToggleGraphPoints();
                }

                reader.Close();
                stream.Close();

            }

            ActiveGraphs.Add(graph);
            CellexalLog.Log("Finished reading velocity file with " + velocities.Count + " velocities");
        }


        private IEnumerator ReadVelocityParticleSystemFromHDF5(H5Reader h5Reader, string path, string subGraphName = "")
        {
            while (referenceManager.graphGenerator.isCreating)
            {
                yield return null;
            }

            CellexalLog.Log("Started reading velocity file " + path);

            Graph graph;
            Graph originalGraph;
            int lastSlashIndex = path.LastIndexOfAny(new char[] { '/', '\\' });
            int lastDotIndex = path.LastIndexOf('.');
            //string graphName = path.Substring(lastSlashIndex + 1, lastDotIndex - lastSlashIndex - 1);
            string graphName = path.ToUpper();
            originalGraph = referenceManager.graphManager.FindGraph(graphName);

            if (subGraphName != string.Empty)
            {
                graph = referenceManager.graphManager.FindGraph(subGraphName);
            }
            else
            {
                graph = originalGraph;
            }

            //print(graphName + " - " + graph.GraphName);


            Dictionary<Graph.GraphPoint, Vector3> velocities = new Dictionary<Graph.GraphPoint, Vector3>(graph.points.Count);

            while (h5Reader.busy)
                yield return null;

            StartCoroutine(h5Reader.GetVelocites(path));

            while (h5Reader.busy)
                yield return null;

            float[] vels = h5Reader._velResult;
            string[] cellNames = h5Reader.index2cellname;

            for (int i = 0; i < cellNames.Length; i++)
            {
                Graph.GraphPoint point = graph.FindGraphPoint(cellNames[i]);

                float diffX = vels[i * 3]; //,System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                float diffY = vels[i * 3 + 1]; //, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                float diffZ = vels[i * 3 + 2];// System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                Vector3 diff = new Vector3(diffX, diffY, diffZ);
                //Method
                Vector3 diffScaled = diff * 30; //arbitrary scaling, ofcourse.. DUH!
                                                  
                diffScaled /= originalGraph.longestAxis;
                if (point != null)
                    velocities[point] = diffScaled / 5f;
                else
                    print(cellNames[i] + " does not exist");
            }

            GameObject particleSystemGameObject = Instantiate(particleSystemPrefab, graph.transform);
            particleSystemGameObject.name = particleSystemGameObjectName;
            VelocityParticleEmitter emitter = particleSystemGameObject.GetComponent<VelocityParticleEmitter>();
            emitter.referenceManager = referenceManager;
            emitter.arrowParticleMaterial = arrowMaterial;
            emitter.circleParticleMaterial = standardMaterial;
            emitter.graph = graph;
            emitter.ArrowEmitRate = 1f / frequency;
            emitter.Velocities = velocities;
            emitter.Threshold = threshold;
            emitter.Speed = speed;
            emitter.UseGraphPointColors = useGraphPointColors;
            emitter.UseArrowParticle = useArrowParticle;
            graph.velocityParticleEmitter = emitter;
            if (ActiveGraphs.Count > 0 && ActiveGraphs[0].graphPointsInactive)
            {
                graph.ToggleGraphPoints();
            }


            ActiveGraphs.Add(graph);
            CellexalLog.Log("Finished reading velocity file with " + velocities.Count + " velocities");
        }

        /// <summary>
        /// Changes how often the particles should be emitted.
        /// </summary>
        /// <param name="amount">Amount to multiply the frequency by.</param>
        public void ChangeFrequency(float amount)
        {
            frequency *= amount;
            if (frequency <= 0.03125)
            {
                frequency = 0.03125f; // 1 / 32 = 0.03125
            }
            else if (frequency >= 32)
            {
                frequency = 32f;
            }

            foreach (Graph g in ActiveGraphs)
            {
                g.velocityParticleEmitter.ArrowEmitRate = 1f / frequency;
            }

            string newFrequencyString = frequency.ToString();
            if (newFrequencyString.Length > 8)
            {
                newFrequencyString = newFrequencyString.Substring(0, 8);
            }
            referenceManager.velocitySubMenu.frequencyText.text = "Frequency: " + newFrequencyString;
        }

        /// <summary>
        /// Change the speed of the emitter arrows by some amount. Speeds lower than 0.001 are set to 0.001.
        /// </summary>
        /// <param name="amount">The amount to change the speed by.</param>
        public void ChangeSpeed(float amount)
        {
            speed *= amount;
            if (speed < 0.5f)
            {
                speed = 0.5f;
            }
            else if (speed > 32f)
            {
                speed = 32f;
            }

            foreach (Graph g in ActiveGraphs)
            {
                g.velocityParticleEmitter.Speed = speed;
            }
            referenceManager.velocitySubMenu.speedText.text = "Speed: " + speed;
        }

        /// <summary>
        /// Multiples the current threshold by some amount. Thresholds lower than 0.001 is set to zero.
        /// </summary>
        /// <param name="amount">How much to multiply the threshold by.</param>
        public void ChangeThreshold(float amount)
        {
            threshold *= amount;
            if (threshold < 0.001f && amount < 1f)
            {
                threshold = 0f;
            }
            else if (threshold == 0f && amount > 1f)
            {
                threshold = 0.001f;
            }

            foreach (Graph g in ActiveGraphs)
            {
                g.velocityParticleEmitter.Threshold = threshold;
            }

            referenceManager.velocitySubMenu.thresholdText.text = "Threshold: " + threshold;
        }

        /// <summary>
        /// Toggles the graphpoints in all graphs that are showing velocity information.
        /// </summary>
        public void ToggleGraphPoints()
        {
            foreach (Graph g in ActiveGraphs)
            {
                g.ToggleGraphPoints();
            }
        }

        /// <summary>
        /// Changes between constant and synched mode.
        /// </summary>
        public void ChangeConstantSynchedMode()
        {
            if (ActiveGraphs.Count == 0)
            {
                return;
            }

            constantEmitOverTime = !ActiveGraphs[0].velocityParticleEmitter.ConstantEmitOverTime;
            foreach (Graph g in ActiveGraphs)
            {
                g.velocityParticleEmitter.ConstantEmitOverTime = constantEmitOverTime;
            }
            referenceManager.velocitySubMenu.constantSynchedModeText.text = "Mode: " + (constantEmitOverTime ? "Constant" : "Synched");
        }

        /// <summary>
        /// Changes between the gradient and graph point color modes.
        /// </summary>
        public void ChangeGraphPointColorMode()
        {
            if (ActiveGraphs.Count == 0)
            {
                return;
            }

            useGraphPointColors = !ActiveGraphs[0].velocityParticleEmitter.UseGraphPointColors;
            foreach (Graph g in ActiveGraphs)
            {
                g.velocityParticleEmitter.UseGraphPointColors = useGraphPointColors;
            }
            referenceManager.velocitySubMenu.graphPointColorsModeText.text = "Mode: " + (useGraphPointColors ? "Graphpoint colors" : "Gradient");
        }

        /// <summary>
        /// Changes the particle between the arrow and the circle.
        /// </summary>
        public void ChangeParticle()
        {
            if (ActiveGraphs.Count == 0)
            {
                return;
            }

            useArrowParticle = !ActiveGraphs[0].velocityParticleEmitter.UseArrowParticle;
            foreach (Graph g in ActiveGraphs)
            {
                g.velocityParticleEmitter.UseArrowParticle = useArrowParticle;
            }
            referenceManager.velocitySubMenu.particleMaterialText.text = "Mode: " + (useArrowParticle ? "Arrow" : "Circle");
        }

        /*
        // unused code that might be useful in the future

        private IEnumerator ReadVelocityFileCoroutine(string path)
        {
            int lastSlashIndex = path.LastIndexOfAny(new char[] { '/', '\\' });
            int lastDotIndex = path.LastIndexOf('.');
            string graphName = path.Substring(lastSlashIndex + 1, lastDotIndex - lastSlashIndex - 1);
            Graph graph = referenceManager.graphManager.FindGraph(graphName);

            GameObject empty = new GameObject();
            GameObject cubeParent = Instantiate(empty, graph.transform);
            cubeParent.name = "Cubes";
            Destroy(empty);
            graph.transform.position = new Vector3(100, 100, 100);

            Dictionary<Graph.GraphPoint, float> cellScores = new Dictionary<Graph.GraphPoint, float>();
            Dictionary<Graph.GraphPoint, List<Graph.GraphPoint>> relations = new Dictionary<Graph.GraphPoint, List<Graph.GraphPoint>>(graph.points.Count);
            Dictionary<Graph.GraphPoint, List<Graph.GraphPoint>> inverseRelations = new Dictionary<Graph.GraphPoint, List<Graph.GraphPoint>>(graph.points.Count);

            foreach (Graph.GraphPoint point in graph.points.Values)
            {
                cellScores[point] = 0f;
                relations[point] = new List<Graph.GraphPoint>();
                inverseRelations[point] = new List<Graph.GraphPoint>();
            }

            float highestScore = 0f;
            float dirInc = 0.01f;
            Vector3 extentsInc = new Vector3(0.005f, 0.005f, 0.005f);
            int maxIters = 10;
            graph.octreeRoot.ResetIteration();
            using (FileStream stream = new FileStream(path, FileMode.Open))
            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] words = line.Split(null);
                    string cellName = words[0];
                    float x = float.Parse(words[4]);
                    float y = float.Parse(words[5]);
                    float z = float.Parse(words[6]);
                    Graph.GraphPoint currentPoint = graph.FindGraphPoint(cellName);

                    Vector3 dir = (currentPoint.Position - graph.ScaleCoordinates(new Vector3(x, y, z))).normalized;
                    Vector3 dirIncVec = dir * dirInc;
                    Vector3 center = currentPoint.Position + dirIncVec;
                    Vector3 extents = extentsInc;

                    List<Graph.GraphPoint> found;
                    int iters = 0;
                    do
                    {
                        Vector3 min = center - extents;
                        Vector3 max = center + extents;
                        found = graph.MinkowskiDetection(min, max);
                        // increase extents and search again if no nodes where found
                        center += dirIncVec;
                        extents += extentsInc;
                        iters++;
                    } while (found.Count == 0 && iters < maxIters);

                    foreach (Graph.GraphPoint foundPoint in found)
                    {
                        relations[currentPoint].Add(foundPoint);
                        inverseRelations[foundPoint].Add(currentPoint);
                    }
                }

                foreach (Graph.GraphPoint point in relations.Keys)
                {
                    HashSet<Graph.GraphPoint> explored = new HashSet<Graph.GraphPoint>();
                    explored.Add(point);
                    PropagateRelations(ref explored, ref cellScores, ref relations, point);
                }

                reader.Close();
                stream.Close();
            }

            //HashSet<Graph.GraphPoint> pointsLeft = new HashSet<Graph.GraphPoint>(graph.points.Values);
            //HashSet<Graph.GraphPoint> connected = new HashSet<Graph.GraphPoint>();
            //HashSet<Graph.GraphPoint> addToConnected = new HashSet<Graph.GraphPoint>();
            //Graph.GraphPoint first = pointsLeft.First();
            //// find all connected points (the subgraph)
            //connected.Add(first);
            //AllConnectedPoints(ref relations, ref inverseRelations, first, ref connected);
            //float minx = float.MaxValue;
            //float miny = float.MaxValue;
            //float minz = float.MaxValue;
            //float maxx = float.MinValue;
            //float maxy = float.MinValue;
            //float maxz = float.MinValue;
            //foreach (Graph.GraphPoint p in connected)
            //{
            //    pointsLeft.Remove(p);
            //    Vector3 pos = p.Position;
            //    if (pos.x < minx) minx = pos.x;
            //    else if (pos.x > maxx) maxx = pos.x;
            //    if (pos.y < miny) miny = pos.y;
            //    else if (pos.y > maxy) maxy = pos.y;
            //    if (pos.z < minz) minz = pos.z;
            //    else if (pos.z > maxz) maxz = pos.z;
            //}


            // connect graph
            //HashSet<Graph.GraphPoint> pointsLeft = new HashSet<Graph.GraphPoint>(graph.points.Values);
            //HashSet<Graph.GraphPoint> connected = new HashSet<Graph.GraphPoint>();
            //HashSet<Graph.GraphPoint> addToConnected = new HashSet<Graph.GraphPoint>();
            //Graph.GraphPoint first = pointsLeft.First();
            //// find all connected points (the subgraph)
            //connected.Add(first);
            //AllConnectedPoints(ref relations, ref inverseRelations, first, ref connected);
            //foreach (Graph.GraphPoint p in connected)
            //{
            //    pointsLeft.Remove(p);
            //}
            //while (pointsLeft.Count > 0)
            //{
            //    // find closest point that isn't connected
            //    float minDist = float.MaxValue;
            //    Graph.GraphPoint minDistPointUnconnected = null;
            //    Graph.GraphPoint minDistPointConnected = null;
            //    foreach (Graph.GraphPoint unconnectedPoint in pointsLeft)
            //    {
            //        foreach (Graph.GraphPoint connectedPoint in connected)
            //        {
            //            float dist = (unconnectedPoint.Position - connectedPoint.Position).sqrMagnitude;
            //            if (dist < minDist)
            //            {
            //                minDist = dist;
            //                minDistPointUnconnected = unconnectedPoint;
            //                minDistPointConnected = connectedPoint;
            //            }
            //        }
            //    }

            //    // remove all points we just added to the previous subgraph
            //    addToConnected.Clear();
            //    addToConnected.Add(minDistPointUnconnected);
            //    AllConnectedPoints(ref relations, ref inverseRelations, minDistPointUnconnected, ref addToConnected);
            //    foreach (Graph.GraphPoint p in addToConnected)
            //    {
            //        connected.Add(p);
            //        pointsLeft.Remove(p);
            //    }

            //    // connect the subgraphs
            //    relations[minDistPointUnconnected].Add(minDistPointConnected);
            //    relations[minDistPointConnected].Add(minDistPointUnconnected);

            //}


            foreach (var rel in relations)
            {
                foreach (var p in rel.Value)
                {
                    var l = Instantiate(triprefab, cubeParent.transform);
                    var lineRenderer = l.GetComponent<LineRenderer>();
                    lineRenderer.SetPositions(new Vector3[]
                    {
                            rel.Key.Position,
                            p.Position
                    });
                    lineRenderer.startColor = Color.green;
                    lineRenderer.endColor = Color.red;
                }
            }

            //yield return new WaitForSeconds(1f);

            //Texture2D texture = graph.texture;
            //highestScore = cellScores.Values.Max();

            //foreach (KeyValuePair<Graph.GraphPoint, float> score in cellScores)
            //{
            //    Graph.GraphPoint point = score.Key;
            //    texture.SetPixel(point.textureCoord.x, point.textureCoord.y, new Color(score.Value / highestScore, 0, 0));
            //}
            //texture.Apply();

            //Texture2D colors = new Texture2D(256, 1);
            //Color[] c = Extensions.Extensions.InterpolateColors(Color.red, Color.blue, 256);
            //for (int i = 0; i < c.Length; ++i)
            //{
            //    colors.SetPixel(i, 1, c[i]);
            //}
            //colors.Apply();
            //graph.graphPointClusters[0].GetComponent<Renderer>().sharedMaterial.SetTexture("_GraphpointColorTex", colors);


            yield break;
        }

        private void PropagateRelations(ref HashSet<Graph.GraphPoint> explored, ref Dictionary<Graph.GraphPoint, float> cellScores, ref Dictionary<Graph.GraphPoint, List<Graph.GraphPoint>> relations, Graph.GraphPoint point)
        {
            foreach (Graph.GraphPoint relatedTo in relations[point])
            {
                float newScore = cellScores[point] + (point.Position - relatedTo.Position).sqrMagnitude;
                if (!explored.Contains(relatedTo) && cellScores[relatedTo] < newScore)
                {
                    explored.Add(relatedTo);
                    cellScores[relatedTo] = newScore;
                    PropagateRelations(ref explored, ref cellScores, ref relations, relatedTo);
                }
            }
        }

        private void AllConnectedPoints(ref Dictionary<Graph.GraphPoint, List<Graph.GraphPoint>> relations, ref Dictionary<Graph.GraphPoint, List<Graph.GraphPoint>> inverseRelations, Graph.GraphPoint point, ref HashSet<Graph.GraphPoint> result)
        {
            foreach (Graph.GraphPoint p in relations[point])
            {
                if (!result.Contains(p))
                {
                    result.Add(p);
                    AllConnectedPoints(ref relations, ref inverseRelations, p, ref result);
                }
            }
            foreach (Graph.GraphPoint p in inverseRelations[point])
            {

                if (!result.Contains(p))
                {
                    result.Add(p);
                    AllConnectedPoints(ref relations, ref inverseRelations, p, ref result);
                }
            }
        }
        */
    }
}
