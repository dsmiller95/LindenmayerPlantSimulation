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
    public class LSystemBindingSystem : SystemBase
    {
        private EntityCommandBufferSystem commandBufferSystem;
        private EntityQuery boundComponentQuery;
        private EntityQuery bindingQuery;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            random = new Unity.Mathematics.Random(1928437918);
        }

        struct UniqueStaticId : System.IEquatable<UniqueStaticId>
        {
            public float plantId;
            public float organId;

            public bool Equals(UniqueStaticId other)
            {
                return other.plantId == plantId && other.organId == organId;
            }

            public override bool Equals(object obj)
            {
                if(obj is UniqueStaticId other)
                {
                    return this.Equals(other);
                }
                return false;
            }

            public override int GetHashCode()
            {
                var plantIdHash = plantId.GetHashCode();
                var plantHashRot = (plantIdHash << 16) | (plantIdHash >> 16);
                return plantHashRot ^ organId.GetHashCode();
            }
        }

        protected override void OnUpdate()
        {
            var deltaTime = Time.DeltaTime;
            var time = (float)Time.ElapsedTime;
            random.NextFloat();
            var rand = random;

            var totalBoundEntities = boundComponentQuery.CalculateEntityCount();
            var boundEntitiesByBoundComponent = new NativeHashMap<UniqueStaticId, Entity>(totalBoundEntities, Allocator.TempJob);
            var boundEntitiesWriter = boundEntitiesByBoundComponent.AsParallelWriter();

            var ecbParallel = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Entities
                .ForEach((Entity entity, int entityInQueryIndex, ref LSystemBoundComponent boundComponent) =>
                {
                    if (boundComponent.ShouldExpire(time))
                    {
                        ecbParallel.DestroyEntity(entityInQueryIndex, entity);
                    }
                    else
                    {
                        var uniqueId = new UniqueStaticId
                        {
                            plantId = boundComponent.plantId,
                            organId = boundComponent.organId
                        };

                        //if (boundEntitiesByBoundComponent.ContainsKey(uniqueId))
                        //{
                        //    Debug.LogError("duplicate unique Id detected. l system unique Id not truly unique");
                        //}
                        //boundEntitiesByBoundComponent.Add(uniqueId, entity);
                        boundEntitiesWriter.TryAdd(uniqueId, entity);
                    }
                })
                .WithStoreEntityQueryInField(ref boundComponentQuery)
                .ScheduleParallel();


            var ecb = commandBufferSystem.CreateCommandBuffer();
            Entities
                .ForEach((Entity entity, ref Translation position, ref Rotation rot, ref NonUniformScale scale, ref LSystemBindingComponent bindingComponent, ref DynamicBuffer<TurtleSpawnedParameters> spawnParameters) =>
            {
                var uniqueId = new UniqueStaticId
                {
                    organId = spawnParameters[1].parameterValue,
                    plantId = spawnParameters[2].parameterValue
                };

                LSystemBoundComponent oldBoundComponent;
                if (boundEntitiesByBoundComponent.TryGetValue(uniqueId, out var boundEntity))
                {
                    oldBoundComponent = GetComponent<LSystemBoundComponent>(boundEntity);
                } else
                {
                    boundEntity = ecb.Instantiate(bindingComponent.boundPrefab);
                    oldBoundComponent = GetComponent<LSystemBoundComponent>(bindingComponent.boundPrefab);
                    oldBoundComponent.organId = uniqueId.organId;
                    oldBoundComponent.plantId = uniqueId.plantId;

                    ecb.SetComponent(boundEntity, rot);
                    ecb.SetComponent(boundEntity, scale);
                }
                ecb.SetComponent(boundEntity, position);

                oldBoundComponent.resourceAmount += spawnParameters[0].parameterValue;
                oldBoundComponent.lastResourceTransferTime = time;
                // can do this with a ECB because the nature of the system should avoid two binding components matching to one bound entity, based on the following assumptions:
                //  1. this system updates at least as frequently as the l-systems
                //  2. one or multiple l-systems will never produce multiple binding components with the exact same organId and plantId
                ecb.SetComponent(boundEntity, oldBoundComponent);
                ecb.DestroyEntity(entity);
            })
                .WithStoreEntityQueryInField(ref bindingQuery)
                .Schedule();

            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
            boundEntitiesByBoundComponent.Dispose(this.Dependency);
        }
    }
}