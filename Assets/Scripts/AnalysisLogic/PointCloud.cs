using System;
using System.Collections;
using System.Collections.Generic;
using CellexalVR;
using CellexalVR.AnalysisObjects;
using DefaultNamespace;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

namespace AnalysisLogic
{
    public class PointCloud : MonoBehaviour
    {
        private VisualEffect vfx;
        private int pointCount;
        private bool spawn = true;
        private EntityManager entityManager;
        private int frameCount;

        public Texture2D positionTextureMap;
        public Texture2D colorMap;
        public float3 minCoordValues;
        public float3 maxCoordValues;
        public float3 longestAxis;
        public float3 scaledOffset;
        public float3 diffCoordValues;
        public Dictionary<string, float3> points = new Dictionary<string, float3>();
        public int pcID;
        public Dictionary<int, string> clusterDict = new Dictionary<int, string>();
        public Dictionary<string, Color> colorDict = new Dictionary<string, Color>();
        public Transform selectionSphere;

        public void Initialize(int id)
        {
            minCoordValues = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            maxCoordValues = new float3(float.MinValue, float.MinValue, float.MinValue);
            pcID = id;
            gameObject.name = "PointCloud" + pcID;
            vfx = GetComponent<VisualEffect>();
            selectionSphere = GameObject.Find("SelSphere").transform;
            vfx.pause = true;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                CreateColorTextureMap();
            }

            if (frameCount >= 50)
            {
                frameCount = 0;
                vfx.SetTexture("ColorMapTex", colorMap);
            }
            
            print(vfx.aliveParticleCount);

            vfx.SetVector3("SelectionPosition", selectionSphere.position);
            vfx.SetFloat("SelectionRadius", selectionSphere.localScale.x / 2f);
        }

        public IEnumerator CreatePositionTextureMap(List<Graph.GraphPoint> graphPoints)
        {
            pointCount = graphPoints.Count;
            vfx.SetInt("SpawnRate", pointCount);
            int width = (int) math.ceil(math.sqrt(graphPoints.Count));
            int height = width;
            positionTextureMap = new Texture2D(width, height, TextureFormat.RGBAFloat, true, true);
            Color col;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int ind = x + (height * y);
                    if (ind >= graphPoints.Count) continue;
                    Vector3 pos = graphPoints[ind].Position + Vector3.one * 0.5f;
                    col = new Color(pos.x, pos.y, pos.z, 1);
                    positionTextureMap.SetPixel(x, y, col);
                }

                // print(y);
                yield return null;
            }

            positionTextureMap.Apply();
            vfx.enabled = true;
            vfx.SetTexture("PositionMapTex", positionTextureMap);
            yield return new WaitForSeconds(1.1f);
            // vfx.SetInt("SpawnRate", 0);
        }

        public IEnumerator CreatePositionTextureMap(List<float3> pointPositions)
        {
            pointCount = pointPositions.Count;
            vfx.SetInt("SpawnRate", pointCount);
            int width = (int) math.ceil(math.sqrt(pointPositions.Count));
            int height = width;
            positionTextureMap = new Texture2D(width, height, TextureFormat.RGBAFloat, true, true);
            Color col;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int ind = x + (height * y);
                    if (ind >= pointPositions.Count) continue;
                    float3 pos = pointPositions[ind] + 0.5f;
                    col = new Color(pos.x, pos.y, pos.z, 1);
                    Entity e = entityManager.Instantiate(PrefabEntities.prefabEntity);
                    float3 wPos = math.transform(transform.localToWorldMatrix, pos);
                    entityManager.SetComponentData(e, new Translation {Value = wPos});
                    entityManager.AddComponent(e, typeof(Point));
                    entityManager.SetComponentData(e, new Point
                    {
                        group = -1,
                        selected = false,
                        xindex = x,
                        yindex = y
                    });
                    positionTextureMap.SetPixel(x, y, col);
                }

                // print(y);
                yield return null;
            }

            positionTextureMap.Apply();
            vfx.enabled = true;
            vfx.SetTexture("PositionMapTex", positionTextureMap);
            vfx.pause = false;
            yield return new WaitForSeconds(1.5f);
            vfx.Stop();
            vfx.Play();
            // vfx.SetInt("SpawnRate", 0);
            GC.Collect();
        }

        public IEnumerator CreateColorTextureMap()
        {
            int width = (int) math.ceil(math.sqrt(pointCount));
            int height = width;
            colorMap = new Texture2D(width, height, TextureFormat.RGBAFloat, true, true);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int ind = x + (width * y);
                    if (ind >= pointCount) continue;
                    Color c = clusterDict.Count == 0 ? Color.white : colorDict[clusterDict[ind]];
                    colorMap.SetPixel(x, y, c);
                }

                yield return null;
            }

            colorMap.Apply();
            vfx.enabled = true;
            vfx.Play();
            vfx.SetTexture("ColorMapTex", colorMap);

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<TextureHandler>().colorTextureMap = colorMap;
        }

        public void UpdateColorTexture(int x, int y)
        {
            colorMap.SetPixel(x, y, Color.red);
            colorMap.Apply();
            vfx.SetTexture("ColorMapTex", colorMap);
        }

        public void AddGraphPoint(string cellName, float x, float y, float z)
        {
            points[cellName] = new float3(x, y, z);
            UpdateMinMaxCoords(x, y, z);
            // if (cells.Contains(cellName)) return;
            // PointSpawner.instance.cells.Add(cellName);
        }

        public void UpdateMinMaxCoords(float x, float y, float z)
        {
            if (x < minCoordValues.x)
                minCoordValues.x = x;
            if (y < minCoordValues.y)
                minCoordValues.y = y;
            if (z < minCoordValues.z)
                minCoordValues.z = z;
            if (x > maxCoordValues.x)
                maxCoordValues.x = x;
            if (y > maxCoordValues.y)
                maxCoordValues.y = y;
            if (z > maxCoordValues.z)
                maxCoordValues.z = z;
        }
    }
}