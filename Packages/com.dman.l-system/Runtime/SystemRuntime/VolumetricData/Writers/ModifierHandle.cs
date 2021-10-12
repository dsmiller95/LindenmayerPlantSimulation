using Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels;
using Unity.Collections;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    public abstract class ModifierHandle : INativeDisposable
    {
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Plays back any changes that have been cached inside this modification handle
        /// </summary>
        /// <param name="layerData">the layer data to apply changes to</param>
        /// <param name="dependency">job handle</param>
        /// <returns>true if any changes were available to be applied, false otherwise</returns>
        public abstract bool ConsolidateChanges(VoxelWorldVolumetricLayerData layerData, ref JobHandleWrapper dependency);

        /// <summary>
        /// remove any persistent effects this handle has on the world. <br/>
        ///     For a double buffered handle, this means subtracting the total sum which this
        ///     handle has contributed to the volume world
        /// </summary>
        public abstract void RemoveEffects(VoxelWorldVolumetricLayerData layerData, ref JobHandleWrapper dependency);

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (IsDisposed)
            {
                return inputDeps;
            }
            IsDisposed = true;
            return InternalDispose(inputDeps);
        }
        public void Dispose()
        {
            this.Dispose(default).Complete();
        }

        protected abstract JobHandle InternalDispose(JobHandle deps);
    }
}
