using Dman.LSystem.SystemRuntime.Turtle;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Dman.LSystem.UnityObjects
{
    [System.Serializable]
    public class InstantiatableEntity
    {
        public char characterToSpawnFrom;
        public TurtleSpawnData prefab;
    }

    [CreateAssetMenu(fileName = "TurtleInstantiateEntity", menuName = "LSystem/TurtleInstantiateEntity")]
    public class TurtleInstantiateEntityOperationSet : TurtleOperationSet
    {

        public InstantiatableEntity[] instantiatableEntities;
        public override void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {
            foreach (var instantiable in instantiatableEntities)
            {
                var entity = TurtleSpawnEntitiesInstantiator.instance.GetEntityPrefab(instantiable.prefab);

                writer.operators.Add(new TurtleOperationWithCharacter
                {
                    characterInRootFile = instantiable.characterToSpawnFrom,
                    operation = new TurtleOperation
                    {
                        operationType = TurtleOperationType.INSTANTIATE_ENTITY,
                        instantiateOperator = new TurtleInstantiateEntityOperator
                        {
                            instantiableEntity = entity
                        }
                    }
                });
            }
        }
    }
    public struct TurtleInstantiateEntityOperator
    {
        public Entity instantiableEntity;

        public void Operate(
            ref TurtleState state,
            int indexInString,
            SymbolString<float> sourceString,
            EntityCommandBuffer spawningCommandBuffer,
            Matrix4x4 localToWorldTransform)
        {
            var paramIndex = sourceString.parameters[indexInString];

            var spawned = spawningCommandBuffer.Instantiate(instantiableEntity);
            var newbuffer = spawningCommandBuffer.SetBuffer<TurtleSpawnedParameters>(spawned);
            var parameterSlice = sourceString.parameters.data.Slice(paramIndex.index, paramIndex.length);
            newbuffer.CopyFrom(parameterSlice.SliceConvert<TurtleSpawnedParameters>());

            var totalLocalToWorld = localToWorldTransform * state.transformation;
            var rot = totalLocalToWorld.rotation;
            Vector3 pos = totalLocalToWorld.GetColumn(3);

            // Extract new local scale
            Vector3 scale = new Vector3(
                totalLocalToWorld.GetColumn(0).magnitude,
                totalLocalToWorld.GetColumn(1).magnitude,
                totalLocalToWorld.GetColumn(2).magnitude
            );
            spawningCommandBuffer.SetComponent(instantiableEntity, new Rotation
            {
                Value = rot
            });
            spawningCommandBuffer.SetComponent(instantiableEntity, new Translation
            {
                Value = pos
            });
            spawningCommandBuffer.SetComponent(instantiableEntity, new NonUniformScale
            {
                Value = scale
            });

            //spawningCommandBuffer.SetComponent(instantiableEntity, new LocalToWorld
            //{
            //    Value = localToWorldTransform * state.transformation
            //});
        }
    }

}
