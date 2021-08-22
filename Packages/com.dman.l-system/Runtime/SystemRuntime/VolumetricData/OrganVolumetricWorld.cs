using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    public enum GizmoOptions
    {
        NONE,
        SELECTED,
        ALWAYS
    }

    public class OrganVolumetricWorld : MonoBehaviour
    {
        public Vector3 voxelOrigin => transform.position;
        public Vector3 worldSize;

        public Vector3Int worldResolution;

        public Gradient heatmapGradient;

        public GizmoOptions gizmos = GizmoOptions.SELECTED;
        public int layerToRender = 0;
        public bool wireCellGizmos = false;
        public bool amountVisualizedGizmos = true;

        public VolumetricResourceLayer[] extraLayers;

        public VolumetricWorldVoxelLayout VoxelLayout => new VolumetricWorldVoxelLayout
        {
            voxelOrigin = voxelOrigin,
            worldSize = worldSize,
            worldResolution = worldResolution,
            dataLayerCount = extraLayers.Length + 1
        };

        public event Action volumeWorldChanged;

        public NativeDelayedReadable nativeVolumeData { get; private set; }
        private List<VolumetricWorldModifierHandle> writableHandles;

        public VolumetricWorldModifierHandle GetNewWritableHandle()
        {
            var writableHandle = new VolumetricWorldModifierHandle(VoxelLayout, this);
            writableHandles.Add(writableHandle);
            return writableHandle;
        }

        //public int IndexForLayer(VolumetricResourceLayer layer)
        //{
        //    for (int i = 0; i < extraLayers.Length; i++)
        //    {
        //        if (extraLayers[i].myId == layer.myId)
        //        {
        //            return i + 1; // add 1, because durability is an exception at 0
        //        }
        //    }
        //    return -1;
        //}

        public JobHandle DisposeWritableHandle(VolumetricWorldModifierHandle handle)
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
            writableHandles = new List<VolumetricWorldModifierHandle>();
            nativeVolumeData = new NativeDelayedReadable(VoxelLayout.totalDataSize, Allocator.Persistent);
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
            var voxelLayout = this.VoxelLayout;
            foreach (var writeHandle in writableHandles)
            {
                if (writeHandle.newDataIsAvailable && writeHandle.writeDependency.IsCompleted)
                {
                    writeHandle.writeDependency.Complete();
                    var consolidationJob = new VoxelMarkerConsolidation
                    {
                        allBaseMarkers = this.nativeVolumeData.data,
                        oldMarkerLevels = writeHandle.oldDurability,
                        newMarkerLevels = writeHandle.newDurability,
                        markerLayerIndex = 0,
                        totalLayersInBase = voxelLayout.dataLayerCount
                    };
                    dependency = consolidationJob.Schedule(writeHandle.newDurability.Length, 1000, dependency);

                    var commandPlaybackJob = new LayerModificationCommandPlaybackJob
                    {
                        commands = writeHandle.modificationCommands,
                        dataArray = this.nativeVolumeData.data,
                        voxelLayout = voxelLayout
                    };
                    dependency = commandPlaybackJob.Schedule(dependency);

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

        private JobHandle DisposeWritableHandleNoRemove(VolumetricWorldModifierHandle handle)
        {
            var deps = JobHandle.CombineDependencies(handle.writeDependency, nativeVolumeData.dataWriterDependencies);
            var subtractCleanupJob = new NativeArraySubtractNegativeProtectionJob
            {
                allBaseMarkers = this.nativeVolumeData.data,
                markerLevelsToRemove = handle.newDurability,
                markerLayerIndex = 0,
                totalLayersInBase = VoxelLayout.dataLayerCount
            };
            deps = subtractCleanupJob.Schedule(VoxelLayout.totalVoxels, 1000, deps);
            nativeVolumeData.RegisterWritingDependency(deps);

            return handle.Dispose(deps);
        }



        private void OnDrawGizmosSelected()
        {
            if (gizmos == GizmoOptions.SELECTED)
            {
                DrawGizmos();
            }
        }
        private void OnDrawGizmos()
        {
            if (gizmos == GizmoOptions.ALWAYS)
            {
                DrawGizmos();
            }
        }

        private void DrawGizmos()
        {
            if(!wireCellGizmos && !amountVisualizedGizmos)
            {
                return;
            }

            if(layerToRender >= extraLayers.Length + 1)
            {
                return;
            }

            var voxelLayout = this.VoxelLayout;
            var maxAmount = 1f;
            if (nativeVolumeData != null)
            {
                nativeVolumeData.dataReaderDependencies.Complete();
                for (int voxelIndex = 0; voxelIndex < voxelLayout.totalVoxels; voxelIndex++)
                {
                    var layerDataIndex = voxelIndex * voxelLayout.dataLayerCount + layerToRender;
                    var val = nativeVolumeData.openReadData[layerDataIndex];
                    maxAmount = Mathf.Max(val, maxAmount);
                }
            }
            else
            {
                maxAmount = (Vector3.one / 4f).sqrMagnitude;
            }
            var voxelSize = voxelLayout.voxelSize;
            for (int x = 0; x < worldResolution.x; x++)
            {
                for (int y = 0; y < worldResolution.y; y++)
                {
                    for (int z = 0; z < worldResolution.z; z++)
                    {
                        var voxelCoordinate = new Vector3Int(x, y, z);
                        var cubeCenter = voxelLayout.CoordinateToCenterOfVoxel(voxelCoordinate);

                        if (amountVisualizedGizmos)
                        {
                            float amount;
                            if (nativeVolumeData != null)
                            {
                                var layerDataIndex = voxelLayout.GetVoxelIndexFromCoordinates(voxelCoordinate) * voxelLayout.dataLayerCount + layerToRender;
                                amount = nativeVolumeData.openReadData[layerDataIndex] / maxAmount;
                            }
                            else
                            {
                                var xScaled = ((voxelCoordinate.x + 0.5f) / (float)worldResolution.x) - 0.5f;
                                var yScaled = ((voxelCoordinate.y + 0.5f) / (float)worldResolution.y) - 0.5f;
                                var zScaled = ((voxelCoordinate.z + 0.5f) / (float)worldResolution.z) - 0.5f;

                                amount = (maxAmount - new Vector3(xScaled, yScaled, zScaled).sqrMagnitude) / maxAmount;
                            }

                            Gizmos.color = heatmapGradient.Evaluate(amount);
                            Gizmos.DrawCube(cubeCenter, voxelSize * 0.7f);
                        }

                        if (wireCellGizmos)
                        {
                            Gizmos.color = new Color(1, 0, 0, 1);
                            Gizmos.DrawWireCube(cubeCenter, voxelSize);
                        }
                    }
                }
            }
        }
    }
}
