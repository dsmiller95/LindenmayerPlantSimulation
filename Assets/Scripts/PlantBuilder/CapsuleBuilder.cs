using ProceduralToolkit;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder
{
    [CreateAssetMenu(fileName = "CapsuleComponent", menuName = "Builders/CapsuleComponent")]
    public class CapsuleBuilder : ComponentBuilder
    {
        public float height = 10;
        public float radius = 1;

        public int childComponentCount = 10;
        public float rotationPerChild = 360 * 3 / 5;

        [Range(0, 1f)]
        public float childBeginRange = .3f;
        [Range(0, 1f)]
        public float childEndRange = .7f;

        public float randomFactor = .05f;

        public float childSizeReduction = 0.2f;


        public override MeshDraft CreateComponentMesh(
            Matrix4x4 meshTransform, 
            int componentLevel, 
            Stack<NextComponentSpawnCommand> extraComponents,
            System.Random rand)
        {
            var baseChildTransform = meshTransform * Matrix4x4.Rotate(Quaternion.Euler(-90, 0, 0));
            for (int childIndex = 0; childIndex < childComponentCount; childIndex++)
            {
                var childTranslateFactor = ((float)childIndex / childComponentCount) * (childEndRange - childBeginRange) + childBeginRange;

                var childTransform = baseChildTransform * Matrix4x4.TRS(
                    new Vector3(0, 0, childTranslateFactor * height),
                    Quaternion.Euler(0, 0, rotationPerChild * childIndex),
                    Vector3.one);
                childTransform *= Matrix4x4.Translate(new Vector3(0, radius, 0));
                childTransform *= Matrix4x4.Scale(Vector3.one * childSizeReduction);
                childTransform *= Matrix4x4.Translate(new Vector3(0, -radius, 0));
                childTransform *= Matrix4x4.TRS(
                    new Vector3(NextRand(rand), 0f, NextRand(rand)),
                    Quaternion.Euler(180 * NextRand(rand), 180 * NextRand(rand), 180 * NextRand(rand)),
                    Vector3.one + new Vector3(NextRand(rand), NextRand(rand), NextRand(rand)));
                extraComponents.Push(new NextComponentSpawnCommand
                {
                    componentIndex = componentLevel + 1,
                    componentTransformation = childTransform
                });
            }

            var resultDraft = MeshDraft.Capsule(height, radius);
            resultDraft.Move(new Vector3(0, height / 2, 0));
            resultDraft.Transform(meshTransform);
            return resultDraft;
        }


        private float NextRand(System.Random rand)
        {
            return (float)((rand.NextDouble() * 2 - 1) * randomFactor);
        }
    }
}