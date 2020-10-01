using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRTK;
using VRTK.GrabAttachMechanics;
using VRTK.SecondaryControllerGrabActions;

namespace CellexalVR.Spatial
{
    /// <summary>
    /// Represents one graph with the same z - coordinate (one slice of the spatial graph).
    /// Each slice can be moved independently if in slice mode otherwise they should be moved together as one object.
    /// </summary>
    public class GraphSlice : MonoBehaviour
    {
        public ReferenceManager referenceManager;
        public bool sliceMode;
        public GameObject replacement;
        public GameObject wire;
        public bool buildingSlice;
        public Texture2D[] textures;
        public Dictionary<int, int> textureWidths = new Dictionary<int, int>();
        public Dictionary<int, int> textureHeights = new Dictionary<int, int>();
        public GraphSlice parentSlice;

        public int SliceNr
        {
            get { return sliceNr; }
            set { sliceNr = value; }
        }

        public Vector3 sliceCoords;
        public Dictionary<string, Graph.GraphPoint> points = new Dictionary<string, Graph.GraphPoint>();
        public List<GameObject> LODGroupParents = new List<GameObject>();
        public Dictionary<int, List<GameObject>> lodGroupClusters = new Dictionary<int, List<GameObject>>();
        public Graph.OctreeNode octreeRoot;
        public SpatialGraph spatialGraph;

        protected Graph graph;

        private Vector3 originalPos;
        private Vector3 originalSc;
        private Quaternion originalRot;
        private VRTK.VRTK_InteractableObject interactableObject;
        private GameObject wirePrefab;
        private GameObject replacementPrefab;
        private Color replacementCol;
        private Color replacementHighlightCol;
        private bool grabbing;
        private int flipped = 1;
        private int sliceNr;
        private Slicer slicer;


        private void Start()
        {
            interactableObject = gameObject.GetComponent<VRTK.VRTK_InteractableObject>();
            interactableObject.InteractableObjectGrabbed += OnGrabbed;
            interactableObject.InteractableObjectUngrabbed += OnUngrabbed;
            originalPos = Vector3.zero; //transform.localPosition;
            originalRot = transform.localRotation;
            originalSc = transform.localScale;
            slicer = GetComponentInChildren<Slicer>(true);
            //GetComponent<Rigidbody>().drag = Mathf.Infinity;
            //GetComponent<Rigidbody>().angularDrag = Mathf.Infinity;
        }

        private void OnGrabbed(object sender, VRTK.InteractableObjectEventArgs e)
        {
            if (grabbing)
                return;
            if (!sliceMode)
            {
                grabbing = true;
            }
        }

        private void OnUngrabbed(object sender, VRTK.InteractableObjectEventArgs e)
        {
            grabbing = false;
        }

        /// <summary>
        /// Animation to move the slice back to its original position within the parent object.
        /// </summary>
        /// <returns></returns>
        public IEnumerator MoveToGraphCoroutine()
        {
            // transform.parent = spatialGraph.transform;
            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;
            Quaternion targetRot = Quaternion.identity;

            float time = 1f;
            float t = 0f;
            while (t < time)
            {
                float progress = Mathf.SmoothStep(0, time, t);
                transform.localPosition = Vector3.Lerp(startPos, originalPos, progress);
                transform.localRotation = Quaternion.Lerp(startRot, targetRot, progress);
                t += (Time.deltaTime / time);
                yield return null;
            }

            transform.localPosition = originalPos;
            transform.localRotation = originalRot;
            //wire.SetActive(false);
            //replacement.GetComponent<Renderer>().material.color = replacementCol;
            //replacement.SetActive(false);
        }


        /// <summary>
        /// Add replacement prefab instance. A replacement is spawned when slices is removed from parent to show where it came from.
        /// </summary>
        public void AddReplacement()
        {
            wirePrefab = spatialGraph.wirePrefab;
            replacementPrefab = spatialGraph.replacementPrefab;
            replacement = Instantiate(replacementPrefab, transform.parent);
            Vector3 maxCoords = graph.ScaleCoordinates(graph.maxCoordValues);
            replacement.transform.localPosition = new Vector3(0, maxCoords.y + 0.2f, sliceCoords.z);
            replacement.gameObject.name = "repl" + this.gameObject.name;
            replacementCol = replacement.GetComponent<Renderer>().material.color;
            replacementHighlightCol = new Color(replacementCol.r, replacementCol.g, replacementCol.b, 1.0f);
            //replacementCol = new Color(0, 205, 255, 0.3f);
            replacement.SetActive(false);

            wire = Instantiate(wirePrefab, transform.parent);
            LineRenderer lr = wire.GetComponent<LineRenderer>();
            lr.startColor = lr.endColor = new Color(255, 255, 255, 0.1f);
            lr.startWidth = lr.endWidth /= 2;
            wire.SetActive(false);
        }

        /// <summary>
        /// Activate/Deactivate a slice. Activating means the slice can be moved individually away from the parent object.
        /// When activating the slices are pulled apart slighly to make it easier to grab them.
        /// </summary>
        /// <param name="activate"></param>
        /// <returns></returns>
        public void ActivateSlice(bool activate, bool move = true)
        {
            foreach (BoxCollider bc in GetComponents<BoxCollider>())
            {
                bc.enabled = activate;
            }

            if (activate)
            {
                Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    rigidbody = gameObject.AddComponent<Rigidbody>();
                }

                rigidbody.useGravity = false;
                rigidbody.isKinematic = false;
                rigidbody.drag = 10;
                rigidbody.angularDrag = 15;
                GetComponent<VRTK_InteractableObject>().isGrabbable = true;
                sliceMode = true;

                if (move)
                {
                    // transform.TransformPoint(targetPos);
                    float time = 1f;
                    StartCoroutine(MoveSlice(sliceCoords, time));
                }
            }

            else
            {
                GetComponent<VRTK_InteractableObject>().isGrabbable = false;
                Destroy(GetComponent<Rigidbody>());
                sliceMode = false;
                slicer.gameObject.SetActive(false);
                GetComponentInChildren<SliceBoxActivator>(true).ToggleCollider(false);
            }
        }

        public IEnumerator MoveSlice(Vector3 targetPos, float animationTime, bool lookAtCamera = false,
            bool rotate = false,
            Quaternion targetRot = default)
        {
            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;
            float t = 0f;
            while (t < animationTime)
            {
                float progress = Mathf.SmoothStep(0, animationTime, t);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, progress);
                if (rotate)
                {
                    transform.localRotation = Quaternion.Lerp(startRot, targetRot, progress);
                }

                t += (Time.deltaTime / animationTime);
                if (lookAtCamera)
                {
                    transform.LookAt(referenceManager.headset.transform);
                }

                yield return null;
            }
        }

        public IEnumerator FlipSlice(float animationTime)
        {
            flipped *= -1;
            Vector3 center = GetComponent<BoxCollider>().bounds.center;
            float t = 0f;
            float angle = 5f;
            float finalAngle = 0f;
            while (finalAngle <= 180)
            {
                transform.RotateAround(center, transform.up, angle);
                finalAngle += angle;
                yield return null;
            }
        }


        public IEnumerator BuildSlice(bool scale = true)
        {
            buildingSlice = true;
            graph = GetComponent<Graph>();
            referenceManager.graphGenerator.newGraph = graph;
            StartCoroutine(
                referenceManager.graphGenerator.SliceClusteringLOD(
                    referenceManager.graphGenerator.nrOfLODGroups, points, scale: scale));

            while (referenceManager.graphGenerator.isCreating)
            {
                yield return null;
            }

            graph.points = points;

            if (referenceManager.graphGenerator.nrOfLODGroups > 1)
            {
                if (GetComponent<LODGroup>() == null)
                {
                    gameObject.AddComponent<LODGroup>();
                }

                referenceManager.graphGenerator.UpdateLODGroups(graph, slice: this);
            }

            spatialGraph.slices.Add(this);
            referenceManager.graphManager.Graphs.Add(graph);

            buildingSlice = false;


            // place slicer correct
            float xMax = points.Max(v => v.Value.Position.x);
            float yMax = points.Max(v => v.Value.Position.y);
            float zMax = points.Max(v => v.Value.Position.z);
            float xMin = points.Min(v => v.Value.Position.x);
            float yMin = points.Min(v => v.Value.Position.y);
            float zMin = points.Min(v => v.Value.Position.z);

            var max = new Vector3(xMax, yMax, zMax);
            var min = new Vector3(xMin, yMin, zMin);
            var diff = max - min;
            var gDiff = graph.maxCoordValues - graph.minCoordValues;
            var ratio = new Vector3(diff.x / gDiff.x, diff.y / gDiff.y,
                diff.z / gDiff.z) + Vector3.one * 0.1f;

            var mid = (min + max) / 2;

            slicer.transform.localScale = ratio;
            slicer.transform.localPosition = mid;

            SliceBoxActivator sba = GetComponentInChildren<SliceBoxActivator>();
            sba.transform.localScale = ratio;
            sba.transform.localPosition = mid;

            var menuScale = slicer.slicingMenuParent.transform.localScale;
            menuScale.y /= ratio.y;
            menuScale.z /= ratio.z;
            slicer.slicingMenuParent.transform.localScale = menuScale;
        }


        public void SetTexture(Dictionary<string, Color32> textureColors, int k)
        {
            Texture2D texture = graph.textures[k];
            foreach (KeyValuePair<string, Graph.GraphPoint> point in points)
            {
                Vector2Int textureCoord = point.Value.textureCoord[k];
                Color32 col = textureColors[point.Key];
                texture.SetPixel(textureCoord.x, textureCoord.y, col);
            }

            texture.Apply();
        }

        public void MoveToReference()
        {
            GetComponent<VRTK_InteractableObject>().isGrabbable = false;
            Destroy(GetComponent<Rigidbody>());
            gameObject.transform.parent = referenceManager.brainModel.transform;
            Vector3 targetPosition = referenceManager.brainModel.startPosition;
            StartCoroutine(MoveSlice(targetPosition, 1, false, true, Quaternion.Euler(0, 90, -180)));
            transform.localScale = Vector3.one * 0.82f;
            slicer.gameObject.SetActive(false);
            GetComponent<SliceManager>().slicesActive = true;
        }

        public void MoveToParentSlice()
        {
            // GetComponent<VRTK_InteractableObject>().isGrabbable = true;
            // if (GetComponent<Rigidbody>() == null)
            // {
            //     var rigidbody = gameObject.AddComponent<Rigidbody>();
            //     rigidbody.useGravity = false;
            //     rigidbody.drag = 10;
            //     rigidbody.angularDrag = 15;
            // }

            if (parentSlice != null)
            {
                SliceManager parentSliceManager = parentSlice.GetComponent<SliceManager>();
                foreach (GraphSlice slice in parentSliceManager.slices)
                {
                    slice.transform.parent = parentSlice.transform;
                }

                parentSliceManager.ActivateSlices(false);
            }

            else
            {
                transform.parent = null;
                GetComponent<SliceManager>().ActivateSlices(false);
            }


            transform.localScale = Vector3.one;


            // StartCoroutine(MoveToGraphCoroutine());
            // transform.localPosition = Vector3.zero;
            // transform.localRotation = Quaternion.identity;
            // transform.localScale = Vector3.one;
            //
            // slicer.gameObject.SetActive(false);
        }
    }
}