using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Valve.VR;

namespace DefaultNamespace
{
    public struct PointSelected : IComponentData
    {
        public int group;
    }


    public class PointMoveSystem : ComponentSystem
    {
        public List<Transform> graphParentTransforms = new List<Transform>();

        private float3 posPreviousFrame;
        private quaternion rotPreviousFrame;
        private int group = 1;

        [BurstCompile]
        private struct SynchPointToParentJob : IJobForEachWithEntity<Point, Translation, Rotation, Scale>
        {
            public float3 graphParentPosition;
            public quaternion graphParentRotation;
            public float graphParentScale;
            public int parentId;

            public void Execute(Entity entity, int index, ref Point point, ref Translation translation,
                ref Rotation rotation, ref Scale scale)
            {
                if (parentId != point.parentId) return;
                translation.Value = graphParentPosition;
                rotation.Value = graphParentRotation;
                scale.Value = graphParentScale;

            }
        }


        [BurstCompile]
        private struct MovePointJob : IJobForEachWithEntity<PointSelected, Translation>
        {
            public void Execute(Entity entity, int index, ref PointSelected pointSelected, ref Translation translation)
            {
                translation.Value += Time.deltaTime * 10f;
            }
        }

        protected override void OnUpdate()
        {
            // EntityQuery entityQuery = GetEntityQuery(typeof(PointSelected), typeof(Translation));
            // MovePointJob deletePointJob = new MovePointJob();
            // JobHandle jobHandle = JobForEachExtensions.Schedule(deletePointJob, entityQuery);
            // jobHandle.Complete();

            EntityQuery entityQuery = GetEntityQuery(typeof(Point), typeof(Translation), typeof(Rotation), typeof(Scale));
            for (int i = 0; i < graphParentTransforms.Count; i++)
            {
                SynchPointToParentJob toParentJob = new SynchPointToParentJob
                {
                    graphParentPosition = graphParentTransforms[i].position,
                    graphParentRotation = graphParentTransforms[i].rotation,
                    graphParentScale = graphParentTransforms[i].localScale.x,
                    parentId = i,
                };
                JobHandle jobHandle = JobForEachExtensions.Schedule(toParentJob, entityQuery);
                jobHandle.Complete();
            }
        }
    }
}