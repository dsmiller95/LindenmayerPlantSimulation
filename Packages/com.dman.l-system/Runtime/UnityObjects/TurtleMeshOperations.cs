using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.Turtle;
using ProceduralToolkit;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [System.Serializable]
    public class MeshKey
    {
        public char Character;
        public Mesh MeshRef;
        [Tooltip("a list of variants available. If populated, indexed by the first parameter of the symbol")]
        public Mesh[] MeshVariants;
        public Material material;
        public Vector3 IndividualScale;

        [Tooltip("Whether or not to scale the mesh based on an input parameter. Will accept the first parameter, unless mesh variants are used. In which case it will use the second parameter.")]
        public bool ParameterScale;
        [Tooltip("If set to true, will scale based on the parameter as if it were a definition of additional volume. It does this by taking a cube root")]
        public bool VolumetricScale = false;
        public Vector3 ScalePerParameter;

        public bool AlsoMove;
        [Tooltip("When checked, scale the mesh along the non-primary axises based on the thickness state")]
        public bool UseThickness;

        public TurtleDataRequirements RequiredDataSpace => CachedOrganTemplates.Select(x => x.DataReqs).Sum();

        public TurtleOrganTemplate[] CachedOrganTemplates;
        public void InteralCacheOrganTemplates()
        {
            CachedOrganTemplates = (new[] { MeshRef }.Concat(MeshVariants)).Select(mesh =>
            {
                var newDraft = new MeshDraft(mesh);
                var bounds = mesh.bounds;
                newDraft.Move(Vector3.right * (-bounds.center.x + bounds.size.x / 2));
                newDraft.Scale(IndividualScale);

                var transformPostMesh = AlsoMove ?
                        Matrix4x4.Translate(new Vector3(bounds.size.x * IndividualScale.x, 0, 0))
                    : Matrix4x4.identity;
                return new TurtleOrganTemplate(
                    newDraft,
                    material,
                    transformPostMesh
                    );
            }).ToArray();
        }
    }
    [CreateAssetMenu(fileName = "TurtleMeshOperations", menuName = "LSystem/TurtleMeshOperations")]
    public class TurtleMeshOperations : TurtleOperationSet
    {
        public MeshKey[] meshKeys;

        public override TurtleDataRequirements DataReqs => meshKeys.Select(x => x.RequiredDataSpace).Sum();

        public override void InternalCacheOperations()
        {
            foreach (var meshKey in meshKeys)
            {
                meshKey.InteralCacheOrganTemplates();
            }
        }

        public override void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {
            foreach (var meshKey in meshKeys)
            {
                var organIndexes = new JaggedIndexing
                {
                    index = writer.indexInOrganTemplates
                };
                foreach (var templateWriter in meshKey.CachedOrganTemplates)
                {
                    templateWriter.WriteIntoNativeData(nativeData, writer);
                }

                organIndexes.length = (ushort)(writer.indexInOrganTemplates - organIndexes.index);

                writer.operators.Add(new TurtleOperationWithCharacter
                {
                    characterInRootFile = meshKey.Character,
                    operation = new TurtleOperation
                    {
                        operationType = TurtleOperationType.ADD_ORGAN,
                        meshOperation = new TurtleMeshOperation
                        {
                            extraNonUniformScaleForOrgan = meshKey.ScalePerParameter,
                            isVolumetricScale = meshKey.VolumetricScale,
                            doScaleMesh = meshKey.ParameterScale,
                            doApplyThiccness = meshKey.UseThickness,
                            organTemplateVariants = organIndexes
                        }
                    }
                });
            }
        }
    }
}
