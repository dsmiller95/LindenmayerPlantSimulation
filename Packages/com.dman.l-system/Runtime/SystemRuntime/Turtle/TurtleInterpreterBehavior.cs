using Cysharp.Threading.Tasks;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.SystemRuntime.VolumetricData;
using Dman.LSystem.SystemRuntime.VolumetricData.Layers;
using Dman.LSystem.UnityObjects;
using Dman.LSystem.UnityObjects.VolumetricResource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class TurtleInterpreterBehavior : MonoBehaviour
    {
        /// <summary>
        /// a set of valid operations, only one operation defintion can be generated per symbol
        /// </summary>
        public List<TurtleOperationSet> operationSets;
        /// <summary>
        /// the begining scale of the turtle's transformation matrix
        /// </summary>
        public Vector3 initialScale = Vector3.one;

        public event Action OnTurtleMeshUpdated;

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

        private void OnDestroy()
        {
            if (System != null)
            {
                System.OnSystemStateUpdated -= OnSystemStateUpdated;
                System.OnSystemObjectUpdated -= OnSystemObjectUpdated;
            }
            if (cancelPending != null)
            {
                cancelPending.Cancel();
                cancelPending.Dispose();
            }
            if (turtle != null)
            {
                turtle.Dispose();
            }
        }

        /// <summary>
        /// Reloads/recompiles all of the turtle configs. call this when one of the <see cref="TurtleOperationSet"/> assets has been modified
        /// </summary>
        public void ReloadConfig()
        {
            if (System != null)
            {
                this.InitializeWithSpecificSystem(System.systemObject);
            }
        }

        /// <summary>
        /// iterate through <paramref name="symbols"/> and assign the generated mesh to the attached meshFilter
        /// </summary>
        /// <param name="symbols"></param>
        public async UniTask InterpretSymbols(DependencyTracker<SymbolString<float>> symbols, CancellationToken token)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("Turtle compilation");

            //var createNewOrgansCommandBuffer = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();


            var meshFilter = GetComponent<MeshFilter>();
            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.materials = turtle.submeshMaterials;
            await turtle.CompileStringToMesh(
                symbols,
                meshFilter.mesh,
                meshFilter.transform.localToWorldMatrix,
                token);
            UnityEngine.Profiling.Profiler.BeginSample("notifying turtle mesh listeners");
            OnTurtleMeshUpdated?.Invoke();
            UnityEngine.Profiling.Profiler.EndSample();
            // TOODO: do this oon startup?

            //UnityEngine.Profiling.Profiler.EndSample();
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
            if (!operationSets.Any(x => x is TurtleMeshOperations))
            {
                // don't create an interpretor if there are no meshes. no point.
                return;
            }
            if (systemObject.compiledSystem == null)
            {
                // compiles so that the custom symbols can be pulled out
                // TODO: extract custom symbols w/o a full system compilation
                systemObject.CompileToCached(silent: true);
            }
            var defaultTurtle = TurtleState.DEFAULT;
            defaultTurtle.transformation = Matrix4x4.Scale(initialScale);

            var volumetricWorld = GameObject.FindObjectOfType<OrganVolumetricWorld>();
            var damageWorld = volumetricWorld?.damageLayer?.effects.OfType<VoxelCapReachedTimestampEffect>().FirstOrDefault();
            turtle = new TurtleInterpretor(
                operationSets,
                defaultTurtle,
                systemObject.linkedFiles,
                systemObject.compiledSystem.customSymbols,
                volumetricWorld,
                damageWorld
                );
        }

        public OrganPositioningTurtleInterpretor GetNewOrganPositionDigestor(Matrix4x4 rootTransformation, IEnumerable<TurtleOperationSet> turtleOperationOverrides = null)
        {
            if (System.systemObject == null)
            {
                return null;
            }
            var turtleOperations = turtleOperationOverrides ?? operationSets;
            if (!turtleOperations.Any(x => x is TurtleMeshOperations))
            {
                // don't create an interpretor if there are no meshes. no point.
                return null;
            }
            if (System.systemObject.compiledSystem == null)
            {
                // compiles so that the custom symbols can be pulled out
                // TODO: extract custom symbols w/o a full system compilation
                System.systemObject.CompileToCached(silent: true);
            }
            // when getting mesh positions, omit operations which have other side effects
            //  or attempt to pull in data from the world
            var filteredOperators = turtleOperations.Where(x =>
                !(x is TurtleInstantiateEntityOperationSet) &&
                !(x is TurtleVolumetricResourceDiffusionOperationSet)).ToList();
            var defaultTurtle = TurtleState.DEFAULT;
            defaultTurtle.transformation = Matrix4x4.Scale(initialScale) * rootTransformation;

            var customSymbols = System.systemObject.compiledSystem.customSymbols;
            customSymbols.hasAutophagy = false;

            var positionProvider = new OrganPositioningTurtleInterpretor(
                filteredOperators,
                defaultTurtle,
                System.systemObject.linkedFiles,
                customSymbols);
            return positionProvider;
        }

        private void OnSystemObjectUpdated()
        {
            if (System != null)
            {
                InitializeWithSpecificSystem(System.systemObject);
            }
        }

        //private CompletableHandle previousTurtle;
        private CancellationTokenSource cancelPending;

        public UniTask turtleCompilationTask { get; private set; }
        public bool IsTurtlePending { get; private set; } = false;

        private void OnSystemStateUpdated()
        {
            if (System != null)
            {
                if (cancelPending != null)
                {
                    cancelPending.Cancel();
                    cancelPending.Dispose();
                }
                cancelPending = new CancellationTokenSource();
                //if (!previousTurtle?.IsComplete() ?? false) previousTurtle.Cancel();

                turtleCompilationTask = CompileTurtle();
                //var mesh = GetComponent<MeshFilter>().mesh;
            }
        }

        private async UniTask CompileTurtle()
        {
            if (this.isActiveAndEnabled)
            {
                IsTurtlePending = true;
                await InterpretSymbols(System.steppingHandle.currentState.currentSymbols, cancelPending.Token);
            }
            IsTurtlePending = false;
        }
    }
}
