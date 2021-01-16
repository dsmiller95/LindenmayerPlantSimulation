using PlantBuilder.NodeGraph.MeshNodes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace PlantBuilder.NodeGraph
{
    //[System.Serializable]
    //public abstract class DeferredEvaluator<T>
    //{
    //    public abstract T Evalute(Dictionary<string, object> context);
    //}

    [Serializable]
    public class DeferredMeshEvaluator// : DeferredEvaluator<PlantMeshComponent>
    {
        public virtual PlantMeshComponent Evalute(Dictionary<string, object> context)
        {
            return null;
        }
    }

    [Serializable]
    public class SerializedDeferredMeshEvaluator
    {
        public byte[] serializedData;

        public static SerializedDeferredMeshEvaluator GetFromInstance(
            DeferredMeshEvaluator deferredMesh)
        {
            var serializer = new BinaryFormatter();
            var stream = new MemoryStream();
            serializer.Serialize(stream, deferredMesh);

            stream.Seek(0, SeekOrigin.Begin);

            //var stringReader = new StreamReader(stream, Encoding.UTF8);

            return new SerializedDeferredMeshEvaluator
            {
                serializedData = stream.ToArray()
            };
        }

        public DeferredMeshEvaluator GetDeserializedGuy()
        {
            var formatter = new BinaryFormatter();
            //var stringData = serializedData;// Encoding.ASCII.GetBytes(guyString);
            var stream = new MemoryStream(serializedData);
            var resultObj = formatter.Deserialize(stream);

            return resultObj as DeferredMeshEvaluator;
        }

        public string GetStringRepresentation()
        {
            var sortaString = string.Join("", serializedData
                .Where(x => x > 0)
                .Select(x => Convert.ToChar(x)));
            return sortaString;
        }
    }
}
