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

        internal int m_Length;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal int m_MinIndex;
        internal int m_MaxIndex;
        internal AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule] internal DisposeSentinel m_DisposeSentinel;
#endif
        public VolumetricWorldVoxelLayout VoxelLayout => voxelLayout;
        private VolumetricWorldVoxelLayout voxelLayout;

        public VoxelWorldVolumetricLayerData(VolumetricWorldVoxelLayout layout, Allocator allocator) : this(layout, new NativeArray<float>(layout.totalDataSize, allocator), allocator)
        {
        }

        public VoxelWorldVolumetricLayerData(Serializable serializedData, Allocator allocator) : this (serializedData.layout, new NativeArray<float>(serializedData.data, allocator), allocator)
        {
        }

        private VoxelWorldVolumetricLayerData(VolumetricWorldVoxelLayout layout, NativeArray<float> data, Allocator allocator)
        {
            voxelLayout = layout;
            array = data;
            m_Length = array.Length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_MinIndex = 0;
            m_MaxIndex = layout.totalDataSize - 1;
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
#endif
        }

        public class Serializable
        {
            public VolumetricWorldVoxelLayout layout;
            public float[] data;
        }

        public int Length
        {
            get
            {
                return m_Length;
            }
        }

        public float this[VoxelIndex index, int layer]
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Clear(ref m_DisposeSentinel);
            AtomicSafetyHandle.Release(m_Safety);
#endif
            return array.Dispose(inputDeps);
        }
        public ReadOnly AsReadOnly()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new ReadOnly(array.AsReadOnly(), m_Length, ref m_Safety, this.voxelLayout);
#else
            return new ReadOnly(array.AsReadOnly(), this.voxelLayout);
#endif
        }

        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct ReadOnly
        {
            [NativeDisableContainerSafetyRestriction]
            private NativeArray<float>.ReadOnly array;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal int m_Length;
            internal AtomicSafetyHandle m_Safety;
#endif
            public VolumetricWorldVoxelLayout VoxelLayout => voxelLayout;
            private VolumetricWorldVoxelLayout voxelLayout;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal ReadOnly(NativeArray<float>.ReadOnly array, int length, ref AtomicSafetyHandle safety, VolumetricWorldVoxelLayout voxelLayout)
            {
                m_Length = length;
                m_Safety = safety;
#else
            internal ReadOnly(NativeArray<float>.ReadOnly array, VolumetricWorldVoxelLayout voxelLayout)
            {
#endif
                this.array = array;
                this.voxelLayout = voxelLayout;
            }

            public float this[VoxelIndex index, int layer]
            {
                get
                {
                    return array[index.Value * voxelLayout.dataLayerCount + layer];
                }
            }
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