using Dman.LSystem.SystemRuntime;
using Dman.LSystem.SystemRuntime.DOTSRenderer;
using Dman.LSystem.SystemRuntime.NativeCollections;
using ProceduralToolkit;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dman.LSystem
{
    public interface ITurtleOperator<T>
    {
        char TargetSymbol { get; }
        T Operate(
            T initialState,
            NativeArray<float> parameters,
            JaggedIndexing parameterIndexing,
            TurtleMeshInstanceTracker<TurtleEntityPrototypeOrganTemplate> targetDraft);
    }

    public class TurtleEntityPrototypeOrganTemplate
    {
        public Entity prototype;
    }

    public interface ITurtleOrganTemplate<T>
    {
        T GetOrganTemplateValue();
    }

    public class TurtleEntityOrganTemplate : ITurtleOrganTemplate<TurtleEntityPrototypeOrganTemplate>
    {
        public TurtleEntityPrototypeOrganTemplate templateValue;

        public TurtleEntityOrganTemplate(MeshDraft draft, Material material)
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
            templateValue = new TurtleEntityPrototypeOrganTemplate
            {
                prototype = prototype
            };
        }

        public TurtleEntityPrototypeOrganTemplate GetOrganTemplateValue()
        {
            return templateValue;
        }
    }

    public class TurtleMeshInstanceTracker<T>
    {
        private IList<ITurtleOrganTemplate<T>> meshTemplates = new List<ITurtleOrganTemplate<T>>();

        private IDictionary<ITurtleOrganTemplate<T>, int> existingTemplates = new Dictionary<ITurtleOrganTemplate<T>, int>();
        private IList<IList<Matrix4x4>> meshTransformsByTemplate = new List<IList<Matrix4x4>>();

        public void AddMeshInstance(ITurtleOrganTemplate<T> template, Matrix4x4 transformation)
        {
            if(!existingTemplates.TryGetValue(template, out int index))
            {
                meshTemplates.Add(template);
                meshTransformsByTemplate.Add(new List<Matrix4x4>());
                index = meshTemplates.Count - 1;
                existingTemplates[template] = index;
            }
            meshTransformsByTemplate[index].Add(transformation);
        }

        public int TemplateTypeCount => meshTemplates.Count;

        public ITurtleOrganTemplate<T> GetMeshTemplate(int templateId)
        {
            return meshTemplates[templateId];
        }
        public IEnumerable<Matrix4x4> GetTurtleMeshTransformsByTemplateType(int templateId)
        {
            return meshTransformsByTemplate[templateId];
        }
    }
}
