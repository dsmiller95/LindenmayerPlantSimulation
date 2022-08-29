using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.Turtle;
using Dman.LSystem.SystemRuntime.VolumetricData;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [System.Serializable]
    public class MeshVariant
    {
        public Mesh mesh;
        public Material materialOverride;
    }

    [System.Serializable]
    public class MeshKey : ITurtleNativeDataWritable
    {
        public char Character;
        public Mesh MeshRef;
        [Tooltip("a list of variants available. If populated, indexed by the first parameter of the symbol")]
        public MeshVariant[] MeshVariants;
        public Material material;
        public Vector3 IndividualScale;
        [Tooltip("Force using the mesh's intrinsic origin as the root of this organ, instead of automatically shifting the origin based on the bounding box")]
        public bool UseMeshOrigin = false;

        [Tooltip("Whether or not to scale the mesh based on an input parameter. Will accept the first parameter, unless mesh variants are used. In which case it will use the second parameter.")]
        public bool ParameterScale;
        public bool ScaleIsAdditional;
        [Tooltip("If set to true, will scale based on the parameter as if it were a definition of additional volume. It does this by taking a cube root")]
        public bool VolumetricScale = false;
        public Vector3 ScalePerParameter;

        public bool AlsoMove;
        [Tooltip("When checked, scale the mesh along the non-primary axises based on the thickness state")]
        public bool UseThickness;

        public float volumetricDurabilityValue;

        /// <summary>
        /// usable to reconfigure this operation to only move the turtle by how much it would be moved
        ///     by this organ. but; it will generate no mesh vertexes
        /// </summary>
        public bool useAsDummyMesh = false;

        public TurtleDataRequirements DataReqs => CachedOrganTemplates.Select(x => x.DataReqs).Sum();

        public TurtleOrganTemplate[] CachedOrganTemplates;
        public void InteralCacheOrganTemplates()
        {
            CachedOrganTemplates = (new[] {
                new MeshVariant{
                }
            }.Concat(MeshVariants)).Select(variant =>
            {
                var overridenMesh = (variant.mesh != null) ? variant.mesh : MeshRef;
                var overridenMaterial = (variant.materialOverride != null) ? variant.materialOverride : material;

                var baseMeshTransform = Matrix4x4.identity;
                var bounds = overridenMesh.bounds;
                Vector3 translatePostMesh;
                if (UseMeshOrigin)
                {
                    translatePostMesh = new Vector3((bounds.center.x + bounds.size.x / 2f) * IndividualScale.x, 0, 0);
                }
                else
                {
                    baseMeshTransform = Matrix4x4.Translate(Vector3.right * (-bounds.center.x + bounds.size.x / 2)) * baseMeshTransform;
                    translatePostMesh = new Vector3(bounds.size.x * IndividualScale.x, 0, 0);
                }
                baseMeshTransform = Matrix4x4.Scale(IndividualScale) * baseMeshTransform;

                return new TurtleOrganTemplate(
                    useAsDummyMesh ? new MeshDraft() : new MeshDraft(overridenMesh),
                    overridenMaterial,
                    translatePostMesh,
                    AlsoMove,
                    baseMeshTransform
                    );
            }).ToArray();
        }

        public override string ToString()
        {
            return $"{Character} : {MeshRef.name}";
        }

        public void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {
            var organIndexes = new JaggedIndexing
            {
                index = writer.indexInOrganTemplates
            };
            foreach (var templateWriter in CachedOrganTemplates)
            {
                templateWriter.WriteIntoNativeData(nativeData, writer);
            }

            organIndexes.length = (ushort)(writer.indexInOrganTemplates - organIndexes.index);

            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = Character,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.ADD_ORGAN,
                    meshOperation = new TurtleOrganOperation
                    {
                        extraNonUniformScaleForOrgan = ScalePerParameter,
                        scaleIsAdditional = ScaleIsAdditional,
                        isVolumetricScale = VolumetricScale,
                        doScale = ParameterScale,
                        doApplyThiccness = UseThickness,
                        organIndexRange = organIndexes,
                        volumetricValue = volumetricDurabilityValue
                    }
                }
            });
        }
    }
    [CreateAssetMenu(fileName = "TurtleMeshOperations", menuName = "LSystem/TurtleMeshOperations")]
    public class TurtleMeshOperations : TurtleOperationSet
    {
        public MeshKey[] meshKeys;

        public override TurtleDataRequirements DataReqs => meshKeys.Select(x => x.DataReqs).Sum();

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
                meshKey.WriteIntoNativeData(nativeData, writer);
            }
        }
    }

    public struct TurtleOrganOperation
    {
        public float3 extraNonUniformScaleForOrgan;
        public bool doScale;
        public bool scaleIsAdditional;
        public bool isVolumetricScale;
        public bool doApplyThiccness;
        public JaggedIndexing organIndexRange;

        public float volumetricValue;

        public void Operate(
            ref TurtleState state,
            int indexInString,
            SymbolString<float> sourceString,
            NativeArray<TurtleOrganTemplate.Blittable> allOrgans,
            NativeList<TurtleOrganInstance> targetOrganInstances,
            TurtleVolumetricHandles volumetricHandles)
        {
            var pIndex = sourceString.parameters[indexInString];

            var meshTransform = state.transformation;

            var selectedMeshIndex = 0;
            if (organIndexRange.length > 1 && pIndex.length > 0)
            {
                var index = math.clamp(((int)sourceString.parameters[pIndex, 0]), 0, organIndexRange.length - 1);
                selectedMeshIndex = index;
            }
            var selectedOrganIndex = organIndexRange.index + selectedMeshIndex;
            var selectedOrgan = allOrgans[selectedOrganIndex];


            var turtleTranslate = selectedOrgan.translation;
            var scaleIndex = organIndexRange.length <= 1 ? 0 : 1;
            float scale = 1f;
            if (doScale && pIndex.length > scaleIndex)
            {
                scale = sourceString.parameters[pIndex, scaleIndex];
                if (isVolumetricScale)
                {
                    scale = Mathf.Pow(scale, 1f / 3f);
                }
                var scaleVector = (scaleIsAdditional ? new float3(1, 1, 1) : new float3(0, 0, 0)) + (extraNonUniformScaleForOrgan * scale);

                if (selectedOrgan.alsoMove)
                {
                    turtleTranslate.Scale(scaleVector);
                }

                meshTransform *= Matrix4x4.Scale(scaleVector);
            }
            if (doApplyThiccness)
            {
                meshTransform *= Matrix4x4.Scale(new Vector3(1, state.thickness, state.thickness));
            }

            if (volumetricHandles.IsCreated && volumetricValue != 0)
            {
                var organCenter = meshTransform.MultiplyPoint(Vector3.zero);
                volumetricHandles.durabilityWriter.WriteVolumetricAmountToDoubleBufferedData(volumetricValue * scale * math.pow(state.thickness, 1.5f), organCenter);
            }

            
            var newOrganEntry = new TurtleOrganInstance
            {
                organIndexInAllOrgans = (ushort)selectedOrganIndex,
                organTransform = meshTransform * selectedOrgan.baseMeshTransform,
                organIdentity = state.organIdentity,
                extraData = state.customData
            };
            targetOrganInstances.Add(newOrganEntry);

            if (selectedOrgan.alsoMove)
            {
                state.transformation *= Matrix4x4.Translate(turtleTranslate);
            }
        }
    }
}
