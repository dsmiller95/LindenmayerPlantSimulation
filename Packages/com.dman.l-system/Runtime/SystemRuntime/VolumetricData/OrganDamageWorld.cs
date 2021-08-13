using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
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

        public event Action onDamageDataUpdated;

        private NativeArray<float> volumetricDamageValues;
        private NativeArray<float> volumetricDestructionTimestamps;
        public JobHandle? damageDataUpdateDependency { get; private set; }
        private JobHandle destructionFlagReadDependencies = default;
        private bool hasDamageChange;
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

        public NativeArray<float> GetDamageValuesReadonly()
        {
            if (damageDataUpdateDependency.HasValue)
            {
                damageDataUpdateDependency?.Complete();
            }
            return volumetricDamageValues;
        }

        public void RegisterReaderOfDestructionFlags(JobHandle readDependency)
        {
            destructionFlagReadDependencies = JobHandle.CombineDependencies(readDependency, destructionFlagReadDependencies);
        }

        private void UpdateDestructionFlags()
        {
            hasDamageChange = false;
            damageDataUpdateDependency?.Complete();

            var durability = volumeWorld.nativeVolumeData.openReadData;

            var updateJob = new UpdatePlantDestructionFlags
            {
                plantDurabilityValues = durability,

                volumetricDamageValues = volumetricDamageValues,
                volumetricDestructionTimestamps = volumetricDestructionTimestamps,
                currentTime = Time.time
            };

            damageDataUpdateDependency = updateJob.Schedule(durability.Length, 1000, destructionFlagReadDependencies);
            volumeWorld.nativeVolumeData.RegisterReadingDependency(damageDataUpdateDependency.Value);
        }

        private void DamageChanged()
        {
            hasDamageChange = true;
        }

        private void Awake()
        {
            var voxelLayout = volumeWorld.voxelLayout;
            volumetricDamageValues = new NativeArray<float>(voxelLayout.totalVolumeDataSize, Allocator.Persistent);
            volumetricDestructionTimestamps = new NativeArray<float>(voxelLayout.totalVolumeDataSize, Allocator.Persistent);

            onDamageDataUpdated += DamageChanged;
        }
        private void Update()
        {
            if (hasDamageChange)
            {
                UpdateDestructionFlags();
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
            var voxelLayout = volumeWorld.voxelLayout;
            var voxelSize = voxelLayout.voxelSize;
            var durabilityValues = volumeWorld.nativeVolumeData.openReadData;

            Gizmos.color = new Color(1, 0, 0, 0.5f);
            for (int i = 0; i < voxelLayout.totalVolumeDataSize; i++)
            {
                var cubeCenter = voxelLayout.GetWorldPositionFromDataIndex(i);

                var damage = volumetricDamageValues[i];
                var durability = durabilityValues[i];

                var ratio = damage / durability;

                Gizmos.DrawCube(cubeCenter, voxelSize * ratio);
            }
        }
    }

    /// <summary>
    /// search for any voxels which have 
    /// </summary>
    public struct UpdatePlantDestructionFlags : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> plantDurabilityValues;

        public NativeArray<float> volumetricDamageValues;
        public NativeArray<float> volumetricDestructionTimestamps;
        public float currentTime;


        public void Execute(int index)
        {
            //volumetricDestructionFlags[index] = false;

            var durability = plantDurabilityValues[index];
            var damage = volumetricDamageValues[index];

            if (Unity.Burst.CompilerServices.Hint.Unlikely(damage > 0 && damage > durability))
            {
                volumetricDestructionTimestamps[index] = currentTime;
                volumetricDamageValues[index] = 0f;
            }
        }
    }
}
