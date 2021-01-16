using GraphProcessor;
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
