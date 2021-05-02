using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

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

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class LSystemMeshMemberUpdateSystem : SystemBase
    {
        private EntityArchetype lsystemOrganArchetype;
        private EntityArchetype lsystemOrganSpawnCommandArchetype;
        private EntityQuery SpawnCommandsQuery;

        EntityCommandBufferSystem destroyOrganCommandBufferSystem => World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();


        protected override void OnUpdate()
        {
            Dependency = HandleOldDestruction(Dependency);
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
    }
}
