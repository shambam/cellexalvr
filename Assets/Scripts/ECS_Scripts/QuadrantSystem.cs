using System.Runtime.CompilerServices;
using CellexalVR.General;
using CellexalVR.Interaction;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace DefaultNamespace
{
    public struct Point : IComponentData
    {
        public int pointId;
        public int cellId;
        public bool selected;
        public int parentId;
        public int group;
        public float3 centerPoint;
        public PointType.EntityType pointType;
    }

    public struct PointType : IComponentData
    {
        public enum EntityType
        {
            GraphPoint,
            Graph,
        }
    }

    public struct QuadrantData
    {
        public Entity entity;
        public float3 position;
        public Point point;
    }

    public class QuadrantSystem : ComponentSystem
    {
        public const int quadrantYMultiplier = 1000;
        public const int quadrantZMultiplier = 100;
        public const int quadrantCellSize = 1;

        public ReferenceManager referenceManger;
        public SelectionManager selectionManager;
        public SelectionToolCollider selectionToolCollider;
        public static NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap;
        public bool selectionActive;
        
        public static int GetPositionHashMapKey(float3 position)
        {
            return (int) (math.floor((position.x * 5) / quadrantCellSize) +
                          (quadrantYMultiplier * math.floor((position.y * 5) / quadrantCellSize)) +
                          (quadrantZMultiplier * math.floor((position.z * 5) / quadrantCellSize)));
        }

        private static int GetEntityCountInHashMap(NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap,
            int hashMapKey)
        {
            QuadrantData quadrantData;
            NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
            int count = 0;
            if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator))
            {
                do
                {
                    count++;
                } while (quadrantMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
            }

            return count;
        }

        [BurstCompile]
        private struct SetQuadrantDataHashMapJob : IJobForEachWithEntity<LocalToWorld, Point>
        {
            public NativeMultiHashMap<int, QuadrantData>.ParallelWriter quadrantMultiHashMap;

            public void Execute(Entity entity, int index, ref LocalToWorld localToWorld,
                ref Point point)
            {
                int hashMapKey = GetPositionHashMapKey(localToWorld.Position);
                quadrantMultiHashMap.Add(hashMapKey, new QuadrantData
                {
                    entity = entity,
                    position = localToWorld.Position,
                    point = point
                });
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            quadrantMultiHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);

        }

        protected override void OnDestroy()
        {
            quadrantMultiHashMap.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (!selectionActive) return;
            // if (selectionToolTransform == null)
            // {
            //     selectionToolTransform = GameObject.Find("SelectionSphere").transform;
            // }

            // Debug.Log($"hashmap key : {GetPositionHashMapKey(selectionToolTransform.position)}, pos : {selectionToolTransform.position}");
            DebugDrawCube(referenceManger.selectionToolCollider.selectionToolColliders[selectionToolCollider.CurrentMeshIndex].transform.position);
            // int key = GetPositionHashMapKey(selectionToolTransform.position);
            // int count = GetEntityCountInHashMap(quadrantMultiHashMap, key);
            // Debug.Log(count);
            
            
            EntityQuery entityQuery = GetEntityQuery(typeof(Point), typeof(LocalToWorld));
            quadrantMultiHashMap.Clear();
            if (entityQuery.CalculateEntityCount() > quadrantMultiHashMap.Capacity)
            {
                quadrantMultiHashMap.Capacity = entityQuery.CalculateEntityCount();
            }

            SetQuadrantDataHashMapJob setQuadrantDataHashMapJob = new SetQuadrantDataHashMapJob
            {
                quadrantMultiHashMap = quadrantMultiHashMap.AsParallelWriter(),
            };
            JobHandle jobHandle = JobForEachExtensions.Schedule(setQuadrantDataHashMapJob, entityQuery);
            jobHandle.Complete();
        }

        private void DebugDrawCube(float3 position)
        {
            Vector3 lowerLeft = new Vector3(math.floor((position.x * 5) / quadrantCellSize) * quadrantCellSize,
                math.floor((position.y * 5) / quadrantCellSize) * quadrantCellSize,
                math.floor((position.z * 5) / quadrantCellSize) * quadrantCellSize);
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