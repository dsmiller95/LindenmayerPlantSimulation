using Dman.LSystem.SystemRuntime.CustomRules;
using Dman.LSystem.SystemRuntime.GlobalCoordinator;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
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

        public VolumetricWorldVoxelLayout voxelLayout => new VolumetricWorldVoxelLayout
        {
            voxelOrigin = voxelOrigin,
            worldSize = worldSize,
            worldResolution = worldResolution
        };

        private NativeDelayedReadable nativeVolumeData;
        private List<VolumetricWorldWritableHandle> writableHandles;

        public VolumetricWorldWritableHandle GetNewWritableHandle()
        {
            var writableHandle = new VolumetricWorldWritableHandle(voxelLayout);
            writableHandles.Add(writableHandle);
            return writableHandle;
        }

        public JobHandle DisposeWritableHandle(VolumetricWorldWritableHandle handle)
        {
            writableHandles.Remove(handle);

            var deps = JobHandle.CombineDependencies(handle.writeDependency, nativeVolumeData.dataWriterDependencies);
            var subtractCleanupJob = new NativeArraySubtractJob
            {
                writeArray = this.nativeVolumeData.data,
                subtractArray = handle.oldData
            };
            deps = subtractCleanupJob.Schedule(this.nativeVolumeData.data.Length, 1000, deps);
            nativeVolumeData.RegisterWritingDependency(deps);

            return handle.Dispose(deps);
        }

        private void Awake()
        {
            writableHandles = new List<VolumetricWorldWritableHandle>();
            nativeVolumeData = new NativeDelayedReadable(voxelLayout.totalVolumeDataSize, Allocator.Persistent);
        }

        private void Start()
        {
        }

        private void Update()
        {
            nativeVolumeData.CompleteAndForceCopy();
            var dependency = this.nativeVolumeData.dataWriterDependencies;
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
                }
            }
            this.nativeVolumeData.RegisterWritingDependency(dependency);
        }

        private void LateUpdate()
        {
        }

        private void OnDestroy()
        {
            nativeVolumeData.Dispose();
            nativeVolumeData = null;
        }



        private void OnDrawGizmosSelected()
        {
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
                maxAmount = 1;
            }
            var voxelLayout = this.voxelLayout;
            var voxelSize = new Vector3(worldSize.x / worldResolution.x, worldSize.y / worldResolution.y, worldSize.z / worldResolution.z);
            var offsetFrom0 = voxelOrigin + (voxelSize / 2f);
            for (int x = 0; x < worldResolution.x; x++)
            {
                for (int y = 0; y < worldResolution.y; y++)
                {
                    for (int z = 0; z < worldResolution.z; z++)
                    {
                        var voxelCoordinate = new Vector3Int(x, y, z);
                        var cubeCenter = Vector3.Scale(voxelSize, voxelCoordinate) + offsetFrom0;
                        Gizmos.color = new Color(1f, 0f, 0f, 1f);
                        Gizmos.DrawWireCube(cubeCenter, voxelSize);

                        float amount = 0f;
                        if(nativeVolumeData != null)
                        {
                            amount = nativeVolumeData.openReadData[voxelLayout.GetDataIndexFromCoordinates(voxelCoordinate)] / (maxAmount * 3f);
                        }else
                        {
                            amount = Mathf.Sin(8f * voxelCoordinate.x / worldResolution.x) / (maxAmount * 3f);
                        }

                        var amountCenter = new Vector3(cubeCenter.x, cubeCenter.y + voxelSize.y / 2 * (amount - 1), cubeCenter.z);
                        var amountCubeSize = new Vector3(voxelSize.x - 0.5f, voxelSize.y * amount, voxelSize.z - 0.5f);

                        Gizmos.color = new Color(0f, 1f, 0f);
                        Gizmos.DrawCube(amountCenter, amountCubeSize);
                    }
                }
            }
        }
    }
}
