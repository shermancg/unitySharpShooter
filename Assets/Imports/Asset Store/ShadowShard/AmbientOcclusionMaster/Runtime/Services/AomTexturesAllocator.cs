using System;
using System.Reflection;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Services
{
    internal class AomTexturesAllocator
    {
        internal void AllocateAoRenderGraphTextureHandles(
            RenderGraph renderGraph,
            UniversalResourceData resourceData,
            UniversalCameraData cameraData,
            AomSettings settings,
            bool isAfterOpaque,
            out TextureHandle aoTexture,
            out TextureHandle blurTexture,
            out TextureHandle spatialTexture,
            out TextureHandle finalTexture)
        {
            RenderTextureDescriptor finalTextureDescriptor = cameraData.cameraTargetDescriptor;
            finalTextureDescriptor.colorFormat = AmbientOcclusionConstants.SupportsR8RenderTextureFormat
                ? RenderTextureFormat.R8
                : RenderTextureFormat.ARGB32;
            finalTextureDescriptor.depthBufferBits = 0;
            finalTextureDescriptor.msaaSamples = 1;

            int downsampleDivider = settings.Downsample ? 2 : 1;
            bool useRedComponentOnly = AmbientOcclusionConstants.SupportsR8RenderTextureFormat &&
                                       settings.BlurQuality > BlurQuality.High;

            RenderTextureDescriptor aoBlurDescriptor = finalTextureDescriptor;
            aoBlurDescriptor.colorFormat = useRedComponentOnly ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
            aoBlurDescriptor.width /= downsampleDivider;
            aoBlurDescriptor.height /= downsampleDivider;

            // Handles
            aoTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, aoBlurDescriptor,
                "_AOM_OcclusionTexture0", false, FilterMode.Bilinear);

            blurTexture = settings.BlurQuality != BlurQuality.Low
                ? UniversalRenderer.CreateRenderGraphTexture(renderGraph, aoBlurDescriptor, "_AOM_OcclusionTexture1", false, FilterMode.Bilinear)
                : TextureHandle.nullHandle;
            
            spatialTexture = settings.TemporalFiltering 
                ? UniversalRenderer.CreateRenderGraphTexture(renderGraph, finalTextureDescriptor, "_AOM_HistoryTexture", false, FilterMode.Bilinear) 
                : TextureHandle.nullHandle;

            finalTexture = isAfterOpaque
                ? resourceData.activeColorTexture
                : UniversalRenderer.CreateRenderGraphTexture(renderGraph, finalTextureDescriptor,
                    AmbientOcclusionConstants.AoTextureName, false, FilterMode.Bilinear);

            if (!isAfterOpaque)
                SetSSAOTextureUsingReflection(resourceData, finalTexture);
        }
        
        internal void AllocateAoPerformerTextureHandles(
            UniversalCameraData cameraData,
            AomSettings settings,
            bool isAfterOpaque,
            RTHandle cameraColorTargetHandle,
            ref RTHandle aoHandle,
            ref RTHandle blurHandle,
            ref RTHandle spatialHandle,
            ref RTHandle finalHandle)
        {
            RenderTextureDescriptor finalTextureDescriptor = cameraData.cameraTargetDescriptor;
            finalTextureDescriptor.colorFormat = AmbientOcclusionConstants.SupportsR8RenderTextureFormat
                ? RenderTextureFormat.R8
                : RenderTextureFormat.ARGB32;
            finalTextureDescriptor.depthBufferBits = 0;
            finalTextureDescriptor.msaaSamples = 1;

            int downsampleDivider = settings.Downsample ? 2 : 1;
            bool useRedComponentOnly = AmbientOcclusionConstants.SupportsR8RenderTextureFormat &&
                                       settings.BlurQuality > BlurQuality.High;

            RenderTextureDescriptor aoBlurDescriptor = finalTextureDescriptor;
            aoBlurDescriptor.colorFormat = useRedComponentOnly ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
            aoBlurDescriptor.width /= downsampleDivider;
            aoBlurDescriptor.height /= downsampleDivider;

            // Handles
            RenderingUtils.ReAllocateHandleIfNeeded(ref aoHandle, aoBlurDescriptor, FilterMode.Bilinear,
                TextureWrapMode.Clamp, name: "_AOM_OcclusionTexture0"); // aoTexture

            if (settings.BlurQuality != BlurQuality.Low)
                RenderingUtils.ReAllocateHandleIfNeeded(ref blurHandle, aoBlurDescriptor, FilterMode.Bilinear,
                    TextureWrapMode.Clamp, name: "_AOM_OcclusionTexture1"); // blurTexture
            else
            {
                blurHandle = null;
            }

            if (settings.TemporalFiltering)
                RenderingUtils.ReAllocateHandleIfNeeded(ref spatialHandle, finalTextureDescriptor, FilterMode.Bilinear,
                    TextureWrapMode.Clamp, name: "_AOM_HistoryTexture"); // spatialTexture
            else
            {
                spatialHandle = null;
            }

            if(isAfterOpaque)
                finalHandle = cameraColorTargetHandle;
            else
            {
                RenderingUtils.ReAllocateHandleIfNeeded(ref finalHandle, finalTextureDescriptor, FilterMode.Bilinear,
                    TextureWrapMode.Clamp, name: "_AOM_OcclusionTexture"); // ssao finalTexture
            }
        }

        internal void SetSourceSize(CommandBuffer cmd, RTHandle source)
        {
            float width = source.rt.width;
            float height = source.rt.height;

            if (source.rt.useDynamicScale)
            {
                width *= ScalableBufferManager.widthScaleFactor;
                height *= ScalableBufferManager.heightScaleFactor;
            }

            cmd.SetGlobalVector(PropertiesIDs.SourceSize, new Vector4(width, height, 1.0f / width, 1.0f / height));
        }

        private void SetSSAOTextureUsingReflection(UniversalResourceData resourceData, TextureHandle textureHandle)
        {
            Type type = typeof(UniversalResourceData);
            PropertyInfo ssaoTextureProperty = type.GetProperty("ssaoTexture",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (ssaoTextureProperty != null)
                ssaoTextureProperty.SetValue(resourceData, textureHandle);
            else
                Debug.LogError("ssaoTexture property not found.");
        }
    }
}