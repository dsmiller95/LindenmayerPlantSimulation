using Dman.LSystem.UnityObjects;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Demo.PlantBuilder
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class BoundFlowerUpdateSystem : SystemBase
    {
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            base.OnCreate();
            random = new Unity.Mathematics.Random(1928437918);
        }

        protected override void OnUpdate()
        {
            var time = Time.DeltaTime;
            random.NextFloat();
            var rand = random;

            Entities
                .ForEach((Entity entity, ref LSystemBoundComponent boundComponent, ref NonUniformScale scale, ref Rotation rotation, ref FlowerComponent flower) =>
                {
                    if (!flower.hasInstantiated)
                    {
                        flower.rotationSpeed = rand.NextFloat(60, 360 * 2);
                        flower.hasInstantiated = true;  
                    }

                    var scaleFactor = boundComponent.resourceAmount;

                    var newScale = math.pow(scaleFactor, 1f / 3f);
                    scale.Value = new float3(newScale, newScale, newScale);
                    rotation.Value = ((Quaternion)rotation.Value) * Quaternion.Euler(0, 0, flower.rotationSpeed * time);
                }).Schedule();

        }
    }
}