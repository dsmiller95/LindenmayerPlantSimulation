using Cysharp.Threading.Tasks;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.Utilities;
using Dman.Utilities.Math;
using System;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static Dman.LSystem.SystemRuntime.Turtle.TurtleStringReadingCompletable;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleMeshBuildingCompletable
    {
        public static async UniTask BuildMesh(
            Mesh targetMesh,
            int totalSubmeshes,
            TurtleMeshBuildingInstructions meshBuilding,
            DependencyTracker<NativeTurtleData> nativeData,
            CancellationToken token)
        {
            if (nativeData.IsDisposed)
            {
                throw new InvalidOperationException("turtle data has been disposed before completable could finish.");
            }

            var meshSizePerSubmesh = new NativeArray<TurtleMeshAllocationCounter>(totalSubmeshes, Allocator.TempJob);
            var organMeshSizeAllocations = new NativeArray<OrganMeshMemorySpaceAllocation>(meshBuilding.organInstances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var meshCountingJob = new TurtleMeshSizeRequirementComputeJob
            {
                allOrgans = nativeData.Data.allOrganData,
                organInstances = meshBuilding.organInstances,
                organMeshAllocations = organMeshSizeAllocations,
                meshSizeCounterPerSubmesh = meshSizePerSubmesh,
            };

            var currentJobHandle = meshCountingJob.Schedule();
            nativeData.RegisterDependencyOnData(currentJobHandle);

            var cancelled = await currentJobHandle.ToUniTaskImmediateCompleteOnCancel(token);
            if (cancelled || token.IsCancellationRequested || nativeData.IsDisposed)
            {
                meshSizePerSubmesh.Dispose();
                organMeshSizeAllocations.Dispose();
                throw new OperationCanceledException();
            }

            targetMesh.MarkDynamic();
            UnityEngine.Profiling.Profiler.BeginSample("mesh data building job");

            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            var lastSubmeshSize = meshSizePerSubmesh[meshSizePerSubmesh.Length - 1];
            var meshData = new MyMeshData
            {
                indices = new NativeArray<uint>(lastSubmeshSize.indexInTriangles + lastSubmeshSize.totalTriangleIndexes, Allocator.TempJob), // TODO: does this have to be persistent? or can it be tempjob since it'll be handed to the mesh?
                vertexData = new NativeArray<MeshVertexLayout>(lastSubmeshSize.indexInVertexes + lastSubmeshSize.totalVertexes, Allocator.TempJob),
                meshBoundsBySubmesh = new NativeArray<Bounds>(totalSubmeshes, Allocator.TempJob)
            };

            var turtleEntitySpawnJob = new TurtleMeshBuildingJob
            {
                templateVertexData = nativeData.Data.vertexData,
                templateTriangleData = nativeData.Data.triangleData,
                templateOrganData = nativeData.Data.allOrganData,
                submeshSizes = meshSizePerSubmesh,

                organInstances = meshBuilding.organInstances,
                organMeshAllocations = organMeshSizeAllocations,

                targetMesh = meshData
            };
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("scheduling");
            currentJobHandle = turtleEntitySpawnJob.Schedule(currentJobHandle);
            nativeData.RegisterDependencyOnData(currentJobHandle);

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.EndSample();


            cancelled = await currentJobHandle.ToUniTaskImmediateCompleteOnCancel(token);

            if (cancelled || token.IsCancellationRequested)
            {
                meshData.Dispose();
                meshSizePerSubmesh.Dispose();
                organMeshSizeAllocations.Dispose();
                throw new OperationCanceledException();
            }

            SetDataToMesh(targetMesh, meshData, meshSizePerSubmesh);

            //Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, targetMesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds);

            meshData.Dispose();
            meshSizePerSubmesh.Dispose();
            organMeshSizeAllocations.Dispose();
        }

        private static void SetDataToMesh(UnityEngine.Mesh mesh, MyMeshData meshData, NativeArray<TurtleMeshAllocationCounter> submeshSizes)
        {
            UnityEngine.Profiling.Profiler.BeginSample("applying mesh data");
            int vertexCount = meshData.vertexData.Length;
            int indexCount = meshData.indices.Length;

            mesh.Clear();

            mesh.SetVertexBufferParams(vertexCount, GetVertexLayout());
            mesh.SetVertexBufferData(meshData.vertexData, 0, 0, vertexCount, 0, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices);

            mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
            mesh.SetIndexBufferData(meshData.indices, 0, 0, indexCount, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices);

            mesh.subMeshCount = submeshSizes.Length;
            var combinedBounds = meshData.meshBoundsBySubmesh[0];
            for (int i = 0; i < submeshSizes.Length; i++)
            {
                var submeshSize = submeshSizes[i];
                var descriptor = new SubMeshDescriptor()
                {
                    baseVertex = 0,
                    topology = MeshTopology.Triangles,
                    indexCount = submeshSize.totalTriangleIndexes,
                    indexStart = submeshSize.indexInTriangles,
                    firstVertex = submeshSize.indexInVertexes,
                    vertexCount = submeshSize.totalVertexes,
                    bounds = meshData.meshBoundsBySubmesh[i],
                };
                combinedBounds.Encapsulate(descriptor.bounds);
                mesh.SetSubMesh(i, descriptor, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices);
            }

            mesh.bounds = combinedBounds;
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private static VertexAttributeDescriptor[] GetVertexLayout()
        {
            return new[]{
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.UNorm8, 4)
                };
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct MeshVertexLayout
        {
            public float3 pos;
            public float3 normal;
            public Color32 color;
            public float2 uv;
            public byte4 extraData;
        }

        struct MyMeshData : INativeDisposable
        {
            public NativeArray<MeshVertexLayout> vertexData;
            public NativeArray<uint> indices;
            public NativeArray<Bounds> meshBoundsBySubmesh;

            public JobHandle Dispose(JobHandle inputDeps)
            {
                return JobHandle.CombineDependencies(
                    vertexData.Dispose(inputDeps),
                    indices.Dispose(inputDeps),
                    meshBoundsBySubmesh.Dispose(inputDeps));
            }

            public void Dispose()
            {
                vertexData.Dispose();
                indices.Dispose();
                meshBoundsBySubmesh.Dispose();
            }
        }

        private struct OrganMeshMemorySpaceAllocation
        {
            public JaggedIndexing vertexMemorySpace;
            public JaggedIndexing trianglesMemorySpace;
        }

        [BurstCompile]
        private struct TurtleMeshBuildingJob : IJob
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
            public NativeArray<TurtleOrganInstance> organInstances;
            [ReadOnly]
            public NativeArray<OrganMeshMemorySpaceAllocation> organMeshAllocations;

            [NativeDisableParallelForRestriction]
            public MyMeshData targetMesh;

            public void Execute()
            {
                var vertexTargetData = targetMesh.vertexData;// targetMesh.GetVertexData<MeshVertexLayout>();
                var triangleIndexes = targetMesh.indices;// targetMesh.GetIndexData<uint>();
                for (int index = 0; index < organInstances.Length; index++)
                {
                    var organInstance = organInstances[index];
                    var organTemplate = templateOrganData[organInstance.organIndexInAllOrgans];
                    var submeshData = submeshSizes[organTemplate.materialIndex];
                    var submeshBounds = targetMesh.meshBoundsBySubmesh[organTemplate.materialIndex];
                    var matrixTransform = (Matrix4x4)organInstance.organTransform;

                    var organMeshSpace = organMeshAllocations[index];
                    var organVertexOffset = submeshData.indexInVertexes + organMeshSpace.vertexMemorySpace.index;
                    var organTriangleOffset = submeshData.indexInTriangles + organMeshSpace.trianglesMemorySpace.index;

                    for (int i = 0; i < organTemplate.vertexes.length; i++)
                    {
                        var sourceVertexData = templateVertexData[organTemplate.vertexes.index + i];
                        var newVertexData = new MeshVertexLayout
                        {
                            pos = matrixTransform.MultiplyPoint(sourceVertexData.vertex),
                            normal = matrixTransform.MultiplyVector(sourceVertexData.normal),
                            uv = sourceVertexData.uv,
                            color = ColorFromIdentity(organInstance.organIdentity, (uint)index),
                            extraData = organInstance.extraData
                        };
                        vertexTargetData[i + organVertexOffset] = newVertexData;
                        submeshBounds.Encapsulate(newVertexData.pos);
                    }
                    targetMesh.meshBoundsBySubmesh[organTemplate.materialIndex] = submeshBounds;

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
                //identity.UIntValue = BitMixer.Mix(identity.UIntValue);

                return identity.color;
            }
        }

        /// <summary>
        /// takes a list of organ instances, and computes the space required to fit them all inside of a mesh object
        /// </summary>
        [BurstCompile]
        private struct TurtleMeshSizeRequirementComputeJob : IJob
        {
            [ReadOnly]
            public NativeArray<TurtleOrganTemplate.Blittable> allOrgans;
            [ReadOnly]
            public NativeArray<TurtleOrganInstance> organInstances;

            // outputs
            public NativeArray<OrganMeshMemorySpaceAllocation> organMeshAllocations;
            public NativeArray<TurtleMeshAllocationCounter> meshSizeCounterPerSubmesh;
            public void Execute()
            {
                for (int i = 0; i < organInstances.Length; i++)
                {
                    var organInstance = organInstances[i];
                    var organConfig = allOrgans[organInstance.organIndexInAllOrgans];
                    var meshSizeForSubmesh = meshSizeCounterPerSubmesh[organConfig.materialIndex];

                    var organAllocationSize = new OrganMeshMemorySpaceAllocation();
                    organAllocationSize.vertexMemorySpace = new JaggedIndexing
                    {
                        index = meshSizeForSubmesh.totalVertexes,
                        length = organConfig.vertexes.length
                    };
                    organAllocationSize.trianglesMemorySpace = new JaggedIndexing
                    {
                        index = meshSizeForSubmesh.totalTriangleIndexes,
                        length = organConfig.trianges.length
                    };

                    meshSizeForSubmesh.totalVertexes += organConfig.vertexes.length;
                    meshSizeForSubmesh.totalTriangleIndexes += organConfig.trianges.length;

                    organMeshAllocations[i] = organAllocationSize;
                    meshSizeCounterPerSubmesh[organConfig.materialIndex] = meshSizeForSubmesh;
                }

                var totalVertexes = 0;
                var totalIndexes = 0;
                for (int i = 0; i < meshSizeCounterPerSubmesh.Length; i++)
                {
                    var meshSize = meshSizeCounterPerSubmesh[i];
                    meshSize.indexInVertexes = totalVertexes;
                    meshSize.indexInTriangles = totalIndexes;
                    totalVertexes += meshSize.totalVertexes;
                    totalIndexes += meshSize.totalTriangleIndexes;
                    meshSizeCounterPerSubmesh[i] = meshSize;
                }
            }
        }
    }
}
