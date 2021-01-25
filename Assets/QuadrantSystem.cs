// using CellexalVR.General;
// using CellexalVR.Interaction;

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace DefaultNamespace
{
    // [MaterialProperty("_Color", MaterialPropertyFormat.Float4)]
    // public struct PointColor : IComponentData
    // {
    //     public float4 color;
    // }
    //
    // [MaterialProperty("_Alpha", MaterialPropertyFormat.Float)]
    // public struct Alpha : IComponentData
    // {
    //     public float value;
    // }

    // [MaterialProperty("_PlanePosition", MaterialPropertyFormat.Float4)]
    // public struct PlanePositionComponent : IComponentData
    // {
    //     public float4 planePosition;
    // }
    //
    // [MaterialProperty("_PlaneNormal", MaterialPropertyFormat.Float4)]
    // public struct PlaneNormalComponent : IComponentData
    // {
    //     public float4 planeNormal;
    // }

    public struct QuadrantData
    {
        public Entity entity;
        public float3 position;
        public Point point;
    }

    public class QuadrantSystem : SystemBase
    {
        public const int quadrantYMultiplier = 1000;
        public const int quadrantZMultiplier = 100;
        public const int quadrantCellSize = 1;
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
                    point = point,
                });
            }
        }

        protected override void OnCreate()
        {
            quadrantMultiHashMap = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
            base.OnCreate();
        }

        protected override void OnDestroy()
        {
            quadrantMultiHashMap.Dispose();
            base.OnDestroy();
        }

        public void SetHashMap()
        {
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
            JobHandle jobHandle = setQuadrantDataHashMapJob.Schedule(entityQuery, Dependency);
            jobHandle.Complete();
        }

        protected override void OnUpdate()
        {
            if (!SelectionTool.instance.selectionActive) return;
            if (Input.GetKeyDown(KeyCode.K))
            {
                SetHashMap();
            }
            // quadrantMultiHashMap.AsParallelWriter();
            // Entities.WithStoreEntityQueryInField(ref entityQuery).ForEach((Entity entity, ref LocalToWorld localToWorld, ref Point point) =>
            // {
            //     int hashMapKey = GetPositionHashMapKey(localToWorld.Position);
            //     quadrantMultiHashMap.Add(hashMapKey, new QuadrantData
            //     {
            //         entity = entity,
            //         position = localToWorld.Position,
            //         point = point,
            //     });
            // }).ScheduleParallel();
        }

        private void DebugDrawCube(float3 position)
        {
            Vector3 lowerLeft = new Vector3(math.floor((position.x * 10) / quadrantCellSize) * quadrantCellSize,
                math.floor((position.y * 10) / quadrantCellSize) * quadrantCellSize,
                math.floor((position.z * 10) / quadrantCellSize) * quadrantCellSize);
            lowerLeft /= 10;
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