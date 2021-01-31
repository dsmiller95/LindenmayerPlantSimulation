using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem
{
    public interface ITurtleOperator<T>
    {
        char TargetSymbol { get; }
        T Operate(T initialState, double[] parameters, MeshDraft targetDraft);
    }

    public abstract class TurtleOperationSet<T> : ScriptableObject
    {
        public abstract IEnumerable<ITurtleOperator<T>> GetOperators();
    }
}
