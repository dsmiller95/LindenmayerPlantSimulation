using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

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
