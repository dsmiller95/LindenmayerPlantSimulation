﻿using Cysharp.Threading.Tasks;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
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

            targetMesh.MarkDynamic();
            UnityEngine.Profiling.Profiler.BeginSample("mesh data building job");

            UnityEngine.Profiling.Profiler.BeginSample("allocating");
            var lastSubmeshSize = meshBuilding.meshSizePerSubmesh[meshBuilding.meshSizePerSubmesh.Length - 1];
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
                submeshSizes = meshBuilding.meshSizePerSubmesh,

                organInstances = meshBuilding.organInstances,

                targetMesh = meshData
            };
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("scheduling");
            var currentJobHandle = turtleEntitySpawnJob.Schedule();
            nativeData.RegisterDependencyOnData(currentJobHandle);

            currentJobHandle = meshBuilding.organInstances.Dispose(currentJobHandle);
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.EndSample();


            // wait for task complete, with safe and immediate cancellation
            var cancelled = false;
            while (!currentJobHandle.IsCompleted && !token.IsCancellationRequested && !cancelled)
            {
                var (cancelledTask, registration) = token.ToUniTask();
                var completedIndex = await UniTask.WhenAny(
                    cancelledTask,
                    // TODO: yield at more spots
                    UniTask.Yield(PlayerLoopTiming.PostLateUpdate, token).SuppressCancellationThrow());
                cancelled = completedIndex == 0;
                registration.Dispose();
            }
            currentJobHandle.Complete();

            if (cancelled || token.IsCancellationRequested)
            {
                meshData.Dispose();
                meshBuilding.meshSizePerSubmesh.Dispose();
                throw new OperationCanceledException();
            }

            SetDataToMesh(targetMesh, meshData, meshBuilding.meshSizePerSubmesh);

            //Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, targetMesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds);

            meshData.Dispose();
            meshBuilding.meshSizePerSubmesh.Dispose();
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
            public NativeList<TurtleOrganInstance> organInstances;

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
    }
}
