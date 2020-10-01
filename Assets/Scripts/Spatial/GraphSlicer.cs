using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using CellexalVR.Menu.Buttons.Slicing;
using Spatial;
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
        private Dictionary<string, Color32>[] oldTextures = new Dictionary<string, Color32>[2];


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
                StartCoroutine(SliceGraph(SlicingMenu.SliceMode.Automatic, 0, activateSlices: true));
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                StartCoroutine(SliceGraph(SlicingMenu.SliceMode.Automatic, 1, activateSlices: true));
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                StartCoroutine(SliceGraph(SlicingMenu.SliceMode.Automatic, activateSlices: true));
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                GetComponent<Graph>().ResetPosition();
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                if (slicerInside)
                {
                    StartCoroutine(SliceGraph(SlicingMenu.SliceMode.Manual, activateSlices: true));
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


        public IEnumerator SliceGraph(SlicingMenu.SliceMode sliceMode, int axis = 2, bool activateSlices = false)
        {
            GC.Collect();
            Resources.UnloadUnusedAssets();
            // Dictionary<string, Color32> oldTextureColors1 = SaveOldTexture();
            // Dictionary<string, Color32> oldTextureColors2 = SaveOldTexture(1);

            for (int i = 0; i < graph.lodGroups; i++)
            {
                if (childSlices.Count > 0)
                {
                    oldTextures[i] = SaveOldTexture(childSlices, i);
                }
                else
                {
                    oldTextures[i] = SaveOldTexture(graph, i);
                }
            }
            // oldTextures[0] = SaveOldTexture(); //oldTextureColors1);
            // oldTextures[1] = SaveOldTexture(1); //oldTextureColors2;

            List<GraphSlice> sls = new List<GraphSlice>();

            switch (sliceMode)
            {
                case SlicingMenu.SliceMode.Automatic:
                    sls = AutoDividePointsIntoSlices(slice.points, axis);
                    break;
                case SlicingMenu.SliceMode.Manual:
                    sls = ManuallyDividePointsIntoSections(slice.points);
                    break;
                case SlicingMenu.SliceMode.Freehand:
                    sls = DividePointsBySelection(slice.points);
                    break;
                case SlicingMenu.SliceMode.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sliceMode), sliceMode, null);
            }

            if (sls.All(x => x.points.Count > 0))
            {
                sliceManager = gameObject.GetComponent<SliceManager>();
                if (!sliceManager)
                {
                    sliceManager = gameObject.AddComponent<SliceManager>();
                }

                GetComponentInChildren<SliceBoxActivator>().sliceManager = sliceManager;

                sliceManager.referenceManager = referenceManager;
                foreach (GraphSlice s in sls)
                {
                    s.sliceCoords[axis] = -0.5f + s.SliceNr * (1f / (sls.Count - 1));
                    s.parentSlice = GetComponent<GraphSlice>();
                }

                StartCoroutine(BuildSpatialSlices(sls.ToArray()));
                while (sls.Any(x => x.buildingSlice) || slicer.sliceAnimationActive)
                {
                    yield return null;
                }

                
                RemoveOldSlice();
                for (int i = 0; i < sls.Count; i++)
                {
                    GraphSlice gs = sls[i].GetComponent<GraphSlice>();
                    Graph g = gs.GetComponent<Graph>();
                    childSlices.Add(g);
                    gs.transform.parent = sliceManager.transform;
                    gs.transform.localScale = Vector3.one;
                    gs.transform.localRotation = Quaternion.identity;
                    sliceManager.slices.Add(gs);

                    int m = 0;

                    for (int k = 0; k < graph.lodGroups; k++)
                    {
                        gs.SetTexture(oldTextures[k], k);
                        // foreach (KeyValuePair<string, Graph.GraphPoint> point in g.points)
                        // {
                        //     Vector2Int textureCoord = point.Value.textureCoord[k];
                        //     Color32 oldColor = oldTextures[k][point.Key];
                        //     g.textures[k].SetPixels32(textureCoord.x, textureCoord.y, 1, 1,
                        //         new Color32[] {oldColor});
                        //     if (m < 10)
                        //     {
                        //         print($"point: {point.Key} , col : {oldColor}, coord : {textureCoord}");
                        //     }
                        //
                        //     m++;
                        // }


                        // graph.textures[k].Apply();
                        // g.textures[k].Apply();
                        // graph.textures[k].Apply();
                        // graph.textures[1].Apply();
                    }

                    for (int j = 0; j < gs.LODGroupParents.Count; j++)
                    {
                        g.lodGroupClusters[j][0].GetComponent<Renderer>().sharedMaterial.mainTexture =
                            g.textures[0];
                    }

                    foreach (List<GameObject> gpCluster in g.lodGroupClusters.Values)
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


        private Dictionary<string, Color32> SaveOldTexture(List<Graph> slices, int i = 0)
        {
            Dictionary<string, Color32> oldTextureColors = new Dictionary<string, Color32>();
            foreach (Graph g in slices)
            {
                Texture2D oldTexture = g.textures[i];
                foreach (KeyValuePair<string, Graph.GraphPoint> point in g.points)
                {
                    Vector2Int textureCoord = point.Value.textureCoord[i];
                    oldTextureColors[point.Key] = oldTexture.GetPixel(textureCoord.x, textureCoord.y);
                }
            }

            return oldTextureColors;
        }

        private Dictionary<string, Color32> SaveOldTexture(Graph g, int i = 0)
        {
            Texture2D oldTexture = g.textures[i];
            Dictionary<string, Color32> oldTextureColors = new Dictionary<string, Color32>();
            foreach (KeyValuePair<string, Graph.GraphPoint> point in g.points)
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
            // referenceManager.graphManager.Graphs.Remove(graph);
            foreach (GameObject obj in graph.lodGroupParents)
            {
                Destroy(obj);
            }

            graph.lodGroupParents.Clear();

            foreach (Graph g in childSlices)
            {
                spatialGraph.slices.Remove(g.GetComponent<GraphSlice>());
                sliceManager.slices.Remove(g.GetComponent<GraphSlice>());
                referenceManager.graphManager.Graphs.Remove(g);
                Destroy(g.gameObject);
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

                if (diff > epsilonToUse) // || Math.Abs(currentCoord - prevCoord) > 0.1f)
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

        private List<GraphSlice> DividePointsBySelection(Dictionary<string, Graph.GraphPoint> points)
        {
            referenceManager.selectionManager.ConfirmSelection();
            List<GraphSlice> slices = new List<GraphSlice>();
            List<Graph.GraphPoint> currrentSelection = referenceManager.selectionManager.GetLastSelection();

            if (currrentSelection.Count == 0 || currrentSelection.Count == points.Count)
            {
                slices.Add(new GraphSlice());
                slices.Add(new GraphSlice());
                return (slices);
            }

            GraphSlice slice1 = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL)
                .GetComponent<GraphSlice>();

            slice1.spatialGraph = spatialGraph;
            slice1.referenceManager = referenceManager;

            slice1.transform.parent = spatialGraph.transform;

            GraphSlice graphSlice = GetComponent<GraphSlice>();
            slice1.SliceNr = graphSlice.SliceNr;
            slice1.gameObject.name = gameObject.name + "_" + graphSlice.SliceNr;

            slice1.transform.position = transform.position;
            slice1.transform.localRotation = Quaternion.identity;


            int currentGroup;
            int prevGroup = -1;
            GraphSlice currentSlice = slice1;
            List<Graph.GraphPoint> gps = points.Values.ToList();
            gps.Sort((x, y) => x.Group.CompareTo(y.Group));
            foreach (Graph.GraphPoint gp in gps)
            {
                currentGroup = gp.Group;
                if (currentGroup != prevGroup)
                {
                    GraphSlice slice = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL)
                        .GetComponent<GraphSlice>();
                    slice.spatialGraph = spatialGraph;
                    slice.referenceManager = referenceManager;
                    slice.transform.parent = spatialGraph.transform;
                    slice.SliceNr = graphSlice.SliceNr + 1;
                    slice.GetComponent<Graph>().GraphName = gameObject.name + "_" + (graphSlice.SliceNr + 1);
                    slice.transform.position = transform.position;
                    slice.transform.localRotation = Quaternion.identity;
                    currentSlice = slice;
                    slices.Add(slice);
                }

                currentSlice.points.Add(gp.Label, gp);
                prevGroup = gp.Group;
            }

            // if (slice1.points.Count == 0 || slice2.points.Count == 0)
            // {
            //     Destroy(slice1.gameObject);
            //     Destroy(slice2.gameObject);
            // }

            slices.Add(slice1);

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
            slice1.GetComponent<Graph>().GraphName = gameObject.name + "_" + graphSlice.SliceNr;
            slice2.SliceNr = graphSlice.SliceNr + 1;
            slice2.GetComponent<Graph>().GraphName = gameObject.name + "_" + (graphSlice.SliceNr + 1);

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


        public IEnumerator BuildSpatialSlices(GraphSlice[] slices)
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