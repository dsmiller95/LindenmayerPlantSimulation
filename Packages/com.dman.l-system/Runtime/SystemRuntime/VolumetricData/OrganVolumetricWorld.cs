using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
using Dman.LSystem.SystemRuntime.VolumetricData.Layers;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
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
        [Range(0.01f, 10f)]
        public float minValue = 0.01f;
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

        public NativeDelayedReadable<VoxelWorldVolumetricLayerData> NativeVolumeData { get; private set; }
        private List<VolumetricWorldModifierHandle> writableHandles;

        public VolumetricWorldModifierHandle GetNewWritableHandle()
        {
            var writableHandle = new VolumetricWorldModifierHandle(VoxelLayout, this);
            writableHandles.Add(writableHandle);
            return writableHandle;
        }

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
            NativeVolumeData = new NativeDelayedReadable<VoxelWorldVolumetricLayerData>(
                new VoxelWorldVolumetricLayerData(VoxelLayout, Allocator.Persistent),
                new VoxelWorldVolumetricLayerData(VoxelLayout, Allocator.Persistent)
                );
            foreach (var layer in extraLayers)
            {
                layer.SetupInternalData();
            }
        }

        private void Start()
        {
        }

        private bool anyChangeLastFrame = false;
        private void Update()
        {
            if (anyChangeLastFrame)
            {
                NativeVolumeData.CompleteAllDependencies();
                NativeVolumeData.openReadData.CopyFrom(NativeVolumeData.data);

                volumeWorldChanged?.Invoke();
            }
            var dependency = this.NativeVolumeData.dataWriterDependencies;

            anyChangeLastFrame = false;
            var voxelLayout = this.VoxelLayout;
            // consolidate write handle's changes
            foreach (var writeHandle in writableHandles)
            {
                if (writeHandle.newDataIsAvailable && writeHandle.writeDependency.IsCompleted)
                {
                    writeHandle.writeDependency.Complete();
                    var consolidationJob = new VoxelMarkerConsolidation
                    {
                        allBaseMarkers = this.NativeVolumeData.data,
                        oldMarkerLevels = writeHandle.oldDurability,
                        newMarkerLevels = writeHandle.newDurability,
                        markerLayerIndex = 0,
                    };
                    dependency = consolidationJob.Schedule(writeHandle.newDurability.Length, 1000, dependency);

                    var commandPlaybackJob = new LayerModificationCommandPlaybackJob
                    {
                        commands = writeHandle.modificationCommands,
                        dataArray = this.NativeVolumeData.data,
                        voxelLayout = voxelLayout
                    };
                    dependency = commandPlaybackJob.Schedule(dependency);

                    writeHandle.RegisterReadDependency(dependency);
                    writeHandle.newDataIsAvailable = false;
                    anyChangeLastFrame = true;
                }
            }

            // apply resource layer updates (EX diffusion)
            foreach (var layer in extraLayers)
            {
                dependency = layer.ApplyLayerWideUpdate(this.NativeVolumeData.data, Time.deltaTime, dependency);
            }

            this.NativeVolumeData.RegisterWritingDependency(dependency);
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
            NativeVolumeData.Dispose();
            NativeVolumeData = null;
        }

        private JobHandle DisposeWritableHandleNoRemove(VolumetricWorldModifierHandle handle)
        {
            var deps = JobHandle.CombineDependencies(handle.writeDependency, NativeVolumeData.dataWriterDependencies);
            var subtractCleanupJob = new NativeArraySubtractNegativeProtectionJob
            {
                allBaseMarkers = this.NativeVolumeData.data,
                markerLevelsToRemove = handle.newDurability,
                markerLayerIndex = 0,
                totalLayersInBase = VoxelLayout.dataLayerCount
            };
            deps = subtractCleanupJob.Schedule(VoxelLayout.totalVoxels, 1000, deps);
            NativeVolumeData.RegisterWritingDependency(deps);

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
            var maxAmount = minValue; // to help prevent lag when values are low
            if (NativeVolumeData != null)
            {
                NativeVolumeData.dataReaderDependencies.Complete();
                for (VoxelIndex voxelIndex = default; voxelIndex.Value < voxelLayout.totalVoxels; voxelIndex.Value++)
                {
                    var val = NativeVolumeData.openReadData[voxelIndex, layerToRender];
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
                            if (NativeVolumeData != null)
                            {
                                var voxelIndex = voxelLayout.GetVoxelIndexFromCoordinates(voxelCoordinate);
                                amount = NativeVolumeData.openReadData[voxelIndex, layerToRender] / maxAmount;
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
