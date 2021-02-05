using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    public abstract class TurtleOperationSet<T> : ScriptableObject
    {
        public abstract IEnumerable<ITurtleOperator<T>> GetOperators();
    }
}
