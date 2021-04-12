using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.DOTSRenderer
{
    public struct LSystemOrganInstanceCommandComponent : IComponentData
    {
        public Entity parent;
        public Entity template;
    }
    [InternalBufferCapacity(8)]
    public struct LSystemOrganInstanceCommandTransformsBuffer : IBufferElementData
    {
        public float4x4 value;
    }
    [InternalBufferCapacity(8)]
    public struct LSystemOrganDeleteChildrenCommandFlag : IComponentData
    {
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class LSystemMeshMemberUpdateSystem : SystemBase
    {
        private EntityArchetype lsystemOrganArchetype;
        private EntityArchetype lsystemOrganSpawnCommandArchetype;
        private EntityQuery SpawnCommandsQuery;

        EntityCommandBufferSystem createOrganCommandBufferSystem => World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        EntityCommandBufferSystem destroyOrganCommandBufferSystem => World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();


        protected override void OnUpdate()
        {
            Dependency = JobHandle.CombineDependencies(
                HandleOldDestruction(Dependency),
                HandleCreation(Dependency)
            );
        }

        private JobHandle HandleOldDestruction(JobHandle dep)
        {
            var destructionCommandBuffer = destroyOrganCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            dep = Entities
                .WithAll<LSystemOrganDeleteChildrenCommandFlag>()
                .ForEach((int entityInQueryIndex, Entity parentEntity,
                in DynamicBuffer<Child> children) =>
                {
                    foreach (var child in children)
                    {
                        destructionCommandBuffer.DestroyEntity(entityInQueryIndex, child.Value);
                    }
                }).WithBurst().ScheduleParallel(dep);
            dep = Entities
                .WithAll<LSystemOrganDeleteChildrenCommandFlag>()
                .ForEach((int entityInQueryIndex, Entity parentEntity) =>
                {
                    destructionCommandBuffer.RemoveComponent<LSystemOrganDeleteChildrenCommandFlag>(entityInQueryIndex, parentEntity);
                }).WithBurst().ScheduleParallel(dep);
            destroyOrganCommandBufferSystem.AddJobHandleForProducer(dep);
            return dep;
        }

        private JobHandle HandleCreation(JobHandle dep)
        {
            var creationCommandBuffer = createOrganCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            dep = Entities.ForEach((int entityInQueryIndex, Entity commandEntity,
                in LSystemOrganInstanceCommandComponent prefab,
                in DynamicBuffer<LSystemOrganInstanceCommandTransformsBuffer> transforms) =>
            {
                foreach (var transform in transforms)
                {
                    var spawnedEntity = creationCommandBuffer.Instantiate(entityInQueryIndex, prefab.template);
                    creationCommandBuffer.RemoveComponent<LSystemOrganTemplateComponentFlag>(entityInQueryIndex, spawnedEntity);
                    creationCommandBuffer.SetComponent(entityInQueryIndex, spawnedEntity, new LocalToParent
                    {
                        Value = transform.value
                    });
                    creationCommandBuffer.SetComponent(entityInQueryIndex, spawnedEntity, new Parent
                    {
                        Value = prefab.parent
                    });
                }
                creationCommandBuffer.DestroyEntity(entityInQueryIndex, commandEntity);
            }).WithBurst().ScheduleParallel(Dependency);
            createOrganCommandBufferSystem.AddJobHandleForProducer(dep);
            return dep;
        }


        protected override void OnCreate()
        {
            lsystemOrganArchetype = EntityManager.CreateArchetype(
                typeof(LSystemOrganComponent),
                typeof(LocalToWorld),
                typeof(LocalToParent),
                typeof(Parent),
                typeof(PreviousParent),
                typeof(RenderMesh),
                typeof(RenderBounds)
                );
            lsystemOrganSpawnCommandArchetype = EntityManager.CreateArchetype(
                typeof(LSystemOrganInstanceCommandComponent),
                typeof(LSystemOrganInstanceCommandTransformsBuffer)
                );
        }
        public void ClearOldOrgans(
            EntityCommandBuffer commandBuffer,
            Entity parent)
        {
            commandBuffer.AddComponent<LSystemOrganDeleteChildrenCommandFlag>(parent);
        }
        public void SpawnOrgans(
            EntityCommandBuffer commandBuffer,
            TurtleEntityPrototypeOrganTemplate entityTemplate,
            IEnumerable<Matrix4x4> transforms,
            Entity parent)
        {
            var prototype = entityTemplate.prototype;

            var entity = commandBuffer.CreateEntity(lsystemOrganSpawnCommandArchetype);
            commandBuffer.SetComponent(entity, new LSystemOrganInstanceCommandComponent
            {
                parent = parent,
                template = prototype
            });
            var dynamicBuffer = commandBuffer.SetBuffer<LSystemOrganInstanceCommandTransformsBuffer>(entity);
            foreach (var transform in transforms)
            {
                dynamicBuffer.Add(new LSystemOrganInstanceCommandTransformsBuffer
                {
                    value = transform
                });
            }
        }

    }
}
