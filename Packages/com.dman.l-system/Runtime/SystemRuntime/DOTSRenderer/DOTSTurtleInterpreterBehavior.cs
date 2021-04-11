using Dman.LSystem.UnityObjects;
using System.Linq;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.DOTSRenderer
{
    public class DOTSTurtleInterpreterBehavior : MonoBehaviour
    {
        /// <summary>
        /// a set of valid operations, only one operation defintion can be generated per symbol
        /// </summary>
        public TurtleOperationSet<TurtleState>[] operationSets;
        /// <summary>
        /// the begining scale of the turtle's transformation matrix
        /// </summary>
        public Vector3 initialScale = Vector3.one;
        /// <summary>
        /// a character which will increment the index of the current target submesh being copied to
        /// </summary>
        public char submeshIndexIncrementor = '`';

        private TurtleInterpretor<TurtleState> turtle;
        private LSystemBehavior System => GetComponent<LSystemBehavior>();

        private Entity lSystemEntity;

        /// <summary>
        /// iterate through <paramref name="symbols"/> and assign the generated mesh to the attached meshFilter
        /// </summary>
        /// <param name="symbols"></param>
        public void InterpretSymbols(SymbolString<double> symbols)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Turtle compilation");
            var turtleResults = turtle.CompileStringToTransformsWithMeshIds(symbols);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("entity buffer filling");
            var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (!manager.Exists(lSystemEntity))
            {
                this.lSystemEntity = manager.CreateEntity();
                manager.AddComponentData(lSystemEntity, new LSystemComponent());
                manager.AddComponentData(lSystemEntity, new Translation
                {
                    Value = this.transform.position
                });
                manager.AddComponentData(lSystemEntity, new Rotation
                {
                    Value = this.transform.rotation
                });
                manager.AddComponentData(lSystemEntity, new NonUniformScale
                {
                    Value = this.transform.localScale
                });
                manager.AddComponent<LocalToWorld>(lSystemEntity);
            }

            var organSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LSystemMeshMemberUpdateSystem>();
            var commandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            var buffer = commandBufferSystem.CreateCommandBuffer();

            organSystem.ClearOldOrgans(buffer, lSystemEntity);

            foreach (var meshInstance in turtleResults.GetTurtleMeshInstances())
            {
                var meshTemplate = turtleResults.GetMeshTemplate(meshInstance.meshIndex);
                organSystem.SpawnOrgan(
                    buffer,
                    meshTemplate,
                    meshInstance,
                    lSystemEntity);
            }
            UnityEngine.Profiling.Profiler.EndSample();

            // Ref is unecessary in the backing API here, which is why we're not re-assigning back from it here
            // TODO: ingest as meshrenderers
            //turtle.CompileStringToMesh(symbols, ref targetMesh, meshRenderer.materials.Length);
        }

        private void Awake()
        {
            var operatorDictionary = operationSets.SelectMany(x => x.GetOperators()).ToDictionary(x => (int)x.TargetSymbol);

            turtle = new TurtleInterpretor<TurtleState>(
                operatorDictionary,
                new TurtleState
                {
                    transformation = Matrix4x4.Scale(initialScale),
                    thickness = 1f
                });
            turtle.submeshIndexIncrementChar = submeshIndexIncrementor;

            if (System != null)
            {
                System.OnSystemStateUpdated += OnSystemStateUpdated;
            }
        }

        private void OnDestroy()
        {
            if (System != null)
            {
                System.OnSystemStateUpdated -= OnSystemStateUpdated;
            }
        }

        private void OnSystemStateUpdated()
        {
            if (System != null)
            {
                this.InterpretSymbols(System.CurrentState);
            }
        }
    }
}
