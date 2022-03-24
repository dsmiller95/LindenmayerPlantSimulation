using Cysharp.Threading.Tasks;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.Utilities;
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
            TurtleMeshBuildingInstructions meshBuilding,
            DependencyTracker<NativeTurtleData> nativeData,
            CancellationToken token)
        {
            if (nativeData.IsDisposed)
            {
                throw new InvalidOperationException("turtle data has been disposed before completable could finish.");
            }

            var meshSizePerSubmesh = new NativeArray<TurtleMeshAllocationCounter>(meshBuilding.totalSubmeshes, Allocator.TempJob);
            var meshCountingJob = new TurtleMeshSizeRequirementComputeJob
            {
                allOrgans = nativeData.Data.allOrganData,
                organInstances = meshBuilding.organInstances,
                meshSizeCounterPerSubmesh = meshSizePerSubmesh,
            };

            var currentJobHandle = meshCountingJob.Schedule();

            var cancelled = await currentJobHandle.ToUniTaskImmediateCompleteOnCancel(token);
            if (cancelled || token.IsCancellationRequested)
            {
                meshSizePerSubmesh.Dispose();
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
                meshBounds = new NativeArray<Bounds>(1, Allocator.TempJob)
            };

            var turtleEntitySpawnJob = new TurtleMeshBuildingJob
            {
                templateVertexData = nativeData.Data.vertexData,
                templateTriangleData = nativeData.Data.triangleData,
                templateOrganData = nativeData.Data.allOrganData,
                submeshSizes = meshSizePerSubmesh,

                organInstances = meshBuilding.organInstances,

                targetMesh = meshData
            };
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("scheduling");
            currentJobHandle = turtleEntitySpawnJob.Schedule(currentJobHandle);
            nativeData.RegisterDependencyOnData(currentJobHandle);

            currentJobHandle = meshBuilding.organInstances.Dispose(currentJobHandle);
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.EndSample();


                cancelled = await currentJobHandle.ToUniTaskImmediateCompleteOnCancel(token);

            if (cancelled || token.IsCancellationRequested)
            {
                meshData.Dispose();
                meshSizePerSubmesh.Dispose();
                throw new OperationCanceledException();
            }

            SetDataToMesh(targetMesh, meshData, meshSizePerSubmesh);

            //Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, targetMesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds);

            meshData.Dispose();
            meshSizePerSubmesh.Dispose();
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
            for (int i = 0; i < submeshSizes.Length; i++)
            {
                var submeshSize = submeshSizes[i];
                var descriptor = new SubMeshDescriptor()
                {
                    baseVertex = 0,
                    topology = MeshTopology.Triangles,
                    indexCount = submeshSize.totalTriangleIndexes,
                    indexStart = submeshSize.indexInTriangles,
                };
                mesh.SetSubMesh(i, descriptor, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices);
            }

            mesh.bounds = meshData.meshBounds[0];
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private static VertexAttributeDescriptor[] GetVertexLayout()
        {
            return new[]{
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
                };
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct MeshVertexLayout
        {
            public float3 pos;
            public float3 normal;
            public Color32 color;
            public float2 uv;
        }

        struct MyMeshData : INativeDisposable
        {
            public NativeArray<MeshVertexLayout> vertexData;
            public NativeArray<uint> indices;
            public NativeArray<Bounds> meshBounds;

            public JobHandle Dispose(JobHandle inputDeps)
            {
                return JobHandle.CombineDependencies(
                    vertexData.Dispose(inputDeps),
                    indices.Dispose(inputDeps),
                    meshBounds.Dispose(inputDeps));
            }

            public void Dispose()
            {
                vertexData.Dispose();
                indices.Dispose();
                meshBounds.Dispose();
            }
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
            public NativeArray<TurtleOrganInstance> organInstances;

            [NativeDisableParallelForRestriction]
            public MyMeshData targetMesh;

            public void Execute()
            {
                var vertexTargetData = targetMesh.vertexData;// targetMesh.GetVertexData<MeshVertexLayout>();
                var triangleIndexes = targetMesh.indices;// targetMesh.GetIndexData<uint>();
                var bounds = targetMesh.meshBounds[0];
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
                        var newVertexData = new MeshVertexLayout
                        {
                            pos = matrixTransform.MultiplyPoint(sourceVertexData.vertex),
                            normal = matrixTransform.MultiplyVector(sourceVertexData.normal),
                            uv = sourceVertexData.uv,
                            color = ColorFromIdentity(organInstance.organIdentity, (uint)index),
                        };
                        vertexTargetData[i + organVertexOffset] = newVertexData;
                        bounds.Encapsulate(newVertexData.pos);
                    }

                    for (int i = 0; i < organTemplate.trianges.length; i++)
                    {
                        triangleIndexes[i + organTriangleOffset] = (uint)
                            (templateTriangleData[i + organTemplate.trianges.index]
                            + organVertexOffset); // offset the triangle indexes by the current index in vertex mem space
                    }
                }
                targetMesh.meshBounds[0] = bounds;
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
        public struct TurtleMeshSizeRequirementComputeJob : IJob
        {
            [ReadOnly]
            public NativeArray<TurtleOrganTemplate.Blittable> allOrgans;

            // input+output
            public NativeArray<TurtleOrganInstance> organInstances;

            // outputs
            public NativeArray<TurtleMeshAllocationCounter> meshSizeCounterPerSubmesh;
            public void Execute()
            {
                for (int i = 0; i < organInstances.Length; i++)
                {
                    var organInstance = organInstances[i];
                    var organConfig = allOrgans[organInstance.organIndexInAllOrgans];
                    var meshSizeForSubmesh = meshSizeCounterPerSubmesh[organConfig.materialIndex];

                    organInstance.vertexMemorySpace = new JaggedIndexing
                    {
                        index = meshSizeForSubmesh.totalVertexes,
                        length = organConfig.vertexes.length
                    };
                    organInstance.trianglesMemorySpace = new JaggedIndexing
                    {
                        index = meshSizeForSubmesh.totalTriangleIndexes,
                        length = organConfig.trianges.length
                    };

                    meshSizeForSubmesh.totalVertexes += organInstance.vertexMemorySpace.length;
                    meshSizeForSubmesh.totalTriangleIndexes += organInstance.trianglesMemorySpace.length;

                    organInstances[i] = organInstance;
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
