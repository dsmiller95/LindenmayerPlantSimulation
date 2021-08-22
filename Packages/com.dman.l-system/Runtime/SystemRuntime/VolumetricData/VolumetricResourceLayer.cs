using Dman.ObjectSets;
using System.Collections;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.VolumetricData
{
    [CreateAssetMenu(fileName = "VolumetricResourceLayer", menuName = "LSystem/VolumetricResourceLayer")]
    public class VolumetricResourceLayer : ScriptableObject
    {
        public int voxelLayerId;
        public string description;
    }
}