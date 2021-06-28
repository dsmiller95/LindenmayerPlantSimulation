using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleOrganSpawningCompletable : ICompletable<TurtleCompletionResult>
    {
        public JobHandle currentJobHandle { get; private set; }

        private Mesh.MeshDataArray meshDataArray;

        private Mesh targetMesh;
        private NativeArray<TurtleMeshAllocationCounter> resultMeshSizeBySubmesh;


        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct MeshVertexLayout
        {
            public float3 pos;
            public float3 normal;
            public Color32 color;
            public float2 uv;
        }

        public TurtleOrganSpawningCompletable(
            Mesh targetMesh,
            NativeArray<TurtleMeshAllocationCounter> resultMeshSizeBySubmesh,
            DependencyTracker<NativeTurtleData> nativeData,
            NativeList<TurtleOrganInstance> organInstances)
        {
            this.targetMesh = targetMesh;
            this.resultMeshSizeBySubmesh = resultMeshSizeBySubmesh;
            UnityEngine.Profiling.Profiler.BeginSample("allocating mesh data");

            meshDataArray = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArray[0];
            var lastMeshSize = resultMeshSizeBySubmesh[resultMeshSizeBySubmesh.Length - 1];
            meshData.SetVertexBufferParams(lastMeshSize.indexInVertexes + lastMeshSize.totalVertexes,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.SNorm8, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
            );

            meshData.SetIndexBufferParams(lastMeshSize.indexInTriangles + lastMeshSize.totalTriangleIndexes, IndexFormat.UInt32);

            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("configuring job");
            var turtleEntitySpawnJob = new TurtleMeshBuildingJob
            {
                templateVertexData = nativeData.Data.vertexData,
                templateTriangleData = nativeData.Data.triangleData,
                templateOrganData = nativeData.Data.allOrganData,
                submeshSizes = resultMeshSizeBySubmesh,

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

        public ICompletable StepNext()
        {
            currentJobHandle.Complete();

            UnityEngine.Profiling.Profiler.BeginSample("applying mesh data");
            var meshData = meshDataArray[0];

            meshData.subMeshCount = resultMeshSizeBySubmesh.Length;
            for (int i = 0; i < resultMeshSizeBySubmesh.Length; i++)
            {
                var submeshSize = resultMeshSizeBySubmesh[i];
                var descriptor = new SubMeshDescriptor()
                {
                    baseVertex = 0,
                    topology = MeshTopology.Triangles,
                    indexCount = submeshSize.totalTriangleIndexes,
                    indexStart = submeshSize.indexInTriangles,
                };
                meshData.SetSubMesh(i, descriptor);
            }

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, targetMesh, MeshUpdateFlags.Default);
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("recalculating bounds");
            targetMesh.RecalculateBounds();
            UnityEngine.Profiling.Profiler.EndSample();

            resultMeshSizeBySubmesh.Dispose();

            return new CompleteCompletable<TurtleCompletionResult>(new TurtleCompletionResult());
        }


        [BurstCompile]
        struct TurtleMeshBuildingJob : IJob
        {
            [ReadOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<NativeVertexDatum> templateVertexData;
            [ReadOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<int> templateTriangleData;
            [ReadOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<TurtleOrganTemplate.Blittable> templateOrganData;

            [ReadOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<TurtleMeshAllocationCounter> submeshSizes;

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
                    var organTemplate = templateOrganData[organInstance.organIndexInAllOrgans];
                    var submeshData = submeshSizes[organTemplate.materialIndex];
                    var matrixTransform = (Matrix4x4)organInstance.organTransform;

                    var organVertexOffset = submeshData.indexInVertexes + organInstance.vertexMemorySpace.index;
                    var organTriangleOffset = submeshData.indexInTriangles + organInstance.trianglesMemorySpace.index;

                    for (int i = 0; i < organTemplate.vertexes.length; i++)
                    {
                        var sourceVertexData = templateVertexData[organTemplate.vertexes.index + i];
                        vertexTargetData[i + organVertexOffset] = new MeshVertexLayout
                        {
                            pos = matrixTransform.MultiplyPoint(sourceVertexData.vertex),
                            normal = matrixTransform.MultiplyVector(sourceVertexData.normal),
                            uv = sourceVertexData.uv,
                            color = ColorFromIdentity(organInstance.organIdentity, (uint)index),
                        };
                    }

                    for (int i = 0; i < organTemplate.trianges.length; i++)
                    {
                        triangleIndexes[i + organTriangleOffset] = (uint)
                            (templateTriangleData[i + organTemplate.trianges.index]
                            + organVertexOffset); // offset the triangle indexes by the current index in vertex mem space
                    }
                }
            }

            private Color32 ColorFromIdentity(UIntFloatColor32 identity, uint index)
            {
                return identity.color;
            }
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            currentJobHandle.Complete();
            meshDataArray.Dispose();
            return JobHandle.CombineDependencies(
                inputDeps,
                resultMeshSizeBySubmesh.Dispose(inputDeps)
                );
        }

        public void Dispose()
        {
            currentJobHandle.Complete();
            meshDataArray.Dispose();
            resultMeshSizeBySubmesh.Dispose();
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
