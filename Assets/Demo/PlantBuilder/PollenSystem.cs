using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Demo.PlantBuilder
{
    public class PollenSystem : SystemBase
    {
        private EntityCommandBufferSystem commandBufferSystem;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            random = new Unity.Mathematics.Random(1928437918);
        }

        protected override void OnUpdate()
        {
            var time = Time.DeltaTime;
            var ecb = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            random.NextFloat();
            var rand = random;
            Entities
                .ForEach((Entity entity, int entityInQueryIndex, ref Translation position, ref Rotation rot, ref NonUniformScale scale, ref PollenComponent poll) =>
            {
                Quaternion rotation = rot.Value;
                rotation *= Quaternion.Euler(
                    rand.NextFloat(-1, 1) * time * 360,
                    rand.NextFloat(-1, 1) * time * 360,
                    rand.NextFloat(-1, 1) * time * 360);
                position.Value += (float3)(rotation * new float3(time, 0, 0));
                rot.Value = rotation;
                poll.lifespanRemaining -= time;
                if (poll.lifespanRemaining <= 0)
                {
                    ecb.DestroyEntity(entityInQueryIndex, entity);
                }
                else
                {
                    var newScale = math.pow(poll.lifespanRemaining / poll.totalLifespan, 1f / 3);
                    scale.Value = new float3(newScale, newScale, newScale);
                }
            }).ScheduleParallel();

            commandBufferSystem.AddJobHandleForProducer(this.Dependency);

        }
    }
}