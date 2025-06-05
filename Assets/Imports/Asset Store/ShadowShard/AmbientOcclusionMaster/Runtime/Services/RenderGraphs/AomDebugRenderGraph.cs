using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Passes;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Services.RenderGraphs
{
    internal class AomDebugRenderGraph
    {
        private readonly Material _material;

        internal AomDebugRenderGraph(Material material) =>
            _material = material;

        internal void Record(RenderGraph renderGraph,
            ContextContainer frameData,
            ProfilingSampler profilingSampler)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            using IUnsafeRenderGraphBuilder builder =
                renderGraph.AddUnsafePass("Blit AOM Debug", out AoDebugPassData passData, profilingSampler);

            builder.AllowPassCulling(false);

            // Setup pass data
            SetupAoDebugPassData(passData, resourceData.activeColorTexture);

            builder.SetRenderFunc((AoDebugPassData data, UnsafeGraphContext rgContext) =>
            {
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(rgContext.cmd);
                ExecuteAoDebugPass(cmd, data);
            });
        }

        private void SetupAoDebugPassData(AoDebugPassData passData, TextureHandle cameraColor) =>
            passData.CameraColor = cameraColor;

        private void ExecuteAoDebugPass(CommandBuffer cmd, AoDebugPassData data) =>
            Blitter.BlitCameraTexture(cmd, data.CameraColor, data.CameraColor, RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store, _material, (int)ShaderPasses.Debug);
    }
}