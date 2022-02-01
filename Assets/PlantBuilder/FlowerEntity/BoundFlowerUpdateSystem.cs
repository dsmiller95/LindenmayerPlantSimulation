using Dman.LSystem.UnityObjects;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Demo.PlantBuilder
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(FlowerBindingSystem))]
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
            var timeDelta = Time.DeltaTime;
            random.NextFloat();
            var rand = random;

            Entities
                .ForEach((Entity entity, ref NonUniformScale scale, ref Rotation rotation, ref FlowerComponent flower, in FlowerResourceAmountComponent flowerAmounts) =>
                {
                    if (!flower.hasInstantiated)
                    {
                        flower.rotationSpeed = rand.NextFloat(60, 360 * 2);
                        flower.hasInstantiated = true;  
                    }

                    var scaleFactor = flowerAmounts.resourceAmount;

                    var newScale = math.pow(scaleFactor, 1f / 2f);
                    scale.Value = new float3(scale.Value.x, newScale, scale.Value.z);
                    rotation.Value = ((Quaternion)rotation.Value) * Quaternion.Euler(0, 0, flower.rotationSpeed * timeDelta);
                }).Schedule();

        }
    }
}