using Dman.LSystem.SystemCompiler.Linker;
using Dman.LSystem.SystemRuntime.LSystemEvaluator;
using Dman.ObjectSets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [CreateAssetMenu(fileName = "LSystemRegistry", menuName = "LSystem/LSystemRegistry")]
    public class LSystemObjectRegistry : UniqueObjectRegistryWithAccess<LSystemObject>
    {
    }
}
