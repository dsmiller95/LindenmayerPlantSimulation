using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.SystemRuntime.Turtle;
using Dman.LSystem.UnityObjects;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.DOTSRenderer
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class TurtleInterpreterBehavior : MonoBehaviour
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


        private void Awake()
        {
            GetComponent<MeshFilter>().mesh = new Mesh();
            if (System != null)
            {
                InitializeWithSpecificSystem(System.systemObject);
                System.OnSystemStateUpdated += OnSystemStateUpdated;
                System.OnSystemObjectUpdated += OnSystemObjectUpdated;
            }
        }

        private void Update()
        {
        }

        private void OnDestroy()
        {
            if (System != null)
            {
                System.OnSystemStateUpdated -= OnSystemStateUpdated;
                System.OnSystemObjectUpdated -= OnSystemObjectUpdated;
            }
            if (turtle != null)
            {
                turtle.Dispose();
            }
        }

        /// <summary>
        /// iterate through <paramref name="symbols"/> and assign the generated mesh to the attached meshFilter
        /// </summary>
        /// <param name="symbols"></param>
        public ICompletable<TurtleCompletionResult> InterpretSymbols(DependencyTracker<SymbolString<float>> symbols)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Turtle compilation");

            //var createNewOrgansCommandBuffer = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            var meshFilter = GetComponent<MeshFilter>();
            var meshRenderer = GetComponent<MeshRenderer>();
            var dep = turtle.CompileStringToTransformsWithMeshIds(
                symbols,
                meshFilter.mesh);
            // TOODO: do this oon startup?
            meshRenderer.materials = turtle.submeshMaterials;

            UnityEngine.Profiling.Profiler.EndSample();
            return dep;
        }

        public void InitializeWithSpecificSystem(LSystemObject systemObject)
        {
            if (turtle != null)
            {
                turtle.Dispose();
            }
            if (systemObject == null)
            {
                return;
            }
            if (systemObject.compiledSystem == null)
            {
                // compiles so that the custom symbols can be pulled out
                // TODO: extract custom symbols w/o a full system compilation
                systemObject.CompileToCached(silent: true);
            }
            turtle = new TurtleInterpretor(
                operationSets,
                new TurtleState
                {
                    transformation = Matrix4x4.Scale(initialScale),
                    thickness = 1f,
                    submeshIndex = 0,
                    organIdentity = new UIntFloatColor32(0)
                },
                systemObject.linkedFiles,
                systemObject.compiledSystem.customSymbols);
            turtle.submeshIndexIncrementChar = submeshIndexIncrementor;
        }

        private void OnSystemObjectUpdated()
        {
            if (System != null)
            {
                InitializeWithSpecificSystem(System.systemObject);
            }
        }

        private CompletableHandle previousTurtle;

        private void OnSystemStateUpdated()
        {
            if (System != null)
            {
                if (!previousTurtle?.IsComplete() ?? false) previousTurtle.Cancel();
                var completable = InterpretSymbols(System.steppingHandle.currentState.currentSymbols);
                previousTurtle = CompletableExecutor.Instance.RegisterCompletable(completable);

                //var mesh = GetComponent<MeshFilter>().mesh;
            }
        }
    }
}
