using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using static CellexalVR.AnalysisObjects.Graph;
using CellexalVR.MarchingCubes;
using CellexalVR.General;
using System;
using System.Linq;
using CellexalVR.AnalysisObjects;
using CellexalVR.Menu.Buttons.Slicing;
using VRTK;

namespace CellexalVR.Spatial
{
    public class SliceManager : MonoBehaviour
    {
        public bool slicesActive;
        public bool controllerInsideSlice;
        public ReferenceManager referenceManager;
        public List<GraphSlice> slices = new List<GraphSlice>();
        public VRTK_InteractableObject interactableObject;

        private SteamVR_TrackedObject rightController;
        private SteamVR_Controller.Device rdevice;
        private GameObject contour;
        private Vector3 startPosition;
        private Rigidbody _rigidBody;
        private bool dispersing;
        private Vector3 positionBeforeDispersing;
        private Quaternion rotationBeforeDispersing;

        private void Start()
        {
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
            rightController = referenceManager.rightController;
            startPosition = transform.position;
            _rigidBody = GetComponent<Rigidbody>();
            interactableObject = GetComponent<VRTK_InteractableObject>();

            // var angle = -(Math.PI);
            // var radius = 1f;
            // for (int i = 0; i < 8; i++)
            // {
            //     Vector3 pos = radius * new Vector3((float) Math.Cos(angle), 1, (float) Math.Sin(angle));
            //     var obj = GameObject.Instantiate(cubePrefab, transform);
            //     obj.transform.localPosition = pos;
            //     angle += (Math.PI) / 8d;
            // }
        }

        private void Update()
        {
            // if (GetComponent<VRTK_InteractableObject>().IsGrabbed())
            // {
            //     referenceManager.multiuserMessageSender.SendMessageMoveGraph(gameObject.name, transform.position,
            //         transform.rotation, transform.localScale);
            // }

            // transform.LookAt(referenceManager.headset.transform);
            rdevice = SteamVR_Controller.Input((int) rightController.index);
            if (rdevice.GetPress(SteamVR_Controller.ButtonMask.Touchpad) &&
                rdevice.GetAxis().y < 0.5f)
            {
                if (rdevice.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
                {
                    ActivateSlices(!slicesActive);
                    referenceManager.multiuserMessageSender.SendMessageActivateSlices();
                }
            }

            if (rdevice.GetPress(SteamVR_Controller.ButtonMask.Touchpad) &&
                rdevice.GetAxis().y > 0.5f)
            {
                if (rdevice.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
                {
                    StartCoroutine(FlipSlices());
                }
            }

            if (_rigidBody != null && _rigidBody.velocity.magnitude > 2f && !dispersing)
            {
                positionBeforeDispersing = transform.localPosition;
                rotationBeforeDispersing = transform.localRotation;
                StartCoroutine(DisperseSlices());

                // ActivateSlices();
            }


            // if (Input.GetKeyDown(KeyCode.P))
            // {
            //     print("Start slicing Graph");
            //     StartCoroutine(slices[0].GetComponent<GraphSlicer>().SliceGraph(false, activateSlices: true));
            // }

            if (Input.GetKeyDown(KeyCode.G))
            {
                ActivateSlices(true);
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                ActivateSlices(false);
            }
        }

        /// <summary>
        /// Places the slices in a grid pattern to be able to look at them all individually.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DisperseSlices()
        {
            dispersing = true;
            _rigidBody.drag = 1;
            _rigidBody.angularDrag = 1;

            float time = 0;

            while (time <= 1.0f)
            {
                time += Time.deltaTime;
                yield return null;
            }

            _rigidBody.velocity = Vector3.zero;
            _rigidBody.angularVelocity = Vector3.zero;

            transform.LookAt(referenceManager.inputFolderGenerator.transform);
            double angle = (Math.PI * 1.1d);
            Vector3 center = Vector3.zero; // referenceManager.headset.transform.position;
            int slicesPerRow = slices.Count / 4;
            float yDiff = transform.position.y;
            float xPos;
            float yPos = (yDiff > 0f) ? -0.5f : -yDiff;
            float zPos;
            float radius = 4.0f;
            List<Vector3> slicePositions = new List<Vector3>();
            for (int j = 0; j < slices.Count; j++)
            {
                if (j % slicesPerRow == 0 && j > 0)
                {
                    angle = (Math.PI * 1.1d);
                    radius += 0.1f;
                    yPos += 1.0f;
                }

                xPos = center.x + (float) Math.Cos(angle) * radius;
                zPos = center.z + (float) Math.Sin(angle) * radius / 2f;
                Vector3 pos = new Vector3(xPos, yPos, zPos);
                slicePositions.Add(pos);
                angle += (Math.PI * 0.9d) / (double) slicesPerRow;
            }

            float animationTime = 1f;
            GraphSlice gs;
            for (int i = 0; i < slices.Count; i++)
            {
                gs = slices[i].GetComponent<GraphSlice>();
                Vector3 pos = slicePositions[i];
                StartCoroutine(gs.MoveSlice(pos.x, pos.y, pos.z, animationTime, true));
                // yield return new WaitForSeconds(0.001f);
            }

            while (time < 1f + animationTime)
            {
                time += Time.deltaTime;
                yield return null;
            }

            ActivateSlices(false);
        }

        /// <summary>
        /// Move graph back to position before slices where dispersed.
        /// </summary>
        /// <returns></returns>
        private IEnumerator GatherSlices()
        {
            yield return new WaitForSeconds(1f);
            transform.localScale = Vector3.one;
            float animationTime = 1f;
            float t = 0;
            Vector3 startPosition = transform.localPosition;
            Quaternion startRotation = transform.localRotation;
            while (t < animationTime)
            {
                float progress = Mathf.SmoothStep(0, animationTime, t);
                transform.localPosition = Vector3.Lerp(startPosition, positionBeforeDispersing, progress);
                transform.localRotation =
                    Quaternion.Lerp(startRotation, rotationBeforeDispersing, progress);
                t += (Time.deltaTime / animationTime);
                yield return null;
            }

            dispersing = false;
        }

        private IEnumerator FlipSlices()
        {
            foreach (GraphSlice slice in slices)
            {
                // GraphSlice slice = graph.GetComponent<GraphSlice>();
                StartCoroutine(slice.FlipSlice(1f));
                yield return new WaitForSeconds(0.01f);
            }
        }

        /// <summary>
        /// Activate/Deactive slicemode. Activating means making each slice of the graph interactable independently of the others.
        /// Deactivating will reorganise them back to their original orientation and they will be moved as one object.
        /// </summary>
        public void ActivateSlices(bool activate, bool move = true)
        {
            if (slicesActive == activate) return;
            slicesActive = activate;
            GetComponentInChildren<SliceBoxActivator>().ToggleCollider(!activate);
            GetComponents<BoxCollider>().All(x => x.enabled = !activate);
            if (!interactableObject)
            {
                interactableObject = GetComponent<VRTK_InteractableObject>();
            }

            interactableObject.isGrabbable = !activate;

            foreach (GraphSlice gs in slices)
            {
                GatherSlicesButton gatherSlicesButton = gs.GetComponentInChildren<GatherSlicesButton>(true);
                gatherSlicesButton.SetButtonActivated(true);
                gatherSlicesButton.sliceManager = this;
                // gs.GetComponentInChildren<GatherSlicesButton>(true).TurnOn();//SetButtonActivated(activate);
                gs.ActivateSlice(activate, move);
                SliceManager manager = gs.GetComponent<SliceManager>();
                if (manager)
                {
                    manager.enabled = activate;
                    enabled = !activate;
                }

                else
                {
                    enabled = true;
                }
            }

            if (activate)
            {
                Destroy(_rigidBody);
                // Destroy(GetComponent<Collider>());
                Transform parentSliceTransform = transform.parent;
                if (!parentSliceTransform) return;
                SliceManager parentSlice = parentSliceTransform.GetComponent<SliceManager>();
                if (!parentSlice) return;
                parentSlice.enabled = false;
                enabled = true;
            }

            else
            {
                Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    print("adding rigidbody");
                    rigidbody = gameObject.AddComponent<Rigidbody>();
                }

                _rigidBody = rigidbody;
                _rigidBody.useGravity = false;
                _rigidBody.isKinematic = false;
                _rigidBody.drag = 10;
                _rigidBody.angularDrag = 15;
                ResetSlices();
                Transform parentSliceTransform = transform.parent;
                if (!parentSliceTransform) return;
                SliceManager parentSlice = parentSliceTransform.GetComponent<SliceManager>();
                if (!parentSlice) return;
                parentSlice.enabled = false;
                enabled = false;
            }
        }

        public void ToggleGraphPointsTransparency(bool toggle)
        {
            GetComponent<Graph>().MakeAllPointsTransparent(toggle);
            // foreach (Graph graph in slices)
            // {
            //     graph.MakeAllPointsTransparent(toggle);
            // }
        }

        /// <summary>
        /// Reset the slices back to their original position inside the parent object.
        /// </summary>
        private void ResetSlices()
        {
            foreach (GraphSlice gs in slices)
            {
                StartCoroutine(gs.MoveToGraphCoroutine());
            }

            if (dispersing)
            {
                StartCoroutine(GatherSlices());
            }
        }

        public GraphSlice GetSlice(string sliceName)
        {
            foreach (GraphSlice slice in slices)
            {
                if (slice.gameObject.name.Equals(sliceName))
                    return slice;
            }

            return null;
        }

        public void ResetPosition()
        {
            transform.position = startPosition;
        }

        public void ResetSizeAndRotation()
        {
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }
    }
}