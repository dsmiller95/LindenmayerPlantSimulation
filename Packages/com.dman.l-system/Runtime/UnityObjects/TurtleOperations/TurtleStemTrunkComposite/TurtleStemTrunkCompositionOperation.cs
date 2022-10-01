using Dman.LSystem.SystemRuntime.NativeCollections;
using Dman.LSystem.SystemRuntime.Turtle;
using Dman.LSystem.SystemRuntime.VolumetricData;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Dman.LSystem.UnityObjects.StemTrunk
{
    [System.Flags]
    public enum ScaleSource
    {
        NONE = 0,
        PARAMETER = 1 << 0,
        THICKNESS = 1 << 1,
    }

    [System.Serializable]
    public class StemKey : ITurtleNativeDataWritable
    {
        public char Character;

        public bool AlsoMove = true;
        public float baseRadius = 1;
        public float radiusScaleFactor = 1;
        public float baseLength = 1;
        public float lengthScaleFactor = 1;
        public bool scaleIsAdditional = false;
        public ScaleSource scalingSource = ScaleSource.THICKNESS;

        public float scalePower = 1;

        public int radialResolution = 5;
        public Material material;
        public bool constrainUVs = false;
        [Tooltip("When checked, the long-axis of the trunk is aligned with the x-axis on UV. otherwise y-axis")]
        public bool flipUvs = false;
        public Rect uvRectMaper = new Rect(new Vector2(.25f, .25f), new Vector2(.5f, .5f));

        /// <summary>
        /// usable to reconfigure this operation to only move the turtle by how much it would be moved
        ///     by this organ. but; it will generate no mesh vertexes
        /// </summary>
        public bool useAsDummyMesh = false;

        public TurtleDataRequirements DataReqs => new TurtleDataRequirements
        {
            organTemplateSize = 0,
            triangleDataSize = 0,
            vertextDataSize = 0
        };

        public void InteralCacheOrganTemplates()
        {
        }

        public override string ToString()
        {
            return $"{Character} : stem '{material.name}'";
        }
        public void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {
            var classIndex = (ushort)writer.GetStemClassIndex(new TurtleStemClass
            {
                materialIndex = (byte)writer.GetMaterialIndex(material),
                radialResolution = (ushort)radialResolution,
                constrainUvs = constrainUVs,
                flipUvs = flipUvs,
                uvRect = uvRectMaper
            });
            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = Character,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.ADD_STEM,
                    stemOperation = new TurtleStemPlacementOperation
                    {
                        stemClassIndex = classIndex,
                        willMove = AlsoMove,
                        scaleSource = scalingSource,
                        lengthRadiusBase = new float2(baseLength, baseRadius),
                        lengthRadiusScale = new float2(lengthScaleFactor, radiusScaleFactor),
                        scaleIsAdditional = scaleIsAdditional,
                        scalePower = Mathf.Max(1, scalePower)
                    }
                }
            });
        }
    }

    [CreateAssetMenu(fileName = "TurtleStemTrunkCompositionOperation", menuName = "LSystem/TurtleStemTrunkCompositionOperation")]
    public class TurtleStemTrunkCompositionOperation : TurtleOperationSet
    {
        public StemKey[] stemKeys;

        public override TurtleDataRequirements DataReqs => stemKeys.Select(x => x.DataReqs).Sum();

        public override void InternalCacheOperations()
        {
            foreach (var meshKey in stemKeys)
            {
                meshKey.InteralCacheOrganTemplates();
            }
        }

        public override void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {
            foreach (var stemKey in stemKeys)
            {
                stemKey.WriteIntoNativeData(nativeData, writer);
            }
        }
    }

    public struct TurtleStemPlacementOperation
    {
        public bool willMove;

        public ushort stemClassIndex;
        public ScaleSource scaleSource;

        public float2 lengthRadiusBase;
        public float2 lengthRadiusScale;
        public bool scaleIsAdditional;

        public float scalePower;
        public void Operate(
            ref TurtleState state,
            int indexInString,
            SymbolString<float> sourceString,
            NativeList<TurtleStemInstance> targetStemInstances)
        {
            var pIndex = sourceString.parameters[indexInString];

            var localLengthRadius = lengthRadiusBase;
            if ((scaleSource & ScaleSource.PARAMETER) != 0 && pIndex.length > 0)
            {
                var scale = sourceString.parameters[pIndex, 0];
                if (scalePower != 1)
                {
                    scale = Mathf.Pow(scale, 1f / scalePower);
                }
                localLengthRadius *= (scaleIsAdditional ? new float2(1, 1) : float2.zero) + scale * lengthRadiusScale;
            }
            if ((scaleSource & ScaleSource.THICKNESS) != 0)
            {
                localLengthRadius *= new float2(1, state.thickness);
            }

            var newStemEntry = new TurtleStemInstance
            {
                stemClassIndex = stemClassIndex,
                orientation = state.transformation * Matrix4x4.Scale(new Vector3(localLengthRadius.x, localLengthRadius.y, localLengthRadius.y)),
                parentIndex = state.indexInStemTree,
                organIdentity = state.organIdentity,
                extraData = state.customData
            };
            state.indexInStemTree = targetStemInstances.Length;
            targetStemInstances.Add(newStemEntry);

            if (willMove)
            {
                state.transformation *= Matrix4x4.Translate(new Vector3(localLengthRadius.x, 0, 0));
            }
        }
    }
}
