using GraphProcessor;
using PlantBuilder.NodeGraph.DeferredEvaluators;
using ProceduralToolkit;
using SplineMesh;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [System.Serializable, NodeMenuItem("Mesh/CombineMeshNode")]
    public class CombineMeshNode : MeshNode
    {
        [Input(name = "Mesh A")]
        public MeshDraftWithExtras A;
        [Input(name = "Mesh B")]
        public MeshDraftWithExtras B;

        public override string name => "Combine Mesh";

        protected override void Process()
        {
            var outputMesh = new CompoundMeshDraft();
            if(A.meshDraft != null)
                outputMesh.Add(A.meshDraft);
            if (B.meshDraft != null)
                outputMesh.Add(B.meshDraft);
            this.output = new MeshDraftWithExtras(outputMesh);
        }
    }
}