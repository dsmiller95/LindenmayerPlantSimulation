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

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleOrganSpawningCompletable : ICompletable<TurtleCompletionResult>
    {
        public JobHandle currentJobHandle { get; private set; }


        private NativeArray<float3> vertexes;
        private NativeArray<float3> normals;
        private NativeArray<float4> tangents;
        private NativeArray<int> triangleIndexes;

        private Mesh targetMesh;

        public TurtleOrganSpawningCompletable(
            Mesh targetMesh,
            TurtleMeshAllocationCounter resultMeshSize,
            DependencyTracker<NativeTurtleData> nativeData,
            NativeList<TurtleOrganInstance> organInstances)
        {
            this.targetMesh = targetMesh;
            UnityEngine.Profiling.Profiler.BeginSample("allocating mesh data");
            vertexes = new NativeArray<float3>(resultMeshSize.totalVertexes, Allocator.Persistent);
            normals = new NativeArray<float3>(resultMeshSize.totalVertexes, Allocator.Persistent);
            tangents = new NativeArray<float4>(resultMeshSize.totalVertexes, Allocator.Persistent);
            triangleIndexes = new NativeArray<int>(resultMeshSize.totalTriangles, Allocator.Persistent);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("configuring job");
            var turtleEntitySpawnJob = new TurtleEntitySpawningJob
            {
                vertexData = nativeData.Data.vertexData,
                triangleData = nativeData.Data.triangleData,
                organData = nativeData.Data.allOrganData,

                organInstances = organInstances,

                vertexes = vertexes,
                normals = normals,
                tangents = tangents,
                triangleIndexes = triangleIndexes
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
            this.targetMesh.SetVertices(vertexes);
            this.targetMesh.SetNormals(normals);
            this.targetMesh.SetTangents(tangents);
            this.targetMesh.SetIndices(triangleIndexes, MeshTopology.Triangles, 0);
            return new CompleteCompletable<TurtleCompletionResult>(new TurtleCompletionResult());
        }


        [BurstCompile]
        public struct TurtleEntitySpawningJob : IJob
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
            public NativeArray<float3> vertexes;
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> normals;
            [NativeDisableParallelForRestriction]
            public NativeArray<float4> tangents;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> triangleIndexes;

            public void Execute()
            {
                for (int index = 0; index < organInstances.Length; index++)
                {
                    var organInstance = organInstances[index];
                    var organTemplate = organData[organInstance.organIndexInAllOrgans];
                    var matrixTransform = (Matrix4x4)organInstance.organTransform;

                    for (int i = 0; i < organTemplate.vertexes.length; i++)
                    {
                        var targetVertexIndex = organInstance.vertexMemorySpace.index + i;
                        var sourceVertexData = vertexData[organTemplate.vertexes.index + i];

                        vertexes[targetVertexIndex] = matrixTransform.MultiplyPoint(sourceVertexData.vertex);
                        normals[targetVertexIndex] = sourceVertexData.normal; // TODO: transform other things
                        tangents[targetVertexIndex] = sourceVertexData.tangent;
                    }

                    for (int i = 0; i < organTemplate.trianges.length; i++)
                    {
                        triangleIndexes[i + organInstance.trianglesMemorySpace.index] =
                            triangleData[i + organTemplate.trianges.index]
                            + organInstance.vertexMemorySpace.index; // offset the triangle indexes by the current index in vertex mem space
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
