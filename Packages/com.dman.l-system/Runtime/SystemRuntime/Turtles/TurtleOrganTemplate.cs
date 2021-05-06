using Dman.LSystem.SystemRuntime.DOTSRenderer;
using Dman.LSystem.SystemRuntime.NativeCollections;
using ProceduralToolkit;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleOrganTemplate: ITurtleNativeDataWritable
    {
        public MeshDraft draft;
        public Material material;
        public Matrix4x4 transform;

        public TurtleOrganTemplate(
            MeshDraft draft,
            Material material,
            Matrix4x4 transform)
        {
            this.draft = draft;
            this.material = material;
            this.transform = transform;
        }

        public TurtleDataRequirements DataReqs => new TurtleDataRequirements
        {
            organTemplateSize = 1,
            vertextDataSize = 0
        };

        public void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            var mesh = draft.ToMesh();

            // Create a RenderMeshDescription using the convenience constructor
            // with named parameters.
            var desc = new RenderMeshDescription(
                mesh,
                material,
                shadowCastingMode: ShadowCastingMode.Off,
                receiveShadows: false);

            // Create empty base entity
            var prototype = entityManager.CreateEntity();

            entityManager.AddComponents(prototype, new ComponentTypes(
                typeof(LSystemOrganComponent),
                typeof(LSystemOrganTemplateComponentFlag)
            ));
            entityManager.AddComponents(prototype, new ComponentTypes(
                typeof(LocalToWorld),
                typeof(LocalToParent),
                typeof(Parent),
                typeof(PreviousParent)
            ));
            // Call AddComponents to populate base entity with the components required
            // by Hybrid Renderer
            RenderMeshUtility.AddComponents(
                prototype,
                entityManager,
                desc);
            var blittable = new Blittable
            {
                prototype = prototype,
                organMatrixTransform = transform
            };

            nativeData.allOrganData[writer.indexInOrganTemplates] = blittable;
            writer.indexInOrganTemplates++;
        }

        public struct Blittable
        {
            public Entity prototype;
            public float4x4 organMatrixTransform;
        }
    }
}
