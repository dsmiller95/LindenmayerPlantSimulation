using PlantBuilder.NodeGraph.MeshNodes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace PlantBuilder.NodeGraph.DeferredEvaluators
{
    [System.Serializable]
    public abstract class DeferredEvaluator<T>
    {
        public abstract T Evalute(Random randomSource, Dictionary<string, object> context);

        public static implicit operator DeferredEvaluator<T>(T v)
        {
            return new DeferredConstantEvaluator<T>(v);
        }
    }

    [System.Serializable]
    public class DeferredConstantEvaluator<T> : DeferredEvaluator<T>
    {
        private T constantValue;
        public DeferredConstantEvaluator(T constantValue)
        {
            this.constantValue = constantValue;
        }
        public override T Evalute(Random randomSource, Dictionary<string, object> context)
        {
            return constantValue;
        }
    }

    [Serializable]
    public class SerializedDeferredMeshEvaluator
    {
        public byte[] serializedData;

        public static SerializedDeferredMeshEvaluator GetFromInstance(
            DeferredEvaluator<MeshDraftWithExtras> deferredMesh)
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

        public DeferredEvaluator<MeshDraftWithExtras> GetDeserializedGuy()
        {
            var formatter = new BinaryFormatter();
            //var stringData = serializedData;// Encoding.ASCII.GetBytes(guyString);
            var stream = new MemoryStream(serializedData);
            var resultObj = formatter.Deserialize(stream);

            return resultObj as DeferredEvaluator<MeshDraftWithExtras>;
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
