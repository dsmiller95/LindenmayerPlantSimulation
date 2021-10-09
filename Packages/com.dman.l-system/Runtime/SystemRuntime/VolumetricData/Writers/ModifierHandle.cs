using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Collections;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    public interface ModifierHandle : INativeDisposable
    {
        public bool IsDisposed { get; }

        /// <summary>
        /// Plays back any changes that have been cached inside this modification handle
        /// </summary>
        /// <param name="layerData">the layer data to apply changes to</param>
        /// <param name="dependency">job handle</param>
        /// <returns>true if any changes were available to be applied, false otherwise</returns>
        bool ConsolidateChanges(VoxelWorldVolumetricLayerData layerData, ref JobHandleWrapper dependency);

        /// <summary>
        /// remove the effect which this writable handle had on the world
        ///     for a double buffered handle, this means subtracting the total sum which this
        ///     handle has contributed to the volume world
        /// </summary>
        void RemoveEffects(VoxelWorldVolumetricLayerData layerData, ref JobHandleWrapper dependency);
    }
}
