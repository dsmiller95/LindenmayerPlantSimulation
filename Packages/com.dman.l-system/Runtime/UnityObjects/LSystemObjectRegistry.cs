using Dman.ObjectSets;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [CreateAssetMenu(fileName = "LSystemRegistry", menuName = "LSystem/LSystemRegistry")]
    public class LSystemObjectRegistry : UniqueObjectRegistryWithAccess<LSystemObject>
    {
    }
}
