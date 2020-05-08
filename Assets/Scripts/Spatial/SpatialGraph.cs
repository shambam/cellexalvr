using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using static CellexalVR.AnalysisObjects.Graph;
using CellexalVR.MarchingCubes;
using CellexalVR.General;
using System;
using CellexalVR.AnalysisObjects;
using Unity.Mathematics;
using VRTK;

namespace CellexalVR.Spatial
{
    /// <summary>
    /// Represents a spatial graph that in turn consists of many slices. The spatial graph is the parent of the graph objects.
    /// </summary>
    public class SpatialGraph : MonoBehaviour
    {
        private SteamVR_TrackedObject rightController;
        private SteamVR_Controller.Device rdevice;
        private GameObject contour;
        private Vector3 startPosition;
        private Rigidbody _rigidBody;
        private bool dispersing;
        private Vector3 positionBeforeDispersing;
        private Quaternion rotationBeforeDispersing;

        public bool slicesActive;
        public List<Graph> slices = new List<Graph>();
        public Dictionary<string, GraphPoint> points = new Dictionary<string, GraphPoint>();
        public GameObject chunkManagerPrefab;
        public GameObject contourParent;
        public Material opaqueMat;
        public ReferenceManager referenceManager;
        public GameObject replacementPrefab;
        public GameObject wirePrefab;
        public GameObject brainModel;
        public GameObject cubePrefab;

        private void Start()
        {
            rightController = referenceManager.rightController;
            startPosition = transform.position;
            GameObject brain = GameObject.Instantiate(brainModel);
            brain.GetComponent<ReferenceMouseBrain>().spatialGraph = this;
            _rigidBody = GetComponent<Rigidbody>();

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
            if (GetComponent<VRTK_InteractableObject>().IsGrabbed())
            {
                referenceManager.multiuserMessageSender.SendMessageMoveGraph(gameObject.name, transform.position,
                    transform.rotation, transform.localScale);
            }

            // transform.LookAt(referenceManager.headset.transform);
            rdevice = SteamVR_Controller.Input((int) rightController.index);
            if (rdevice.GetPress(SteamVR_Controller.ButtonMask.Touchpad) &&
                rdevice.GetAxis().y < 0.5f)
            {
                if (rdevice.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
                {
                    ActivateSlices();
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
        }

        public IEnumerator AddSlices()
        {
            foreach (Graph graph in GetComponentsInChildren<Graph>())
            {
                foreach (BoxCollider bc in graph.GetComponents<BoxCollider>())
                {
                    Vector3 size = bc.size;
                    size.z += 0.01f;
                    bc.size = size;
                    bc.enabled = false;
                }

                foreach (KeyValuePair<string, Graph.GraphPoint> gpPair in graph.points)
                {
                    points[gpPair.Key] = gpPair.Value;
                }

                slices.Add(graph);
            }

            yield return null;
        }

        /// <summary>
        /// Create a mesh using the marching cubes algorithm. Read the coordinates and add a density value of one to each point.
        /// </summary>
        /// <returns></returns>
        public IEnumerator CreateMesh()
        {
            string path = Directory.GetCurrentDirectory() + @"\Data\" + CellexalUser.DataSourceFolder + @"\" +
                          "slice.mds";
            ChunkManager chunkManager = GameObject.Instantiate(chunkManagerPrefab).GetComponent<ChunkManager>();
            yield return null;
            int i = 0;
            using (StreamReader sr = new StreamReader(path))
            {
                string header = sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    string[] coords = sr.ReadLine()
                        .Split(new string[] {" ", "\t"}, StringSplitOptions.RemoveEmptyEntries);
                    i++;
                    int x = (int) float.Parse(coords[1]);
                    int y = (int) float.Parse(coords[2]);
                    int z = (int) float.Parse(coords[3]);
                    chunkManager.addDensity(x, y, z, 1);

                    //chunkManager.addDensity(x, y, z + (1 % z), 1);
                    //chunkManager.addDensity(x, y, z + z * (1 % z), 1);
                    //chunkManager.addDensity(x, y, z + z * (1 % z), 1);
                }
            }
            //print(i);

            chunkManager.toggleSurfaceLevelandUpdateCubes(0);


            foreach (MeshFilter mf in chunkManager.GetComponentsInChildren<MeshFilter>())
            {
                mf.mesh.RecalculateBounds();
                mf.mesh.RecalculateNormals();
            }

            contour = Instantiate(contourParent);
            chunkManager.transform.parent = contour.transform;
            contour.transform.localScale = Vector3.one * 0.15f;
            BoxCollider bc = contour.AddComponent<BoxCollider>();
            bc.center = Vector3.one * 4;
            bc.size = Vector3.one * 6;
        }

        /// <summary>
        /// Create a mesh inside the full spatial graph mesh. This is used when colouring by gene expression to create a kernel to visualise.
        /// </summary>
        /// <param name="geneName"></param>
        /// <returns></returns>
        public IEnumerator CreateMeshFromAShape(string geneName)
        {
            //string path = Directory.GetCurrentDirectory() + @"\Data\" + CellexalUser.DataSourceFolder + @"\" + "gene1triang" + ".hull";
            string vertPath = Directory.GetCurrentDirectory() + @"\Data\" + CellexalUser.DataSourceFolder + @"\" +
                              geneName + ".mesh";
            ChunkManager chunkManager = GameObject.Instantiate(chunkManagerPrefab).GetComponent<ChunkManager>();
            chunkManager.gameObject.name = geneName;
            yield return null;
            using (StreamReader sr = new StreamReader(vertPath))
            {
                sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    string[] line = sr.ReadLine().Split(null);
                    chunkManager.addDensity((int) float.Parse(line[1]), (int) float.Parse(line[2]),
                        (int) float.Parse(line[3]), 1);
                }
            }

            List<int> triangles = new List<int>();
            CellexalLog.Log("Started reading " + vertPath);
            chunkManager.toggleSurfaceLevelandUpdateCubes(0);
            foreach (MeshFilter mf in chunkManager.GetComponentsInChildren<MeshFilter>())
            {
                Renderer r = mf.gameObject.GetComponent<Renderer>();
                r.material = opaqueMat;
                r.material.color = Color.red;
                mf.mesh.RecalculateBounds();
                mf.mesh.RecalculateNormals();
            }

            chunkManager.transform.parent = contour.transform;
            yield return null;
            chunkManager.transform.localScale = Vector3.one;
            chunkManager.transform.localPosition = Vector3.zero;
            chunkManager.transform.localRotation = Quaternion.identity;
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
            foreach (Graph graph in slices)
            {
                GraphSlice slice = graph.GetComponent<GraphSlice>();
                StartCoroutine(slice.FlipSlice(1f));
                yield return new WaitForSeconds(0.01f);
            }
        }

        /// <summary>
        /// Activate/Deactive slicemode. Activating means making each slice of the graph interactable independently of the others.
        /// Deactivating will reorganise them back to their original orientation and they will be moved as one object.
        /// </summary>
        public void ActivateSlices(bool move = true)
        {
            foreach (GraphSlice gs in GetComponentsInChildren<GraphSlice>())
            {
                if (!slicesActive)
                {
                    Destroy(_rigidBody);
                    Destroy(GetComponent<Collider>());
                    gs.ActivateSlice(true, move);
                }
                else
                {
                    Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
                    if (rigidbody == null)
                    {
                        rigidbody = gameObject.AddComponent<Rigidbody>();
                    }

                    _rigidBody = rigidbody;
                    _rigidBody.useGravity = false;
                    _rigidBody.isKinematic = false;
                    _rigidBody.drag = 10;
                    _rigidBody.angularDrag = 15;
                    gs.ActivateSlice(false, move);
                    ResetSlices();
                    BoxCollider collider = GetComponent<BoxCollider>();
                    if (collider == null)
                    {
                        gameObject.AddComponent<BoxCollider>();
                    }
                }
            }

            slicesActive = !slicesActive;
        }

        public void ToggleGraphPointsTransparency(bool toggle)
        {
            foreach (Graph graph in slices)
            {
                graph.MakeAllPointsTransparent(toggle);
            }
        }

        /// <summary>
        /// Reset the slices back to their original position inside the parent object.
        /// </summary>
        private void ResetSlices()
        {
            foreach (GraphSlice gs in GetComponentsInChildren<GraphSlice>())
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
            foreach (GraphSlice slice in GetComponentsInChildren<GraphSlice>())
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