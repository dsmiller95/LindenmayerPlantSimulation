using Unity.Entities;
using Unity.Jobs;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class TurtleInterpretingSystem : SystemBase
    {

        public JobHandle allTurtleInterpretors;

        protected override void OnUpdate()
        {
        }

        public void InterpretTurtle(SymbolString<float> symbolString)
        {

        }
    }
}
