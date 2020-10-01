using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

// namespace DefaultNamespace
// {
//     public class FindPointsSystem : JobComponentSystem
//     {
//         private SelectionTool selectionTool;
//
//         private struct EntityWithPosition
//         {
//             public Entity entity;
//             public float3 position;
//         }
//
//         protected override void OnCreate()
//         {
//             selectionTool = GameObject.Find("SelectionTool").GetComponent<SelectionTool>();
//             base.OnCreate();
//         }
//
//         // [RequireComponentTag(typeof(Point))]
//         // [BurstCompile]
//         // private struct FindGraphPointsInQuadrantSystemJob : IJobForEachWithEntity<Translation, Point>
//         // {
//         //     [ReadOnly] public NativeMultiHashMap<int, QuadrantData> quadrantMultiHashMap;
//         //
//         //     public void Execute(Entity entity, int index, ref Translation translation, ref Point point)
//         //     {
//         //         float3 position = translation.Value;
//         //         int hashMapKey = QuadrantSystem.GetPositionHashMapKey(position);
//         //
//         //         // QuadrantData quadrantData;
//         //         // NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
//         //         // if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator))
//         //         // {
//         //         //     do
//         //         //     {
//         //         //     } while (quadrantMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
//         //         // }
//         //     }
//         //
//         //     private void FindPoints(int hashMapKey, float3 position)
//         //     {
//         //         QuadrantData quadrantData;
//         //         NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
//         //         if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator))
//         //         {
//         //             do
//         //             {
//         //                 if (math.distance(position, selectionTool.transform.position) < 0.1f)
//         //                 {
//         //                     AddPointToSelection();
//         //                 }
//         //             } while (quadrantMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
//         //         }
//         //     }
//         // }
//         //
//         // public void AddPointToSelection()
//         // {
//         //     
//         // }
//         //
//         // protected override JobHandle OnUpdate(JobHandle inputDeps)
//         // {
//         //     FindGraphPointsInQuadrantSystemJob findGraphPointsInQuadrantSystemJob =
//         //         new FindGraphPointsInQuadrantSystemJob
//         //         {
//         //             quadrantMultiHashMap = QuadrantSystem.quadrantMultiHashMap,
//         //         };
//         //     JobHandle jobHandle = findGraphPointsInQuadrantSystemJob.Schedule(this, inputDeps);
//         //     return jobHandle;
//         // }
//     }
// }