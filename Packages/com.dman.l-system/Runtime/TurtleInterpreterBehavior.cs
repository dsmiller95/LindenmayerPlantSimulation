using System;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(LSystemBehavior))]
    public class TurtleInterpreterBehavior : MonoBehaviour
    {
        public TurtleOperationSet<TurtleState>[] operationSets;

        public float secondsPerUpdate;
        public Vector3 initialScale = Vector3.one;

        public char meshIndexIncrementor = '`';
        public float timeBeforeRestart = 5;

        private TurtleInterpretor<TurtleState> turtle;
        private LSystemBehavior system => GetComponent<LSystemBehavior>();

        private void Awake()
        {
            var operatorDictionary = operationSets.SelectMany(x => x.GetOperators()).ToDictionary(x => (int)x.TargetSymbol);

            turtle = new TurtleInterpretor<TurtleState>(
                operatorDictionary,
                new TurtleState
                {
                    transformation = Matrix4x4.Scale(initialScale)
                });
            turtle.meshIndexIncrementChar = meshIndexIncrementor;
        }

        private float lastUpdate;
        private int currentUpdates = 0;
        private void Update()
        {
            var maxUpdates = system.systemObject.iterations;
            if (currentUpdates < maxUpdates && Time.time > lastUpdate + secondsPerUpdate)
            {
                lastUpdate = Time.time;
                UpdateMeshAndSystem();
                currentUpdates++;
            }
            else if (currentUpdates >= maxUpdates && Time.time > lastUpdate + timeBeforeRestart)
            {
                lastUpdate = Time.time;
                currentUpdates = 2;
                system.Reset();
                if (system.systemValid)
                {
                    system.StepSystem();
                    UpdateMeshAndSystem();
                }
            }
        }

        private void TriggerSimulationRestart()
        {
            currentUpdates = system.systemObject.iterations;
        }

        private void UpdateMeshAndSystem()
        {
            if (!system.systemValid)
            {
                TriggerSimulationRestart();
                return;
            }
            var output = turtle.CompileStringToMesh(system.currentState);
            GetComponent<MeshFilter>().mesh = output;
            if (!system.StepSystem())
            {
                TriggerSimulationRestart();
            }
        }
    }
}
