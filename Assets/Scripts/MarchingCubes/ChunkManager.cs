using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CellexalVR.MarchingCubes
{


    public class ChunkManager : MonoBehaviour
    {
        public Transform sphereHolder;
        public static int chunkResolution = 9;
        public static int size = 9;
        public static int n = (ChunkManager.chunkResolution - 1) * size + 1;
        public static bool addVertexSpheres = false;
        public static bool addDensitySpheres = false;
        public static float surfaceLevel = 0.6f;


        public BoxCollider boxCollider;
        public GameObject chunkPrefab;
        public ChunkScript[,,] chunks;
        public float[,,] density = new float[n, n, n];
        public GameObject[,,] densitySpheres = new GameObject[n, n, n];
        private GameObject turtle;

        [Range(0, 1)] public float brushSize;

        public void OnValidate()
        {
            //transform.localScale = (Vector3.one / size) * 2;
        }

        // Start is called before the first frame update
        void Awake()
        {
            boxCollider.center = transform.position + Vector3.one * size * 0.5f;
            boxCollider.size = Vector3.one * size;

            turtle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            turtle.transform.localPosition = Vector3.one * 0.5f;
            turtle.transform.localScale = Vector3.one * 0.2f / chunkResolution;
            chunks = new ChunkScript[size, size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    for (int k = 0; k < size; k++)
                    {
                        //print(i + " " + j + " " + k);
                        ChunkScript chunk = Instantiate(chunkPrefab, new Vector3(i, j, k), Quaternion.identity, transform).GetComponent<ChunkScript>();
                        chunk.chunkManager = this;
                        chunk.index = new int[] { i, j, k };
                        chunks[i, j, k] = chunk;
                    }
                }
            }
            //createSphereDensity();
            //transform.localScale = (Vector3.one / size) * 2;
            //transform.localPosition = Vector3.one * (size / 2);

        }

        private void ReadCoords()
        {
            string path = "C:\\Users\\vrproject\\Documents\\CellexalVR\\cellexalvr\\Data\\BrainSlidesTest\\tsne.mds";

            using (StreamReader streamReader = new StreamReader(path))
            {
                string header = streamReader.ReadLine();
                while (!streamReader.EndOfStream)
                {
                    string[] coords = streamReader.ReadLine().Split(null);
                    addDensity(int.Parse(coords[1]), int.Parse(coords[2]), int.Parse(coords[3]), 1);

                }
                {

                }
            }
            foreach (ChunkScript c in chunks)
            {
                c.updateVertices();
                c.updateTriangles();
            }


        }

        //private void Update()
        //{
        //    if (Input.GetKey(KeyCode.W))
        //    {
        //        turtle.transform.Translate(Vector3.forward / 50f);
        //    }
        //    if (Input.GetKey(KeyCode.S))
        //    {
        //        turtle.transform.Translate(-Vector3.forward / 50f);
        //    }
        //    if (Input.GetKey(KeyCode.A))
        //    {
        //        turtle.transform.Translate(-Vector3.right / 50f);
        //    }
        //    if (Input.GetKey(KeyCode.D))
        //    {
        //        turtle.transform.Translate(Vector3.right / 50f);
        //    }
        //    if (Input.GetKey(KeyCode.Q))
        //    {
        //        turtle.transform.Translate(-Vector3.up / 50f);
        //    }
        //    if (Input.GetKey(KeyCode.E))
        //    {
        //        turtle.transform.Translate(Vector3.up / 50f);
        //    }
        //    if (Input.GetKey(KeyCode.Q))
        //    {
        //        addSphericalDensity(turtle.transform.position);
        //    }
        //    if (Input.GetKeyDown(KeyCode.F))
        //    {
        //        toggleSurfaceLevelandUpdateCubes(0.05f);
        //    }
        //    if (Input.GetKeyDown(KeyCode.R))
        //    {
        //        toggleSurfaceLevelandUpdateCubes(-0.05f);
        //    }
        //    if (Input.GetKeyDown(KeyCode.Space))
        //    {
        //        foreach (ChunkScript c in chunks)
        //        {
        //            c.updateVertices();
        //            c.updateTriangles();
        //        }
        //    }
        //    if (Input.GetKeyDown(KeyCode.T))
        //    {
        //        ReadCoords();
        //    }
        //}

        public void addSphericalDensity(Vector3 pos)
        {
            pos = transform.InverseTransformPoint(pos);
            HashSet<ChunkScript> chunksToUpdate = new HashSet<ChunkScript>();
            for (float i = -0.5f; i < 1; i++)
            {
                for (float j = -0.5f; j < 1; j++)
                {
                    for (float k = -0.5f; k < 1; k++)
                    {
                        Vector3 temp = pos + new Vector3(i, j, k);
                        if (temp.x < 0 || temp.x > size || temp.y < 0 || temp.y > size || temp.z < 0 || temp.z > size)
                            continue;
                        chunksToUpdate.Add(getChunkFromPos(temp));
                    }
                }
            }
            pos = pos * (chunkResolution - 1);
            int rad = (int)(((chunkResolution - 1) / 2) * brushSize);
            for (int i = 0; i < chunkResolution - 1; i++)
            {
                for (int j = 0; j < rad * 2; j++)
                {
                    for (int k = 0; k < rad * 2; k++)
                    {
                        int x = Mathf.RoundToInt(pos.x + i - rad);
                        int y = Mathf.RoundToInt(pos.y + j - rad);
                        int z = Mathf.RoundToInt(pos.z + k - rad);
                        float dist = Vector3.Distance(pos, new Vector3(x, y, z)) / rad;
                        addDensity(x, y, z, Mathf.Clamp01(1.0f - dist));
                    }
                }
            }
            foreach (ChunkScript c in chunksToUpdate)
            {
                c.updateVertices();
                c.updateTriangles();
            }
        }

        private ChunkScript getChunkFromPos(Vector3 pos)
        {
            return chunks[(int)pos.x, (int)pos.y, (int)pos.z];
        }

        public void toggleSurfaceLevelandUpdateCubes(float i)
        {
            //surfaceLevel += i;
            //surfaceLevel = Mathf.Clamp01(surfaceLevel);
            foreach (ChunkScript c in chunks)
            {
                c.updateVertices();
                c.updateTriangles();
            }
        }

        public void createSphereDensity()
        {
            Vector3 centre = Vector3.one * (n - 1) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    for (int k = 0; k < n; k++)
                    {
                        float dist = Vector3.Distance(centre, new Vector3(i, j, k)) / ((n - 1) / 2);
                        addDensity(i, j, k, Mathf.Clamp01(1.0f - dist));
                    }
                }
            }
        }

        public void addDensity(int x, int y, int z, float d)
        {
            if (x < 0 || x >= n || y < 0 || y >= n || z < 0 || z >= n)
                return;
            density[x, y, z] += d;
            if (addDensitySpheres)
            {
                if (densitySpheres[x, y, z] == null)
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.SetParent(sphereHolder);
                    sphere.transform.localPosition = new Vector3(x, y, z) / (chunkResolution - 1);
                    sphere.transform.localScale = Vector3.one / ((ChunkManager.chunkResolution - 1) * 3) * density[x, y, z];
                    densitySpheres[x, y, z] = sphere;
                }
                densitySpheres[x, y, z].GetComponent<Renderer>().material.color = new Color(1 - density[x, y, z], 1 - density[x, y, z], 1 - density[x, y, z], density[x, y, z]);
            }

        }

        public void setDensity(int x, int y, int z, float d)
        {
            if (x < 0 || x >= n || y < 0 || y >= n || z < 0 || z >= n)
                return;
            density[x, y, z] = d;
            if (addDensitySpheres)
            {
                if (densitySpheres[x, y, z] == null)
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.SetParent(sphereHolder);
                    sphere.transform.localPosition = new Vector3(x, y, z) / (chunkResolution - 1);
                    sphere.transform.localScale = Vector3.one / ((ChunkManager.chunkResolution - 1) * 3) * d;
                    densitySpheres[x, y, z] = sphere;
                }
                densitySpheres[x, y, z].GetComponent<Renderer>().material.color = new Color(1 - d, 1 - d, 1 - d, d);
            }
        }

        public void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position + transform.localScale * size * 0.5f, transform.localScale * size);
        }

    }
}
