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
        public int radialResolution;
        public Material material;

        public bool AlsoMove;
        public float baseRadius;
        public float baseLength;
        public ScaleSource scalingSource;

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
            writer.operators.Add(new TurtleOperationWithCharacter
            {
                characterInRootFile = Character,
                operation = new TurtleOperation
                {
                    operationType = TurtleOperationType.ADD_STEM,
                    stemOperation = new TurtleStemPlacementOperation
                    {
                        materialIndex = (byte)writer.GetMaterialIndex(material),
                        willMove = AlsoMove,
                        radialResolution = (ushort)radialResolution,
                        scaleSource = scalingSource,
                        baseLength = baseLength,
                        baseRadius = baseRadius
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
        public byte materialIndex;
        public bool willMove;

        public ushort radialResolution;
        public ScaleSource scaleSource;

        public float baseLength;
        public float baseRadius;

        public void Operate(
            ref TurtleState state,
            int indexInString,
            SymbolString<float> sourceString,
            NativeList<TurtleStemInstance> targetStemInstances)
        {
            var pIndex = sourceString.parameters[indexInString];

            var turtleTranslation = new Vector3(baseLength, 0, 0);
            var stemScale = new float3(1, baseRadius, baseRadius);
            if (scaleSource == ScaleSource.PARAMETER && pIndex.length > 0)
            {
                var scale = sourceString.parameters[pIndex, 0];
                turtleTranslation *= scale;
                stemScale *= scale;
            }
            if (scaleSource == ScaleSource.THICKNESS)
            {
                stemScale *= new float3(1, state.thickness, state.thickness);
            }

            var newStemEntry = new TurtleStemInstance
            {
                materialIndex = materialIndex,
                radialResolution = radialResolution,
                orientation = state.transformation * Matrix4x4.Scale(stemScale),
                parentIndex = state.indexInStemTree,
                organIdentity = state.organIdentity,
            };
            state.indexInStemTree = targetStemInstances.Length;
            targetStemInstances.Add(newStemEntry);

            if (willMove)
            {
                state.transformation *= Matrix4x4.Translate(turtleTranslation);
            }
        }
    }
}
