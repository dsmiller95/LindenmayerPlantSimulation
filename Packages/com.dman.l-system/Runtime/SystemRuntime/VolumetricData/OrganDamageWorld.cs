using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Dman.ReactiveVariables;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    [RequireComponent(typeof(OrganVolumetricWorld))]
    public class OrganDamageWorld : MonoBehaviour
    {
        public bool drawGizmos = true;

        /// <summary>
        /// how many seconds a voxel's "destroy" command should remain active for systems to pick up on
        ///     this should scale with the l-system's update frequency. The plants' time between
        ///     updates should never be able to be more than this value
        /// </summary>
        public float timeDestructionCommandsStayActive = 2f;
        public float regenerationPerSecondAsPercentOfDurability = 0.05f;
        public FloatReference simulationSpeed;

        public event Action onDamageDataUpdated;

        private NativeArray<float> volumetricDamageValues;
        private NativeArray<float> volumetricDestructionTimestamps;
        public JobHandle? damageDataUpdateDependency { get; private set; }
        private JobHandle destructionFlagReadDependencies = default;
        private bool hasDamageChange;
        private float lastDamageChange;
        public OrganVolumetricWorld volumeWorld => GetComponent<OrganVolumetricWorld>();

        public float DamageAtVoxel(int voxelIndex)
        {
            damageDataUpdateDependency?.Complete();
            return volumetricDamageValues[voxelIndex];
        }

        public void ApplyDamage(int voxelIndex, float damageAmount)
        {
            damageDataUpdateDependency?.Complete();
            volumetricDamageValues[voxelIndex] += damageAmount;
            onDamageDataUpdated?.Invoke();
        }

        public NativeArray<float> GetDestructionCommandTimestampsReadOnly()
        {
            if (damageDataUpdateDependency.HasValue)
            {
                damageDataUpdateDependency?.Complete();
            }
            return volumetricDestructionTimestamps;
        }

        public NativeArray<float> GetDamageValuesReadSafe()
        {
            if (damageDataUpdateDependency.HasValue)
            {
                damageDataUpdateDependency?.Complete();
            }
            return volumetricDamageValues;
        }


        public void RegisterDamageValuesWriter(JobHandle writer)
        {
            if (damageDataUpdateDependency.HasValue)
            {
                damageDataUpdateDependency?.Complete();
            }
            hasDamageChange = true;
            damageDataUpdateDependency = writer;
        }



        public void RegisterReaderOfDestructionFlags(JobHandle readDependency)
        {
            destructionFlagReadDependencies = JobHandle.CombineDependencies(readDependency, destructionFlagReadDependencies);
        }

        private void UpdateDestructionFlags()
        {
            hasDamageChange = false;
            damageDataUpdateDependency?.Complete();

            var voxelLayers = volumeWorld.NativeVolumeData.openReadData;
            var updateJob = new UpdatePlantDestructionFlags
            {
                voxelLayerData = voxelLayers,
                voxelLayout = volumeWorld.VoxelLayout,

                volumetricDamageValues = volumetricDamageValues,
                volumetricDestructionTimestamps = volumetricDestructionTimestamps,
                currentTime = Time.time,
                durabilityRegenerationFactor = Mathf.Exp(regenerationPerSecondAsPercentOfDurability * Time.deltaTime * simulationSpeed.CurrentValue) - 1
            };

            damageDataUpdateDependency = updateJob.Schedule(volumetricDamageValues.Length, 1000, destructionFlagReadDependencies);
            volumeWorld.NativeVolumeData.RegisterReadingDependency(damageDataUpdateDependency.Value);
        }

        private void RepairDamage()
        {
            damageDataUpdateDependency?.Complete();
            var voxelLayers = volumeWorld.NativeVolumeData.openReadData;
            var dampenDamage = new DampenDamageJob
            {
                voxelLayerData = voxelLayers,
                voxelLayout = volumeWorld.VoxelLayout,
                volumetricDamageValues = volumetricDamageValues,
                durabilityRegenerationFactor = Mathf.Exp(regenerationPerSecondAsPercentOfDurability * Time.deltaTime * simulationSpeed.CurrentValue) - 1
            };
            damageDataUpdateDependency = dampenDamage.Schedule(volumetricDamageValues.Length, 1000, destructionFlagReadDependencies);
            volumeWorld.NativeVolumeData.RegisterReadingDependency(damageDataUpdateDependency.Value);
        }

        private void DamageChanged()
        {
            hasDamageChange = true;
            lastDamageChange = Time.time;
        }

        private void Awake()
        {
            var voxelLayout = volumeWorld.VoxelLayout;
            volumetricDamageValues = new NativeArray<float>(voxelLayout.totalVoxels, Allocator.Persistent);
            volumetricDestructionTimestamps = new NativeArray<float>(voxelLayout.totalVoxels, Allocator.Persistent);

            onDamageDataUpdated += DamageChanged;
        }
        private void Update()
        {
            if (hasDamageChange)
            {
                UpdateDestructionFlags();
            }
            else
            {
                RepairDamage();
            }
        }

        private void OnDestroy()
        {
            damageDataUpdateDependency?.Complete();
            volumetricDamageValues.Dispose();
            volumetricDestructionTimestamps.Dispose();

            onDamageDataUpdated -= DamageChanged;
        }

        public void OnDrawGizmos()
        {
            if (!drawGizmos || !volumetricDamageValues.IsCreated || !volumetricDestructionTimestamps.IsCreated)
            {
                return;
            }

            damageDataUpdateDependency?.Complete();
            var volumeWorld = GetComponent<OrganVolumetricWorld>();
            var voxelLayout = volumeWorld.VoxelLayout;
            var voxelSize = voxelLayout.voxelSize;
            var voxelLayerData = volumeWorld.NativeVolumeData.openReadData;

            Gizmos.color = new Color(1, 0, 0, 0.5f);
            for (VoxelIndex voxelIndex = default; voxelIndex.Value < voxelLayout.totalVoxels; voxelIndex.Value++)
            {
                var cubeCenter = voxelLayout.GetWorldPositionFromVoxelIndex(voxelIndex);

                var damage = volumetricDamageValues[voxelIndex.Value];
                var durability = voxelLayerData[voxelIndex, 0];

                var ratio = damage / durability;

                Gizmos.DrawCube(cubeCenter, voxelSize * ratio);
            }
        }
    }

    /// <summary>
    /// search for any voxels which have 
    /// </summary>
    [BurstCompile]
    public struct DampenDamageJob : IJobParallelFor
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
    /// search for any voxels which have 
    /// </summary>
    [BurstCompile]
    public struct UpdatePlantDestructionFlags : IJobParallelFor
    {
        [ReadOnly]
        public VoxelWorldVolumetricLayerData voxelLayerData;
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
            var durability = voxelLayerData[voxelIndex, 0];
            var damage = volumetricDamageValues[voxelIndex.Value];

            if (Unity.Burst.CompilerServices.Hint.Unlikely(damage > 0 && damage > durability))
            {
                volumetricDestructionTimestamps[voxelIndex.Value] = currentTime;
                volumetricDamageValues[voxelIndex.Value] = 0f;
            }else
            {
                var reductionAmount = voxelLayerData[voxelIndex, 0] * durabilityRegenerationFactor;
                volumetricDamageValues[voxelIndex.Value] = math.max(volumetricDamageValues[voxelIndex.Value] - reductionAmount, 0);
            }
        }
    }
}
