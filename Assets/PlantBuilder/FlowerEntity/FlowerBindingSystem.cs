using Dman.LSystem.UnityObjects;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Demo.PlantBuilder
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class FlowerBindingSystem : SystemBase
    {
        protected EntityCommandBufferSystem commandBufferSystem;
        protected EntityQuery boundComponentQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var currentTime = (float)Time.ElapsedTime;

            var boundEntitiesByBoundComponent = new NativeHashMap<LSystemBoundComponent, Entity>(boundComponentQuery.CalculateEntityCount(), Allocator.TempJob);
            var boundEntitiesWriter = boundEntitiesByBoundComponent.AsParallelWriter();

            var ecbParallel = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
                .ForEach((Entity entity, int entityInQueryIndex, ref LSystemBoundComponent boundComponent, ref FlowerResourceAmountComponent flowerAmounts) =>
                {
                    if (flowerAmounts.ShouldExpire(currentTime))
                    {
                        ecbParallel.DestroyEntity(entityInQueryIndex, entity);
                        return;
                    }
                    boundEntitiesWriter.TryAdd(boundComponent, entity);
                })
                .WithStoreEntityQueryInField(ref boundComponentQuery)
                .ScheduleParallel();

            //var ecb = commandBufferSystem.CreateCommandBuffer();
            Entities
                .WithReadOnly(boundEntitiesByBoundComponent)
                .ForEach((Entity entity,
                    int entityInQueryIndex,
                    in Translation position,
                    in Rotation rot,
                    in NonUniformScale scale,
                    in LSystemBindingComponent bindingComponent,
                    in DynamicBuffer<TurtleSpawnedParameters> spawnParameters) =>
                {
                    var boundOrganId = new LSystemBoundComponent
                    {
                        organId = spawnParameters[1].parameterValue,
                        plantId = spawnParameters[2].parameterValue
                    };

                    FlowerResourceAmountComponent flowerResource;
                    if (boundEntitiesByBoundComponent.TryGetValue(boundOrganId, out var boundEntity))
                    {
                        flowerResource = GetComponent<FlowerResourceAmountComponent>(boundEntity);
                    }
                    else
                    {
                        boundEntity = ecbParallel.Instantiate(entityInQueryIndex, bindingComponent.boundPrefab);
                        ecbParallel.SetComponent(entityInQueryIndex, boundEntity, boundOrganId);
                        ecbParallel.SetComponent(entityInQueryIndex, boundEntity, rot);
                        ecbParallel.SetComponent(entityInQueryIndex, boundEntity, scale);

                        flowerResource = GetComponent<FlowerResourceAmountComponent>(bindingComponent.boundPrefab);
                    }
                    ecbParallel.SetComponent(entityInQueryIndex, boundEntity, position);

                    // make updates to the flower based on bind command parameters
                    flowerResource.resourceAmount += spawnParameters[0].parameterValue;
                    flowerResource.lastResourceTransferTime = currentTime;
                    ecbParallel.SetComponent(entityInQueryIndex, boundEntity, flowerResource);

                    // can do this with a ECB because the nature of the system should avoid two binding components matching to one bound entity, based on the following assumptions:
                    //  1. this system updates at least as frequently as the l-systems
                    //  2. one or multiple l-systems will never produce multiple binding components with the exact same organId and plantId
                    ecbParallel.DestroyEntity(entityInQueryIndex, entity);
                })
                .ScheduleParallel();

            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
            boundEntitiesByBoundComponent.Dispose(this.Dependency);
        }
    }
}