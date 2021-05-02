using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.Turtle;
using Dman.LSystem.UnityObjects;
using System.Linq;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.DOTSRenderer
{
    public class DOTSTurtleInterpreterBehavior : MonoBehaviour
    {
        /// <summary>
        /// a set of valid operations, only one operation defintion can be generated per symbol
        /// </summary>
        public TurtleOperationSet[] operationSets;
        /// <summary>
        /// the begining scale of the turtle's transformation matrix
        /// </summary>
        public Vector3 initialScale = Vector3.one;
        /// <summary>
        /// a character which will increment the index of the current target submesh being copied to
        /// </summary>
        public char submeshIndexIncrementor = '`';

        private TurtleInterpretor turtle;
        private LSystemBehavior System => GetComponent<LSystemBehavior>();

        private Entity lSystemEntity;

        private void Update()
        {
            var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (manager.Exists(lSystemEntity))
            {
                manager.SetComponentData(lSystemEntity, new Rotation
                {
                    Value = this.transform.rotation
                });
            }
        }

        /// <summary>
        /// iterate through <paramref name="symbols"/> and assign the generated mesh to the attached meshFilter
        /// </summary>
        /// <param name="symbols"></param>
        public void InterpretSymbols(SymbolString<float> symbols, LSystemBehavior behaviorHandler)
        {
            var dep = this.InterpretSymbols(symbols);
            behaviorHandler.steppingHandle.RegisterDependencyForSymbols(dep);
            return;
        }
        public JobHandle InterpretSymbols(SymbolString<float> symbols)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Turtle compilation");
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

            var clearOldOrgansCommandBuffer = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            var createNewOrgansCommandBuffer = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            var deleteBuffer = clearOldOrgansCommandBuffer.CreateCommandBuffer();

            organSystem.ClearOldOrgans(deleteBuffer, lSystemEntity);

            var dep = turtle.CompileStringToTransformsWithMeshIds(
                symbols,
                createNewOrgansCommandBuffer,
                lSystemEntity);

            UnityEngine.Profiling.Profiler.EndSample();
            return dep;
        }

        private void Awake()
        {
            turtle = new TurtleInterpretor(
                operationSets,
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
            if(turtle != null)
            {
                turtle.Dispose();
            }
        }

        private void OnSystemStateUpdated()
        {
            if (System != null)
            {
                this.InterpretSymbols(System.CurrentState, System);
            }
        }
    }
}
