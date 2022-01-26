using GraphProcessor;
using PlantBuilder.NodeGraph.MeshNodes;
using System;
using UnityEngine;

namespace PlantBuilder.NodeGraph.Core
{
    [NodeCustomEditor(typeof(MeshNode))]
    public class CapsuleMeshNodeView : BaseNodeView, IDisposable
    {
        private MeshRenderingVisualElement meshRenderTool;
        private MeshNode target => nodeTarget as MeshNode;
        public override void Enable()
        {
            base.Enable();
            target.onProcessed += TargetNodeProcessed;
            meshRenderTool?.Dispose();
            meshRenderTool = new MeshRenderingVisualElement(new Rect(0, 0, 300, 300));
            controlsContainer.Add(meshRenderTool);

            var trueOwner = owner as PlantMeshGeneratorView;
            trueOwner.onWindowDisposed += Dispose;

            TargetNodeProcessed();
        }

        private void TargetNodeProcessed()
        {
            //var rootGraph = owner.graph as PlantMeshGeneratorGraph;

            var newMeshDraft = target?.output;//?.Evalute(rootGraph.MyRandom, PlantMeshGeneratorView.DEFAULT_CONTEXT);
            var newMesh = newMeshDraft?.ToMesh();
            meshRenderTool.PreviewMesh = newMesh;
        }

        public override void OnRemoved()
        {
            Debug.Log("I've been removed. or have I?");
            this.Dispose();
            base.OnRemoved();
        }

        public void Dispose()
        {
            meshRenderTool?.Dispose();
            var trueOwner = owner as PlantMeshGeneratorView;
            trueOwner.onWindowDisposed -= Dispose;
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