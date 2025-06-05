using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Passes;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Services.RenderGraphs
{
    internal class AomRenderGraph
    {
        private readonly AomTexturesAllocator _texturesAllocator = new();
        private readonly AomParametersService _parametersService = new();

        private RenderBufferLoadAction _blurLoadAction;

        internal void InitBlueNoise(Texture2D[] blueNoiseTextures) => 
            _parametersService.InitBlueNoise(blueNoiseTextures);

        internal void Record(RenderGraph renderGraph,
            ContextContainer frameData,
            ProfilingSampler profilingSampler,
            AomSettings settings,
            Material material,
            ShaderPasses aoPass,
            bool isAfterOpaque)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            // Allocate render graph texture handles
            _texturesAllocator.AllocateAoRenderGraphTextureHandles(renderGraph,
                resourceData,
                cameraData,
                settings,
                isAfterOpaque,
                out TextureHandle aoTexture,
                out TextureHandle blurTexture,
                out TextureHandle spatialTexture,
                out TextureHandle finalTexture);

            // Get the resources
            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
            TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;
            TextureHandle motionVectorsTexture = resourceData.motionVectorColor;

            // Update keywords and other shader params
            _parametersService.SetupKeywordsAndParameters(material, settings, cameraData);

            using IUnsafeRenderGraphBuilder builder = renderGraph.AddUnsafePass("Blit AOM", out AoPassData passData, profilingSampler);
            builder.AllowGlobalStateModification(true);
            builder.AllowPassCulling(false);

            SetupAoPassData(passData, material, resourceData.cameraColor, aoTexture, blurTexture, spatialTexture, finalTexture);
            DeclarePassTextures(builder, passData, settings, cameraDepthTexture, cameraNormalsTexture, motionVectorsTexture, isAfterOpaque);

            builder.SetRenderFunc((AoPassData data, UnsafeGraphContext rgContext) =>
            {
                CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(rgContext.cmd);

                SetupCameraTextures(cmd, data);
                ExecuteAoPass(aoPass, cmd, data);
                ExecuteBlurPasses(cmd, data, settings, isAfterOpaque);
                
                if (settings.TemporalFiltering)
                {
                    ExecuteTemporalFilterPass(cmd, data, isAfterOpaque);
                    data.Material.SetTexture(PropertiesIDs.AomHistoryTexture, data.FinalTexture);
                }
                
                if (isAfterOpaque)
                    return;

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, true);
                _parametersService.SetAmbientOcclusionParams(cmd, settings.DirectLightingStrength);
            });
        }

        private void SetupAoPassData(
            AoPassData passData,
            Material material,
            TextureHandle cameraColor,
            TextureHandle aoTexture,
            TextureHandle blurTexture,
            TextureHandle spatialTexture,
            TextureHandle finalTexture)
        {
            passData.Material = material;
            passData.CameraColor = cameraColor;
            passData.AOTexture = aoTexture;
            passData.BlurTexture = blurTexture;
            passData.SpatialTexture = spatialTexture;
            passData.FinalTexture = finalTexture;
        }

        private void DeclarePassTextures(
            IUnsafeRenderGraphBuilder builder,
            AoPassData passData,
            AomSettings settings,
            TextureHandle cameraDepthTexture,
            TextureHandle cameraNormalsTexture,
            TextureHandle motionVectorsTexture,
            bool isAfterOpaque)
        {
            if (cameraDepthTexture.IsValid())
                builder.UseTexture(cameraDepthTexture, AccessFlags.Read);

            if (settings.DepthSource == DepthSource.DepthNormals && cameraNormalsTexture.IsValid())
            {
                builder.UseTexture(cameraNormalsTexture, AccessFlags.Read);
                passData.CameraNormalsTexture = cameraNormalsTexture;
            }

            if (settings.TemporalFiltering && motionVectorsTexture.IsValid())
            {
                builder.UseTexture(motionVectorsTexture, AccessFlags.Read);
                passData.MotionVectorsTexture = motionVectorsTexture;
            }
            
            builder.UseTexture(passData.AOTexture, AccessFlags.ReadWrite);

            if (settings.BlurQuality != BlurQuality.Low)
                builder.UseTexture(passData.BlurTexture, AccessFlags.ReadWrite);
            
            if (settings.TemporalFiltering && passData.SpatialTexture.IsValid()) 
                builder.UseTexture(passData.SpatialTexture, AccessFlags.ReadWrite);

            if (!isAfterOpaque && passData.FinalTexture.IsValid())
            {
                builder.UseTexture(passData.FinalTexture, AccessFlags.ReadWrite);
                builder.SetGlobalTextureAfterPass(passData.FinalTexture, PropertiesIDs.AoFinalTexture);
            }
        }

        private void SetupCameraTextures(CommandBuffer cmd, AoPassData data)
        {
            if (data.CameraColor.IsValid())
                _texturesAllocator.SetSourceSize(cmd, data.CameraColor);

            if (data.CameraNormalsTexture.IsValid())
                data.Material.SetTexture(PropertiesIDs.CameraNormalsTexture, data.CameraNormalsTexture);
            
            if (data.MotionVectorsTexture.IsValid())
                data.Material.SetTexture(PropertiesIDs.MotionVectorTexture, data.MotionVectorsTexture);
        }

        private void ExecuteAoPass(ShaderPasses aoPass, CommandBuffer cmd, AoPassData data) =>
            Blitter.BlitCameraTexture(cmd, data.AOTexture, data.AOTexture,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, data.Material, (int)aoPass);

        private void ExecuteBlurPasses(CommandBuffer cmd, AoPassData data, AomSettings settings, bool isAfterOpaque)
        {
            bool settingsAfterOpaque = isAfterOpaque && !settings.TemporalFiltering;
            _blurLoadAction = settingsAfterOpaque ? RenderBufferLoadAction.Load : RenderBufferLoadAction.DontCare;
            
            switch (settings.BlurQuality)
            {
                case BlurQuality.High:
                    PerformBilateralBlur(cmd, data, settings.TemporalFiltering ? data.SpatialTexture : data.FinalTexture, _blurLoadAction, settingsAfterOpaque);
                    break;

                case BlurQuality.Medium:
                    PerformGaussianBlur(cmd, data, settings.TemporalFiltering ? data.SpatialTexture : data.FinalTexture, _blurLoadAction, settingsAfterOpaque);
                    break;

                case BlurQuality.Low:
                    PerformKawaseBlur(cmd, data, settings.TemporalFiltering ? data.SpatialTexture : data.FinalTexture, _blurLoadAction, settingsAfterOpaque);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void ExecuteTemporalFilterPass(CommandBuffer cmd, AoPassData data, bool isAfterOpaque)
        {
            RenderBufferLoadAction finalLoadAction = isAfterOpaque ? RenderBufferLoadAction.Load : RenderBufferLoadAction.DontCare;
            
            Blitter.BlitCameraTexture(cmd, data.SpatialTexture, data.FinalTexture,
                finalLoadAction, RenderBufferStoreAction.Store, data.Material,
                (int)(isAfterOpaque ? ShaderPasses.TemporalFilterAfterOpaque : ShaderPasses.TemporalFilter));
        }

        private void PerformBilateralBlur(CommandBuffer cmd, AoPassData data, TextureHandle finalTexture,
            RenderBufferLoadAction finalLoadAction, bool settingsAfterOpaque)
        {
            Blitter.BlitCameraTexture(cmd, data.AOTexture, data.BlurTexture,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                data.Material, (int)ShaderPasses.BilateralBlurHorizontal);

            Blitter.BlitCameraTexture(cmd, data.BlurTexture, data.AOTexture,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                data.Material, (int)ShaderPasses.BilateralBlurVertical);

            Blitter.BlitCameraTexture(cmd, data.AOTexture, finalTexture, finalLoadAction,
                RenderBufferStoreAction.Store, data.Material,
                (int)(settingsAfterOpaque ? ShaderPasses.BilateralAfterOpaque : ShaderPasses.BilateralBlurFinal));
        }

        private void PerformGaussianBlur(CommandBuffer cmd, AoPassData data, TextureHandle finalTexture,
            RenderBufferLoadAction finalLoadAction, bool settingsAfterOpaque)
        {
            Blitter.BlitCameraTexture(cmd, data.AOTexture, data.BlurTexture,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                data.Material, (int)ShaderPasses.GaussianBlurHorizontal);

            Blitter.BlitCameraTexture(cmd, data.BlurTexture, finalTexture, finalLoadAction,
                RenderBufferStoreAction.Store, data.Material,
                (int)(settingsAfterOpaque ? ShaderPasses.GaussianAfterOpaque : ShaderPasses.GaussianBlurVertical));
        }

        private void PerformKawaseBlur(CommandBuffer cmd, AoPassData data, TextureHandle finalTexture, 
            RenderBufferLoadAction finalLoadAction, bool settingsAfterOpaque) =>
            Blitter.BlitCameraTexture(cmd, data.AOTexture, finalTexture, finalLoadAction,
                RenderBufferStoreAction.Store, data.Material,
                (int)(settingsAfterOpaque ? ShaderPasses.KawaseAfterOpaque : ShaderPasses.KawaseBlur));
    }
}