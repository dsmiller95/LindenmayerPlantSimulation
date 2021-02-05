using ProceduralToolkit;

namespace Dman.LSystem
{
    public interface ITurtleOperator<T>
    {
        char TargetSymbol { get; }
        T Operate(T initialState, double[] parameters, MeshDraft targetDraft);
    }

}
