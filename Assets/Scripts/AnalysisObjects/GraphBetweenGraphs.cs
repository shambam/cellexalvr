using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CellexalVR.General;
using CellexalVR.SceneObjects;
using System.Linq;
using CellexalVR.DesktopUI;
using System;
using System.Threading;

namespace CellexalVR.AnalysisObjects
{
    /// <summary>
    /// Represents a graph that is created between to graphs when using cell to cell tracking.
    /// A selection is made in one graph and the respective points are found in another.
    /// This graph is created where each points position is in the middle of the two corresponding points.
    /// Lines goes from points in one graph through this graph and finally to the other graph.
    /// Graphpoints in this graph behave just as in normal graphs and can be recoloured in every way.
    /// </summary>
    public class GraphBetweenGraphs : MonoBehaviour
    {
        public Graph graph1, graph2;
        public ReferenceManager referenceManager;
        public GameObject lineBetweenTwoGraphPointsPrefab;
        public GameObject pointClusterPrefab;
        public GameObject clusterDebugBox;


        private GameObject velocityParticleSystemPrefab;
        private Graph graph;
        private Transform t1, t2;
        private List<LineBetweenTwoPoints> lines = new List<LineBetweenTwoPoints>();
        private List<PointCluster> pointClusters = new List<PointCluster>();
        private LineBetweenTwoPoints firstLine;
        private List<LineBetweenTwoPoints> orderedLines = new List<LineBetweenTwoPoints>();
        private Dictionary<Graph.GraphPoint, Vector3> velocitiesFromGraph = new Dictionary<Graph.GraphPoint, Vector3>();
        private Dictionary<Graph.GraphPoint, Vector3> velocitiesMidGraph = new Dictionary<Graph.GraphPoint, Vector3>();
        private Dictionary<Graph.GraphPoint, Vector3> velocitiesToGraph = new Dictionary<Graph.GraphPoint, Vector3>();
        private GameObject velocityParticleSystemFromGraph;
        private GameObject velocityParticleSystemMidGraph;
        private GameObject velocityParticleSystemToGraph;
        private List<GameObject> particleSystems = new List<GameObject>();
        private int clusterCount = 0;
        private List<Graph.GraphPoint> graphPoints;
        private List<Vector3> centroids = new List<Vector3>();
        private List<Vector3> toGraphCentroids = new List<Vector3>();
        private List<Tuple<HashSet<Graph.GraphPoint>, Vector3>> clusters = new List<Tuple<HashSet<Graph.GraphPoint>, Vector3>>();
        private List<Tuple<HashSet<Graph.GraphPoint>, Vector3>> toGraphClusters = new List<Tuple<HashSet<Graph.GraphPoint>, Vector3>>();

        private int clusterNr;
        //private bool isLargeSet;
        // Use this for initialization
        void Start()
        {
            t1 = graph1.transform;
            t2 = graph2.transform;
            velocityParticleSystemPrefab = referenceManager.velocityGenerator.particleSystemPrefab;
        }

        // Update is called once per frame
        void Update()
        {
            if (t1 == null)
            {
                graph1.ctcGraphs.Remove(gameObject);
                RemoveGraph();
                return;
            }
            if (t2 == null)
            {
                graph2.ctcGraphs.Remove(gameObject);
                RemoveGraph();
                return;
            }
            if (!(t1.gameObject.activeSelf && t2.gameObject.activeSelf))
            {
                gameObject.SetActive(false);
            }
            if (t1.hasChanged || t2.hasChanged)
            {
                transform.position = (t1.position + t2.position) / 2f;
            }
        }

        /// <summary>
        /// Draws lines between graphpoints representing the same cell in to different graphs (i.e share the same label).
        /// </summary>
        /// <param name="points"> The graphpoints to draw the lines from. </param>
        /// /// <param name="newGraph">New graph containing the points between the two graphs. </param>
        /// /// <param name="fromGraph">The graph the lines go from. </param>
        /// /// <param name="toGraph"> The graph to draw lines to. </param>
        public void CreateGraphBetweenGraphs(List<Graph.GraphPoint> points, Graph newGraph, Graph fromGraph, Graph toGraph)
        {
            graph = newGraph;
            graphPoints = points;
            //isLargeSet = graphPoints.Count > 10000;
            newGraph.GraphName = "CTC_" + fromGraph.GraphName + "_" + toGraph.GraphName;
            newGraph.tag = "Untagged";
            newGraph.transform.position = fromGraph.transform.position + (toGraph.transform.position - fromGraph.transform.position) / 2f;
            newGraph.transform.localScale /= 2;
            newGraph.SetInfoTextVisible(false);
            foreach (Graph.GraphPoint g in points)
            {
                var sourceCell = fromGraph.points[g.Label];
                var targetCell = toGraph.FindGraphPoint(g.Label);
                if (targetCell == null)
                {
                    continue;
                }
                var fromGpPos = sourceCell.WorldPosition;
                var toGpPos = targetCell.WorldPosition;
                var midPosition = fromGpPos + (toGpPos - fromGpPos) / 2f;
                var gp = referenceManager.graphGenerator.AddGraphPoint(referenceManager.cellManager.GetCell(g.Label),
                                                                        toGpPos.x, toGpPos.y, toGpPos.z);
            }
            if (newGraph.points.Count == 0)
            {
                Destroy(newGraph.gameObject);
                referenceManager.graphGenerator.isCreating = false;
                return;
            }
            referenceManager.graphGenerator.SliceClustering();
            referenceManager.graphManager.Graphs.Add(newGraph);
            fromGraph.ctcGraphs.Add(newGraph.gameObject);
            toGraph.ctcGraphs.Add(newGraph.gameObject);
            if (!(fromGraph.graphActive && toGraph.graphActive))
            {
                gameObject.SetActive(false);
            }
        }
        /// <summary>
        /// Main function for clustering the lines. If many lines are present it is convenient to cluster them together for better visibility and less lag.
        /// Start by clustering the two graphs using mean shift clustering. Assign points to the clusters and then check if many points in a cluster in one graph goes to the same cluster in the other.
        /// These are points that should be bundles together. Points that do not cluster in both graphs are not bundled but rendered normally.
        /// </summary>
        /// <param name="points">The selection points.</param>
        /// <param name="fromGraph"> THe graph the points were selected from. The lines goes FROM one graph TO another.</param>
        /// <param name="toGraph">The other graph.</param>
        /// <param name="clusterSize">To be considered a cluster and for the points to be bundled there has to be this many points.</param>
        /// <param name="neighbourDistance">The distance to other points to be considered in the same cluster. </param>
        public IEnumerator ClusterLines(int clusterSize = 10,
                                    float neighbourDistance = 0.10f, float kernelBandwidth = 2.0f, bool bundle = false)
        {
            List<Graph.GraphPoint> toGraphPoints = new List<Graph.GraphPoint>();
            foreach (Graph.GraphPoint point in graphPoints)
            {
                var gp = graph2.FindGraphPoint(point.Label);
                if (gp != null)
                {
                    //Graph.GraphPoint newGp = gp;
                    //newGp.Position = newGpWorldPos;
                    toGraphPoints.Add(gp);
                    //graph.FindGraphPoint(point.Label).ColorSelectionColor(point.Group, false);
                }
                else
                {
                    continue;
                }
            }
            Thread t = new Thread(
                () =>
                {
                    DoClustering(graphPoints, toGraphPoints, neighbourDistance, kernelBandwidth);
                });
            t.Start();
            while (t.IsAlive)
            {
                yield return null;
            }
            HashSet<Graph.GraphPoint> prevjoinedclusters = new HashSet<Graph.GraphPoint>();
            if (bundle)
            {
                for (int i = 0; i < clusters.Count; i++)
                {
                    var fromCluster = clusters[i];
                    for (int j = 0; j < toGraphClusters.Count; j++)
                    {
                        var toCluster = toGraphClusters[j];
                        if (!(fromCluster.Item1.Count > clusterSize && toCluster.Item1.Count > clusterSize))
                        {
                            continue;
                        }
                        var joinedCluster = from gpfrom in fromCluster.Item1
                                            join gpto in toCluster.Item1 on gpfrom.Label equals gpto.Label
                                            select gpfrom;
                        if (joinedCluster.ToList().Count > clusterSize)
                        {
                            prevjoinedclusters.UnionWith(joinedCluster);
                            AddCluster(graph1, graph2, joinedCluster);
                        }
                    }
                    yield return null;
                }
                // if (clusters.Count > 0)
                // {
                //     AddParticles(graph1, graph2);
                // }
            }

            yield return null;
            var pointsOutsideClusters = graphPoints.Except(prevjoinedclusters);
            foreach (Graph.GraphPoint point in pointsOutsideClusters)
            {
                AddLine(graph1, graph2, point);
            }

        }
        /// <summary>
        /// Function to run the heavier calculations in a separate thread. 
        /// </summary>
        /// <param name="graphPoints"></param>
        /// <param name="toGraphPoints"></param>
        /// <param name="distance"></param>
        /// <param name="kernelBandwidth"></param>
        private void DoClustering(List<Graph.GraphPoint> graphPoints, List<Graph.GraphPoint> toGraphPoints, float distance, float kernelBandwidth)
        {
            distance = (graphPoints.Count > (0.2f * graph1.points.Count)) ? 0.20f : 0.10f; // Larger selections might benefit from more course clustering.
            centroids = MeanShiftClustering(graphPoints, neighbourDistance: distance, kernelBandwidth: kernelBandwidth);
            toGraphCentroids = MeanShiftClustering(toGraphPoints, neighbourDistance: distance, kernelBandwidth: kernelBandwidth);
            clusters = AssignPointsToClusters(centroids, graphPoints, distance);
            toGraphClusters = AssignPointsToClusters(toGraphCentroids, toGraphPoints, distance);
        }

        /// <summary>
        /// Mean shift clustering to find clusters with high density. Read more about it here https://en.wikipedia.org/wiki/Mean_shift.
        /// With some modifications to suit the needs of this application.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="iterations"></param>
        /// <param name="neighbourDistance"></param>
        /// <param name="kernelBandwidth"></param>
        /// <returns></returns>
        public List<Vector3> MeanShiftClustering(List<Graph.GraphPoint> points, int iterations = 3, float neighbourDistance = 0.05f, float kernelBandwidth = 2.5f)
        {
            List<Vector3> centroids = new List<Vector3>();
            // Create grid of points that cover the graph area. Graph resides in a cube from -0.5 - 0.5. 
            int gridSize = 5;
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    for (int k = 0; k < gridSize; k++)
                    {
                        Vector3 v = new Vector3(-0.5f + (i * 1f / 4), -0.5f + (j * 1f / 4), -0.5f + (k * 1f / 4));
                        centroids.Add(v);
                    }
                }
            }
            List<List<Vector3>> oldPoints = new List<List<Vector3>>();
            Vector3 meanShift;
            for (int n = 0; n < iterations; n++)
            {
                for (int i = 0; i < centroids.Count; i++)
                {
                    Vector3 centroid = centroids[i];
                    // Step 1: Calculate neighbouring points N(x) for each point x
                    var neighbours = GetNeighbours(points, centroid, neighbourDistance);
                    // We dont want all the centroids to converge.
                    // It is better to keep more clusters to be able to bundle more lines together.
                    //if (neighbours.Count > 50)
                    //{
                    //    continue;
                    //}
                    if (neighbours.Count == 0)
                    {
                        centroids.RemoveAt(i);
                        continue;
                    }
                    // Step 2: For each point calculate the mean shift m(x)
                    Vector3 nom = Vector3.zero;
                    float denom = 0.0f;
                    foreach (Tuple<Graph.GraphPoint, int> neighbour in neighbours)
                    {
                        float distance = Vector3.Distance(neighbour.Item1.Position, centroid);
                        float weight = GaussianKernel(distance, kernelBandwidth);
                        nom += weight * neighbour.Item1.Position;
                        denom += weight;
                    }
                    meanShift = nom / denom;
                    // Step 3: Update each meanshift for the points x <- m(x)
                    centroids[i] = meanShift;
                }
                oldPoints.Add(centroids);
            }
            return centroids;
        }

        private List<Tuple<Graph.GraphPoint, int>> GetNeighbours(List<Graph.GraphPoint> points, Vector3 centroid, float distance = 0.15f)
        {
            List<Tuple<Graph.GraphPoint, int>> neighbours = new List<Tuple<Graph.GraphPoint, int>>();
            //List<Graph.GraphPoint> neighbours = points.FindAll(x => Vector3.Distance(centroid, x.Position) < distance);
            for (int i = 0; i < points.Count; i++)
            {
                if (Vector3.Distance(centroid, points[i].Position) < distance)
                {
                    Tuple<Graph.GraphPoint, int> tuple = new Tuple<Graph.GraphPoint, int>(points[i], i);
                    neighbours.Add(tuple);
                }
            }
            return neighbours;
        }

        private float GaussianKernel(float distance, float bandwidth)
        {
            return (1 / bandwidth * Mathf.Sqrt(2 * Mathf.PI)) * Mathf.Exp(-0.5f * (Mathf.Pow(distance / bandwidth, 2)));
        }

        private List<Tuple<HashSet<Graph.GraphPoint>, Vector3>> AssignPointsToClusters(List<Vector3> centroids, List<Graph.GraphPoint> points, float distance = 0.10f)
        {
            //Graph.GraphPoint[] gps = points.ToArray();
            //List<Graph.GraphPoint> gps = new List<Graph.GraphPoint>(points);
            HashSet<Graph.GraphPoint> gps = new HashSet<Graph.GraphPoint>(points);
            List<Tuple<HashSet<Graph.GraphPoint>, Vector3>> clusters = new List<Tuple<HashSet<Graph.GraphPoint>, Vector3>>();
            List<Vector3> previousClusters = new List<Vector3>();
            foreach (Vector3 centroid in centroids)
            {
                var neighbours = GetNeighbours(gps.ToList(), centroid, distance);
                List<Tuple<Graph.GraphPoint, int>> ps = new List<Tuple<Graph.GraphPoint, int>>(neighbours);
                //HashSet<Graph.GraphPoint> ps = new HashSet<Graph.GraphPoint>(neighbours);
                Vector3 center = CalculateCentroid(ps); //calculate center of points to get better looking lines.
                if (previousClusters.Any(x => Vector3.Distance(x, center) < (distance)))
                {
                    continue;
                }
                var centerNeighbours = GetNeighbours(gps.ToList(), center, distance);
                //var centerNeighbours = neighbours;
                if (neighbours.Count == 0)
                {
                    continue;
                }
                HashSet<Graph.GraphPoint> cluster = new HashSet<Graph.GraphPoint>();
                int currentGroup = centerNeighbours[0].Item1.Group;
                //foreach (Graph.GraphPoint gp in centerNeighbours)
                for (int i = 0; i < centerNeighbours.Count; i++)
                {
                    if (centerNeighbours[i].Item1.Group != currentGroup)
                    {
                        break;
                    }
                    cluster.Add(centerNeighbours[i].Item1);
                    //gps[centerNeighbours[i].Item2];
                    //print("neighbour count : " + centerNeighbours.Count + ", gps count : " + gps.Count + ", " + i + " - " + centerNeighbours[i].Item2);
                    //gps.RemoveAt(centerNeighbours[i].Item2);
                }
                var newGps = gps.Except(cluster);
                gps = new HashSet<Graph.GraphPoint>(newGps);
                clusters.Add(new Tuple<HashSet<Graph.GraphPoint>, Vector3>(cluster, center));
                previousClusters.Add(center);
                //Draw centroid boxes
                //GameObject obj = Instantiate(clusterDebugBox, points[0].parent.transform);
                //obj.transform.localPosition = center;

            }
            return clusters;

        }

        /// <summary>
        /// Adds point clusters in corresponding from, mid and to graphs.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="fromCluster"></param>
        /// <param name="toCluster"></param>
        /// <param name="joinedCluster"></param>
        private void AddCluster(Graph from, Graph to, IEnumerable<Graph.GraphPoint> joinedCluster)
        {
            HashSet<Graph.GraphPoint> fromCluster = new HashSet<Graph.GraphPoint>();
            HashSet<Graph.GraphPoint> midCluster = new HashSet<Graph.GraphPoint>();
            HashSet<Graph.GraphPoint> toCluster = new HashSet<Graph.GraphPoint>();
            HashSet<Vector3> prevAddedHulls = new HashSet<Vector3>();
            foreach (Graph.GraphPoint gp in joinedCluster)
            {
                fromCluster.Add(from.FindGraphPoint(gp.Label));
                midCluster.Add(graph.FindGraphPoint(gp.Label));
                toCluster.Add(to.FindGraphPoint(gp.Label));
            }
            PointCluster pointCluster = Instantiate(pointClusterPrefab).GetComponent<PointCluster>();
            pointCluster.referenceManager = referenceManager;
            pointCluster.ClusterId = clusterNr;
            pointCluster.gameObject.name = "PointCluster" + clusterCount++;
            pointClusters.Add(pointCluster);
            pointCluster.rightController = referenceManager.rightController;
            Vector3 fromCentroid = CalculateCentroid(fromCluster);
            Vector3 midCentroid = CalculateCentroid(midCluster);
            Vector3 toCentroid = CalculateCentroid(toCluster);
            //Vector3 fromClusterHull = CalculateClusterHull(fromCluster, fromCentroid);
            Vector3 midClusterHull = CalculateClusterHull(midCluster, midCentroid);
            //Vector3 toClusterHull = CalculateClusterHull(toCluster, toCentroid);
            //if (!prevAddedHulls.Contains(fromClusterHull))
            //{
            //    prevAddedHulls.Add(fromClusterHull);
            //}
            if (!prevAddedHulls.Contains(midClusterHull))
            {
                prevAddedHulls.Add(midClusterHull);
            }
            //if (!prevAddedHulls.Contains(toClusterHull))
            //{
            //    prevAddedHulls.Add(toClusterHull);
            //}
            pointCluster.t1 = from.transform;
            pointCluster.t2 = to.transform;
            pointCluster.t3 = graph.transform;
            pointCluster.fromGraphCentroid = fromCentroid;
            pointCluster.midGraphCentroid = midCentroid;
            pointCluster.toGraphCentroid = toCentroid;
            pointCluster.fromPointCluster = fromCluster;
            pointCluster.midPointCluster = midCluster;
            pointCluster.toPointCluster = toCluster;


            LineBetweenTwoPoints line = AddBundledLine(from, to, midCluster);
            pointCluster.LineColor = line.LineColor;
            pointCluster.lineRenderer = line.GetComponent<LineRenderer>();
            line.midClusterHull = midClusterHull;
            line.transform.parent = pointCluster.transform;
            line.fromGraphCentroid = fromCentroid;
            line.midGraphCentroid = midCentroid;
            line.toGraphCentroid = toCentroid;
            line.fromPointCluster = fromCluster;
            line.midPointCluster = midCluster;
            line.toPointCluster = toCluster;

            Vector3 dir;
            foreach (Graph.GraphPoint gp in fromCluster)
            {
                dir = fromCentroid - gp.Position;
                if (dir.magnitude > 0.05f)
                {
                    velocitiesFromGraph[gp] = dir / 5f;
                }
            }

            foreach (Graph.GraphPoint gp in toCluster)
            {
                dir = toCentroid - gp.Position;
                if (dir.magnitude > 0.05f)
                {
                    velocitiesToGraph[gp] = dir / 5f;
                }
            }
            foreach (Graph.GraphPoint gp in midCluster)
            {
                dir = midCentroid - gp.Position;
                if (dir.magnitude > 0.05f)
                {
                    velocitiesMidGraph[gp] = dir / 5f;
                }
            }
            clusterNr++;
        }

        private LineBetweenTwoPoints AddBundledLine(Graph from, Graph to, IEnumerable<Graph.GraphPoint> cluster)
        {
            LineBetweenTwoPoints line = Instantiate(lineBetweenTwoGraphPointsPrefab).GetComponent<LineBetweenTwoPoints>();
            line.t1 = from.transform;
            line.t2 = to.transform;
            line.t3 = graph.transform;

            line.selectionManager = referenceManager.selectionManager;
            LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
            Color color = from.FindGraphPoint(cluster.ToList()[0].Label).GetColor();
            //float alpha = isLargeSet ? 0.1f : 0.5f;
            line.LineColor = new Color(color.r, color.g, color.b, 0.2f);
            lineRenderer.startColor = lineRenderer.endColor = line.LineColor;
            lines.Add(line);
            //line.transform.parent = graph.lineParent.transform;
            line.centroids = true;
            line.gameObject.SetActive(true);
            return line;
        }

        private void AddParticles(Graph from, Graph to)
        {
            if (velocitiesFromGraph.Count > 0)
            {
                velocityParticleSystemFromGraph = Instantiate(velocityParticleSystemPrefab, from.transform);
                InitParticleSystem(velocityParticleSystemFromGraph, from, velocitiesFromGraph);
            }

            if (velocitiesMidGraph.Count > 0)
            {
                velocityParticleSystemMidGraph = Instantiate(velocityParticleSystemPrefab, graph.transform);
                InitParticleSystem(velocityParticleSystemMidGraph, graph, velocitiesMidGraph);
                velocityParticleSystemMidGraph.transform.localScale /= 2;
            }

            if (velocitiesToGraph.Count > 0)
            {
                velocityParticleSystemToGraph = Instantiate(velocityParticleSystemPrefab, to.transform);
                InitParticleSystem(velocityParticleSystemToGraph, to, velocitiesToGraph);
            }
        }

        private Vector3 CalculateCentroid(HashSet<Graph.GraphPoint> cluster)
        {
            Vector3 centroid = new Vector3();
            //Transform parent;
            foreach (Graph.GraphPoint gp in cluster)
            {
                centroid += gp.Position;
                //parent = gp.parent.transform;
            }
            centroid /= cluster.Count;
            //GameObject obj = Instantiate(clusterDebugBox, parent);
            //obj.transform.localPosition = centroid;
            return centroid;
        }
        private Vector3 CalculateCentroid(List<Tuple<Graph.GraphPoint, int>> cluster)
        {
            Vector3 centroid = new Vector3();
            //Transform parent;
            foreach (Tuple<Graph.GraphPoint, int> gp in cluster)
            {
                centroid += gp.Item1.Position;
                //parent = gp.parent.transform;
            }
            centroid /= cluster.Count;
            //GameObject obj = Instantiate(clusterDebugBox, parent);
            //obj.transform.localPosition = centroid;
            return centroid;
        }

        public Vector3 CalculateClusterHull(HashSet<Graph.GraphPoint> cluster, Vector3 centroid)
        {
            float distance;
            float maxDistance = 0f;
            foreach (Graph.GraphPoint gp in cluster)
            {
                distance = Vector3.Distance(centroid, gp.Position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                }
            }
            return new Vector3(maxDistance, maxDistance, maxDistance);
        }


        /// <summary>
        /// Adds line that goes from a point in one graph to respective point in another.
        /// </summary>
        /// <param name="from">Graph that lines goes from.</param>
        /// <param name="to">Graph that line goes to.</param>
        /// <param name="point">The graphpoint that line goes between.</param>
        public LineBetweenTwoPoints AddLine(Graph from, Graph to, Graph.GraphPoint point)
        {
            Color color = point.GetColor();
            to.points.TryGetValue(point.Label, out Graph.GraphPoint targetCell);
            if (targetCell == null)
            {
                return null;
            }
            else
            {
                var sourceCell = from.points[point.Label];
                LineBetweenTwoPoints line = Instantiate(lineBetweenTwoGraphPointsPrefab, graph.lineParent.transform).GetComponent<LineBetweenTwoPoints>();
                line.t1 = sourceCell.parent.transform;
                line.t2 = targetCell.parent.transform;
                line.graphPoint1 = sourceCell;
                line.graphPoint2 = targetCell;
                //var midPosition = (line.t1.TransformPoint(sourceCell.Position) + line.t2.TransformPoint(targetCell.Position)) / 2f;
                var gp = graph.FindGraphPoint(point.Label);
                line.graphPoint3 = gp;
                line.t3 = gp.parent.transform;
                line.selectionManager = referenceManager.selectionManager;
                LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
                //float alpha = isLargeSet ? 0.05f : 0.1f;
                lineRenderer.startColor = lineRenderer.endColor = new Color(color.r, color.g, color.b, 0.08f);
                lines.Add(line);
                //line.transform.parent = graph.lineParent.transform;
                line.gameObject.SetActive(true);
                return line;
            }
        }

        public void TogglePointClusterColliders(bool b, string exception)
        {
            foreach (PointCluster pc in pointClusters.FindAll(x => x.gameObject.name != exception))
            {
                pc.GetComponent<BoxCollider>().enabled = b;
            }
        }

        public void RemoveLines()
        {
            foreach (LineBetweenTwoPoints line in lines)
            {
                Destroy(line.gameObject);
            }
            lines.Clear();
        }

        public void RemoveClusters()
        {
            foreach (PointCluster pc in pointClusters)
            {
                Destroy(pc.gameObject);
            }
            foreach (GameObject obj in particleSystems)
            {
                Destroy(obj);
            }
            particleSystems.Clear();
            pointClusters.Clear();
        }

        public void RemoveGraph()
        {
            graph1.ctcGraphs.Remove(gameObject);
            graph2.ctcGraphs.Remove(gameObject);
            referenceManager.graphManager.Graphs.Remove(GetComponent<Graph>());
            // bundle lines Destroy(velocityParticleSystemFromGraph.gameObject);
            // bundle lines Destroy(velocityParticleSystemMidGraph.gameObject);
            // bundle lines Destroy(velocityParticleSystemToGraph.gameObject);
            RemoveLines();
            RemoveClusters();
            clusterCount = 0;
            Destroy(this.gameObject);
        }


        /// <summary>
        /// Helper function to initiate start values for particle system.
        /// </summary>
        private void InitParticleSystem(GameObject obj, Graph parent, Dictionary<Graph.GraphPoint, Vector3> velocities)
        {
            VelocityParticleEmitter emitter = obj.GetComponent<VelocityParticleEmitter>();
            //var particleSystem = obj.GetComponent<ParticleSystem>();
            //emitter.particleSystem = particleSystem;
            emitter.referenceManager = referenceManager;
            emitter.arrowParticleMaterial = referenceManager.velocityGenerator.arrowMaterial;
            emitter.circleParticleMaterial = referenceManager.velocityGenerator.standardMaterial;
            emitter.graph = parent;
            emitter.ArrowEmitRate = 1f / 0.5f;
            emitter.Velocities = velocities;
            emitter.Threshold = 0f;
            emitter.Speed = 2.0f;
            emitter.UseGraphPointColors = true;
            //emitter.UseArrowParticle = false;
            emitter.ConstantEmitOverTime = false;
            particleSystems.Add(obj);
            //emitter.ChangeFrequency(4.0f);
            //ParticleSystem.TrailModule trailModule = particleSystem.GetComponent<ParticleSystem>().trails;
            //trailModule.enabled = true;
            //trailModule.lifetime = 2.0f;
            //trailModule.inheritParticleColor = true;
            //trailModule.widthOverTrail = 0.010f;
            //trailModule.dieWithParticles = true;
            //emitter.particleMaterial = referenceManager.velocityGenerator.standardMaterial;
        }

        public PointCluster GetCluster(int id)
        {
            foreach (PointCluster cluster in pointClusters)
            {
                if (cluster.ClusterId == id)
                {
                    return cluster;
                }
            }
            return null;
        }
    }
}
