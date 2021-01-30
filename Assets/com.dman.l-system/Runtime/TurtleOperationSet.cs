using Dman.LSystem.SystemRuntime;
using ProceduralToolkit;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    public interface ITurtleOperator
    {
        char TargetSymbol { get; }
        TurtleState Operate(TurtleState initialState, double[] parameters, MeshDraft targetDraft);
    }

    public abstract class TurtleOperationSet : ScriptableObject
    {
        public abstract IEnumerable<ITurtleOperator> GetOperators();
    }
}
