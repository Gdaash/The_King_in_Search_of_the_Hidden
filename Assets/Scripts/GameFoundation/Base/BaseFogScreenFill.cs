using UnityEngine;
using UnityEngine.Rendering;

namespace GameFoundation.Base
{
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class BaseFogScreenFill : MonoBehaviour
    {
        private SpriteRenderer source;
        private Camera view;
        private Mesh mesh;
        private MeshRenderer extension;
        private GameObject edge;
        private Rect previous;
        private void OnEnable()
        {
            source = GetComponent<SpriteRenderer>(); view = Camera.main;
            edge = new GameObject("Fog edge continuation", typeof(MeshFilter), typeof(MeshRenderer));
            edge.transform.SetParent(transform, false);
            mesh = new Mesh { name = "Fog viewport border" };
            edge.GetComponent<MeshFilter>().sharedMesh = mesh;
            extension = edge.GetComponent<MeshRenderer>();
            extension.sharedMaterial = source.sharedMaterial;
            extension.sortingLayerID = source.sortingLayerID; extension.sortingOrder = source.sortingOrder;
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", source.sprite.texture);
            properties.SetFloat("_OpaqueSideEdges", 1); extension.SetPropertyBlock(properties);
            source.GetPropertyBlock(properties); properties.SetFloat("_OpaqueSideEdges", 1); source.SetPropertyBlock(properties);
            RenderPipelineManager.beginCameraRendering += BeforeRender;
        }
        private void BeforeRender(ScriptableRenderContext context, Camera camera)
        {
            if (camera != view || source.sprite == null) return;
            // Source is an unpacked full-rect fog sprite. Clamp sampling repeats its painted edge.
            var bounds = source.sprite.bounds;
            var a = transform.InverseTransformPoint(view.ViewportToWorldPoint(new Vector3(0, 0, view.nearClipPlane)));
            var b = transform.InverseTransformPoint(view.ViewportToWorldPoint(new Vector3(1, 1, view.nearClipPlane)));
            var rect = Rect.MinMaxRect(Mathf.Min(a.x, bounds.min.x) - .02f, Mathf.Min(a.y, bounds.min.y) - .02f,
                Mathf.Max(b.x, bounds.max.x) + .02f, Mathf.Max(b.y, bounds.max.y) + .02f);
            if (rect == previous) return; previous = rect;
            var vertices = new[] {
                new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin), new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMin, rect.yMax),
                new Vector3(bounds.min.x,bounds.min.y), new Vector3(bounds.max.x,bounds.min.y), new Vector3(bounds.max.x,bounds.max.y), new Vector3(bounds.min.x,bounds.max.y) };
            var uv = new Vector2[8]; var colors = new Color[8];
            for (int i=0;i<8;i++) { uv[i] = new Vector2((vertices[i].x-bounds.min.x)/bounds.size.x,(vertices[i].y-bounds.min.y)/bounds.size.y); colors[i]=source.color; }
            mesh.Clear(); mesh.vertices=vertices; mesh.uv=uv; mesh.colors=colors;
            mesh.triangles=new[]{0,4,5,0,5,1,1,5,6,1,6,2,2,6,7,2,7,3,3,7,4,3,4,0}; mesh.RecalculateBounds();
        }
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeRender;
            if (source) { var properties = new MaterialPropertyBlock(); source.GetPropertyBlock(properties); properties.SetFloat("_OpaqueSideEdges", 0); source.SetPropertyBlock(properties); }
            if (edge) Destroy(edge); if (mesh) Destroy(mesh);
        }
    }
}
