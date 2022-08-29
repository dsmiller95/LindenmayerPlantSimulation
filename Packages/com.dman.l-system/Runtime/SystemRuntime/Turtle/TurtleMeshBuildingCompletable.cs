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
            var lastSubmeshSize = meshSizePerSubmesh[meshSizePerSubmesh.Length - 1];
            var meshData = new TurtleMeshData
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

            currentJobHandle = turtleEntitySpawnJob.Schedule(meshBuilding.organInstances.Length, 100, currentJobHandle);
            nativeData.RegisterDependencyOnData(currentJobHandle);

            var meshBoundsJob = new TurtleMeshBoundsJob
            {
                submeshSizes = meshSizePerSubmesh,
                targetMesh = meshData
            };
            currentJobHandle = meshBoundsJob.Schedule(currentJobHandle);
            
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

        private static void SetDataToMesh(UnityEngine.Mesh mesh, TurtleMeshData meshData, NativeArray<TurtleMeshAllocationCounter> submeshSizes)
        {
            UnityEngine.Profiling.Profiler.BeginSample("applying mesh data");
            int vertexCount = meshData.vertexData.Length;
            int indexCount = meshData.indices.Length;

            mesh.Clear();

            mesh.SetVertexBufferParams(vertexCount, TurtleMeshData.GetVertexLayout());
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
