using Dman.LSystem.SystemRuntime;
using System;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(LSystemBehavior))]
    public class TurtleInterpreterBehavior : MonoBehaviour
    {
        /// <summary>
        /// a set of valid operations, only one operation defintion can be generated per symbol
        /// </summary>
        public TurtleOperationSet<TurtleState>[] operationSets;
        /// <summary>
        /// the begining scale of the turtle's transformation matrix
        /// </summary>
        public Vector3 initialScale = Vector3.one;
        /// <summary>
        /// a character which will increment the index of the current target submesh being copied to
        /// </summary>
        public char submeshIndexIncrementor = '`';

        private TurtleInterpretor<TurtleState> turtle;
        private LSystemBehavior System => GetComponent<LSystemBehavior>();

        /// <summary>
        /// iterate through <paramref name="symbols"/> and assign the generated mesh to the attached meshFilter
        /// </summary>
        /// <param name="symbols"></param>
        public void InterpretSymbols(SymbolString<double> symbols)
        {
            var meshfilter = GetComponent<MeshFilter>();
            var targetMesh = meshfilter.mesh;

            var meshRenderer = GetComponent<MeshRenderer>();
            

            // Ref is unecessary in the backing API here, which is why we're not re-assigning back from it here
            turtle.CompileStringToMesh(symbols, ref targetMesh, meshRenderer.materials.Length);
        }

        private void Awake()
        {
            var operatorDictionary = operationSets.SelectMany(x => x.GetOperators()).ToDictionary(x => (int)x.TargetSymbol);

            turtle = new TurtleInterpretor<TurtleState>(
                operatorDictionary,
                new TurtleState
                {
                    transformation = Matrix4x4.Scale(initialScale)
                });
            turtle.submeshIndexIncrementChar = submeshIndexIncrementor;

            if(System != null)
            {
                System.OnSystemStateUpdated += OnSystemStateUpdated;
            }
        }

        private void OnDestroy()
        {
            if (System != null)
            {
                System.OnSystemStateUpdated += OnSystemStateUpdated;
            }
        }

        private void OnSystemStateUpdated()
        {
            if (System != null)
            {
                this.InterpretSymbols(System.CurrentState);
            }
        }

    }
}
