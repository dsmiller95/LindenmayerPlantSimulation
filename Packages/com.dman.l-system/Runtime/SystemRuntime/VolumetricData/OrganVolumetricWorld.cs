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
    public class OrganVolumetricWorld : MonoBehaviour
    {
        public Vector3 voxelOrigin => transform.position;
        public Vector3 worldSize;

        public Vector3Int worldResolution;

        public Gradient heatmapGradient;

        public bool gizmos = true;

        public VolumetricWorldVoxelLayout voxelLayout => new VolumetricWorldVoxelLayout
        {
            voxelOrigin = voxelOrigin,
            worldSize = worldSize,
            worldResolution = worldResolution
        };

        public event Action volumeWorldChanged;

        public NativeDelayedReadable nativeVolumeData { get; private set; }
        private List<VolumetricWorldWritableHandle> writableHandles;

        public VolumetricWorldWritableHandle GetNewWritableHandle()
        {
            var writableHandle = new VolumetricWorldWritableHandle(voxelLayout);
            writableHandles.Add(writableHandle);
            return writableHandle;
        }

        public JobHandle DisposeWritableHandle(VolumetricWorldWritableHandle handle)
        {
            if (handle.isDisposed)
            {
                return default;
            }
            writableHandles.Remove(handle);

            return DisposeWritableHandleNoRemove(handle);
        }

        private void Awake()
        {
            writableHandles = new List<VolumetricWorldWritableHandle>();
            nativeVolumeData = new NativeDelayedReadable(voxelLayout.totalVolumeDataSize, Allocator.Persistent);
        }

        private void Start()
        {
        }

        private bool anyChangeLastFrame = false;
        private void Update()
        {
            if (anyChangeLastFrame)
            {
                nativeVolumeData.CompleteAndForceCopy();
                volumeWorldChanged?.Invoke();
            }
            var dependency = this.nativeVolumeData.dataWriterDependencies;

            anyChangeLastFrame = false;
            foreach (var writeHandle in writableHandles)
            {
                if (writeHandle.newDataIsAvailable && writeHandle.writeDependency.IsCompleted)
                {
                    var addSubJob = new NativeArrayAddSubtractJob
                    {
                        writeArray = this.nativeVolumeData.data,
                        subtractArray = writeHandle.oldData,
                        addArray = writeHandle.newData
                    };
                    dependency = addSubJob.Schedule(nativeVolumeData.data.Length, 1000, dependency);
                    writeHandle.RegisterReadDependency(dependency);
                    writeHandle.newDataIsAvailable = false;
                    anyChangeLastFrame = true;
                }
            }
            this.nativeVolumeData.RegisterWritingDependency(dependency);
        }

        private void LateUpdate()
        {
        }

        private void OnDestroy()
        {
            var dep = default(JobHandle);
            foreach (var handle in writableHandles)
            {
                dep = JobHandle.CombineDependencies(DisposeWritableHandleNoRemove(handle), dep);
            }
            writableHandles.Clear();
            dep.Complete();
            nativeVolumeData.Dispose();
            nativeVolumeData = null;
        }

        private JobHandle DisposeWritableHandleNoRemove(VolumetricWorldWritableHandle handle)
        {
            var deps = JobHandle.CombineDependencies(handle.writeDependency, nativeVolumeData.dataWriterDependencies);
            var subtractCleanupJob = new NativeArraySubtractNegativeProtectionJob
            {
                writeArray = this.nativeVolumeData.data,
                subtractArray = handle.newData
            };
            deps = subtractCleanupJob.Schedule(this.nativeVolumeData.data.Length, 1000, deps);
            nativeVolumeData.RegisterWritingDependency(deps);

            return handle.Dispose(deps);
        }



        private void OnDrawGizmosSelected()
        {
            if (!gizmos)
            {
                return;
            }

            var maxAmount = 1f;
            if(nativeVolumeData != null)
            {
                nativeVolumeData.dataReaderDependencies.Complete();
                for (int i = 0; i < nativeVolumeData.openReadData.Length; i++)
                {
                    var val = nativeVolumeData.openReadData[i];
                    maxAmount = Mathf.Max(val, maxAmount);
                }
            }else
            {
                maxAmount = (Vector3.one / 4f).sqrMagnitude;
            }
            var voxelLayout = this.voxelLayout;
            var voxelSize = voxelLayout.voxelSize;
            for (int x = 0; x < worldResolution.x; x++)
            {
                for (int y = 0; y < worldResolution.y; y++)
                {
                    for (int z = 0; z < worldResolution.z; z++)
                    {
                        var voxelCoordinate = new Vector3Int(x, y, z);
                        var cubeCenter = voxelLayout.CoordinateToCenterOfVoxel(voxelCoordinate);

                        float amount;
                        if(nativeVolumeData != null)
                        {
                            amount = nativeVolumeData.openReadData[voxelLayout.GetDataIndexFromCoordinates(voxelCoordinate)] / maxAmount;
                        }else
                        {
                            var xScaled = ((voxelCoordinate.x + 0.5f) / (float)worldResolution.x) - 0.5f;
                            var yScaled = ((voxelCoordinate.y + 0.5f) / (float)worldResolution.y) - 0.5f;
                            var zScaled = ((voxelCoordinate.z + 0.5f) / (float)worldResolution.z) - 0.5f;

                            amount = (maxAmount - new Vector3(xScaled, yScaled, zScaled).sqrMagnitude) / maxAmount;
                        }

                        Gizmos.color = heatmapGradient.Evaluate(amount);
                        Gizmos.DrawCube(cubeCenter, voxelSize * 0.7f);
                    }
                }
            }
        }
    }
}
