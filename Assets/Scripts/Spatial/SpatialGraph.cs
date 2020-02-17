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
        private bool slicesActive;

        public Dictionary<string, GraphPoint> points = new Dictionary<string, GraphPoint>();
        public GameObject chunkManagerPrefab;
        public GameObject contourParent;
        public Material opaqueMat;
        public ReferenceManager referenceManager;
        public GameObject replacementPrefab;
        public GameObject wirePrefab;

        void Start()
        {
            rightController = referenceManager.rightController;
        }

        void Update()
        {
            rdevice = SteamVR_Controller.Input((int)rightController.index);
            if (rdevice.GetPress(SteamVR_Controller.ButtonMask.Touchpad) && rdevice.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0).y < 0.5f)
            {
                if (rdevice.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
                {
                    ActivateSlices();
                    referenceManager.multiuserMessageSender.SendMessageActivateSlices();
                }
            }

        }

        public IEnumerator AddSlices()
        {
            foreach (Graph graph in GetComponentsInChildren<Graph>())
            {
                foreach (KeyValuePair<string, Graph.GraphPoint> gpPair in graph.points)
                {
                    points[gpPair.Key] = gpPair.Value;
                }
            }
            yield return null;
        }

        /// <summary>
        /// Create a mesh using the marching cubes algorithm. Read the coordinates and add a density value of one to each point.
        /// </summary>
        /// <returns></returns>
        public IEnumerator CreateMesh()
        {
            string path = Directory.GetCurrentDirectory() + @"\Data\" + CellexalUser.DataSourceFolder + Path.DirectorySeparatorChar + "slice.mds";
            ChunkManager chunkManager = GameObject.Instantiate(chunkManagerPrefab).GetComponent<ChunkManager>();
            yield return null;
            int i = 0;
            using (StreamReader sr = new StreamReader(path))
            {

                string header = sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    string[] coords = sr.ReadLine().Split(new string[] { " ", "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    i++;
                    int x = (int)float.Parse(coords[1]);
                    int y = (int)float.Parse(coords[2]);
                    int z = (int)float.Parse(coords[3]);
                    chunkManager.addDensity(x, y, z, 1);

                    //chunkManager.addDensity(x, y, z + (1 % z), 1);
                    //chunkManager.addDensity(x, y, z + z * (1 % z), 1);
                    //chunkManager.addDensity(x, y, z + z * (1 % z), 1);
                }
            }
            print(i);

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

            //string path = Directory.GetCurrentDirectory() + @"\Data\" + CellexalUser.DataSourceFolder + Path.DirectorySeparatorChar + "gene1triang" + ".hull";
            string vertPath = Directory.GetCurrentDirectory() + @"\Data\" + CellexalUser.DataSourceFolder + Path.DirectorySeparatorChar + geneName + ".mesh";
            ChunkManager chunkManager = GameObject.Instantiate(chunkManagerPrefab).GetComponent<ChunkManager>();
            chunkManager.gameObject.name = geneName;
            yield return null;
            using (StreamReader sr = new StreamReader(vertPath))
            {
                sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    string[] line = sr.ReadLine().Split(null);
                    chunkManager.addDensity((int)float.Parse(line[1]), (int)float.Parse(line[2]), (int)float.Parse(line[3]), 1);
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
        /// Activate/Deactive slicemode. Activating means making each slice of the graph interactable independently of the others.
        /// Deactivating will reorganise them back to their original orientation and they will be moved as one object.
        /// </summary>
        public void ActivateSlices()
        {
            slicesActive = !slicesActive;
            foreach (GraphSlice gs in GetComponentsInChildren<GraphSlice>())
            {
                if (slicesActive)
                {
                    StartCoroutine(gs.ActivateSlice(true));
                }
                else
                {
                    StartCoroutine(gs.ActivateSlice(false));
                    ResetSlices();
                }
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

        }
    }
}
