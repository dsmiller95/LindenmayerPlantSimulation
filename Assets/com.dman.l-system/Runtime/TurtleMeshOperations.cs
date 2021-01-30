using Dman.MeshDraftExtensions;
using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;

namespace Dman.LSystem
{
    [System.Serializable]
    public struct MeshKey
    {
        public char Character;
        public Mesh MeshRef;
        public Vector3 IndividualScale;
        public bool AlsoMove;
    }
    [CreateAssetMenu(fileName = "TurtleMeshOperations", menuName = "LSystem/TurtleMeshOperations")]
    public class TurtleMeshOperations : TurtleOperationSet
    {
        public MeshKey[] meshKeys;

        public override IEnumerable<ITurtleOperator> GetOperators()
        {
            foreach (var meshKey in meshKeys)
            {
                var newDraft = new MeshDraft(meshKey.MeshRef);
                var bounds = meshKey.MeshRef.bounds;
                newDraft.Move(Vector3.right * (-bounds.center.x + bounds.size.x / 2));
                newDraft.Scale(meshKey.IndividualScale);

                var transformPostMesh = meshKey.AlsoMove ? 
                      Matrix4x4.Translate(new Vector3(bounds.size.x * meshKey.IndividualScale.x, 0, 0))
                    : Matrix4x4.identity;
                yield return new TurtleMeshOperator(meshKey.Character, transformPostMesh, newDraft);
            }
        }

        class TurtleMeshOperator : ITurtleOperator
        {
            private MeshDraft generatedMesh;
            private Matrix4x4 transformPostMesh;
            public char TargetSymbol { get; private set; }
            public TurtleMeshOperator(char symbol, Matrix4x4 transform, MeshDraft generatedMesh)
            {
                TargetSymbol = symbol;
                transformPostMesh = transform;
                this.generatedMesh = generatedMesh;
            }

            public TurtleState Operate(TurtleState initialState, double[] parameters, MeshDraft targetDraft)
            {
                targetDraft.AddWithTransform(generatedMesh, initialState.transformation);
                initialState.transformation *= transformPostMesh;
                return initialState;
            }
        }
    }

}
