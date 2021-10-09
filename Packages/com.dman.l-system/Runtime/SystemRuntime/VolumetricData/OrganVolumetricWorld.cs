using Dman.LSystem.SystemRuntime.NativeCollections.NativeVolumetricSpace;
using Dman.LSystem.SystemRuntime.ThreadBouncer;
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

    public class OrganVolumetricWorld : MonoBehaviour, ISaveableData
    {
        public Vector3 voxelOrigin => transform.position;
        public Vector3 worldSize;

        public Vector3Int worldResolution;

        public Gradient heatmapGradient;

        public GizmoOptions gizmos = GizmoOptions.SELECTED;
        public int layerToRender = 0;
        [Range(0.01f, 100f)]
        public float minValue = 0.01f;
        public bool wireCellGizmos = false;
        public bool amountVisualizedGizmos = true;

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
            voxelOrigin = voxelOrigin,
            worldSize = worldSize,
            worldResolution = worldResolution,
            dataLayerCount = AllLayers.Length + 1
        };

        public event Action volumeWorldChanged;

        public NativeDelayedReadable<VoxelWorldVolumetricLayerData> NativeVolumeData { get; private set; }
        private List<ModifierHandle> writableHandles;

        public DoubleBufferModifierHandle GetDoubleBufferedWritableHandle(int layerIndex = 0)
        {
            var writableHandle = new DoubleBufferModifierHandle(VoxelLayout, layerIndex);
            writableHandles.Add(writableHandle);
            return writableHandle;
        }
        public CommandBufferModifierHandle GetCommandBufferWritableHandle()
        {
            var writableHandle = new CommandBufferModifierHandle(VoxelLayout);
            writableHandles.Add(writableHandle);
            return writableHandle;
        }

        public JobHandle DisposeWritableHandle(ModifierHandle handle)
        {
            if (handle.IsDisposed)
            {
                return default;
            }
            writableHandles.Remove(handle);

            var layerData = this.NativeVolumeData.data;
            JobHandleWrapper dependency = NativeVolumeData.dataWriterDependencies;
            handle.RemoveEffects(layerData, ref dependency);
            return handle.Dispose(dependency);
        }

        private void Awake()
        {
            writableHandles = new List<ModifierHandle>();
            NativeVolumeData = new NativeDelayedReadable<VoxelWorldVolumetricLayerData>(
                new VoxelWorldVolumetricLayerData(VoxelLayout, Allocator.Persistent),
                new VoxelWorldVolumetricLayerData(VoxelLayout, Allocator.Persistent)
                );
            var layout = VoxelLayout;

            // damage world should have a cap reached effect
            if (damageLayer == null)
            {
                Debug.LogWarning("No damage layer specified in the volumetric world, no damage effects can happen");
            } else {
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
                layer.SetupInternalData(layout, layerId);
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
            foreach (var writeHandle in writableHandles)
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
                if(layer.ApplyLayerWideUpdate(this.NativeVolumeData.data, Time.deltaTime, ref dependency))
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
            foreach (var handle in writableHandles)
            {
                handle.RemoveEffects(layerData, ref dependency);
                disposeDependency += handle.Dispose(dependency);
            }
            writableHandles.Clear();
            (disposeDependency + dependency).Handle.Complete();

            NativeVolumeData.Dispose();
            NativeVolumeData = null;

            var layout = VoxelLayout;
            foreach (var layer in AllLayers)
            {
                layer.CleanupInternalData(layout);
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
            if(!wireCellGizmos && !amountVisualizedGizmos)
            {
                return;
            }

            if(layerToRender >= AllLayers.Length + 1)
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

        #region Saveable
        [System.Serializable]
        class VolumetricWorldSaveObject
        {
            public VolumetricWorldSaveObject(OrganVolumetricWorld source)
            {
                source.NativeVolumeData.CompleteAllDependencies();
                var dataToSave = source.NativeVolumeData.openReadData;

            }

            public void Apply(OrganVolumetricWorld target)
            {
            }
        }

        public string UniqueSaveIdentifier => "VolumetricWorld";
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

        public ISaveableData[] GetDependencies()
        {
            return new ISaveableData[0];
        }

        #endregion
    }
}
