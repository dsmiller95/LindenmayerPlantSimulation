using GraphProcessor;
using ProceduralToolkit;
using UnityEngine;

namespace PlantBuilder.NodeGraph.MeshNodes
{
    [System.Serializable, NodeMenuItem("Mesh/Source/Asset")]
    public class MeshAssetNode : MeshNode
    {
        [Input(name = "Mesh Asset"), SerializeField]
        public Mesh meshAsset;

        public override string name => "Mesh";

        protected override void Process()
        {
            if (meshAsset == null)
            {
                return;
            }
            output = new MeshDraftWithExtras(
                new MeshDraft(meshAsset),
                meshAsset.bounds);
        }
    }
}