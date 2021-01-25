using System.Collections.Generic;
using System.Linq;
using AnalysisLogic;
using CellexalVR.Spatial;
using Unity.Burst;
// using CellexalVR.AnalysisObjects;
// using CellexalVR.General;
// using CellexalVR.Interaction;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

namespace DefaultNamespace
{
    public struct SelectedPointComponent : IComponentData
    {
    }

    public class PointSelectionSystem : SystemBase
    {
        // private ReferenceManager referenceManager;
        // private SelectionManager selectionManager;
        // private SelectionToolCollider selectionToolCollider;
        private Entity brainParent;
        private bool textureChanged;
        private int frameCount;

        private GameObject newParent;

        private EntityManager entityManager;
        public Texture2D colorTextureMap;

        // [ReadOnly] public NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap = QuadrantSystem.quadrantMultiHashMap;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityManager = World.EntityManager;
            ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        public void CreateMesh()
        {
            EntityQuery group = GetEntityQuery(typeof(SelectedPointComponent), typeof(Point));
            NativeArray<Entity> points = group.ToEntityArray(Allocator.Temp);
        }

        protected override void OnUpdate()
        {
            if (SelectionTool.instance == null || !SelectionTool.instance.selectionActive) return;
            float3 selectionToolCenter = SelectionTool.instance.transform.position;
            int hashMapKey = QuadrantSystem.GetPositionHashMapKey(selectionToolCenter);
            CheckOneQuadrantYLayer(hashMapKey, selectionToolCenter, -1);
            CheckOneQuadrantYLayer(hashMapKey, selectionToolCenter, 0);
            CheckOneQuadrantYLayer(hashMapKey, selectionToolCenter, 1);

            // if (frameCount >= 50)
            // {
            //     frameCount = 0;
            // }

            // frameCount++;

            // EntityQuery entityQuery = GetEntityQuery(typeof(Point), typeof(LocalToWorld));
            // AddPointsInsideJob addPointsInsideJob = new AddPointsInsideJob
            // {
            //     quadrantMultiHashMap = QuadrantSystem.quadrantMultiHashMap,
            //     selectionToolCenter = selectionToolCenter,
            //     entityManager = World.EntityManager,
            //     layerMask = selToolLayerMask
            // };
            // JobHandle jobHandle = addPointsInsideJob.Schedule(entityQuery, Dependency);
            // jobHandle.Complete();

            // EntityCommandBuffer.ParallelWriter ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
            // NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap = QuadrantSystem.quadrantMultiHashMap;
            // Entities.WithAll<Point>().ForEach((Entity entity, ref Translation translation) =>
            // {
            //     int hashMapKey = QuadrantSystem.GetPositionHashMapKey(selectionToolCenter);
            //     CheckOneQuadrantYLayer(hashMapKey, float3.zero, selectionToolCenter, -1, quadrantMultiHashMap);
            //     CheckOneQuadrantYLayer(hashMapKey, float3.zero, selectionToolCenter, 0, quadrantMultiHashMap);
            //     CheckOneQuadrantYLayer(hashMapKey, float3.zero, selectionToolCenter, 1, quadrantMultiHashMap);
            // }).Run();
        }

        [BurstCompile]
        private void CheckOneQuadrantYLayer(int hashMapKey, float3 selectionToolCenter, int y)
        {
            AddIfPointInsideSelectionTool(hashMapKey + y * QuadrantSystem.quadrantYMultiplier, selectionToolCenter); // current quadrant
            AddIfPointInsideSelectionTool((hashMapKey + 1) + (y * QuadrantSystem.quadrantYMultiplier), selectionToolCenter); // one to the right
            AddIfPointInsideSelectionTool((hashMapKey - 1) + y * QuadrantSystem.quadrantYMultiplier, selectionToolCenter); // one to the left
            AddIfPointInsideSelectionTool((hashMapKey) + 1 * QuadrantSystem.quadrantZMultiplier + y * QuadrantSystem.quadrantYMultiplier, selectionToolCenter);
            AddIfPointInsideSelectionTool((hashMapKey) - 1 * QuadrantSystem.quadrantZMultiplier + y * QuadrantSystem.quadrantYMultiplier, selectionToolCenter);
            AddIfPointInsideSelectionTool((hashMapKey + 1) + 1 * QuadrantSystem.quadrantZMultiplier + y * QuadrantSystem.quadrantYMultiplier, selectionToolCenter);
            AddIfPointInsideSelectionTool((hashMapKey - 1) - 1 * QuadrantSystem.quadrantZMultiplier + y * QuadrantSystem.quadrantYMultiplier, selectionToolCenter);
            AddIfPointInsideSelectionTool((hashMapKey + 1) - 1 * QuadrantSystem.quadrantZMultiplier + y * QuadrantSystem.quadrantYMultiplier, selectionToolCenter);
            AddIfPointInsideSelectionTool((hashMapKey - 1) + 1 * QuadrantSystem.quadrantZMultiplier + y * QuadrantSystem.quadrantYMultiplier, selectionToolCenter);
        }

        [BurstCompile]
        private void AddIfPointInsideSelectionTool(int hashMapKey, float3 selectionToolCenter)
        {
            // TODO: Make checking for points into parallel jobs. (Raycasting seems to only work in main thread....)
            if (QuadrantSystem.quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out QuadrantData quadrantData,
                out NativeMultiHashMapIterator<int> nativeMultiHashMapIterator))
            {
                do
                {
                    Point p = GetComponent<Point>(quadrantData.entity);
                    if (p.selected) continue;
                    Vector3 difference = selectionToolCenter - quadrantData.position;
                    if (!Physics.Raycast(quadrantData.position, difference, difference.magnitude,
                        1 << 8))
                    {
                        // Debug.DrawRay(quadrantData.position, difference, Color.green);
                        AddGraphPointToSelection(quadrantData);
                    }
                } while (QuadrantSystem.quadrantMultiHashMap.TryGetNextValue(out quadrantData,
                    ref nativeMultiHashMapIterator));
            }
        }


        [BurstCompile]
        private void AddGraphPointToSelection(QuadrantData quadrantData)
        {
            // if (quadrantData.point.pointType == PointType.EntityType.Graph) return;
            // List<Dictionary<string, Entity>> dicts = PointSpawner.instance.entityDicts;
            // foreach (Entity entity in dicts.Select(dict => dict[quadrantData.point.label.ToString()]))
            // {
            Point p = GetComponent<Point>(quadrantData.entity);
            // p.selected = true;
            p.group = SelectionTool.instance.currentGroup;
            // Debug.Log($"point selected : {p.yindex}, {p.xindex}");
            // if (point.pointType == PointType.EntityType.GraphPointSpatial)
            // {
            // MoveToReferenceBrain(quadrantData, entity, point);
            // }

            // Color color = SelectionTool.instance.GetCurrentColor(); //new Color(1, 0, 0, 1); //TextureHandler.instance.colors[SelectionTool.instance.currentGroup];
            // SetComponent(entity, new PointColor {color = new half4((half) color.r, (half) color.g, (half) color.b, (half) 1f)});
            // SetComponent(entity, new Alpha {value = 2f});
            entityManager.SetComponentData(quadrantData.entity, p);
            entityManager.AddComponent<SelectedPointComponent>(quadrantData.entity);
            // }

            // TextureHandler.instance.ColorPoint(quadrantData.entity, colorIndex);
            // entityManager.AddComponent<PointSelectedForColoring>(quadrantData.entity);
        }

        // [BurstCompile]
        // private void MoveToReferenceBrain(QuadrantData quadrantData, Entity entity, Point point)
        // {
        //     if (brainParent == Entity.Null)
        //     {
        //         GameObject brain = GameObject.Find("BrainParent");
        //         if (!brain) return;
        //         brainParent = AddNewParent(brain);
        //         int graphNr = PointSpawner.instance.nrOfGraphs++;
        //         PointMoveSystem pointMoveSystem = World.GetExistingSystem<PointMoveSystem>();
        //         pointMoveSystem.graphParentTransforms.Add(brain.transform);
        //         pointMoveSystem.graphParentTransforms[graphNr] = brain.transform;
        //     }
        //
        //     Point qdPoint = quadrantData.point;
        //     entityManager.AddComponent<PointMovedToNewParent>(quadrantData.entity);
        //     entityManager.AddComponent<MoveTowards>(quadrantData.entity);
        //     entityManager.SetComponentData(quadrantData.entity, new MoveTowards {speed = 0.5f});
        //     SetComponent(quadrantData.entity, new PointMovedToNewParent
        //     {
        //         previousParent = quadrantData.point.parent,
        //         newParent = brainParent
        //     });
        //     qdPoint.parentId = -1;
        //     qdPoint.previousParent = point.parent;
        //     qdPoint.parent = brainParent;
        //     qdPoint.offset = point.offset;
        //     // Debug.Log($"p : {point.offset}");
        //     SetComponent(quadrantData.entity, qdPoint);
        // }

        // [BurstCompile]
        // public Entity AddNewParent(GameObject newParent)
        // {
        //     float3 newParentPos = newParent.transform.position;
        //     quaternion newParentRot = newParent.transform.rotation;
        //     float3 newParentScale = newParent.transform.localScale;
        //     int graphNr = PointSpawner.instance.nrOfGraphs;
        //     // PointMoveSystem pointMoveSystem = World.GetExistingSystem<PointMoveSystem>();
        //     // pointMoveSystem.graphParentTransforms.Add(newParent.transform);
        //     // pointMoveSystem.graphParentTransforms[graphNr] = newParent.transform;
        //     newParent.transform.hasChanged = false;
        //     Entity parent = entityManager.CreateEntity(PointSpawner.instance.parentEntityArcheType);
        //
        //     entityManager.SetComponentData(parent, new Translation
        //     {
        //         Value = newParentPos
        //     });
        //     entityManager.SetComponentData(parent, new Rotation
        //     {
        //         Value = newParentRot
        //     });
        //     entityManager.SetComponentData(parent, new Scale
        //     {
        //         Value = newParentScale.x
        //     });
        //     entityManager.SetComponentData(parent, new GraphParent
        //     {
        //         graphNr = graphNr,
        //         pointCount = 0
        //     });
        //     // entityManager.AddComponent<ReferenceBrain>(brainParent);
        //     return parent;
        // }
    }
}