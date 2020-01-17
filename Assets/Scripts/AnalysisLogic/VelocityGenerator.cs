using CellexalVR.AnalysisObjects;
using CellexalVR.DesktopUI;
using CellexalVR.General;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CellexalVR.AnalysisLogic
{
    public class VelocityGenerator : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public List<Graph> ActiveGraphs { get; set; }
        public GameObject particleSystemPrefab;
        public Material arrowMaterial;
        public Material standardMaterial;
        public GameObject triprefab;
        public GameObject sphereprefab;
        public GameObject convexHullPrefab;
        public Material noCullBackSideMaterial;

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
        [ConsoleCommand("velocityReader", folder: "Data", aliases: new string[] { "readvelocityfile", "rvf" })]
        public void ReadVelocityFile(string path)
        {
            StartCoroutine(ReadVelocityParticleSystem(path));
        }

        /// <summary>
        /// Reads a velocity file and starts the particle system that visualises the velcoity information for a subgraph.
        /// </summary>
        /// <param name="path">The path to the .mds file that contains the velocity information.</param>
        /// <param name="subGraphName">The name of the subgraph.</param>
        public void ReadVelocityFile(string path, string subGraphName)
        {
            StartCoroutine(ReadVelocityParticleSystem(path, subGraphName));
        }

        private IEnumerator ReadVelocityParticleSystem(string path, string subGraphName = "")
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
            string graphName = path.Substring(lastSlashIndex + 1, lastDotIndex - lastSlashIndex - 1);
            originalGraph = referenceManager.graphManager.FindGraph(graphName);
            //print(path);
            //print(lastSlashIndex);
            //print(lastDotIndex);

            //print(graphName);
            //print(originalGraph.GraphName);
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
                    float xfrom = float.Parse(words[1]);
                    float yfrom = float.Parse(words[2]);
                    float zfrom = float.Parse(words[3]);
                    float xto = float.Parse(words[4]);
                    float yto = float.Parse(words[5]);
                    float zto = float.Parse(words[6]);

                    Vector3 from = originalGraph.ScaleCoordinates(new Vector3(xfrom, yfrom, zfrom));
                    Vector3 to = originalGraph.ScaleCoordinates(new Vector3(xto, yto, zto));
                    Vector3 diff = to - from;

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

        [ConsoleCommand("velocityGenerator", aliases: new string[] { "dt", "delaunaytriangulation" })]
        public void DelaunayTriangulation(string graphName)
        {
            Graph graph = referenceManager.graphManager.FindGraph(graphName);

            if (graph is null)
            {
                graph = referenceManager.graphManager.FindGraph("");
            }

            List<Vector3> pos = new List<Vector3>(graph.points.Count);
            foreach (var gp in graph.points.Values)
            {
                pos.Add(gp.Position);
            }
            DelaunayTriangulation(graph, pos, Color.white, "all");
            CellexalEvents.CommandFinished.Invoke(true);
            //StartCoroutine(DelaunayTriangulationCoroutine(graph, pos, Color.white, "all"));
        }

        public void DelaunayTriangulation(Graph graph, List<Vector3> pos, Color color, string attributeName)
        {
            CellexalLog.Log("Started delaunay triangulation");

            System.Diagnostics.Stopwatch stopwatchTotal = new System.Diagnostics.Stopwatch();
            stopwatchTotal.Start();

            // delaunay triangulation
            List<Vector4Int> tetras = new List<Vector4Int>();
            List<float> circumRadiusesSqr = new List<float>();
            List<Vector3> circumCenters = new List<Vector3>();
            List<Vector4Int> badTetras = new List<Vector4Int>();
            float maxDist = 0.3f;

            // make debug parents
            GameObject empty = new GameObject();
            GameObject lineParent = Instantiate(empty, graph.transform);
            lineParent.name = "Lines";
            GameObject sphereParent = Instantiate(empty, graph.transform);
            sphereParent.name = "Spheres";
            Destroy(empty);

            #region helper_functions
            // helper functions for later
            void AddCircumRadiusAndCenter(Vector4Int v)
            {
                Vector3 sideA = pos[v.y] - pos[v.x];
                Vector3 sideB = pos[v.z] - pos[v.x];
                Vector3 sideC = pos[v.w] - pos[v.x];
                float a = sideA.magnitude; // side 1
                float b = sideB.magnitude; // side 2
                float c = sideC.magnitude; // side 3
                Vector3 circumSphereCenter = pos[v.x] + ((a * a * Vector3.Cross(sideB, sideC) + b * b * Vector3.Cross(sideC, sideA) + c * c * Vector3.Cross(sideA, sideB))
                                             / (2 * Vector3.Dot(sideA, Vector3.Cross(sideB, sideC))));
                float circumSphereRadius = (pos[v.x] - circumSphereCenter).sqrMagnitude;

                circumRadiusesSqr.Add(circumSphereRadius);
                circumCenters.Add(circumSphereCenter);
            }

            // first arg is an index in pos, second arg is an index in circumCenters / circumRadiuses
            bool InsideCircumSphere(int p, int c)
            {
                float dist = (circumCenters[c] - pos[p]).sqrMagnitude;
                return (p < 3 || dist < maxDist) && dist < circumRadiusesSqr[c];
            }

            // returns true if the edge between the two first args is the same as the edge between the last two args
            bool EqualEdge(int x1, int x2, int y1, int y2)
            {
                return x1 == y1 && x2 == y2 || x1 == y2 && x2 == y1;
            }

            // returns true if an edge is shared in another tetrahedron. first two args are the vertices defining the edge, third arg is the other tetrahedron
            bool SharesEdge(int v1, int v2, Vector4Int other)
            {
                return EqualEdge(v1, v2, other.x, other.y) ||
                       EqualEdge(v1, v2, other.x, other.z) ||
                       EqualEdge(v1, v2, other.x, other.w) ||
                       EqualEdge(v1, v2, other.y, other.z) ||
                       EqualEdge(v1, v2, other.y, other.w) ||
                       EqualEdge(v1, v2, other.z, other.w);
            }

            // returns true if a triangle is shared in another tetrahedron, first three args are the vertices of the triangle, fourth arg is the other tetrahedron
            bool SharesTri(int v1, int v2, int v3, Vector4Int other)
            {
                return SharesEdge(v1, v2, other) &&
                       SharesEdge(v1, v3, other) &&
                       SharesEdge(v2, v3, other);

            }

            // returns true if a tetrahedron contains a vertex that was part of the original tetrahedron, i.e. is part of the edge
            bool HasEdgeVert(Vector4Int tetra)
            {
                return tetra.x <= 3 || tetra.y <= 3 || tetra.z <= 3 || tetra.w <= 3;
            }

            bool HasOnlyOneEdgeVert(Vector4Int tetra)
            {
                if (tetra.x <= 3)
                {
                    return tetra.y > 3 && tetra.z > 3 && tetra.w > 3;
                }
                else if (tetra.y <= 3)
                {
                    return tetra.x > 3 && tetra.z > 3 && tetra.w > 3;
                }
                else if (tetra.z <= 3)
                {
                    return tetra.x > 3 && tetra.y > 3 && tetra.w > 3;
                }
                else
                {
                    return tetra.x > 3 && tetra.y > 3 && tetra.z > 3;
                }

            }

            // returns the three indices of a tetra that is not on the edge, assuming that _one_ of the vertices are on the edge and the other three are not
            Vector3Int NonEdgeVertices(Vector4Int tetra)
            {
                if (tetra.x <= 3)
                {
                    return new Vector3Int(tetra.y, tetra.z, tetra.w);
                }
                else if (tetra.y <= 3)
                {
                    return new Vector3Int(tetra.x, tetra.z, tetra.w);
                }
                else if (tetra.z <= 3)
                {
                    return new Vector3Int(tetra.x, tetra.y, tetra.w);
                }
                else
                {
                    return new Vector3Int(tetra.x, tetra.y, tetra.z);
                }
            }

            void DebugPrint(string s)
            {
                //print(s);
                CellexalLog.Log(s);
            }

            void DebugInstantiateObjects()
            {
                // remove old debug objects
                foreach (Transform t in sphereParent.transform)
                {
                    Destroy(t.gameObject);
                }
                foreach (Transform t in lineParent.transform)
                {
                    Destroy(t.gameObject);
                }
                UnityEngine.Random.InitState(0);
                for (int j = 0; j < tetras.Count; ++j)
                {
                    Vector4Int tetra = tetras[j];
                    GameObject go = Instantiate(triprefab, lineParent.transform);
                    //go.transform.parent = graph.transform;
                    //go.transform.localPosition = Vector3.zero;
                    LineRenderer lineRenderer = go.GetComponent<LineRenderer>();
                    Vector3[] scaled = new Vector3[] { pos[tetra.x], pos[tetra.y], pos[tetra.z], pos[tetra.w] };
                    // move the lines slightly away from eachother
                    float halfWidth = lineRenderer.startWidth / 2f;
                    scaled[0] -= (scaled[1] + scaled[2] + scaled[3]).normalized * halfWidth;
                    scaled[1] -= (scaled[0] + scaled[2] + scaled[3]).normalized * halfWidth;
                    scaled[2] -= (scaled[0] + scaled[1] + scaled[3]).normalized * halfWidth;
                    scaled[3] -= (scaled[0] + scaled[1] + scaled[2]).normalized * halfWidth;
                    lineRenderer.SetPositions(new Vector3[] { scaled[0], scaled[1], scaled[2], scaled[0], scaled[3], scaled[2], scaled[3], scaled[1] });
                    lineRenderer.startColor = lineRenderer.endColor = UnityEngine.Random.ColorHSV(0, 1, 0.6f, 1, 0.6f, 1, 1, 1);

                    //GameObject sphere = Instantiate(sphereprefab, sphereParent.transform);
                    //sphere.transform.localPosition = circumCenters[j];
                    //float radius = Mathf.Sqrt(circumRadiusesSqr[j]);
                    //sphere.transform.localScale = new Vector3(radius, radius, radius) * 2f;
                    ////print(" radius " + radius);
                    //Material m = new Material(sphere.GetComponent<MeshRenderer>().material);
                    //m.color = UnityEngine.Random.ColorHSV(0, 1, 0.6f, 1, 0.6f, 1, 0.2f, 0.2f);

                    //sphere.GetComponent<MeshRenderer>().material = m;
                }
            }
            #endregion

            // add a tetrahedron that contains all points
            pos.InsertRange(0, new Vector3[] {
                new Vector3(0, 2f, -1f),
                new Vector3(3f, -2f, -1f),
                new Vector3(-3f, -2f, -1f),
                new Vector3(0, 0, 3f)
            });
            tetras.Add(new Vector4Int(0, 1, 2, 3));
            AddCircumRadiusAndCenter(tetras[0]);

            for (int i = 4; i < pos.Count; ++i)
            {

                // find bad tetras
                badTetras.Clear();
                for (int j = 0; j < tetras.Count; ++j)
                {
                    if (InsideCircumSphere(i, j))
                    {
                        badTetras.Add(tetras[j]);
                        tetras.RemoveAt(j);
                        circumCenters.RemoveAt(j);
                        circumRadiusesSqr.RemoveAt(j);
                        j--;
                    }
                }

                // find the boundary of the polygonal hole
                List<int> polygon = new List<int>();
                for (int j = 0; j < badTetras.Count; ++j)
                {
                    bool sharedTrixyz = false;
                    bool sharedTrixyw = false;
                    bool sharedTrixzw = false;
                    bool sharedTriyzw = false;
                    for (int k = 0; k < badTetras.Count; ++k)
                    {
                        if (k == j)
                        {
                            continue;
                        }
                        Vector4Int v1 = badTetras[j];
                        Vector4Int v2 = badTetras[k];
                        // add any triangle that is not shared in any other tetra in badTetras
                        if (!sharedTrixyz)
                        {
                            sharedTrixyz = SharesTri(v1.x, v1.y, v1.z, v2);
                        }
                        if (!sharedTrixyw)
                        {
                            sharedTrixyw = SharesTri(v1.x, v1.y, v1.w, v2);
                        }
                        if (!sharedTrixzw)
                        {
                            sharedTrixzw = SharesTri(v1.x, v1.z, v1.w, v2);
                        }
                        if (!sharedTriyzw)
                        {
                            sharedTriyzw = SharesTri(v1.y, v1.z, v1.w, v2);
                        }

                    }
                    if (!sharedTrixyz)
                    {
                        polygon.Add(badTetras[j].x);
                        polygon.Add(badTetras[j].y);
                        polygon.Add(badTetras[j].z);
                    }
                    if (!sharedTrixyw)
                    {
                        polygon.Add(badTetras[j].x);
                        polygon.Add(badTetras[j].y);
                        polygon.Add(badTetras[j].w);
                    }
                    if (!sharedTrixzw)
                    {
                        polygon.Add(badTetras[j].x);
                        polygon.Add(badTetras[j].z);
                        polygon.Add(badTetras[j].w);
                    }
                    if (!sharedTriyzw)
                    {
                        polygon.Add(badTetras[j].y);
                        polygon.Add(badTetras[j].z);
                        polygon.Add(badTetras[j].w);
                    }
                }

                for (int j = 0; j < polygon.Count; j += 3)
                {
                    Vector4Int newTetra = new Vector4Int(polygon[j], polygon[j + 1], polygon[j + 2], i);
                    if (HasEdgeVert(newTetra))
                    {
                        tetras.Add(newTetra);
                        AddCircumRadiusAndCenter(newTetra);
                    }
                }
            }

            CellexalLog.Log("Tetrahedrons count after triangulation: " + tetras.Count);

            GameObject convexHullGameObject = Instantiate(convexHullPrefab);
            MeshFilter meshFilter = convexHullGameObject.GetComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            Vector3[] verts = new Vector3[tetras.Count * 3];
            int[] tris = new int[tetras.Count * 3];
            for (int i = 0, tetraIndex = 0; tetraIndex < tetras.Count; i += 3, tetraIndex += 1)
            {
                if (!HasOnlyOneEdgeVert(tetras[tetraIndex]))
                {
                    continue;
                }

                Vector3Int nonEdgeVerts = NonEdgeVertices(tetras[tetraIndex]);
                verts[i] = pos[nonEdgeVerts.x];
                verts[i + 1] = pos[nonEdgeVerts.y];
                verts[i + 2] = pos[nonEdgeVerts.z];
                tris[i] = i;
                tris[i + 1] = i + 1;
                tris[i + 2] = i + 2;
            }
            mesh.vertices = verts;
            mesh.triangles = tris;
            //MeshUtility.Optimize(mesh);

            CellexalLog.Log("Mesh vertices after optimization: " + mesh.vertexCount);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;
            convexHullGameObject.name = graph.GraphName + "_" + attributeName;
            convexHullGameObject.transform.position = graph.transform.position;
            convexHullGameObject.transform.rotation = graph.transform.rotation;
            convexHullGameObject.transform.parent = graph.transform;
            color.a = 0.2f;
            convexHullGameObject.gameObject.GetComponent<MeshRenderer>().material.color = color;
            referenceManager.cellManager.convexHulls[graph.GraphName + "_" + attributeName] = convexHullGameObject;

            stopwatchTotal.Stop();
            CellexalLog.Log("Finished delaunay triangulation in " + stopwatchTotal.Elapsed.ToString());
        }

        /*
        private class GraphNode
        {
            //Vector3 pos;
            public List<GraphNode> neighbours;

            public GraphNode()
            {
                neighbours = new List<GraphNode>();
            }

            public void Connect(GraphNode other)
            {
                neighbours.Add(other);
                other.neighbours.Add(this);
            }
        }
        */
    }

    struct Vector4Int
    {
        public int x;
        public int y;
        public int z;
        public int w;

        public Vector4Int(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static Vector4Int operator +(Vector4Int v1, Vector4Int v2)
        {
            return new Vector4Int(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z, v1.w + v2.w);
        }
    }

}
