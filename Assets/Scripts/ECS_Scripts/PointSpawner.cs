using DefaultNamespace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CellexalVR.AnalysisLogic;
using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;
using Directory = System.IO.Directory;
using Random = Unity.Mathematics.Random;


public class PointSpawner : MonoBehaviour
{
    public static PointSpawner instance;
    public CellManager cellManager;
    [SerializeField] public Material material;
    [SerializeField] public Material outlineMaterial;

    [SerializeField] public Mesh mesh;

    [SerializeField] private Color[] colors;
    [HideInInspector] public Material[] selectedMaterials;


    private static EntityManager _entityManager;

    private static RenderMeshProxy _meshInstanceRenderer;
    private static EntityArchetype _pointArchetype;
    private Random random;
    private float spawnTimer;
    private int graphNr;
    private bool spawned;
    private float3 minCoordValues;
    private float3 maxCoordValues;
    private float3 longestAxis;
    private float3 scaledOffset;
    private float3 diffCoordValues;

    public List<Vector3> positions = new List<Vector3>();
    public ReferenceManager referenceManager;
    public Color[] geneExpressionColors;
    public Texture2D graphPointColors;

    private void OnValidate()
    {
        if (gameObject.scene.IsValid())
        {
            referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
        }
    }

    private void Awake()
    {
        instance = this;
        QuadrantSystem quadrantSystem = World.Active.GetExistingSystem<QuadrantSystem>();
        quadrantSystem.referenceManger = referenceManager;
        quadrantSystem.selectionManager = referenceManager.selectionManager;
        quadrantSystem.selectionToolCollider = referenceManager.selectionToolCollider;
    }

    // Start is called before the first frame update
    private void Start()
    {
        // CreateShaderColors();
        CellexalEvents.ConfigLoaded.AddListener(CreateMaterials);

        // string dir = Directory.GetCurrentDirectory() + "//tsne3D.mds";
        // var positions = ReadData(dir);
        // ScaleAllCoordinates();
        // var g1 = CreateEntities(positions);
        // CreateGraphTexture(16000, positions.Count / 16000, g1);

        // string dir2 = Directory.GetCurrentDirectory() + "//slice_coords.mds";
        // positions = ReadData(dir2);
        // ScaleAllCoordinates();
        // var g2 = CreateEntities(positions);
        // CreateGraphTexture(16000, positions.Count / 16000, g2);
    }

    private void CreateMaterials()
    {
        Debug.Log("creating materials");
        var selColors = CellexalConfig.Config.SelectionToolColors;
        selectedMaterials = new Material[selColors.Length];
        for (int i = 0; i < selColors.Length; i++)
        {
            Material mat = new Material(material)
                {color = selColors[i]}; // color = new Color((i + 1) * 10, (i) * 2, 50)};
            selectedMaterials[i] = mat;
        }
        
    }

    private List<Vector3> ReadData(string dir)
    {
        List<Vector3> positions = new List<Vector3>();
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        minCoordValues = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
        maxCoordValues = new float3(float.MinValue, float.MinValue, float.MinValue);
        using (StreamReader streamReader = new StreamReader(dir))
        {
            streamReader.ReadLine();

            while (!streamReader.EndOfStream)
            {
                string[] words = streamReader.ReadLine().Split(null);
                float x = (float.Parse(words[1])); // / 30.0f) + 1.5f;
                float y = (float.Parse(words[2])); // / 30.0f) + 1;
                float z = float.Parse(words[3]); // / 30.0f;
                // cellManager.AddCell(words[0], new Vector3(x, y, z));
                positions.Add(new Vector3(x, y, z));
                UpdateMinMaxCoords(x, y, z);
            }
        }

        return positions;
    }


    public void AddGraphPoint(float x, float y, float z)
    {
        positions.Add(new Vector3(x, y, z));
        UpdateMinMaxCoords(x, y, z);
    }


    private void UpdateMinMaxCoords(float x, float y, float z)
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

    private void ScaleAllCoordinates()
    {
        diffCoordValues = maxCoordValues - minCoordValues;
        longestAxis = math.max(diffCoordValues.x, math.max(diffCoordValues.y, diffCoordValues.z));
        maxCoordValues += (float3) mesh.bounds.size;
        minCoordValues -= (float3) mesh.bounds.size;
        scaledOffset = (diffCoordValues / longestAxis) / 2;
    }

    public void CreateEntities(Graph graph)
    {
        ScaleAllCoordinates();
        var gps = graph.points.Values.ToList();
        Material graphPointMaterial = Instantiate(material);
        _entityManager = World.Active.EntityManager;

        EntityArchetype entityArchetype = _entityManager.CreateArchetype(
            typeof(Translation),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(LocalToParent),
            typeof(Parent),
            typeof(Point)
        );

        EntityArchetype entityArchetypeParent = _entityManager.CreateArchetype(
            typeof(LocalToWorld),
            typeof(Translation),
            typeof(Rotation),
            typeof(Scale),
            typeof(Point)
        );

        Entity parent = _entityManager.CreateEntity(entityArchetypeParent);
        _entityManager.SetComponentData(parent, new Translation {Value = new float3(1, 1, 0)});
        _entityManager.SetComponentData(parent, new Rotation {Value = new quaternion(0, 1, 0, 0)});
        _entityManager.SetComponentData(parent, new Scale {Value = 1f});
        _entityManager.SetComponentData(parent, new Point {pointType = PointType.EntityType.Graph, parentId = graphNr});
        NativeArray<Entity> entityArray = new NativeArray<Entity>(positions.Count, Allocator.Temp);
        _entityManager.CreateEntity(entityArchetype, entityArray);
        for (int i = 0; i < entityArray.Length; i++)
        {
            Entity entity = entityArray[i];
            _entityManager.SetComponentData(entity, new Parent {Value = parent});
            _entityManager.SetComponentData(entity,
                new Point {pointId = i, pointType = PointType.EntityType.GraphPoint});

            // move one of the graph's corners to origo
            Translation translation = new Translation
                {Value = new float3(positions[i].x, positions[i].y, positions[i].z)};
            translation.Value -= minCoordValues;
            // uniformly scale all axes down based on the longest axis
            // this makes the longest axis have length 1 and keeps the proportions of the graph
            translation.Value /= longestAxis;
            // move the graph a bit so (0, 0, 0) is the center point
            translation.Value -= scaledOffset;
            _entityManager.SetComponentData(entity, translation);
            _entityManager.SetSharedComponentData(entity, new RenderMesh
            {
                mesh = mesh,
                material = graphPointMaterial,
            });
        }

        entityArray.Dispose();
        var pointMoveSystem = World.Active.GetExistingSystem<PointMoveSystem>();
        // var parentGraph = Instantiate(referenceManager.graphGenerator.graphPrefab);
        // parentGraph.gameObject.name = "Graph" + graphNr;
        pointMoveSystem.graphParentTransforms.Add(graph.transform);
        pointMoveSystem.graphParentTransforms[graphNr] = graph.transform;
        graphNr++;
        referenceManager.graphGenerator.isCreating = false;
        // g.graphPointMaterial = graphPointMaterial;
    }

    // private void CreateGraphTexture(int width, int height, Graph g)
    // {
    //     Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
    //
    //     texture.filterMode = FilterMode.Point;
    //     texture.anisoLevel = 0;
    //     for (int i = 0; i < texture.width; ++i)
    //     {
    //         for (int j = 0; j < texture.height; ++j)
    //         {
    //             texture.SetPixel(i, j, Color.yellow);
    //         }
    //     }
    //
    //     texture.Apply();
    //     g.texture = texture;
    //     g.graphPointMaterial.mainTexture = texture;
    // }
    //
    // /// <summary>
    // /// Creates the texture that the CombinedGraph shader uses to fetch colors for the graphpoints.
    // /// </summary>
    // public void CreateShaderColors()
    // {
    //     Color lowColor = Color.blue;
    //     Color midColor = Color.yellow;
    //     Color highColor = Color.red;
    //     int nbrOfExpressionColors = 30;
    //
    //     int nbrOfSelectionColors = selColors;
    //
    //     if (nbrOfExpressionColors + nbrOfSelectionColors > 254)
    //     {
    //         nbrOfSelectionColors = 254 - nbrOfExpressionColors;
    //     }
    //     else if (nbrOfExpressionColors < 3)
    //     {
    //         nbrOfExpressionColors = 3;
    //     }
    //
    //     int halfNbrOfExpressionColors = nbrOfExpressionColors / 2;
    //
    //     Color[] lowMidExpressionColors =
    //         Extensions.InterpolateColors(lowColor, midColor, halfNbrOfExpressionColors);
    //     Color[] midHighExpressionColors = Extensions.InterpolateColors(midColor, highColor,
    //         nbrOfExpressionColors - halfNbrOfExpressionColors + 1);
    //
    //     geneExpressionColors = new Color[30 + 1];
    //     geneExpressionColors[0] = Color.white;
    //     Array.Copy(lowMidExpressionColors, 0, geneExpressionColors, 1, lowMidExpressionColors.Length);
    //     Array.Copy(midHighExpressionColors, 1, geneExpressionColors, 1 + lowMidExpressionColors.Length,
    //         midHighExpressionColors.Length - 1);
    //
    //     //// reservered colors
    //     //graphpointColors[255] = Color.white;
    //
    //     graphPointColors = new Texture2D(256, 1, TextureFormat.ARGB32, false);
    //     int pixel = 0;
    //     for (int i = 0; i < halfNbrOfExpressionColors; ++i)
    //     {
    //         graphPointColors.SetPixel(pixel, 0, lowMidExpressionColors[i]);
    //         pixel++;
    //     }
    //
    //     for (int i = 1; i < nbrOfExpressionColors - halfNbrOfExpressionColors + 1; ++i)
    //     {
    //         graphPointColors.SetPixel(pixel, 0, midHighExpressionColors[i]);
    //         pixel++;
    //     }
    //
    //     for (int i = 0; i < nbrOfSelectionColors; ++i)
    //     {
    //         graphPointColors.SetPixel(pixel, 0, colors[i]);
    //         pixel++;
    //     }
    //
    //     // Setting a block of colours because when going from linear to gamma space in the shader could cause rounding errors.
    //     //graphPointColors.SetPixel(255, 0, CellexalConfig.Config.GraphDefaultColor);
    //     //graphPointColors.SetPixel(254, 0, CellexalConfig.Config.GraphDefaultColor);
    //     //graphPointColors.SetPixel(253, 0, CellexalConfig.Config.GraphDefaultColor);       
    //     //graphPointColors.SetPixel(252, 0, CellexalConfig.Config.GraphZeroExpressionColor);
    //     //graphPointColors.SetPixel(251, 0, CellexalConfig.Config.GraphZeroExpressionColor);
    //     //graphPointColors.SetPixel(250, 0, CellexalConfig.Config.GraphZeroExpressionColor);
    //
    //     graphPointColors.SetPixel(255, 0, new Color(0.84f, 0.84f, 0.84f));
    //     graphPointColors.SetPixel(254, 0, Color.black);
    //
    //     graphPointColors.filterMode = FilterMode.Point;
    //     graphPointColors.Apply();
    //
    //     material.SetTexture("_GraphpointColorTex", graphPointColors);
    //     foreach (Graph graph in graphs)
    //     {
    //         graph.graphPointMaterial.SetTexture("_GraphpointColorTex", graphPointColors);
    //     }
    // }
}