using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System;
using System.Collections.Generic;
using Dman.LSystem.SystemRuntime.NativeCollections;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Dman.LSystem.SystemRuntime.DOTSRenderer;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleOrganSpawningCompletable : ICompletable<TurtleCompletionResult>
    {
        public JobHandle currentJobHandle { get; private set; }


        //private NativeArray<MeshVertexLayout> vertexData;
        //private NativeArray<uint> triangleIndexes;

        private Mesh.MeshDataArray meshDataArray;

        private Mesh targetMesh;
        private TurtleMeshAllocationCounter resultMeshSize;


        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct MeshVertexLayout
        {
            public Vector3 pos;
            public Vector3 normal;
            public Vector2 uv;
        }

        public TurtleOrganSpawningCompletable(
            Mesh targetMesh,
            TurtleMeshAllocationCounter resultMeshSize,
            DependencyTracker<NativeTurtleData> nativeData,
            NativeList<TurtleOrganInstance> organInstances)
        {
            this.targetMesh = targetMesh;
            this.resultMeshSize = resultMeshSize;
            UnityEngine.Profiling.Profiler.BeginSample("allocating mesh data");

            meshDataArray = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArray[0];
            meshData.SetVertexBufferParams(resultMeshSize.totalVertexes,
                new VertexAttributeDescriptor(VertexAttribute.Position),
                new VertexAttributeDescriptor(VertexAttribute.Normal),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
            );

            meshData.SetIndexBufferParams(resultMeshSize.totalTriangleIndexes, IndexFormat.UInt32);

            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("configuring job");
            var turtleEntitySpawnJob = new TurtleEntitySpawningJob
            {
                vertexData = nativeData.Data.vertexData,
                triangleData = nativeData.Data.triangleData,
                organData = nativeData.Data.allOrganData,

                organInstances = organInstances,

                targetMesh = meshData
            };
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("scheduling job");
            currentJobHandle = turtleEntitySpawnJob.Schedule();
            nativeData.RegisterDependencyOnData(currentJobHandle);

            currentJobHandle = organInstances.Dispose(currentJobHandle);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public ICompletable<TurtleCompletionResult> StepNext()
        {
            currentJobHandle.Complete();

            UnityEngine.Profiling.Profiler.BeginSample("applying mesh data");
            var meshData = meshDataArray[0];

            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, resultMeshSize.totalTriangleIndexes));

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, targetMesh, MeshUpdateFlags.Default);
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("recalculating bounds");
            targetMesh.RecalculateBounds();
            UnityEngine.Profiling.Profiler.EndSample();

            return new CompleteCompletable<TurtleCompletionResult>(new TurtleCompletionResult());
        }


        [BurstCompile]
        struct TurtleEntitySpawningJob : IJob
        {
            [ReadOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<NativeVertexDatum> vertexData;
            [ReadOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<int> triangleData;
            [ReadOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<TurtleOrganTemplate.Blittable> organData;

            [ReadOnly]
            public NativeList<TurtleOrganInstance> organInstances;

            [NativeDisableParallelForRestriction]
            public Mesh.MeshData targetMesh;

            public void Execute()
            {
                var vertexTargetData = targetMesh.GetVertexData<MeshVertexLayout>();
                var triangleIndexes = targetMesh.GetIndexData<uint>();
                for (int index = 0; index < organInstances.Length; index++)
                {
                    var organInstance = organInstances[index];
                    var organTemplate = organData[organInstance.organIndexInAllOrgans];
                    var matrixTransform = (Matrix4x4)organInstance.organTransform;

                    for (int i = 0; i < organTemplate.vertexes.length; i++)
                    {
                        var targetVertexIndex = organInstance.vertexMemorySpace.index + i;
                        var sourceVertexData = vertexData[organTemplate.vertexes.index + i];
                        vertexTargetData[targetVertexIndex] = new MeshVertexLayout
                        {
                            pos = matrixTransform.MultiplyPoint(sourceVertexData.vertex),
                            normal = matrixTransform.MultiplyVector(sourceVertexData.normal),
                            uv = sourceVertexData.uv
                        };
                    }

                    for (int i = 0; i < organTemplate.trianges.length; i++)
                    {
                        triangleIndexes[i + organInstance.trianglesMemorySpace.index] = (uint)
                            (triangleData[i + organTemplate.trianges.index]
                            + organInstance.vertexMemorySpace.index); // offset the triangle indexes by the current index in vertex mem space
                    }
                }
            }
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(inputDeps, currentJobHandle);
        }

        public void Dispose()
        {
            currentJobHandle.Complete();
        }

        public TurtleCompletionResult GetData()
        {
            return default;
        }

        public string GetError()
        {
            return null;
        }

        public bool HasErrored()
        {
            return false;
        }

        public bool IsComplete()
        {
            return false;
        }


    }
}
