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

        private NativeArray<float> volumetricDamageValues;
        private NativeArray<bool> volumetricDestructionFlags;
        private JobHandle? destructionFlagUpdateDependency;
        private JobHandle destructionFlagReadDependencies = default;
        private bool hasDamageChange;
        public OrganVolumetricWorld volumeWorld => GetComponent<OrganVolumetricWorld>();

        public float DamageAtVoxel(int voxelIndex)
        {
            destructionFlagUpdateDependency?.Complete();
            return volumetricDamageValues[voxelIndex];
        }

        public void ApplyDamage(int voxelIndex, float damageAmount)
        {
            destructionFlagUpdateDependency?.Complete();
            hasDamageChange = true;
            volumetricDamageValues[voxelIndex] += damageAmount;
        }

        public NativeArray<bool> GetDestructionFlagsReadOnly()
        {
            if (destructionFlagUpdateDependency.HasValue)
            {
                destructionFlagUpdateDependency?.Complete();
            }
            return volumetricDestructionFlags;
        }

        public void RegisterReaderOfDestructionFlags(JobHandle readDependency)
        {
            destructionFlagReadDependencies = JobHandle.CombineDependencies(readDependency, destructionFlagReadDependencies);
        }

        private void UpdateDestructionFlags()
        {
            hasDamageChange = false;
            destructionFlagUpdateDependency?.Complete();

            var durability = volumeWorld.nativeVolumeData.openReadData;

            var updateJob = new UpdatePlantDestructionFlags
            {
                plantDurabilityValues = durability,

                volumetricDamageValues = volumetricDamageValues,
                volumetricDestructionFlags = volumetricDestructionFlags
            };

            destructionFlagUpdateDependency = updateJob.Schedule(durability.Length, 1000, destructionFlagReadDependencies);
            volumeWorld.nativeVolumeData.RegisterReadingDependency(destructionFlagUpdateDependency.Value);
        }

        private void Awake()
        {
            var voxelLayout = volumeWorld.voxelLayout;
            volumetricDamageValues = new NativeArray<float>(voxelLayout.totalVolumeDataSize, Allocator.Persistent);
            volumetricDestructionFlags = new NativeArray<bool>(voxelLayout.totalVolumeDataSize, Allocator.Persistent);
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
            destructionFlagUpdateDependency?.Complete();
            volumetricDamageValues.Dispose();
            volumetricDestructionFlags.Dispose();
        }

        public void OnDrawGizmos()
        {
            if (!drawGizmos || !volumetricDamageValues.IsCreated || !volumetricDestructionFlags.IsCreated)
            {
                return;
            }

            destructionFlagUpdateDependency?.Complete();
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
        public NativeArray<bool> volumetricDestructionFlags;


        public void Execute(int index)
        {
            //volumetricDestructionFlags[index] = false;

            var durability = plantDurabilityValues[index];
            var damage = volumetricDamageValues[index];

            if (damage > 0 && damage > durability) //Unity.Burst.CompilerServices.Hint.Unlikely(damage > 0 && damage > durability))
            {
                volumetricDestructionFlags[index] = true;
                volumetricDamageValues[index] = 0f;
            }
        }
    }
}
