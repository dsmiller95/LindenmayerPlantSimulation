using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.SystemRuntime.Turtle;
using Dman.LSystem.UnityObjects;
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


        private void Update()
        {
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
            GetComponent<MeshFilter>().mesh = new Mesh();
        }

        private void OnDestroy()
        {
            if (System != null)
            {
                System.OnSystemStateUpdated -= OnSystemStateUpdated;
            }
            if (turtle != null)
            {
                turtle.Dispose();
            }
        }

        private void OnSystemStateUpdated()
        {
            if (System != null)
            {
                var completable = InterpretSymbols(System.steppingHandle.currentState.currentSymbols);
                CompletableExecutor.Instance.RegisterCompletable(completable);

                //var mesh = GetComponent<MeshFilter>().mesh;
            }
        }
    }
}
