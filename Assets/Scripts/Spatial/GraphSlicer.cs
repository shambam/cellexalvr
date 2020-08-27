using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using UnityEngine;

namespace CellexalVR.Spatial
{
    public class GraphSlicer : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public GameObject slicer;
        public SpatialGraph spatialGraph;


        private Graph graph;
        private GraphGenerator graphGenerator;
        private bool removingOldSlices;
        private bool buildingGraph;
        private Plane plane;
        private List<GameObject> oldGraphObjects = new List<GameObject>();
        private List<Graph.GraphPoint> sortedPointsX;
        private List<Graph.GraphPoint> sortedPointsY;
        private List<Graph.GraphPoint> sortedPointsZ;
        private Dictionary<string, Graph.GraphPoint> pointsDict = new Dictionary<string, Graph.GraphPoint>();
        private Vector3 minCoords = Vector3.negativeInfinity;
        private Vector3 diffVect = Vector3.negativeInfinity;
        private GraphSlice slice;

        private void Start()
        {
            slice = GetComponent<GraphSlice>();
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            graph = GetComponent<Graph>();
            spatialGraph = slice.spatialGraph;
            // graph = spatialGraph.GetComponent<Graph>();
            // referenceManager = spatialGraph.referenceManager;
            // plane = slicer.AddComponent<Plane>();
        }

        private void Update()
        {
            if (referenceManager.consoleManager.consoleGameObject.activeSelf) return;
            if (slicer == null || !slicer.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.K))
            {
                StartCoroutine(SliceGraph(true, 0));
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                StartCoroutine(SliceGraph(true, 1));
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                StartCoroutine(SliceGraph());
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                StartCoroutine(SliceGraph(false, activateSlices: true));
            }
        }


        public IEnumerator SliceGraph(bool automatic = true, int axis = 2, bool activateSlices = false)
        {
            GC.Collect();
            Resources.UnloadUnusedAssets();


            Dictionary<string, Color32> oldTextureColors = SaveOldTexture();
            // if (spatialGraph.slices.Count > 0)

            // StartCoroutine(BuildSpatialGraph(useSlicer, axis, dividers));
            // List<Dictionary<string, Graph.GraphPoint>> slices = new List<Dictionary<string, Graph.GraphPoint>>();
            List<GraphSlice> sls = new List<GraphSlice>();
            if (automatic)
            {
                sls = AutoDividePointsIntoSlices(slice.points, axis);
            }
            else
            {
                sls = ManuallyDividePointsIntoSections(slice.points);
            }

            // Texture2D[] oldTextures = new Texture2D[graph.textures.Length];
            // for (int i = 0; i < graph.textures.Length; i++)
            // {
            //     oldTextures[i] = graph.textures[i];
            // }

            // Texture2D oldTexture = graph.textures[0];
            // if (slices.Any(x => x.Count != 0))
            if (sls.Any(x => x.points.Count == 0)) yield break;
            StartCoroutine(BuildSpat(sls.ToArray()));

            while (sls.Any(x => x.buildingSlice))
            {
                yield return null;
            }

            RemoveOldSlice();


            // for (int i = 0; i < graph.textures.Length; i++)
            // {
            //     graph.textures[i] = oldTextures[i];
            // }

            SliceManager manager = gameObject.AddComponent<SliceManager>();
            manager.referenceManager = referenceManager;
            for (int i = 0; i < sls.Count; i++)
            {
                GraphSlice gs = sls[i].GetComponent<GraphSlice>();
                Graph graph = gs.GetComponent<Graph>();
                gs.transform.parent = manager.transform;
                manager.slices.Add(gs);
                Vector3 p = transform.localPosition;
                float pos = -0.5f + i * (1f / (sls.Count - 1));
                p[axis] = pos;
                gs.sliceCoords[axis] = pos;


                // int k = 0;
                foreach (KeyValuePair<string, Graph.GraphPoint> point in gs.points)
                {
                    Vector2Int textureCoord = point.Value.textureCoord[0];
                    Color32 oldColor = oldTextureColors[point.Key];

                    graph.textures[0].SetPixels32(textureCoord.x, textureCoord.y, 1, 1, new Color32[] {oldColor});
                }

                for (int j = 0; j < gs.LODGroupParents.Count; j++)
                {
                    graph.lodGroupClusters[j][0].GetComponent<Renderer>().sharedMaterial.mainTexture =
                        graph.textures[0];
                }

                graph.textures[0].Apply();

                foreach (List<GameObject> gpCluster in graph.lodGroupClusters.Values)
                {
                    gpCluster.ForEach(x => x.SetActive(true));
                }


                if (activateSlices)
                {
                    manager.ActivateSlices(true);
                }
            }
        }

        private Dictionary<string, Color32> SaveOldTexture()
        {
            Texture2D oldTexture = graph.textures[0];
            Dictionary<string, Color32> oldTextureColors = new Dictionary<string, Color32>();
            int k = 0;
            foreach (KeyValuePair<string, Graph.GraphPoint> point in graph.points)
            {
                Vector2Int textureCoord = point.Value.textureCoord[0];
                oldTextureColors[point.Key] =
                    oldTexture.GetPixel(textureCoord.x, textureCoord.y);
            }

            return oldTextureColors;
        }


        private void RemoveOldSlice()
        {
            spatialGraph.slices.Remove(GetComponent<GraphSlice>());
            referenceManager.graphManager.Graphs.Remove(graph);
            // Destroy(gameObject);
            // Destroy(GetComponent<LODGroup>());
            // foreach (Transform child in transform)
            // {
            //     Destroy(child.gameObject);
            // }
            foreach (GameObject obj in graph.lodGroupParents)
            {
                Destroy(obj);
            }

            slicer.SetActive(false);

            Destroy(graph);
        }

        private IEnumerator RemoveOldSlices()
        {
            // spatialGraph.ActivateSlices(false);
            oldGraphObjects = new List<GameObject>();
            foreach (GraphSlice slice in spatialGraph.slices)
                // foreach (GameObject cluster in GetComponent<Graph>().LODGroupClusters[0])
            {
                oldGraphObjects.Add(slice.gameObject);
                Destroy(slice);
                // referenceManager.graphManager.spatialGraphs.Remove(graph.GetComponent<SpatialGraph>());
                yield return null;
            }

            // referenceManager.graphManager.spatialGraphs.Clear();
            spatialGraph.slices.Clear();
            removingOldSlices = false;
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
            List<GraphSlice> slices = new List<GraphSlice>();

            // List<Dictionary<string, Graph.GraphPoint>> slices = new List<Dictionary<string, Graph.GraphPoint>>();
            // Dictionary<string, Graph.GraphPoint> slice = new Dictionary<string, Graph.GraphPoint>();
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
            float currentCoord, diff, prevCoord;
            var point = sortedPoints[0];
            slice.points.Add(point.Label, point);
            float firstCoord = prevCoord = point.Position[axis];
            float lastCoord = sortedPoints[sortedPoints.Count - 1].Position[axis];
            float dividers = 20f;
            float epsilonToUse = Math.Abs(firstCoord - lastCoord) / (float) dividers;
            for (int i = 1; i < sortedPoints.Count; i++)
            {
                point = sortedPoints[i];
                // gpTuple = spatialGraph.points[n];
                currentCoord = point.Position[axis];
                // when we reach new slice (new x/y/z coordinate) build the graph and then start adding to a new one.
                diff = Math.Abs(currentCoord - firstCoord);

                if (diff > epsilonToUse || Math.Abs(currentCoord - prevCoord) > 0.1f)
                {
                    slices.Add(slice);
                    slice = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL)
                        .GetComponent<GraphSlice>();
                    // slice = Instantiate(referenceManager.graphGenerator.spatialSlicePrefab, spatialGraph.transform)
                    //     .GetComponent<GraphSlice>();
                    slice.transform.position = transform.position;
                    slice.spatialGraph = spatialGraph;
                    slice.referenceManager = referenceManager;
                    slice.SliceNr = ++sliceNr;
                    slice.gameObject.name = "Slice" + sliceNr;
                    firstCoord = currentCoord;
                }

                else if (i == sortedPoints.Count - 1)
                {
                    slices.Add(slice);
                }

                slice.points.Add(point.Label, point);
                prevCoord = currentCoord;
            }


            return slices;
        }

        private List<GraphSlice> ManuallyDividePointsIntoSections(
            Dictionary<string, Graph.GraphPoint> points)
        {
            Slicer sl = GetComponentInChildren<Slicer>();
            Vector3 v1 = sl.t1.position; // - sl.t2.position;
            Vector3 v2 = sl.t2.position; // - sl.t1.position;
            Vector3 v3 = v2 - v1;
            Vector3 n = Vector3.Cross(v3, Vector3.right).normalized;
            Vector3 midPoint = new Vector3((v2.x + v1.x) / 2f, (v2.y + v1.y) / 2f, (v2.z + v1.z) / 2f);
            var pl = new Plane(n, midPoint);
            // var pl = new Plane(slicer.transform.forward,
            //     slicer.transform.position);

            // var slice1 = Instantiate(referenceManager.graphGenerator.spatialSlicePrefab, spatialGraph.transform)
            //     .GetComponent<GraphSlice>();
            // var slice2 = Instantiate(referenceManager.graphGenerator.spatialSlicePrefab, spatialGraph.transform)
            //     .GetComponent<GraphSlice>();


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

            slice1.transform.position = transform.position;
            slice2.transform.position = transform.position;


            float xMin, yMin, zMin, totalXDiff, totalYDiff, totalZDiff;
            if (sortedPointsX == null)
            {
                sortedPointsX = SortPoints(points.Values.ToList(), 0);
            }

            xMin = sortedPointsX[0].Position.x;
            float xMax = sortedPointsX[sortedPointsX.Count - 1].Position.x;
            totalXDiff = Math.Abs(xMax - xMin);

            if (sortedPointsY == null)
            {
                sortedPointsY = SortPoints(points.Values.ToList(), 1);
            }

            yMin = sortedPointsY[0].Position.y;
            float yMax = sortedPointsY[sortedPointsY.Count - 1].Position.y;
            totalYDiff = Math.Abs(yMax - yMin);

            if (sortedPointsZ == null)
            {
                sortedPointsZ = SortPoints(points.Values.ToList(), 2);
            }

            zMin = sortedPointsZ[0].Position.z;
            float zMax = sortedPointsZ[sortedPointsZ.Count - 1].Position.z;
            totalZDiff = Math.Abs(zMax - zMin);

            minCoords = new Vector3(xMin, yMin, zMin);
            diffVect = new Vector3(totalXDiff, totalYDiff, totalZDiff);
            int p = 0;

            // var maxCoords = new Vector3(xMax, yMax, zMax);
            // var mid = new Vector3(sortedPointsX[(int) (sortedPointsX.Count / 2)].WorldPosition.x,
            //     sortedPointsX[(int) (sortedPointsY.Count / 2)].WorldPosition.y,
            //     sortedPointsX[(int) (sortedPointsZ.Count / 2)].WorldPosition.z);
            // print(
            //     $"minCoords: {minCoords}, mid gp wPos:" +
            //     $" {sortedPointsZ[(int) (sortedPointsZ.Count / 2)].WorldPosition}," +
            //     $" mid gp pos: {sortedPointsZ[(int) (sortedPointsZ.Count / 2)].Position} plane pos: {midPoint}," +
            //     $" mid: {mid}, max: {maxCoords}," +
            //     $" trans point : {transform.TransformPoint(sortedPointsZ[(int) (sortedPointsZ.Count / 2)].Position)}, " +
            //     $" trans plane : {transform.TransformPoint(midPoint)}");
            foreach (Graph.GraphPoint point in points.Values)
            {
                if (pl.GetSide(transform.TransformPoint(point.Position)))
                {
                    slice2.points.Add(point.Label, point);
                }
                else
                {
                    slice1.points.Add(point.Label, point);
                }
            }

            List<GraphSlice> slices = new List<GraphSlice> {slice1, slice2};
            if (slice1.points.Count == 0)
            {
                slices.Remove(slice1);
                Destroy(slice1.gameObject);
            }

            if (slice2.points.Count == 0)
            {
                slices.Remove(slice2);
                Destroy(slice2.gameObject);
            }

            print($"sideA: {slice1.points.Count}, sideB: {slice2.points.Count}");
            return slices;
        }


        public IEnumerator BuildSpat(GraphSlice[] slices)
        {
            int sliceNr = 0;
            // var sliceParent = Instantiate(referenceManager.graphGenerator.spatialGraphPrefab, spatialGraph.transform);
            // sliceParent.gameObject.name = gameObject.name;
            // Destroy(sliceParent.GetComponent<Graph>());
            foreach (GraphSlice slice in slices)
            {
                StartCoroutine(slice.BuildSlice(sliceNr == 0));
                while (slice.buildingSlice)
                {
                    yield return null;
                }

                sliceNr++;
            }

            // if (spatialGraph.slices.Count > 1)
            // {
            //     for (int i = 0; i < spatialGraph.slices.Count; i++)
            //     {
            //         float pos = -0.5f + i * (1f / (spatialGraph.slices.Count - 1));
            //         GraphSlice slice = spatialGraph.slices[i].GetComponent<GraphSlice>();
            //         slice.sliceCoords[2] = pos;
            //         // print($"name: {slice.gameObject.name}, pos: {pos}");
            //     }
            // }
        }
    }
}