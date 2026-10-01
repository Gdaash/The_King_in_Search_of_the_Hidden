using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>Applies the world portion of the CRT presentation after URP post processing.</summary>
public sealed class ThoseUnderHexCrtRendererFeature : ScriptableRendererFeature
{
    [Tooltip("Material using Hidden/ThoseUnderHex/CRT World.")]
    public Material material;

    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    private CrtPass pass;

    public override void Create()
    {
        pass = new CrtPass { renderPassEvent = renderPassEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null || !ThoseUnderHexCrtSettings.IsEnabled)
            return;

        pass.Setup(material);
        renderer.EnqueuePass(pass);
    }

    private sealed class CrtPass : ScriptableRenderPass
    {
        private const string PassName = "Those UnderHex CRT";
        private Material material;

        public void Setup(Material sourceMaterial)
        {
            material = sourceMaterial;
            // The pass samples the camera color, so URP must provide an intermediate target.
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc descriptor = renderGraph.GetTextureDesc(source);
            descriptor.name = "CameraColor-ThoseUnderHexCRT";
            descriptor.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(descriptor);

            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0), PassName);
            resourceData.cameraColor = destination;
        }
    }
}
