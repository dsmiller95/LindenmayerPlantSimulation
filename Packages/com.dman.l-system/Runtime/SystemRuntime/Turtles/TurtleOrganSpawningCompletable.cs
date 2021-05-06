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

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleOrganSpawningCompletable : ICompletable<TurtleCompletionResult>
    {
        public JobHandle currentJobHandle { get; private set; }


        public TurtleOrganSpawningCompletable(
            EntityCommandBufferSystem spawnCommandBuffer,
            Entity parentEntity,
            DependencyTracker<NativeTurtleData> nativeData,
            NativeList<TurtleOrganInstance> organInstances)
        {
            UnityEngine.Profiling.Profiler.BeginSample("clear old organs");
            var organSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<LSystemMeshMemberUpdateSystem>();
            var clearOldOrgansCommandBuffer = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            var deleteBuffer = clearOldOrgansCommandBuffer.CreateCommandBuffer();
            organSystem.ClearOldOrgans(deleteBuffer, parentEntity);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("allocating command buffer");
            var buffer = spawnCommandBuffer.CreateCommandBuffer();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("configuring job");
            var turtleEntitySpawnJob = new TurtleEntitySpawningJob
            {
                organData = nativeData.Data.allOrganData,
                organInstances = organInstances,
                organSpawnBuffer = buffer,
                parentEntity = parentEntity
            };
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("scheduling job");
            currentJobHandle = turtleEntitySpawnJob.Schedule();
            spawnCommandBuffer.AddJobHandleForProducer(currentJobHandle);
            nativeData.RegisterDependencyOnData(currentJobHandle);

            currentJobHandle = organInstances.Dispose(currentJobHandle);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public ICompletable<TurtleCompletionResult> StepNext()
        {
            currentJobHandle.Complete();
            return new CompleteCompletable<TurtleCompletionResult>(new TurtleCompletionResult());
        }


        [BurstCompile]
        public struct TurtleEntitySpawningJob : IJob
        {
            [ReadOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<TurtleOrganTemplate.Blittable> organData;

            [ReadOnly]
            public NativeList<TurtleOrganInstance> organInstances;

            public EntityCommandBuffer organSpawnBuffer;
            public Entity parentEntity;

            public void Execute()
            {
                for (int index = 0; index < organInstances.Length; index++)
                {
                    var organInstance = organInstances[index];
                    var organTemplate = organData[organInstance.organIndexInAllOrgans];


                    var newOrgan = organSpawnBuffer.Instantiate(organTemplate.prototype);
                    organSpawnBuffer.RemoveComponent<LSystemOrganTemplateComponentFlag>(newOrgan);
                    organSpawnBuffer.SetComponent(newOrgan, new LocalToParent
                    {
                        Value = organInstance.organTransform
                    });
                    organSpawnBuffer.SetComponent(newOrgan, new Parent
                    {
                        Value = parentEntity
                    });
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
