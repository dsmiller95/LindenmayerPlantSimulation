using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using PlantBuilder.NodeGraph.MeshNodes;
using UnityEngine;

namespace PlantBuilder.NodeGraph
{
    public class PlantMeshGeneratorGraph : BaseGraph
    {
        public System.Random MyRandom { get; private set; }
        public int seed;

        public void ResetRandom()
        {
            this.MyRandom = new System.Random(seed);
        }
        public void Reseed()
        {
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            this.ResetRandom();
        }

        public MeshDraftWithExtras GenerateMesh(bool reseed = false)
        {
            var output = GetParameterValue<MeshDraftWithExtras>("output");
            return output;
            //output.serializedValue.OnAfterDeserialize();
            //var serializedGenerator = output.serializedValue.value as SerializedDeferredMeshEvaluator;
            //if (output == null)
            //{
            //    Debug.LogError("'output mesh' parameter not defined");
            //    return default;
            //}

            //var generator = serializedGenerator.GetDeserializedGuy();

            //return generator.Evalute(this.MyRandom, new System.Collections.Generic.Dictionary<string, object>());

        }

        protected override void OnEnable()
        {
            ResetRandom();
            base.OnEnable();
        }

        private void Awake()
        {
            Reseed();
        }
    }
}
