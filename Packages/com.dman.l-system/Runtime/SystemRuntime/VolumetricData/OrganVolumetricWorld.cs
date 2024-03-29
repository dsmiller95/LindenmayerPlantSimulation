﻿using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.VolumetricData.Layers;
using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Dman.SceneSaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
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

    [RequireComponent(typeof(VoxelVolumeDefinition))]
    public class OrganVolumetricWorld : MonoBehaviour, ISaveableData
    {

        private VoxelVolumeDefinition _volumeDefinition;
        public VoxelVolumeDefinition VolumeDefinition
        {
            get
            {
                if (_volumeDefinition == null)
                {
                    _volumeDefinition = GetComponent<VoxelVolumeDefinition>();
                }
                return _volumeDefinition;
            }
        }

        public Gradient heatmapGradient;

        public GizmoOptions gizmos = GizmoOptions.SELECTED;
        public int layerToRender = 0;
        [Range(0.01f, 100f)]
        public float minValue = 0.01f;

        public VolumetricResourceLayer damageLayer;

        public VolumetricResourceLayer[] extraLayers;

        /// <summary>
        /// compiles all resource layers together into a single list.
        ///     currently excludes the "durability" layer as a special case
        /// </summary>
        private VolumetricResourceLayer[] AllLayers
        {
            get
            {
                if (_allLayers == null)
                {
                    SetAllLayers();
                }
                return _allLayers;
            }
        }
        private VolumetricResourceLayer[] _allLayers;

        private void SetAllLayers()
        {
            _allLayers = extraLayers
                .Append(damageLayer)
                .Where(x => x != null)
                .Distinct()
                .ToArray();
        }

        public VolumetricWorldVoxelLayout VoxelLayout => new VolumetricWorldVoxelLayout
        {
            volume = VolumeDefinition.Volume,
            dataLayerCount = AllLayers.Length + 1
        };

        public event Action volumeWorldChanged;

        public NativeDelayedReadable<VoxelWorldVolumetricLayerData> NativeVolumeData { get; private set; }
        private List<ModifierHandle> WritableHandles
        {
            get
            {
                if (_writableHandles == null)
                {
                    _writableHandles = new List<ModifierHandle>();
                }
                return _writableHandles;
            }
        }
        private List<ModifierHandle> _writableHandles;

        public DoubleBufferModifierHandle GetDoubleBufferedWritableHandle(int layerIndex = 0)
        {
            var writableHandle = new DoubleBufferModifierHandle(VoxelLayout.volume, layerIndex);
            WritableHandles.Add(writableHandle);
            return writableHandle;
        }
        public CommandBufferModifierHandle GetCommandBufferWritableHandle()
        {
            var writableHandle = new CommandBufferModifierHandle(VoxelLayout.volume);
            WritableHandles.Add(writableHandle);
            return writableHandle;
        }

        public JobHandle DisposeWritableHandle(ModifierHandle handle)
        {
            if (handle.IsDisposed)
            {
                return default;
            }
            WritableHandles.Remove(handle);

            var layerData = this.NativeVolumeData.data;
            JobHandleWrapper dependency = NativeVolumeData.dataWriterDependencies;
            handle.RemoveEffects(layerData, ref dependency);
            return handle.Dispose(dependency);
        }

        public bool HasResourceLayer(VolumetricResourceLayer layer)
        {
            return _allLayers.Contains(layer);
        }

        private void Awake()
        {
            NativeVolumeData = new NativeDelayedReadable<VoxelWorldVolumetricLayerData>(
                new VoxelWorldVolumetricLayerData(VoxelLayout, Allocator.Persistent),
                new VoxelWorldVolumetricLayerData(VoxelLayout, Allocator.Persistent)
                );

            // damage world should have a cap reached effect
            if (damageLayer == null)
            {
                //Debug.LogWarning("No damage layer specified in the volumetric world, no damage effects can happen");
            }
            else
            {
                var damageCapEffect = damageLayer.effects.OfType<VoxelCapReachedTimestampEffect>().FirstOrDefault();
                if (damageCapEffect == null)
                {
                    Debug.LogError("No VoxelCapReachedTimestampEffect detected inside the damage layer. this is required to detect when a voxel is damaged to the point of breaking");
                }
            }
            SetAllLayers();
            var layerId = 1;
            foreach (var layer in AllLayers)
            {
                layer.SetupInternalData(VolumeDefinition.Volume, layerId);
                layerId++;
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
            JobHandleWrapper dependency = this.NativeVolumeData.dataWriterDependencies;

            anyChangeLastFrame = false;
            // consolidate write handle's changes
            foreach (var writeHandle in WritableHandles)
            {
                var layerData = this.NativeVolumeData.data;
                if (writeHandle.ConsolidateChanges(layerData, ref dependency))
                {
                    anyChangeLastFrame = true;
                }
            }

            // apply resource layer updates (EX diffusion)
            foreach (var layer in AllLayers)
            {
                if (layer.ApplyLayerWideUpdate(this.NativeVolumeData.data, Time.deltaTime, ref dependency))
                {
                    anyChangeLastFrame = true;
                }
            }

            this.NativeVolumeData.RegisterWritingDependency(dependency);
        }

        private void LateUpdate()
        {
        }

        private void OnDestroy()
        {
            JobHandleWrapper dependency = NativeVolumeData.dataWriterDependencies;
            var disposeDependency = default(JobHandleWrapper);
            var layerData = this.NativeVolumeData.data;
            foreach (var handle in WritableHandles)
            {
                handle.RemoveEffects(layerData, ref dependency);
                disposeDependency += handle.Dispose(dependency);
            }
            WritableHandles.Clear();
            (disposeDependency + dependency).Handle.Complete();

            NativeVolumeData.Dispose();
            NativeVolumeData = null;

            var volume = VolumeDefinition.Volume;
            foreach (var layer in AllLayers)
            {
                layer.CleanupInternalData(volume);
            }
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
            if (layerToRender >= AllLayers.Length + 1)
            {
                return;
            }

            var voxelVolume = this.VolumeDefinition.Volume;
            var maxAmount = minValue; // to help prevent lag when values are low
            if (NativeVolumeData != null)
            {
                NativeVolumeData.dataReaderDependencies.Complete();
                for (VoxelIndex voxelIndex = default; voxelIndex.Value < voxelVolume.totalVoxels; voxelIndex.Value++)
                {
                    var val = NativeVolumeData.openReadData[voxelIndex, layerToRender];
                    maxAmount = Mathf.Max(val, maxAmount);
                }
            }
            else
            {
                maxAmount = (Vector3.one / 4f).sqrMagnitude;
            }
            for (int x = 0; x < voxelVolume.worldResolution.x; x++)
            {
                for (int y = 0; y < voxelVolume.worldResolution.y; y++)
                {
                    for (int z = 0; z < voxelVolume.worldResolution.z; z++)
                    {
                        var voxelCoordinate = new Vector3Int(x, y, z);
                        var cubeCenter = voxelVolume.GetWorldPositionFromVoxelCoordinates(voxelCoordinate);

                        float amount;
                        if (NativeVolumeData != null)
                        {
                            var voxelIndex = voxelVolume.GetVoxelIndexFromVoxelCoordinates(voxelCoordinate);
                            amount = NativeVolumeData.openReadData[voxelIndex, layerToRender] / maxAmount;
                        }
                        else
                        {
                            var xScaled = ((voxelCoordinate.x + 0.5f) / (float)voxelVolume.worldResolution.x) - 0.5f;
                            var yScaled = ((voxelCoordinate.y + 0.5f) / (float)voxelVolume.worldResolution.y) - 0.5f;
                            var zScaled = ((voxelCoordinate.z + 0.5f) / (float)voxelVolume.worldResolution.z) - 0.5f;

                            amount = (maxAmount - new Vector3(xScaled, yScaled, zScaled).sqrMagnitude) / maxAmount;
                        }

                        Gizmos.color = heatmapGradient.Evaluate(amount);
                        Gizmos.DrawCube(cubeCenter, voxelVolume.voxelSize * 0.7f);
                    }
                }
            }
        }

        #region Saveable
        [System.Serializable]
        class VolumetricWorldSaveObject
        {
            VoxelWorldVolumetricLayerData.Serializable worldData;

            public VolumetricWorldSaveObject(OrganVolumetricWorld source)
            {
                source.NativeVolumeData.CompleteAllDependencies();
                using var dataToSave = new VoxelWorldVolumetricLayerData(source.NativeVolumeData.openReadData, Allocator.TempJob);
                var dep = default(JobHandleWrapper);

                foreach (var handle in source.WritableHandles)
                {
                    handle.RemoveEffects(dataToSave, ref dep);
                }
                dep.Handle.Complete();

                worldData = dataToSave.AsSerializable();
            }

            public void Apply(OrganVolumetricWorld target)
            {
                target.NativeVolumeData.CompleteAllDependencies();
                using var loadedData = new VoxelWorldVolumetricLayerData(worldData, Allocator.TempJob);
                var disposeDependency = default(JobHandleWrapper);

                foreach (var handle in target.WritableHandles)
                {
                    disposeDependency += handle.Dispose(default);
                }
                disposeDependency.Handle.Complete();

                target.WritableHandles.Clear();

                target.NativeVolumeData.data.CopyFrom(loadedData);
                target.NativeVolumeData.openReadData.CopyFrom(loadedData);
            }
        }

        public string UniqueSaveIdentifier => "VolumetricWorld";
        public int LoadOrderPriority => -5;
        public object GetSaveObject()
        {
            return new VolumetricWorldSaveObject(this);
        }

        public void SetupFromSaveObject(object save)
        {
            if (save is VolumetricWorldSaveObject saveObj)
            {
                saveObj.Apply(this);
            }
        }

        #endregion
    }
}
