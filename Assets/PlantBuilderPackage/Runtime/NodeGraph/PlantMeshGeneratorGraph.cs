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

        public void Reseed()
        {
            seed = Random.Range(int.MinValue, int.MaxValue);
            this.ResetRandom();
        }
        public void ResetRandom()
        {
            this.MyRandom = new System.Random(seed);
        }

        public PlantMeshComponent GenerateMesh(bool reseed = false)
        {
            var output = GetExposedParameter("output");
            output.serializedValue.OnAfterDeserialize();
            var serializedGenerator = output.serializedValue.value as SerializedDeferredMeshEvaluator;
            if (serializedGenerator == null)
            {
                Debug.LogError("'output mesh' parameter not defined");
                return null;
            }

            var generator = serializedGenerator.GetDeserializedGuy();

            if (reseed)
                this.Reseed();
            else
                this.ResetRandom();
            return generator.Evalute(this.MyRandom, new System.Collections.Generic.Dictionary<string, object>());

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
