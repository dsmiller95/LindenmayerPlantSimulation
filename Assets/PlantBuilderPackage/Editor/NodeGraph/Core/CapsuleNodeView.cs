using GraphProcessor;
using PlantBuilder.NodeGraph.MeshNodes;
using ProceduralToolkit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlantBuilder.NodeGraph.Core
{
    [NodeCustomEditor(typeof(CapsuleMeshNode))]
    public class CapsuleMeshNodeView : BaseNodeView
    {
        private MeshRenderingVisualElement meshRenderTool;
        public override void Enable()
        {
            var target = nodeTarget as CapsuleMeshNode;

            //owner.win

            var heightLabel = new Label($"height: {target.height.ToString()}");
            var widthLabel = new Label($"width: {target.radius.ToString()}");

            controlsContainer.Add(heightLabel);
            controlsContainer.Add(widthLabel);

            target.onProcessed += TargetNodeProcessed;
            meshRenderTool?.Dispose();
            meshRenderTool = new MeshRenderingVisualElement(new Rect(0, 0, 300, 300));
            controlsContainer.Add(meshRenderTool);

            TargetNodeProcessed();

            

            //var nodeOutput = target.output;

            //var newMeshDraft = target.output.Evalute(PlantMeshGeneratorView.DEFAULT_CONTEXT)?.meshDraft;
            ////newMeshDraft.Rotate(Quaternion.Euler(30, 30, 30));
            //var newMesh = newMeshDraft?.ToMesh(true, true);

            //var previewPanel = new MeshRenderingVisualElement(newMesh, new Rect(0, 0, 300, 300));
            //controlsContainer.Add(previewPanel);

            //var defaultMaterial = Resources.Load(
            //    "Material/" + PlantMeshGeneratorView.DEFAULT_MATERIAL_NAME,
            //    typeof(Material)) as Material;

            //testscene.BeginPreview(previewSize, sceneStyle);
            //testscene.DrawMesh(newMesh,
            //    Matrix4x4.identity,
            //    defaultMaterial,
            //    0);

            //testscene.EndAndDrawPreview(previewSize);
        }

        private void TargetNodeProcessed()
        {
            var target = nodeTarget as CapsuleMeshNode;

            var newMeshDraft = target?.output?.Evalute(PlantMeshGeneratorView.DEFAULT_CONTEXT)?.meshDraft;
            //newMeshDraft.Rotate(Quaternion.Euler(30, 30, 30));
            var newMesh = newMeshDraft?.ToMesh(true, true);
            meshRenderTool.PreviewMesh = newMesh;
        }

        public override void OnRemoved()
        {
            Debug.Log("I've been removed. or have I?");
            meshRenderTool?.Dispose();
            base.OnRemoved();
        }

        //public void OnPostVisualCreation()
        //{
        //    // Make invisble so you don't see the size re-adjustment
        //    // (Non-visible objects still go through transforms in the layout engine)
        //    Visual.visible = false;
        //    Visual.schedule.Execute(WaitOneFrame);
        //}

        //private void WaitOneFrame(TimerState obj)
        //{
        //    // Because waiting once wasn't working
        //    Visual.schedule.Execute(AutoSize);
        //}

        //private void AutoSize(TimerState obj)
        //{
        //    // Do any measurements, size adjustments you need (NaNs not an issue now)
        //    Visual.MarkDirtyRepaint();
        //    Visual.visible = true;
        //}
    }
}