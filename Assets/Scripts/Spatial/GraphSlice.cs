using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using System.Collections;
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
        public int sliceNr;
        public float zCoord;


        protected Graph graph;

        private Vector3 originalPos;
        private Vector3 originalSc;
        private Quaternion originalRot;
        private SpatialGraph spatGraph;
        private VRTK.VRTK_InteractableObject interactableObject;
        private GameObject wirePrefab;
        private GameObject replacementPrefab;
        private Color replacementCol;
        private Color replacementHighlightCol;
        private bool grabbing;
        private int flipped = 1;


        private void Start()
        {
            graph = gameObject.GetComponent<Graph>();
            spatGraph = transform.parent.gameObject.GetComponent<SpatialGraph>();
            interactableObject = gameObject.GetComponent<VRTK.VRTK_InteractableObject>();
            interactableObject.InteractableObjectGrabbed += OnGrabbed;
            interactableObject.InteractableObjectUngrabbed += OnUngrabbed;
            originalPos = transform.localPosition;
            originalRot = transform.localRotation;
            originalSc = transform.localScale;
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
            transform.parent = spatGraph.transform;
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
            wirePrefab = spatGraph.wirePrefab;
            replacementPrefab = spatGraph.replacementPrefab;
            replacement = Instantiate(replacementPrefab, transform.parent);
            Vector3 maxCoords = graph.ScaleCoordinates(graph.maxCoordValues);
            replacement.transform.localPosition = new Vector3(0, maxCoords.y + 0.2f, zCoord);
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
                    Vector3 targetPos = new Vector3(transform.position.x, transform.position.y, zCoord);
                    // transform.TransformPoint(targetPos);
                    float time = 1f;
                    StartCoroutine(MoveSlice(targetPos.x, targetPos.y, targetPos.z, time));
                }
            }
            else
            {
                GetComponent<VRTK_InteractableObject>().isGrabbable = false;
                Destroy(GetComponent<Rigidbody>());
                sliceMode = false;
                //graph.transform.localPosition = Vector3.zero;
            }
        }

        public IEnumerator MoveSlice(float x, float y, float z, float animationTime, bool rotate = false)
        {
            Vector3 startPos = transform.localPosition;
            Vector3 targetPosition = new Vector3(x, y, z);
            float t = 0f;
            while (t < animationTime)
            {
                float progress = Mathf.SmoothStep(0, animationTime, t);
                transform.localPosition = Vector3.Lerp(startPos, targetPosition, progress);
                t += (Time.deltaTime / animationTime);
                if (rotate)
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

    }
}