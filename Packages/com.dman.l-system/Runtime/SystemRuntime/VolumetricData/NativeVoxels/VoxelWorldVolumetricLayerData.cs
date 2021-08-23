using System;
using System.Collections;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData.NativeVoxels
{
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [DebuggerDisplay("Length = {" + nameof(Length) + "}")]
    [DebuggerTypeProxy(typeof(VoxelWorldVolumetricLayerDataDebugView))]
    public struct VoxelWorldVolumetricLayerData : IDisposable, INativeDisposable
    {
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float> array;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal int m_Length;
        internal int m_MinIndex;
        internal int m_MaxIndex;
        internal AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule] internal DisposeSentinel m_DisposeSentinel;
#endif
        public VolumetricWorldVoxelLayout VoxelLayout => voxelLayout;
        private VolumetricWorldVoxelLayout voxelLayout;

        public VoxelWorldVolumetricLayerData(VolumetricWorldVoxelLayout layout, Allocator allocator)
        {
            voxelLayout = layout;
            array = new NativeArray<float>(layout.totalDataSize, allocator);
            m_Length = array.Length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_MinIndex = 0;
            m_MaxIndex = layout.totalDataSize - 1;
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif
        }

        public int Length
        {
            get
            {
                return m_Length;
            }
        }

        public float this[VoxelIndex index, int layer = 0]
        {
            get
            {
                return array[index.Value * voxelLayout.dataLayerCount + layer];
            }
            set
            {
                array[index.Value * voxelLayout.dataLayerCount + layer] = value;
            }
        }

        public bool IsCreated
        {
            get
            {
                return array.IsCreated;
            }
        }

        public void CopyFrom(VoxelWorldVolumetricLayerData other)
        {
            if (other.voxelLayout.totalDataSize == voxelLayout.totalDataSize)
            {
                this.array.CopyFrom(other.array);
            }
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            array.Dispose();
        }

        internal float[] ToArray()
        {
            return array.ToArray();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class VoxelWorldVolumetricLayerDataDebugView
    {
        private VoxelWorldVolumetricLayerData array;

        public VoxelWorldVolumetricLayerDataDebugView(VoxelWorldVolumetricLayerData array)
        {
            this.array = array;
        }

        public float[] Items
        {
            get
            {
                return array.ToArray();
            }
        }
    }
}