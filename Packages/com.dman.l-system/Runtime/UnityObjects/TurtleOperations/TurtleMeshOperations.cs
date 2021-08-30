using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.Turtle;
using Dman.LSystem.SystemRuntime.VolumetricData;
using ProceduralToolkit;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
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

        public TurtleDataRequirements RequiredDataSpace => CachedOrganTemplates.Select(x => x.DataReqs).Sum();

        public TurtleOrganTemplate[] CachedOrganTemplates;
        public void InteralCacheOrganTemplates()
        {
            CachedOrganTemplates = (new[] { MeshRef }.Concat(MeshVariants)).Select(mesh =>
            {
                var newDraft = new MeshDraft(mesh);
                var bounds = mesh.bounds;
                Vector3 translatePostMesh;
                if (UseMeshOrigin)
                {
                    translatePostMesh = new Vector3((bounds.center.x + bounds.size.x / 2f) * IndividualScale.x, 0, 0);
                }
                else
                {
                    newDraft.Move(Vector3.right * (-bounds.center.x + bounds.size.x / 2));
                    translatePostMesh = new Vector3(bounds.size.x * IndividualScale.x, 0, 0);
                }
                newDraft.Scale(IndividualScale);

                return new TurtleOrganTemplate(
                    newDraft,
                    material,
                    translatePostMesh,
                    AlsoMove
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
                            scaleIsAdditional = meshKey.ScaleIsAdditional,
                            isVolumetricScale = meshKey.VolumetricScale,
                            doScaleMesh = meshKey.ParameterScale,
                            doApplyThiccness = meshKey.UseThickness,
                            organTemplateVariants = organIndexes,
                            volumetricValue = meshKey.volumetricDurabilityValue
                        }
                    }
                });
            }
        }
    }

    public struct TurtleMeshOperation
    {
        public float3 extraNonUniformScaleForOrgan;
        public bool doScaleMesh;
        public bool scaleIsAdditional;
        public bool isVolumetricScale;
        public bool doApplyThiccness;
        public JaggedIndexing organTemplateVariants;

        public float volumetricValue;

        public void Operate(
            ref TurtleState state,
            NativeArray<TurtleMeshAllocationCounter> meshSizeCounterPerSubmesh,
            int indexInString,
            SymbolString<float> sourceString,
            NativeArray<TurtleOrganTemplate.Blittable> allOrgans,
            NativeList<TurtleOrganInstance> targetOrganInstances,
            VolumetricWorldNativeWritableHandle volumetricNativeWriter)
        {
            var pIndex = sourceString.parameters[indexInString];

            var meshTransform = state.transformation;

            var selectedMeshIndex = 0;
            if (organTemplateVariants.length > 1 && pIndex.length > 0)
            {
                var index = math.clamp(((int)sourceString.parameters[pIndex, 0]), 0,  organTemplateVariants.length - 1);
                selectedMeshIndex = index;
            }
            var selectedOrganIndex = organTemplateVariants.index + selectedMeshIndex;
            var selectedOrgan = allOrgans[selectedOrganIndex];


            var turtleTranslate = selectedOrgan.translation;
            var scaleIndex = organTemplateVariants.length <= 1 ? 0 : 1;
            float scale = 1f;
            if (doScaleMesh && pIndex.length > scaleIndex)
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

            if (volumetricValue != 0)
            {
                var organCenter = meshTransform.MultiplyPoint(Vector3.zero);
                volumetricNativeWriter.WriteVolumetricDurabilityToTarget(volumetricValue * scale * math.pow(state.thickness, 1.5f), organCenter);
            }

            var meshSizeForSubmesh = meshSizeCounterPerSubmesh[selectedOrgan.materialIndex];

            var newOrganEntry = new TurtleOrganInstance
            {
                organIndexInAllOrgans = (ushort)selectedOrganIndex,
                organTransform = meshTransform,
                vertexMemorySpace = new JaggedIndexing
                {
                    index = meshSizeForSubmesh.totalVertexes,
                    length = selectedOrgan.vertexes.length
                },
                trianglesMemorySpace = new JaggedIndexing
                {
                    index = meshSizeForSubmesh.totalTriangleIndexes,
                    length = selectedOrgan.trianges.length
                },
                organIdentity = state.organIdentity
            };
            targetOrganInstances.Add(newOrganEntry);

            meshSizeForSubmesh.totalVertexes += newOrganEntry.vertexMemorySpace.length;
            meshSizeForSubmesh.totalTriangleIndexes += newOrganEntry.trianglesMemorySpace.length;
            meshSizeCounterPerSubmesh[selectedOrgan.materialIndex] = meshSizeForSubmesh;

            if (selectedOrgan.alsoMove)
            {
                state.transformation *= Matrix4x4.Translate(turtleTranslate);
            }
        }
    }
}
