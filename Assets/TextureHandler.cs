using Unity.Entities;
using UnityEngine;

namespace DefaultNamespace
{
    public class TextureHandler : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        private EntityManager entityManager;
        private int frameCount;

        public Texture2D colorTextureMap;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityManager = World.EntityManager;
            ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }


        protected override void OnUpdate()
        {
            if (frameCount >= 50)
            {
                frameCount = 0;
                EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();
                Entities.WithAll<SelectedPointComponent>().WithoutBurst().ForEach((Entity e, int entityInQueryIndex, ref Point p, ref SelectedPointComponent sp) =>
                {
                    // p.selected = false;
                    ecb.RemoveComponent<SelectedPointComponent>(e);
                    // Debug.Log(p.xindex);
                    // entityManager.RemoveComponent<SelectedPointComponent>(e);
                    // entityManager.RemoveComponent<SelectedPointComponent>(e);
                    colorTextureMap.SetPixel(p.xindex, p.yindex, Color.red);
                }).Run();
                ecbSystem.AddJobHandleForProducer(Dependency);
                colorTextureMap.Apply();
            }

            frameCount++;
        }
    }
}