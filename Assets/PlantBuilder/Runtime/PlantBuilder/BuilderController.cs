using ProceduralToolkit;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlantBuilder
{
    [Serializable]
    public class BuilderConfiguration
    {
        public ComponentBuilder builder;
        public Material material;
    }

    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class BuilderController : MonoBehaviour
    {
        public ComponentBuilder[] builders;

        public int Seed;

        // Start is called before the first frame update
        void Start()
        {
            this.BuildComponents();
        }

        // Update is called once per frame
        void Update()
        {

        }


        public void ResetSeed()
        {
            Seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }
        

        public void BuildComponents()
        {
            if(builders.Length <= 0)
            {
                return;
            }
            var combinedMesh = new CompoundMeshDraft();

            var newMeshes = new Stack<NextComponentSpawnCommand>();
            newMeshes.Push(new NextComponentSpawnCommand
            {
                componentIndex = 0,
                componentTransformation = Matrix4x4.identity
            });

            var randGen = new System.Random(Seed);
            while (newMeshes.Count > 0)
            {
                var nextMesh = newMeshes.Pop();
                if(nextMesh.componentIndex >= builders.Length)
                {
                    continue;
                }
                var nextComponent = builders[nextMesh.componentIndex];
                var nextDraft = nextComponent
                    .CreateComponentMesh(
                        nextMesh.componentTransformation,
                        nextMesh.componentIndex,
                        newMeshes,
                        randGen);
                nextDraft.name = nextMesh.componentIndex.ToString();
                combinedMesh.Add(nextDraft);
            }


            var meshFilter = GetComponent<MeshFilter>();

            combinedMesh.MergeDraftsWithTheSameName();
            combinedMesh.SortDraftsByName();


            //var meshMesh = meshFilter.mesh;
            meshFilter.mesh = combinedMesh.ToMeshWithSubMeshes(true, true);
            //meshFilter.mesh = meshMesh;
        }
    }
}