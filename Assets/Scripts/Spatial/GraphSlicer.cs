using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace CellexalVR.Spatial
{
    public class GraphSlicer : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public Slicer slicer;
        public SpatialGraph spatialGraph;


        private Graph graph;
        private GraphGenerator graphGenerator;
        private bool removingOldSlices;
        private bool buildingGraph;
        private Plane plane;
        private List<Graph.GraphPoint> sortedPointsX;
        private List<Graph.GraphPoint> sortedPointsY;
        private List<Graph.GraphPoint> sortedPointsZ;
        private Dictionary<string, Graph.GraphPoint> pointsDict = new Dictionary<string, Graph.GraphPoint>();
        private GraphSlice slice;
        private bool slicerInside;
        private List<Graph> childSlices = new List<Graph>();
        private SliceManager sliceManager;


        private void Start()
        {
            slice = GetComponent<GraphSlice>();
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            graph = GetComponent<Graph>();
            spatialGraph = slice.spatialGraph;
            slicerInside = true;
        }

        private void Update()
        {
            if (referenceManager.consoleManager.consoleGameObject.activeSelf) return;
            if (slicer == null || !slicer.gameObject.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.K))
            {
                StartCoroutine(SliceGraph(true, 0, activateSlices: true));
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                StartCoroutine(SliceGraph(true, 1, activateSlices: true));
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                StartCoroutine(SliceGraph(activateSlices: true));
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                if (slicerInside)
                {
                    StartCoroutine(SliceGraph(false, activateSlices: true));
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name.Equals("Slicer"))
            {
                slicerInside = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name.Equals("Slicer"))
            {
                slicerInside = false;
            }
        }


        public IEnumerator SliceGraph(bool automatic = true, int axis = 2, bool activateSlices = false)
        {
            GC.Collect();
            Resources.UnloadUnusedAssets();
            Dictionary<string, Color32> oldTextureColors1 = SaveOldTexture();
            Dictionary<string, Color32> oldTextureColors2 = SaveOldTexture(1);
            
            Dictionary<string, Color32>[] oldTextures = new Dictionary<string, Color32>[2];
            oldTextures[0] = oldTextureColors1;
            oldTextures[1] = oldTextureColors2;
            
            List<GraphSlice> sls = new List<GraphSlice>();

            if (automatic)
            {
                sls = AutoDividePointsIntoSlices(slice.points, axis);
            }

            else
            {
                sls = ManuallyDividePointsIntoSections(slice.points);
            }


            if (sls.All(x => x.points.Count > 0))
            {
                sliceManager = gameObject.GetComponent<SliceManager>();
                if (!sliceManager)
                {
                    sliceManager = gameObject.AddComponent<SliceManager>();
                }

                sliceManager.referenceManager = referenceManager;
                sls.ForEach(s => s.sliceCoords[axis] = -0.5f + s.SliceNr * (1f / (sls.Count - 1)));
                StartCoroutine(BuildSpat(sls.ToArray()));
                while (sls.Any(x => x.buildingSlice) || slicer.sliceAnimationActive)
                {
                    yield return null;
                }
                
                RemoveOldSlice();
                for (int i = 0; i < sls.Count; i++)
                {
                    GraphSlice gs = sls[i].GetComponent<GraphSlice>();
                    Graph graph = gs.GetComponent<Graph>();
                    childSlices.Add(graph);
                    gs.transform.parent = sliceManager.transform;
                    sliceManager.slices.Add(gs);

                    foreach (KeyValuePair<string, Graph.GraphPoint> point in gs.points)
                    {
                        for (int k = 0; k < graph.lodGroups; k++)
                        {
                            Vector2Int textureCoord = point.Value.textureCoord[k];
                            Color32 oldColor = oldTextures[k][point.Key];
                            graph.textures[k].SetPixels32(textureCoord.x, textureCoord.y, 1, 1,
                                new Color32[] {oldColor});
                        }
                    }

                    for (int j = 0; j < gs.LODGroupParents.Count; j++)
                    {
                        graph.lodGroupClusters[j][0].GetComponent<Renderer>().sharedMaterial.mainTexture =
                            graph.textures[0];
                    }

                    graph.textures[0].Apply();
                    graph.textures[1].Apply();

                    foreach (List<GameObject> gpCluster in graph.lodGroupClusters.Values)
                    {
                        gpCluster.ForEach(x => x.SetActive(true));
                    }
                }


                if (activateSlices)
                {
                    sliceManager.ActivateSlices(true);
                }
            }
        }

        private Dictionary<string, Color32> SaveOldTexture(int i = 0)
        {
            Texture2D oldTexture = graph.textures[i];
            Dictionary<string, Color32> oldTextureColors = new Dictionary<string, Color32>();
            foreach (KeyValuePair<string, Graph.GraphPoint> point in graph.points)
            {
                Vector2Int textureCoord = point.Value.textureCoord[i];
                oldTextureColors[point.Key] =
                    oldTexture.GetPixel(textureCoord.x, textureCoord.y);
            }

            return oldTextureColors;
        }


        private void RemoveOldSlice()
        {
            spatialGraph.slices.Remove(GetComponent<GraphSlice>());
            referenceManager.graphManager.Graphs.Remove(graph);
            foreach (GameObject obj in graph.lodGroupParents)
            {
                Destroy(obj);
            }

            foreach (Graph g in childSlices)
            {
                spatialGraph.slices.Remove(g.GetComponent<GraphSlice>());
                sliceManager.slices.Remove(g.GetComponent<GraphSlice>());
                referenceManager.graphManager.Graphs.Remove(g);
                Destroy(g.gameObject);
                Destroy(g);
            }

            childSlices.Clear();
            slicer.gameObject.SetActive(false);
        }


        private static List<Graph.GraphPoint> SortPoints(IReadOnlyCollection<Graph.GraphPoint> points, int axis)
        {
            List<Graph.GraphPoint> sortedPoints = new List<Graph.GraphPoint>(points.Count);
            foreach (Graph.GraphPoint gp in points)
            {
                sortedPoints.Add(gp);
            }

            sortedPoints.Sort((x, y) => x.Position[axis].CompareTo(y.Position[axis]));
            return sortedPoints;
        }

        private List<GraphSlice> AutoDividePointsIntoSlices(Dictionary<string, Graph.GraphPoint> points, int axis = 0)
        {
            List<Vector3> cutPositions = new List<Vector3>();
            List<GraphSlice> slices = new List<GraphSlice>();
            List<Graph.GraphPoint> sortedPoints = new List<Graph.GraphPoint>(spatialGraph.pointsDict.Count);
            if (axis == 0)
            {
                if (sortedPointsX == null)
                {
                    sortedPointsX = SortPoints(points.Values.ToList(), 0);
                }

                sortedPoints = sortedPointsX;
            }

            else if (axis == 1)
            {
                if (sortedPointsY == null)
                {
                    sortedPointsY = SortPoints(points.Values.ToList(), 1);
                }

                sortedPoints = sortedPointsY;
            }
            else if (axis == 2)
            {
                if (sortedPointsZ == null)
                {
                    sortedPointsZ = SortPoints(points.Values.ToList(), 2);
                }

                sortedPoints = sortedPointsZ;
            }

            // spatialGraph.points.Sort((x, y) => x.Item2[axis].CompareTo(y.Item2[axis]));
            int sliceNr = 0;
            GraphSlice slice = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL)
                .GetComponent<GraphSlice>();
            // GraphSlice slice = Instantiate(referenceManager.graphGenerator.spatialSlicePrefab, spatialGraph.transform)
            //     .GetComponent<GraphSlice>();
            slice.transform.position = transform.position;
            slice.spatialGraph = spatialGraph;
            slice.referenceManager = referenceManager;
            slice.SliceNr = ++sliceNr;
            slice.gameObject.name = "Slice" + sliceNr;
            GraphSlice graphSlice = GetComponent<GraphSlice>();
            slice.SliceNr = graphSlice.SliceNr;
            slice.gameObject.name = gameObject.name + "_" + graphSlice.SliceNr;
            float currentCoord, diff, prevCoord;
            Graph.GraphPoint point = sortedPoints[0];
            slice.points.Add(point.Label, point);
            float firstCoord = prevCoord = point.Position[axis];
            float lastCoord = sortedPoints[sortedPoints.Count - 1].Position[axis];
            float dividers = 20f;
            float epsilonToUse = Math.Abs(firstCoord - lastCoord) / (float) dividers;

            if (axis == spatialGraph.mainAxis)
            {
                epsilonToUse = 0.01f;
            }
            
            for (int i = 1; i < sortedPoints.Count; i++)
            {
                point = sortedPoints[i];
                currentCoord = point.Position[axis];
                // when we reach new slice (new x/y/z coordinate) build the graph and then start adding to a new one.
                diff = Math.Abs(currentCoord - firstCoord);

                if (diff > epsilonToUse)// || Math.Abs(currentCoord - prevCoord) > 0.1f)
                {
                    cutPositions.Add(point.Position);
                    slices.Add(slice);
                    slice = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL)
                        .GetComponent<GraphSlice>();
                    slice.transform.position = transform.position;
                    slice.spatialGraph = spatialGraph;
                    slice.referenceManager = referenceManager;
                    slice.SliceNr = ++sliceNr;
                    slice.gameObject.name = gameObject.name + "_" + sliceNr;
                    firstCoord = currentCoord;
                }

                else if (i == sortedPoints.Count - 1)
                {
                    slices.Add(slice);
                }

                slice.points.Add(point.Label, point);
                prevCoord = currentCoord;
            }

            StartCoroutine(slicer.sliceAnimation(cutPositions.ToArray(), axis));

            return slices;
        }

        private List<GraphSlice> ManuallyDividePointsIntoSections(
            Dictionary<string, Graph.GraphPoint> points)
        {
            StartCoroutine(slicer.sliceAnimation());
            Plane pl = slicer.GetPlane();

            GraphSlice slice1 = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL)
                .GetComponent<GraphSlice>();
            GraphSlice slice2 = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL)
                .GetComponent<GraphSlice>();

            slice1.spatialGraph = spatialGraph;
            slice2.spatialGraph = spatialGraph;
            slice1.referenceManager = referenceManager;
            slice2.referenceManager = referenceManager;

            slice1.transform.parent = spatialGraph.transform;
            slice2.transform.parent = spatialGraph.transform;

            GraphSlice graphSlice = GetComponent<GraphSlice>();
            slice1.SliceNr = graphSlice.SliceNr;
            slice1.gameObject.name = gameObject.name + "_" + graphSlice.SliceNr;
            slice2.SliceNr = graphSlice.SliceNr + 1;
            slice2.gameObject.name = gameObject.name + "_" + (graphSlice.SliceNr + 1);

            slice1.transform.position = slice2.transform.position = transform.position;
            slice1.transform.localRotation = slice2.transform.localRotation = Quaternion.identity;

            if (sortedPointsX == null)
            {
                sortedPointsX = SortPoints(points.Values.ToList(), 0);
            }

            if (sortedPointsY == null)
            {
                sortedPointsY = SortPoints(points.Values.ToList(), 1);
            }

            if (sortedPointsZ == null)
            {
                sortedPointsZ = SortPoints(points.Values.ToList(), 2);
            }

            int p = 0;

            float minZ = float.PositiveInfinity;
            int firstSlice = 0;

            foreach (Graph.GraphPoint point in points.Values)
            {
                if (pl.GetSide(transform.TransformPoint(point.Position)))
                {
                    slice2.points.Add(point.Label, point);
                    if (point.Position.z < minZ)
                    {
                        minZ = point.Position.z;
                        firstSlice = 2;
                    }
                }
                else
                {
                    slice1.points.Add(point.Label, point);
                    if (point.Position.z < minZ)
                    {
                        minZ = point.Position.z;
                        firstSlice = 1;
                    }
                }
            }


            List<GraphSlice> slices = new List<GraphSlice>();
            if (firstSlice == 1)
            {
                slices.Add(slice1);
                slices.Add(slice2);
            }
            else
            {
                slices.Add(slice2);
                slices.Add(slice1);
            }

            if (slice1.points.Count == 0 || slice2.points.Count == 0)
            {
                Destroy(slice1.gameObject);
                Destroy(slice2.gameObject);
            }

            print($"sideA: {slice1.points.Count}, sideB: {slice2.points.Count}");
            return slices;
        }


        public IEnumerator BuildSpat(GraphSlice[] slices)
        {
            int sliceNr = 0;
            foreach (GraphSlice slice in slices)
            {
                Graph g = slice.GetComponent<Graph>();
                g.diffCoordValues = graph.diffCoordValues;
                g.maxCoordValues = graph.maxCoordValues;
                g.minCoordValues = graph.minCoordValues;
                StartCoroutine(slice.BuildSlice(slice.gameObject.name.Equals("Slice0")));
                while (slice.buildingSlice)
                {
                    yield return null;
                }

                sliceNr++;
            }
        }
    }
}