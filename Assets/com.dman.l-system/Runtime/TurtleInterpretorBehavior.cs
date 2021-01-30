using ProceduralToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    [Serializable]
    public struct TransformKey
    {
        public char Character;
        public Vector3 eulerRotation;
        public Vector3 translation;
        public Vector3 scale;
    }

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(LSystemBehavior))]
    public class TurtleInterpretorBehavior : MonoBehaviour
    {
        public TurtleOperationSet[] operationSets;

        public float secondsPerUpdate;
        public Vector3 initialScale = Vector3.one;

        public char meshIndexIncrementor = '`';
        public float timeBeforeRestart = 5;

        private TurtleInterpretor turtle;
        private LSystemBehavior system => GetComponent<LSystemBehavior>();

        private void Awake()
        {
            var operatorDictionary = operationSets.SelectMany(x => x.GetOperators()).ToDictionary(x => (int)x.TargetSymbol);

            turtle = new TurtleInterpretor(operatorDictionary, Matrix4x4.Scale(initialScale));
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
            }else if (currentUpdates >= maxUpdates && Time.time > lastUpdate + timeBeforeRestart)
            {
                lastUpdate = Time.time;
                currentUpdates = 2;
                system.Reset();
                if (system.systemValid)
                {
                    system.StepSystem();
                    this.UpdateMeshAndSystem();
                }
            }
        }

        private void TriggerSimulationRestart()
        {
            currentUpdates = system.systemObject.iterations;
        }

        private void UpdateMeshAndSystem()
        {
            if(!system.systemValid)
            {
                this.TriggerSimulationRestart();
                return;
            }
            var output = turtle.CompileStringToMesh(system.currentState);
            GetComponent<MeshFilter>().mesh = output;
            if (!system.StepSystem())
            {
                this.TriggerSimulationRestart();
            }
        }
    }
}
