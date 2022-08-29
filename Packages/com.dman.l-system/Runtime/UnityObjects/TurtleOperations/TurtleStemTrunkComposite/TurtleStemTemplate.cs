using Dman.LSystem.SystemRuntime.NativeCollections;
using UnityEngine;

namespace Dman.LSystem.SystemRuntime.Turtle
{
    public class TurtleStemTemplate : ITurtleNativeDataWritable
    {
        public Material material;
        public bool alsoMove;

        public TurtleStemTemplate(
            Material material,
            bool shouldMove)
        {
            this.material = material;
            this.alsoMove = shouldMove;
        }

        public TurtleDataRequirements DataReqs => new TurtleDataRequirements
        {
            organTemplateSize = 0,
            vertextDataSize = 0,
            triangleDataSize = 0
        };

        public void WriteIntoNativeData(NativeTurtleData nativeData, TurtleNativeDataWriter writer)
        {

            var existingMaterialIndex = writer.GetMaterialIndex(material);
            var blittable = new Blittable
            {
                alsoMove = alsoMove,
                materialIndex = (byte)existingMaterialIndex
            };
            // TODO:
            //nativeData.allOrganData[writer.indexInOrganTemplates] = blittable;
            writer.indexInOrganTemplates++;
        }

        public struct Blittable
        {
            public bool alsoMove;
            public byte materialIndex;
        }
    }
}
