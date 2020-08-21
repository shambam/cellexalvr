using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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


        private SpatialGraph spatialGraph;
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

        private void Start()
        {
            spatialGraph = GetComponent<SpatialGraph>();
            referenceManager = spatialGraph.referenceManager;
            // plane = slicer.AddComponent<Plane>();
        }

        private void Update()
        {
            if (referenceManager.consoleManager.consoleGameObject.activeSelf) return;
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
                print("Start slicing Graph");
                StartCoroutine(SliceGraph(false, activateSlices: true));
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                spatialGraph.ActivateSlices(!spatialGraph.slicesActive);
            }
        }


        public IEnumerator SliceGraph(bool automatic = true, int axis = 2, bool activateSlices = false)
        {
            GC.Collect();
            Resources.UnloadUnusedAssets();
            // if (spatialGraph.slices.Count > 0)

            // StartCoroutine(BuildSpatialGraph(useSlicer, axis, dividers));
            List<Dictionary<string, Graph.GraphPoint>> slices = new List<Dictionary<string, Graph.GraphPoint>>();
            if (automatic)
            {
                slices = AutoDividePointsIntoSlices(axis);
            }
            else
            {
                slices = ManuallyDividePointsIntoSections(spatialGraph.pointsDict);
            }

            if (slices.Any(x => x.Count != 0))
            {
                if (GetComponentsInChildren<GraphSlice>().Any(x => x.LODGroupClusters[0].Count > 0))
                {
                    removingOldSlices = true;
                    StartCoroutine(RemoveOldSlices());
                }

                while (removingOldSlices)
                {
                    yield return null;
                }
                
                StartCoroutine(BuildSpatialGraph(slices, axis));
                while (buildingGraph)
                {
                    yield return null;
                }

                foreach (GameObject obj in oldGraphObjects)
                {
                    Destroy(obj);
                    // referenceManager.graphManager.spatialGraphs.Remove(graph.GetComponent<SpatialGraph>());
                    yield return null;
                }

                if (activateSlices)
                {
                    spatialGraph.ActivateSlices(true);
                }
            }
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

        private List<Dictionary<string, Graph.GraphPoint>> AutoDividePointsIntoSlices(int axis = 0)
        {
            List<Dictionary<string, Graph.GraphPoint>> slices = new List<Dictionary<string, Graph.GraphPoint>>();
            Dictionary<string, Graph.GraphPoint> slice = new Dictionary<string, Graph.GraphPoint>();
            List<Graph.GraphPoint> sortedPoints = new List<Graph.GraphPoint>(spatialGraph.pointsDict.Count);
            if (axis == 0)
            {
                if (sortedPointsX == null)
                {
                    sortedPointsX = new List<Graph.GraphPoint>(spatialGraph.pointsDict.Count);
                    foreach (Graph.GraphPoint gp in spatialGraph.pointsDict.Values)
                    {
                        sortedPointsX.Add(gp);
                    }

                    sortedPointsX.Sort((x, y) => x.Position[axis].CompareTo(y.Position[axis]));
                }

                sortedPoints = sortedPointsX;
            }

            else if (axis == 1)
            {
                if (sortedPointsY == null)
                {
                    sortedPointsY = new List<Graph.GraphPoint>(spatialGraph.pointsDict.Count);
                    foreach (Graph.GraphPoint gp in spatialGraph.pointsDict.Values)
                    {
                        sortedPointsY.Add(gp);
                    }

                    sortedPointsY.Sort((x, y) => x.Position[axis].CompareTo(y.Position[axis]));
                }

                sortedPoints = sortedPointsY;
            }
            else if (axis == 2)
            {
                if (sortedPointsZ == null)
                {
                    sortedPointsZ = new List<Graph.GraphPoint>(spatialGraph.pointsDict.Count);
                    foreach (Graph.GraphPoint gp in spatialGraph.pointsDict.Values)
                    {
                        sortedPointsZ.Add(gp);
                    }

                    sortedPointsZ.Sort((x, y) => x.Position[axis].CompareTo(y.Position[axis]));
                }

                sortedPoints = sortedPointsZ;
            }

            // spatialGraph.points.Sort((x, y) => x.Item2[axis].CompareTo(y.Item2[axis]));
            float currentCoord, diff, prevCoord;
            var point = sortedPoints[0];
            slice.Add(point.Label, point);
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
                    slice = new Dictionary<string, Graph.GraphPoint>();
                    firstCoord = currentCoord;
                }

                else if (i == sortedPoints.Count - 1)
                {
                    slices.Add(slice);
                }

                slice.Add(point.Label, point);
                prevCoord = currentCoord;
            }


            return slices;
        }

        private List<Dictionary<string, Graph.GraphPoint>> ManuallyDividePointsIntoSections(
            Dictionary<string, Graph.GraphPoint> points)
        {
            var pl = new Plane(slicer.transform.forward,
                slicer.transform.position);
            Dictionary<string, Graph.GraphPoint> sideA = new Dictionary<string, Graph.GraphPoint>();
            Dictionary<string, Graph.GraphPoint> sideB = new Dictionary<string, Graph.GraphPoint>();
            // List<Tuple<string, Vector3>> sideA = new List<Tuple<string, Vector3>>();
            // List<Tuple<string, Vector3>> sideB = new List<Tuple<string, Vector3>>();
            float xMin, yMin, zMin, totalXDiff, totalYDiff, totalZDiff;
            if (sortedPointsX != null)
            {
                xMin = sortedPointsX[0].Position.x;
                totalXDiff = Math.Abs(sortedPointsX[sortedPointsX.Count - 1].Position.x - xMin);
            }

            else
            {
                xMin = points.Min(v => (v.Value.Position.x));
                totalXDiff = Math.Abs(points.Max(x => (x.Value.Position.x)) - xMin);
            }

            if (sortedPointsY != null)
            {
                yMin = sortedPointsY[0].Position.y;
                totalYDiff = Math.Abs(sortedPointsY[sortedPointsY.Count - 1].Position.y - yMin);
            }

            else
            {
                yMin = points.Min(v => (v.Value.Position.y));
                totalYDiff = Math.Abs(points.Max(y => (y.Value.Position.y)) - yMin);
            }

            if (sortedPointsZ != null)
            {
                zMin = sortedPointsZ[0].Position.z;
                totalZDiff = Math.Abs(sortedPointsZ[sortedPointsZ.Count - 1].Position.z - zMin);
            }

            else
            {
                zMin = points.Min(v => (v.Value.Position.z));
                totalZDiff = Math.Abs(points.Max(z => (z.Value.Position.z)) - zMin);
            }

            minCoords = new Vector3(xMin, yMin, zMin);
            diffVect = new Vector3(totalXDiff, totalYDiff, totalZDiff);
            foreach (Graph.GraphPoint point in points.Values)
            {
                if (pl.GetSide(point.WorldPosition))
                {
                    sideB.Add(point.Label, point);
                }
                else
                {
                    sideA.Add(point.Label, point);
                }
            }

            var slices = new List<Dictionary<string, Graph.GraphPoint>>();
            slices.Add(sideA);
            slices.Add(sideB);
            print($"sideA: {sideA.Count}, sideB: {sideB.Count}");
            return slices;
        }

        public IEnumerator BuildSpatialGraph(List<Dictionary<string, Graph.GraphPoint>> slices, int axis)
        {
            buildingGraph = true;
            int sliceNr = 0;
            Vector3 maxCoords = new Vector3();
            Vector3 minCoords = new Vector3();
            maxCoords.x = spatialGraph.pointsDict.Max(v => (v.Value.Position.x));
            maxCoords.y = spatialGraph.pointsDict.Max(v => (v.Value.Position.y));
            maxCoords.z = spatialGraph.pointsDict.Max(v => (v.Value.Position.z));
            minCoords.x = spatialGraph.pointsDict.Min(v => (v.Value.Position.x));
            minCoords.y = spatialGraph.pointsDict.Min(v => (v.Value.Position.y));
            minCoords.z = spatialGraph.pointsDict.Min(v => (v.Value.Position.z));

            // Graph combGraph = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL);
            // yield return null;
            //
            // referenceManager.graphManager.Graphs.Add(combGraph);
            // referenceManager.graphManager.originalGraphs.Add(combGraph);
            // Transform transform1 = combGraph.transform;
            // transform1.parent = transform;
            // transform1.localPosition = new Vector3(0, 0, 0);
            // combGraph.LODGroups = referenceManager.graphGenerator.nrOfLODGroups;
            // combGraph.textures = new Texture2D[referenceManager.graphGenerator.nrOfLODGroups];
            // GraphSlice gs = combGraph.gameObject.AddComponent<Spatial.GraphSlice>();
            // gs.referenceManager = referenceManager;
            // gs.sliceNr = ++sliceNr;
            // combGraph.gameObject.name = "Slice" + sliceNr;
            // combGraph.GraphName = "Slice" + sliceNr;
            // combGraph.maxCoordValues = maxCoords;
            // combGraph.minCoordValues = minCoords;
            foreach (Dictionary<string, Graph.GraphPoint> points in slices)
            {
                if (points.Count == 0)
                {
                    continue;
                }

                var graphSlice = Instantiate(referenceManager.graphGenerator.spatialSlicePrefab, transform).GetComponent<GraphSlice>();
                graphSlice.referenceManager = referenceManager;
                graphSlice.points = points;
                spatialGraph.slices.Add(graphSlice);

                // Graph combGraph = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL);
                // yield return null;
                // referenceManager.graphManager.Graphs.Add(combGraph);
                // referenceManager.graphManager.originalGraphs.Add(combGraph);
                // Transform transform1 = combGraph.transform;
                // transform1.parent = transform;
                // transform1.localPosition = new Vector3(0, 0, 0);
                // combGraph.LODGroups = referenceManager.graphGenerator.nrOfLODGroups;
                // combGraph.textures = new Texture2D[referenceManager.graphGenerator.nrOfLODGroups];
                // GraphSlice gs = combGraph.gameObject.AddComponent<Spatial.GraphSlice>();
                // gs.referenceManager = referenceManager;
                // gs.sliceNr = ++sliceNr;
                // combGraph.gameObject.name = "Slice" + sliceNr;
                // combGraph.GraphName = "Slice" + sliceNr;
                // yield return null;

                // Dictionary<string, Graph.GraphPoint> pointsInSlice =
                //     new Dictionary<string, Graph.GraphPoint>(points.Count);
                // foreach (Graph.GraphPoint point in points.Values)
                // {
                // Cell cell = referenceManager.cellManager.AddCell(point.Label);
                // Graph.GraphPoint gp = referenceManager.graphGenerator.AddGraphPoint(cell, point.Position.x,
                //     point.Position.y,
                //     point.Position.z, combGraph);
                // }

                // combGraph.maxCoordValues = maxCoords;
                // combGraph.minCoordValues = minCoords;
                StartCoroutine(
                    referenceManager.graphGenerator.SliceClusteringLOD(
                        referenceManager.graphGenerator.nrOfLODGroups, points, graphSlice));
                // referenceManager.graphGenerator.SliceClustering();

                while (referenceManager.graphGenerator.isCreating)
                {
                    yield return null;
                }

                // foreach (GraphSlice slice in slices)
                // {
                //     
                // }
                if (referenceManager.graphGenerator.nrOfLODGroups > 1)
                {
                    if (graphSlice.GetComponent<LODGroup>() == null)
                    {
                        graphSlice.gameObject.AddComponent<LODGroup>();
                    }
                    referenceManager.graphGenerator.UpdateLODGroups(slice: graphSlice);
                }
            }

            // spatialGraph.AddSlices();

            if (spatialGraph.slices.Count > 1)
            {
                for (int i = 0; i < spatialGraph.slices.Count; i++)
                {
                    float pos = -0.5f + i * (1f / (spatialGraph.slices.Count - 1));
                    GraphSlice slice = spatialGraph.slices[i].GetComponent<GraphSlice>();
                    slice.sliceCoords[axis] = pos;
                    // print($"name: {slice.gameObject.name}, pos: {pos}");
                }
            }

            buildingGraph = false;

            // var point = spatialGraph.points[spatialGraph.points.Count / 2];
            // gp = referenceManager.graphManager.FindGraphPoint("Slice20", point.Item1);
            // print($"point: {gp.WorldPosition}, slicer: {slicer.transform.position}");
        }

        // public IEnumerator BuildSpatialGraph(bool useSlicer = false, int axis = 2, int dividers = 0)
        // {
        //     buildingGraph = true;
        //     int sliceNr = 0;
        //     spatialGraph.points.Sort((x, y) => x.Item2[axis].CompareTo(y.Item2[axis]));
        //     Vector3 maxCoords = new Vector3();
        //     Vector3 minCoords = new Vector3();
        //     maxCoords.x = spatialGraph.points.Max(v => (v.Item2.x));
        //     maxCoords.y = spatialGraph.points.Max(v => (v.Item2.y));
        //     maxCoords.z = spatialGraph.points.Max(v => (v.Item2.z));
        //     minCoords.x = spatialGraph.points.Min(v => (v.Item2.x));
        //     minCoords.y = spatialGraph.points.Min(v => (v.Item2.y));
        //     minCoords.z = spatialGraph.points.Min(v => (v.Item2.z));
        //     Graph combGraph = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL);
        //     yield return null;
        //     referenceManager.graphManager.Graphs.Add(combGraph);
        //     referenceManager.graphManager.originalGraphs.Add(combGraph);
        //     Transform transform1 = combGraph.transform;
        //     transform1.parent = transform;
        //     transform1.localPosition = new Vector3(0, 0, 0);
        //     combGraph.LODGroups = referenceManager.graphGenerator.nrOfLODGroups;
        //     combGraph.textures = new Texture2D[referenceManager.graphGenerator.nrOfLODGroups];
        //     GraphSlice gs = combGraph.gameObject.AddComponent<Spatial.GraphSlice>();
        //     gs.referenceManager = referenceManager;
        //     gs.sliceNr = ++sliceNr;
        //     combGraph.gameObject.name = "Slice" + sliceNr;
        //     combGraph.GraphName = "Slice" + sliceNr;
        //     yield return null;
        //
        //     Tuple<string, Vector3> gpTuple = spatialGraph.points[0];
        //     Cell cell = referenceManager.cellManager.AddCell(gpTuple.Item1);
        //
        //     Graph.GraphPoint gp = referenceManager.graphGenerator.AddGraphPoint(cell, gpTuple.Item2.x,
        //         gpTuple.Item2.y,
        //         gpTuple.Item2.z);
        //
        //     float currentCoord = gpTuple.Item2[axis];
        //     float firstCoord = currentCoord;
        //     float prevCoord = currentCoord;
        //     float lastCoord = spatialGraph.points[spatialGraph.points.Count - 1].Item2[axis];
        //     // difference to decide when to start on new slice.
        //     float epsilonToUse, diff;
        //     bool normalZSlicing = false;
        //     if (dividers == 0 && axis == 2)
        //     {
        //         epsilonToUse = 0.01f;
        //         normalZSlicing = true;
        //     }
        //     else
        //     {
        //         epsilonToUse = Math.Abs(firstCoord - lastCoord) / (float) dividers;
        //     }
        //
        //     float totalZDiff = Math.Abs(firstCoord - lastCoord);
        //     float firstZ = gpTuple.Item2[axis];
        //     float relativeVal = (slicer.transform.localPosition.z + 0.5f);
        //     Mesh quadMesh = slicer.GetComponentInChildren<MeshFilter>().mesh;
        //     // plane = new Plane(quadMesh.bounds.center, quadMesh.bounds.max, quadMesh.bounds.min);
        //     plane = new Plane(slicer.transform.InverseTransformDirection(slicer.transform.forward),
        //         slicer.transform.localPosition);
        //     print(
        //         $"forward: {slicer.transform.TransformDirection(slicer.transform.forward)}, {slicer.transform.forward}");
        //     for (int n = 1; n < spatialGraph.points.Count; n++)
        //     {
        //         gpTuple = spatialGraph.points[n];
        //         currentCoord = gpTuple.Item2[axis];
        //         // when we reach new slice (new x/y/z coordinate) build the graph and then start adding to a new one.
        //         bool newSliceComp;
        //         if (!useSlicer)
        //         {
        //             diff = normalZSlicing
        //                 ? Math.Abs(currentCoord - prevCoord)
        //                 : Math.Abs(currentCoord - firstCoord);
        //             newSliceComp = diff > epsilonToUse;
        //         }
        //         else
        //         {
        //             // convert coordinate to value in range (0, 1) 
        //             var convertedCoord = new Vector3(0, 0, (currentCoord - firstZ) / totalZDiff - 0.5f);
        //             newSliceComp = plane.GetSide(convertedCoord);
        //             // newSliceComp = (currentCoord - firstZ) >= (relativeVal * totalZDiff);
        //         }
        //
        //
        //         if (newSliceComp)
        //         {
        //             // float d = Math.Abs((currentCoord - firstZ) - (relativeVal * totalZDiff));
        //             // print($"d: {d}, slicer: {relativeVal * totalZDiff}");
        //             firstCoord = currentCoord;
        //             // gs.zCoord = gp.WorldPosition.z;
        //             combGraph.maxCoordValues = maxCoords;
        //             combGraph.minCoordValues = minCoords;
        //             StartCoroutine(
        //                 referenceManager.graphGenerator.SliceClusteringLOD(
        //                     referenceManager.graphGenerator.nrOfLODGroups));
        //
        //             while (referenceManager.graphGenerator.isCreating)
        //             {
        //                 yield return null;
        //             }
        //
        //             if (referenceManager.graphGenerator.nrOfLODGroups > 1)
        //             {
        //                 combGraph.gameObject.AddComponent<LODGroup>();
        //                 referenceManager.graphGenerator.UpdateLODGroups(combGraph);
        //             }
        //
        //             combGraph = referenceManager.graphGenerator.CreateGraph(GraphGenerator.GraphType.SPATIAL);
        //             combGraph.LODGroups = referenceManager.graphGenerator.nrOfLODGroups;
        //             combGraph.textures = new Texture2D[referenceManager.graphGenerator.nrOfLODGroups];
        //             yield return null;
        //             referenceManager.graphManager.Graphs.Add(combGraph);
        //             referenceManager.graphManager.originalGraphs.Add(combGraph);
        //             combGraph.transform.parent = transform;
        //             yield return null;
        //             gs = combGraph.gameObject.AddComponent<GraphSlice>();
        //             gs.referenceManager = referenceManager;
        //             gs.sliceNr = ++sliceNr;
        //             combGraph.transform.localPosition = new Vector3(0, 0, 0);
        //             combGraph.GraphName = "Slice" + sliceNr;
        //             combGraph.gameObject.name = "Slice" + sliceNr;
        //             // slicer.transform.localPosition = new Vector3(0, 0, 0.5f);
        //             relativeVal = 1.1f;
        //             plane.Flip();
        //             // slicer.transform.localPosition = new Vector3(0, 0, 0.6f);
        //             // quadMesh = slicer.GetComponentInChildren<MeshFilter>().mesh;
        //             // plane = new Plane(quadMesh.normals[0], new Vector3(0, 0, 0.6f));
        //         }
        //
        //         // last gp: finish the final slice
        //         else if (n == spatialGraph.points.Count - 1)
        //         {
        //             cell = referenceManager.cellManager.AddCell(gpTuple.Item1);
        //             gp = referenceManager.graphGenerator.AddGraphPoint(cell, gpTuple.Item2.x, gpTuple.Item2.y,
        //                 gpTuple.Item2.z);
        //             // try 
        //             // {
        //             //     // gs.zCoord = gp.WorldPosition.z;
        //             // }
        //             // catch (Exception e)
        //             // {
        //             //     GC.Collect();
        //             // }
        //
        //             combGraph.maxCoordValues = maxCoords;
        //             combGraph.minCoordValues = minCoords;
        //             StartCoroutine(
        //                 referenceManager.graphGenerator.SliceClusteringLOD(
        //                     referenceManager.graphGenerator.nrOfLODGroups));
        //
        //             while (referenceManager.graphGenerator.isCreating)
        //             {
        //                 yield return null;
        //             }
        //
        //             if (referenceManager.graphGenerator.nrOfLODGroups > 1)
        //             {
        //                 combGraph.gameObject.AddComponent<LODGroup>();
        //                 referenceManager.graphGenerator.UpdateLODGroups(combGraph);
        //             }
        //
        //             combGraph.transform.localPosition = new Vector3(0, 0, 0);
        //             continue;
        //         }
        //
        //         cell = referenceManager.cellManager.AddCell(gpTuple.Item1);
        //         gp = referenceManager.graphGenerator.AddGraphPoint(cell, gpTuple.Item2.x, gpTuple.Item2.y,
        //             gpTuple.Item2.z);
        //         prevCoord = currentCoord;
        //     }
        //
        //     // if (spatialGraph.slices.Count > 0)
        //     // {
        //     //     removingOldSlices = true;
        //     //     StartCoroutine(RemoveOldSlices());
        //     // }
        //     //
        //     // while (removingOldSlices)
        //     // {
        //     //     yield return null;
        //     // }
        //
        //     spatialGraph.AddSlices();
        //
        //     if (spatialGraph.slices.Count > 1)
        //     {
        //         for (int i = 0; i < spatialGraph.slices.Count; i++)
        //         {
        //             float pos = -0.5f + i * (1f / (spatialGraph.slices.Count - 1));
        //             GraphSlice slice = spatialGraph.slices[i].GetComponent<GraphSlice>();
        //             slice.sliceCoords[axis] = pos;
        //             // print($"name: {slice.gameObject.name}, pos: {pos}");
        //         }
        //     }
        //
        //     buildingGraph = false;
        //     print($"building graph done");
        //
        //
        //     // var point = spatialGraph.points[spatialGraph.points.Count / 2];
        //     // gp = referenceManager.graphManager.FindGraphPoint("Slice20", point.Item1);
        //     // print($"point: {gp.WorldPosition}, slicer: {slicer.transform.position}");
        // }
    }
}