using System;
using CellexalVR.AnalysisObjects;
using CellexalVR.General;
using CellexalVR.Interaction;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace DefaultNamespace
{
    public class PointSelectionSystem : ComponentSystem
    {
        private ReferenceManager referenceManager;
        private SelectionManager selectionManager;
        private SelectionToolCollider selectionToolCollider;
        private Entity parent;

        private GameObject newParent;

        // [ReadOnly] public NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap =
        // QuadrantSystem.quadrantMultiHashMap;

        public bool selectionActive;


        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            if (referenceManager == null)
            {
                referenceManager = GameObject.Find("InputReader").GetComponent<ReferenceManager>();
                selectionManager = referenceManager.selectionManager;
                selectionToolCollider = referenceManager.selectionToolCollider;
            }

            if (!selectionActive) return;
            float3 selectionToolCenter = selectionToolCollider
                .selectionToolColliders[selectionToolCollider.CurrentMeshIndex].transform.position;
            int hashMapKey = QuadrantSystem.GetPositionHashMapKey(selectionToolCenter);
            CheckOneQuadrantYLayer(hashMapKey, selectionToolCenter, -1);
            CheckOneQuadrantYLayer(hashMapKey, selectionToolCenter, 0);
            CheckOneQuadrantYLayer(hashMapKey, selectionToolCenter, 1);
        }

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

        private void AddIfPointInsideSelectionTool(int hashMapKey, float3 selectionToolCenter)
        {
            QuadrantData quadrantData;
            NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
            if (QuadrantSystem.quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData,
                out nativeMultiHashMapIterator))
            {
                do
                {
                    Vector3 difference = selectionToolCenter - quadrantData.position;
                    if (!Physics.Raycast(quadrantData.position, difference, difference.magnitude,
                        Graph.selectionToolLayerMask) && !quadrantData.point.selected)
                    {
                        AddGraphPointToSelection(quadrantData);
                        Debug.DrawRay(quadrantData.position, difference, Color.green);
                    }
                } while (QuadrantSystem.quadrantMultiHashMap.TryGetNextValue(out quadrantData,
                    ref nativeMultiHashMapIterator));
            }

            // if (math.distance(selectionToolCenter, quadrantData.position) < 0.15f && !quadrantData.point.selected)
        }

        private void AddGraphPointToSelection(QuadrantData quadrantData)
        {
            // World.Active.EntityManager.DestroyEntity(quadrantData.entity);
            // return;
            // PostUpdateCommands.AddComponent(quadrantData.entity,
            //     new PointSelected {group = selectionTool.currentGroup});
            if (quadrantData.point.pointType == PointType.EntityType.Graph) return;
            if (parent == Entity.Null)
            {
                parent = AddNewParent();
            }

            EntityManager entityManager = World.Active.EntityManager;
            entityManager.SetComponentData(quadrantData.entity,
                new Point {group = selectionToolCollider.CurrentColorIndex, selected = true});
            try
            {
                entityManager.SetComponentData(quadrantData.entity, new Parent {Value = parent});
            }
            catch (Exception e)
            {
                Debug.Log($"could not add parent : {quadrantData.entity.Index} , {quadrantData.point.pointType}");
            }

            // entityManager.SetComponentData(quadrantData.entity, new Point{  = 1});
            PostUpdateCommands.RemoveComponent(quadrantData.entity, typeof(RenderMesh));
            PostUpdateCommands.AddSharedComponent(quadrantData.entity, new RenderMesh
            {
                mesh = PointSpawner.instance.mesh,
                material = PointSpawner.instance.selectedMaterials[selectionToolCollider.CurrentColorIndex]
                // material = PointSpawner.instance.material
            });
        }

        private Entity AddNewParent()
        {
            newParent = GameObject.Find("BrainParent"); //.GetComponent<AllenReferenceBrain>().rootModel;
            float3 brainPos = newParent.transform.position;
            quaternion brainRot = newParent.transform.rotation;
            float3 brainScale = newParent.transform.localScale;
            // newParent.gameObject.name = "Graph3";
            var pointMoveSystem = World.Active.GetExistingSystem<PointMoveSystem>();
            pointMoveSystem.graphParentTransforms.Add(newParent.transform);
            pointMoveSystem.graphParentTransforms[2] = newParent.transform;
            EntityArchetype entityArchetypeParent = World.Active.EntityManager.CreateArchetype(
                typeof(LocalToWorld),
                typeof(Translation),
                typeof(Rotation),
                typeof(Scale),
                typeof(Point)
            );
            parent = World.Active.EntityManager.CreateEntity(entityArchetypeParent);
            EntityManager entityManager = World.Active.EntityManager;
            entityManager.SetComponentData(parent, new Translation {Value = brainPos});
            entityManager.SetComponentData(parent, new Rotation {Value = brainRot});
            entityManager.SetComponentData(parent, new Scale {Value = brainScale.x});
            entityManager.SetComponentData(parent, new Point {pointType = PointType.EntityType.Graph, parentId = 2});
            return parent;
        }

        private void DebugDrawCube(float3 position)
        {
            Vector3 lowerLeft = new Vector3(math.floor((position.x * 5) / QuadrantSystem.quadrantCellSize) * QuadrantSystem.quadrantCellSize,
                math.floor((position.y * 5) / QuadrantSystem.quadrantCellSize) * QuadrantSystem.quadrantCellSize,
                math.floor((position.z * 5) / QuadrantSystem.quadrantCellSize) * QuadrantSystem.quadrantCellSize);
            lowerLeft /= 5;
            float size = 0.1f;
            Vector3[] corners = new Vector3[]
            {
                lowerLeft,
                new Vector3(lowerLeft.x + (size * 2f), lowerLeft.y, lowerLeft.z),
                new Vector3(lowerLeft.x, lowerLeft.y + (size * 2f), lowerLeft.z),
                new Vector3(lowerLeft.x, lowerLeft.y, lowerLeft.z + size * 2f),
                new Vector3(lowerLeft.x + (size * 2f), lowerLeft.y + (size * 2f), lowerLeft.z + (size * 2f)),
                new Vector3(lowerLeft.x + (size * 2f), lowerLeft.y, lowerLeft.z + (size * 2f)),
                new Vector3(lowerLeft.x, lowerLeft.y + (size * 2f), lowerLeft.z + (size * 2f)),
                new Vector3(lowerLeft.x + (size * 2f), lowerLeft.y + (size * 2f), lowerLeft.z)
            };

            Debug.DrawLine(corners[0], corners[1]);
            Debug.DrawLine(corners[0], corners[2]);
            Debug.DrawLine(corners[0], corners[3]);
            Debug.DrawLine(corners[1], corners[5]);
            Debug.DrawLine(corners[2], corners[6]);
            Debug.DrawLine(corners[3], corners[5]);
            Debug.DrawLine(corners[3], corners[6]);
            Debug.DrawLine(corners[4], corners[7]);
            Debug.DrawLine(corners[1], corners[7]);
            Debug.DrawLine(corners[4], corners[5]);
            Debug.DrawLine(corners[4], corners[6]);
            Debug.DrawLine(corners[2], corners[7]);
        }
    }
}