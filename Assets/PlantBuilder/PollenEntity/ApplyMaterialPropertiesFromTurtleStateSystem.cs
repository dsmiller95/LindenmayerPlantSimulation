using Dman.LSystem.UnityObjects;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Demo.PlantBuilder
{
    public class ApplyMaterialPropertiesFromTurtleStateSystem : SystemBase
    {
        private EntityCommandBufferSystem commandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var ecb = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
                .WithChangeFilter<TurtleSpawnedState>()
                .ForEach((
                    Entity entity, 
                    int entityInQueryIndex, 
                    ref ColorIndexOverride colorIndex,
                    ref VariegationOverride variegation,
                    in TurtleSpawnedState spawnedState) =>
            {
                colorIndex.Value = spawnedState.customData.x / 255f;
                variegation.Value = spawnedState.customData.y / 255f;

                ecb.RemoveComponent<TurtleSpawnedState>(entityInQueryIndex, entity);
            }).ScheduleParallel();
            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}