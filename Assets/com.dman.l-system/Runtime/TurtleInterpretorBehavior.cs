using ProceduralToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem
{
    [Serializable]
    public struct MeshKey
    {
        public char Character;
        public Mesh MeshRef;
        public Vector3 IndividualScale;
    }

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
        public MeshKey[] meshKeys;
        public TransformKey[] transformKeys;
        public float secondsPerUpdate;
        public int maxUpdates = 10;
        public Vector3 initialScale = Vector3.one;

        public char meshIndexIncrementor = '`';

        private TurtleInterpretor turtle;
        private int currentUpdates = 0;

        private void Awake()
        {
            var transformDict = transformKeys.ToDictionary(x => (int)x.Character, x => Matrix4x4.TRS(x.translation, Quaternion.Euler(x.eulerRotation), x.scale));
            var draftdict = new Dictionary<int, MeshDraft>();

            foreach (var meshKey in meshKeys)
            {
                var newDraft = new MeshDraft(meshKey.MeshRef);
                var bounds = meshKey.MeshRef.bounds;
                newDraft.Move(Vector3.right * (-bounds.center.x + bounds.size.x / 2));
                newDraft.Scale(meshKey.IndividualScale);

                draftdict[meshKey.Character] = newDraft;
                if (!transformDict.ContainsKey(meshKey.Character))
                {
                    transformDict[meshKey.Character] = Matrix4x4.Translate(
                        new Vector3(bounds.size.x * meshKey.IndividualScale.x, 0, 0));
                }
            }

            turtle = new TurtleInterpretor(draftdict, transformDict, Matrix4x4.Scale(initialScale));
            turtle.meshIndexIncrementChar = meshIndexIncrementor;
        }

        private float lastUpdate;
        private void Update()
        {
            if (currentUpdates < maxUpdates && Time.time > lastUpdate + secondsPerUpdate)
            {
                lastUpdate = Time.time;
                UpdateMeshAndSystem();
                currentUpdates++;
            }
        }

        private void UpdateMeshAndSystem()
        {
            var system = GetComponent<LSystemBehavior>();

            var output = turtle.CompileStringToMesh(system.currentState);
            GetComponent<MeshFilter>().mesh = output;
            system.StepSystem();
        }
    }
}
