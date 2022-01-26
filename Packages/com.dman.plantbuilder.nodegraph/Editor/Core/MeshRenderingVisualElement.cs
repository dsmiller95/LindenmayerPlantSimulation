using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlantBuilder.NodeGraph.Core
{
    public class MeshRenderingVisualElement : VisualElement, IDisposable
    {
        public Mesh PreviewMesh { get; set; }
        private Rect previewRect;

        private PreviewRenderUtility _cached;
        private PreviewRenderUtility previewRenderer
        {
            get
            {
                if (_cached == null)
                {
                    _cached = new PreviewRenderUtility();
                    //previewRenderer.camera.clearFlags = CameraClearFlags.Nothing;
                    _cached.camera.transform.position = new Vector3(4, 4, 0);
                    _cached.camera.transform.LookAt(Vector3.zero, Vector3.up);
                }
                return _cached;
            }
        }
        public MeshRenderingVisualElement(Rect previewSize)
        {
            previewRect = previewSize;
            style.width = new Length(previewSize.width, LengthUnit.Pixel);
            style.height = new Length(previewSize.height, LengthUnit.Pixel);
            style.color = new StyleColor(Color.magenta);

            var immediateContainer = new IMGUIContainer(() =>
            {
                DoRenderMesh();
            });

            contentContainer.Add(immediateContainer);
        }

        public new void RemoveFromHierarchy()
        {
            previewRenderer.Cleanup();
            base.RemoveFromHierarchy();
        }

        private void DoRenderMesh()
        {
            //Debug.Log(this.localBound);
            var renderSize = localBound;
            if (float.IsNaN(renderSize.width) || renderSize.width <= 1
                || float.IsNaN(renderSize.height) || renderSize.height <= 1
                || PreviewMesh == null)
            {
                return;
            }
            try
            {
                var defaultMaterial = Resources.Load(
                    "Material/" + PlantMeshGeneratorView.DEFAULT_MATERIAL_NAME,
                    typeof(Material)) as Material;

                var sceneStyle = new GUIStyle();
                sceneStyle.padding = new RectOffset(10, 10, 10, 10);

                //renderSize.width = Math.Max(renderSize.width, 100);
                //renderSize.height = Math.Max(renderSize.height, 100);
                previewRenderer.BeginPreview(renderSize, sceneStyle);//, sceneStyle);

                var boundSize = PreviewMesh.bounds.size.AbsoluteValue();
                var meshScalingFactor = Math.Max(Math.Max(boundSize.x, boundSize.y), Math.Max(boundSize.z, 1));
                var translation = -PreviewMesh.bounds.center / meshScalingFactor;


                previewRenderer.DrawMesh(PreviewMesh,
                    Matrix4x4.TRS(translation, Quaternion.identity, (1 / meshScalingFactor) * Vector3.one),
                    defaultMaterial,
                    0);
                previewRenderer.camera.Render();


                previewRenderer.EndAndDrawPreview(localBound);
                //var renderedTexture = previewRenderer.EndPreview();
                //EditorGUI.DrawTextureTransparent(renderSize, renderedTexture);
                //this.style.backgroundImage = new StyleBackground(renderedTexture);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Dispose()
        {
            previewRenderer.Cleanup();
        }
    }
}
