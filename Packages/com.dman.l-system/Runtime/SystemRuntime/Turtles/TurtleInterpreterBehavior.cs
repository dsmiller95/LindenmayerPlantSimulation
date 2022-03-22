﻿using Cysharp.Threading.Tasks;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.SystemRuntime.Turtle;
using Dman.LSystem.SystemRuntime.VolumetricData;
using Dman.LSystem.SystemRuntime.VolumetricData.Layers;
using Dman.LSystem.UnityObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        public List<TurtleOperationSet> operationSets;
        /// <summary>
        /// the begining scale of the turtle's transformation matrix
        /// </summary>
        public Vector3 initialScale = Vector3.one;

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
        public async UniTask InterpretSymbols(DependencyTracker<SymbolString<float>> symbols, CancellationToken token)
        {
            //UnityEngine.Profiling.Profiler.BeginSample("Turtle compilation");

            //var createNewOrgansCommandBuffer = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            var meshFilter = GetComponent<MeshFilter>();
            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.materials = turtle.submeshMaterials;
            await turtle.CompileStringToTransformsWithMeshIds(
                symbols,
                meshFilter.mesh,
                meshFilter.transform.localToWorldMatrix,
                token);
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
            var volumetricWorld = GameObject.FindObjectOfType<OrganVolumetricWorld>();
            var damageWorld = volumetricWorld?.damageLayer?.effects.OfType<VoxelCapReachedTimestampEffect>().FirstOrDefault();
            turtle = new TurtleInterpretor(
                operationSets,
                new TurtleState
                {
                    transformation = Matrix4x4.Scale(initialScale),
                    thickness = 1f,
                    organIdentity = new UIntFloatColor32(0)
                },
                systemObject.linkedFiles,
                systemObject.compiledSystem.customSymbols,
                volumetricWorld,
                damageWorld
                );
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

        private void OnSystemStateUpdated()
        {
            if (System != null)
            {
                if(cancelPending != null)
                {
                    cancelPending.Cancel();
                    cancelPending.Dispose();
                }
                cancelPending = new CancellationTokenSource();
                //if (!previousTurtle?.IsComplete() ?? false) previousTurtle.Cancel();

                CompileTurtle().Forget();

                //var mesh = GetComponent<MeshFilter>().mesh;
            }
        }

        private async UniTask CompileTurtle()
        {
            await InterpretSymbols(System.steppingHandle.currentState.currentSymbols, cancelPending.Token);
            //previousTurtle = CompletableExecutor.Instance.RegisterCompletable(await completable);
        }
    }
}
