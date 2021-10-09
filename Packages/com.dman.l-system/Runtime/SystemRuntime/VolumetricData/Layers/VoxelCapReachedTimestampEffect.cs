using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.Layers
{
    /// <summary>
    /// this diffuser diffuses only to adjacent voxels. not as high quality as the kernel diffuser, but can handle
    ///     boundary conditions.
    /// </summary>
    [CreateAssetMenu(fileName = "VoxelCapReachedTimestampEffect", menuName = "LSystem/Resource Layers/VoxelCapReachedTimestampEffect")]
    public class VoxelCapReachedTimestampEffect : VolumetricLayerEffect
    {
        /// <summary>
        /// the layer which defines the cap on the resource amount which this layer is attached
        ///     to
        /// </summary>
        public int voxelResourceLayerCap = 0;
        public float regenerationPerSecondAsPercentOfDurability = 0.05f;

        /// <summary>
        /// convenience value, used in consumers of the timestamps to determine how old a command has to be
        ///     before it is invalid
        /// </summary>
        public float timeCommandStaysActive = 2f;

        public JobHandle? damageDataUpdateDependency { get; private set; }
        private NativeArray<float> volumetricDestructionTimestamps;
        private bool hasDamageChange;

        public override void SetupInternalData(VolumetricWorldVoxelLayout layout)
        {
            base.SetupInternalData(layout);
            volumetricDestructionTimestamps = new NativeArray<float>(layout.totalVoxels, Allocator.Persistent);
        }

        public override void CleanupInternalData(VolumetricWorldVoxelLayout layout)
        {
            base.CleanupInternalData(layout);
            damageDataUpdateDependency?.Complete();
            volumetricDestructionTimestamps.Dispose();
        }

        public override bool ApplyEffectToLayer(DoubleBuffered<float> layerData, VoxelWorldVolumetricLayerData readonlyLayerData, float deltaTime, ref JobHandleWrapper dependecy)
        {
            if (hasDamageChange)
            {
                UpdateDestructionFlags(layerData, readonlyLayerData, deltaTime, ref dependecy);
            }
            else
            {
                RepairDamage(layerData, readonlyLayerData, deltaTime, ref dependecy);
            }
            return true;
        }




        private JobHandle destructionFlagReadDependencies = default;
        public void RegisterReaderOfDestructionFlags(JobHandle readDependency)
        {
            destructionFlagReadDependencies = JobHandle.CombineDependencies(readDependency, destructionFlagReadDependencies);
        }
        public NativeArray<float> GetDestructionCommandTimestampsReadOnly()
        {
            if (damageDataUpdateDependency.HasValue)
            {
                damageDataUpdateDependency?.Complete();
            }
            return volumetricDestructionTimestamps;
        }

        private void UpdateDestructionFlags(DoubleBuffered<float> layerData, VoxelWorldVolumetricLayerData readonlyLayerData, float deltaTime, ref JobHandleWrapper dependency)
        {
            hasDamageChange = false;
            damageDataUpdateDependency?.Complete();

            var damageValues = layerData.CurrentData; // this is not a parallized job, so edits are made in-place

            var voxelLayers = readonlyLayerData;
            var updateJob = new UpdatePlantDestructionFlags
            {
                voxelLayerData = voxelLayers,
                flagCapLayerIndex = voxelResourceLayerCap,
                voxelLayout = voxelLayers.VoxelLayout,

                volumetricDamageValues = damageValues, 
                volumetricDestructionTimestamps = volumetricDestructionTimestamps,
                currentTime = Time.time,
                durabilityRegenerationFactor = Mathf.Exp(regenerationPerSecondAsPercentOfDurability * deltaTime) - 1
            };

            dependency = JobHandle.CombineDependencies(destructionFlagReadDependencies, dependency);

            dependency = updateJob.Schedule(damageValues.Length, 1000, dependency);
        }

        private void RepairDamage(DoubleBuffered<float> layerData, VoxelWorldVolumetricLayerData readonlyLayerData, float deltaTime, ref JobHandleWrapper dependency)
        {
            damageDataUpdateDependency?.Complete();
            var voxelLayers = readonlyLayerData;
            var damageValues = layerData.CurrentData; // this is not a parallized job, so edits are made in-place
            var dampenDamage = new DampenDamageJob
            {
                voxelLayerData = voxelLayers,
                voxelLayout = voxelLayers.VoxelLayout,
                volumetricDamageValues = damageValues,
                durabilityRegenerationFactor = Mathf.Exp(regenerationPerSecondAsPercentOfDurability * Time.deltaTime) - 1
            };

            dependency = JobHandle.CombineDependencies(destructionFlagReadDependencies, dependency);
            dependency = dampenDamage.Schedule(damageValues.Length, 1000, dependency);
        }
        /// <summary>
        /// search for any voxels which have 
        /// </summary>
        [BurstCompile]
        struct DampenDamageJob : IJobParallelFor
        {
            [ReadOnly]
            public VoxelWorldVolumetricLayerData voxelLayerData;
            public VolumetricWorldVoxelLayout voxelLayout;
            public NativeArray<float> volumetricDamageValues;
            public float durabilityRegenerationFactor;

            public void Execute(int index)
            {
                var voxelIndex = new VoxelIndex
                {
                    Value = index
                };

                var reductionAmount = voxelLayerData[voxelIndex, 0] * durabilityRegenerationFactor;
                volumetricDamageValues[voxelIndex.Value] = math.max(volumetricDamageValues[voxelIndex.Value] - reductionAmount, 0);
            }
        }

        /// <summary>
        /// search for any voxels which exceet a certain layer
        /// </summary>
        [BurstCompile]
        struct UpdatePlantDestructionFlags : IJobParallelFor
        {
            [ReadOnly]
            public VoxelWorldVolumetricLayerData voxelLayerData;
            public int flagCapLayerIndex;
            public VolumetricWorldVoxelLayout voxelLayout;

            public NativeArray<float> volumetricDamageValues;
            public NativeArray<float> volumetricDestructionTimestamps;
            public float currentTime;
            public float durabilityRegenerationFactor;


            public void Execute(int index)
            {
                //volumetricDestructionFlags[index] = false;
                var voxelIndex = new VoxelIndex
                {
                    Value = index
                };
                var durability = voxelLayerData[voxelIndex, flagCapLayerIndex];
                var damage = volumetricDamageValues[voxelIndex.Value];

                if (Unity.Burst.CompilerServices.Hint.Unlikely(damage > 0 && damage > durability))
                {
                    volumetricDestructionTimestamps[voxelIndex.Value] = currentTime;
                    volumetricDamageValues[voxelIndex.Value] = 0f;
                }
                else
                {
                    var reductionAmount = voxelLayerData[voxelIndex, flagCapLayerIndex] * durabilityRegenerationFactor;
                    volumetricDamageValues[voxelIndex.Value] = math.max(volumetricDamageValues[voxelIndex.Value] - reductionAmount, 0);
                }
            }
        }
    }
}
