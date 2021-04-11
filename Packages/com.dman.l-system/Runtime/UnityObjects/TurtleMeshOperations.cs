using Dman.MeshDraftExtensions;
using ProceduralToolkit;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [System.Serializable]
    public struct MeshKey
    {
        public char Character;
        public Mesh MeshRef;
        [Tooltip("a list of variants available. If populated, indexed by the first parameter of the symbol")]
        public Mesh[] MeshVariants;
        public Material material;
        public Vector3 IndividualScale;

        [Tooltip("Whether or not to scale the mesh based on an input parameter. Will accept the first parameter, unless mesh variants are used. In which case it will use the second parameter.")]
        public bool ParameterScale;
        public Vector3 ScalePerParameter;

        public bool AlsoMove;
        [Tooltip("When checked, scale the mesh along the non-primary axises based on the thickness state")]
        public bool UseThickness;
    }
    [CreateAssetMenu(fileName = "TurtleMeshOperations", menuName = "LSystem/TurtleMeshOperations")]
    public class TurtleMeshOperations : TurtleOperationSet<TurtleState>
    {
        public MeshKey[] meshKeys;

        public override IEnumerable<ITurtleOperator<TurtleState>> GetOperators()
        {
            foreach (var meshKey in meshKeys)
            {
                var meshes = new[] { meshKey.MeshRef }.Concat(meshKey.MeshVariants)
                    .Select(mesh =>
                    {
                        var newDraft = new MeshDraft(mesh);
                        var bounds = mesh.bounds;
                        newDraft.Move(Vector3.right * (-bounds.center.x + bounds.size.x / 2));
                        newDraft.Scale(meshKey.IndividualScale);

                        var transformPostMesh = meshKey.AlsoMove ?
                              Matrix4x4.Translate(new Vector3(bounds.size.x * meshKey.IndividualScale.x, 0, 0))
                            : Matrix4x4.identity;
                        return (
                        new TurtleMeshTemplate
                        {
                            draft = newDraft,
                            material = meshKey.material
                        }, transformPostMesh);
                    });
                yield return new TurtleMeshOperator(
                    meshKey.Character,
                    meshes.ToArray(),
                    meshKey.ParameterScale,
                    meshKey.ScalePerParameter,
                    meshKey.UseThickness);
            }
        }

        class TurtleMeshOperator : ITurtleOperator<TurtleState>
        {
            private (TurtleMeshTemplate, Matrix4x4)[] generatedMeshes;
            private bool scaling;
            private Vector3 scalePerParameter;
            private bool thickness;
            public char TargetSymbol { get; private set; }
            public TurtleMeshOperator(
                char symbol,
                (TurtleMeshTemplate, Matrix4x4)[] generatedMeshes,
                bool scaling,
                Vector3 scalePerParameter,
                bool thickness)
            {
                TargetSymbol = symbol;
                this.generatedMeshes = generatedMeshes;
                this.scaling = scaling;
                this.scalePerParameter = scalePerParameter;
                this.thickness = thickness;
            }

            public TurtleState Operate(TurtleState initialState, double[] parameters, TurtleMeshInstanceTracker targetDraft)
            {
                var meshTransform = initialState.transformation;

                var selectedMesh = generatedMeshes[0];
                if (generatedMeshes.Length > 1 && parameters.Length > 0)
                {
                    var index = ((int)parameters[0]) % generatedMeshes.Length;
                    selectedMesh = generatedMeshes[index];
                }


                var scaleIndex = generatedMeshes.Length <= 1 ? 0 : 1;
                if (scaling && parameters.Length > scaleIndex)
                {
                    var scale = parameters[scaleIndex];
                    meshTransform *= Matrix4x4.Scale(scalePerParameter * (float)scale);
                }
                if (thickness)
                {
                    meshTransform *= Matrix4x4.Scale(new Vector3(1, initialState.thickness, initialState.thickness));
                }

                var meshId = targetDraft.AddOrGetMeshTemplate(selectedMesh.Item1);
                targetDraft.AddMeshInstance(meshId, meshTransform);

                initialState.transformation *= selectedMesh.Item2;
                return initialState;
            }
        }
    }

}
