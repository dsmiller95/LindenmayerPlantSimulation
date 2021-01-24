using Dman.MeshDraftExtensions;
using ProceduralToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Dman.LSystem
{
    public class TurtleInterpretor
    {
        private IDictionary<int, MeshDraft> draftsByKey;
        /// <summary>
        /// keys to use to apply transforms to the turtle's position
        ///     if a key is also contained in the drafts dictionary, the transformation is
        ///     applied after the draft is placed.
        /// </summary>
        private IDictionary<int, Matrix4x4> transformationByKey;
        public int branchStartChar = '[';
        public int branchEndChar = ']';
        private Matrix4x4 rootTransform;

        public TurtleInterpretor(IDictionary<int, MeshDraft> draftsByKey, IDictionary<int, Matrix4x4> transformationsByKey) : this(draftsByKey, transformationsByKey, Matrix4x4.identity) { }

        public TurtleInterpretor(IDictionary<int, MeshDraft> draftsByKey, IDictionary<int, Matrix4x4> transformationsByKey, Matrix4x4 rootTransform)
        {
            this.draftsByKey = draftsByKey;
            this.transformationByKey = transformationsByKey;
            this.rootTransform = rootTransform;
        }

        public MeshDraft CompileStringToMesh(SymbolString symbols)
        {
            var resultMesh = new MeshDraft();
            var currentTransform = rootTransform;

            var stateStack = new Stack<Matrix4x4>();

            for (int symbolIndex = 0; symbolIndex < symbols.symbols.Length; symbolIndex++)
            {
                var symbol = symbols.symbols[symbolIndex];
                if(symbol == branchStartChar)
                {
                    stateStack.Push(currentTransform);
                    continue;
                }
                if(symbol == branchEndChar)
                {
                    currentTransform = stateStack.Pop();
                    continue;
                }
                if(draftsByKey.TryGetValue(symbol, out var newDraft))
                {
                    resultMesh.AddWithTransform(newDraft, currentTransform);
                }
                if (transformationByKey.TryGetValue(symbol, out var transform))
                {
                    currentTransform *= transform;
                }
            }

            return resultMesh;
        }
    }
}
