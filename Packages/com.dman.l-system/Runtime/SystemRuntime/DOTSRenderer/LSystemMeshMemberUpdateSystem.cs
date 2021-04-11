using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.DOTSRenderer
{
    public class LSystemMeshMemberUpdateSystem : SystemBase
    {
        private EntityArchetype lsystemOrganArchetype;
        private EntityQuery SpawnCommandsQuery;

        EntityCommandBufferSystem commandBufferSystem => World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

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
        }
        public void ClearOldOrgans(
            EntityCommandBuffer commandbuffer,
            Entity parent)
        {
            commandbuffer.DestroyEntitiesForEntityQuery(GetEntityQuery(
                typeof(LSystemOrganComponent)));
        }
        public void SpawnOrgan(
            EntityCommandBuffer commandbuffer,
            TurtleMeshTemplate meshTemplate,
            TurtleMeshInstance instance,
            Entity parent)
        {
            var entity = commandbuffer.CreateEntity(lsystemOrganArchetype);
            commandbuffer.SetComponent(entity, new LocalToParent
            {
                Value = instance.transformation
            });
            commandbuffer.SetComponent(entity, new Parent
            {
                Value = parent
            });
            var mesh = meshTemplate.draft.ToMesh();

            var desc = new RenderMeshDescription(
                mesh,
                meshTemplate.material,
                shadowCastingMode: UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows: false);
            RenderMeshUtility.AddComponents(entity, commandbuffer, desc);
        }

        protected override void OnUpdate()
        {

        }
    }
}
