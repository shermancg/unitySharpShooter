using System;
using System.Reflection;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Services.Performers
{
    internal class AomPerformer
    {
        private readonly AomTexturesAllocator _texturesAllocator = new();
        private readonly AomParametersService _parametersService = new();

        private RTHandle _aoHandle;
        private RTHandle _blurHandle;
        private RTHandle _spatialHandle;
        private RTHandle _finalHandle;
        private RenderBufferLoadAction _blurLoadAction;

        internal void InitBlueNoise(Texture2D[] blueNoiseTextures) => 
            _parametersService.InitBlueNoise(blueNoiseTextures);

        [Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
        internal void OnCameraSetup(
            CommandBuffer cmd,
            ScriptableRenderer renderer,
            RenderingData renderingData,
            AomSettings settings, 
            Material material,
            bool isAfterOpaque, 
            Action<RTHandle> configureTarget)
        {
            ContextContainer frameData = GetFrameDataUsingReflection(renderingData);
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            
            _parametersService.SetupKeywordsAndParameters(material, settings, cameraData);
            
            _texturesAllocator.AllocateAoPerformerTextureHandles(
                cameraData, 
                settings,
                isAfterOpaque,
                renderer.cameraColorTargetHandle,
                ref _aoHandle, 
                ref _blurHandle, 
                ref _spatialHandle, 
                ref _finalHandle);
            
            _texturesAllocator.SetSourceSize(cmd, _finalHandle);
            configureTarget.Invoke(_finalHandle);
        }

        [Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
        internal void Execute(
            ScriptableRenderContext context,
            ProfilingSampler profilingSampler,
            CameraData cameraData,
            AomSettings settings, 
            Material material,
            ShaderPasses aoPass,
            bool isAfterOpaque,
            bool isDisabled)
        {
            if (material == null)
            {
                Debug.LogErrorFormat(
                    "{0}.Execute(): Missing material. ShadowShard AmbientOcclusionMaster pass will not execute. Check for missing reference in the renderer resources.",
                    GetType().Name);
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, profilingSampler))
            {
                _parametersService.SetGlobalSsaoKeyword(cmd, isAfterOpaque, isDisabled);
                
                cmd.SetGlobalTexture(AmbientOcclusionConstants.AoTextureName, _finalHandle);

#if ENABLE_VR && ENABLE_XR_MODULE
                bool isFoveatedEnabled = HandleFoveatedRendering(cmd, cameraData, settings);
#endif

                ExecuteAmbientOcclusion(cmd, material, aoPass);
                ExecuteBlurPasses(cmd, settings, material, isAfterOpaque);

                if (settings.TemporalFiltering)
                {
                    ExecuteTemporalFilterPass(cmd, material, isAfterOpaque);
                    material.SetTexture(PropertiesIDs.AomHistoryTexture, _finalHandle);
                }
                
                _parametersService.SetAmbientOcclusionParams(cmd, settings.DirectLightingStrength);

#if ENABLE_VR && ENABLE_XR_MODULE
                if (isFoveatedEnabled)
                    cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
#endif
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        internal void Dispose()
        {
            _aoHandle?.Release();
            _blurHandle?.Release();
            _spatialHandle?.Release();
            _finalHandle?.Release();
        }

        private ContextContainer GetFrameDataUsingReflection(RenderingData renderingData)
        {
            // Get the type of RenderingData
            Type renderingDataType = typeof(RenderingData);

            // Find the 'frameData' field, which is internal
            FieldInfo frameDataField =
                renderingDataType.GetField("frameData", BindingFlags.NonPublic | BindingFlags.Instance);

            if (frameDataField != null)
            {
                // Retrieve the value of the internal 'frameData' field
                return (ContextContainer)frameDataField.GetValue(renderingData);
            }
            else
            {
                Debug.LogError("frameData field not found.");
                return null;
            }
        }

        private bool HandleFoveatedRendering(CommandBuffer cmd, CameraData cameraData, AomSettings settings)
        {
            bool isFoveatedEnabled = false;
            if (cameraData.xr.supportsFoveatedRendering)
            {
                if (settings.Downsample ||
                    SystemInfo.foveatedRenderingCaps == FoveatedRenderingCaps.NonUniformRaster ||
                    (SystemInfo.foveatedRenderingCaps == FoveatedRenderingCaps.FoveationImage &&
                     settings.DepthSource == DepthSource.Depth))
                {
                    cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
                }
                else if (SystemInfo.foveatedRenderingCaps == FoveatedRenderingCaps.FoveationImage)
                {
                    cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Enabled);
                    isFoveatedEnabled = true;
                }
            }

            if (isFoveatedEnabled)
                cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);

            return isFoveatedEnabled;
        }

        [Obsolete("Obsolete")]
        private void ExecuteAmbientOcclusion(CommandBuffer cmd, Material material, ShaderPasses aoPass)
        {
            BlitAoTexture(cmd, ref _aoHandle, ref _aoHandle, RenderBufferLoadAction.DontCare, 
                RenderBufferStoreAction.Store, material, aoPass);
        }

        [Obsolete("Obsolete")]
        private void ExecuteBlurPasses(CommandBuffer cmd, AomSettings settings, Material material, bool isAfterOpaque)
        {
            bool settingsAfterOpaque = isAfterOpaque && !settings.TemporalFiltering;
            _blurLoadAction = settingsAfterOpaque ? RenderBufferLoadAction.Load : RenderBufferLoadAction.DontCare;
            
            switch (settings.BlurQuality)
            {
                case BlurQuality.High:
                    PerformBilateralBlur(cmd, settings.TemporalFiltering ? _spatialHandle : _finalHandle, _blurLoadAction, material, settingsAfterOpaque);
                    break;

                case BlurQuality.Medium:
                    PerformGaussianBlur(cmd, settings.TemporalFiltering ? _spatialHandle : _finalHandle, _blurLoadAction, material, settingsAfterOpaque);
                    break;

                case BlurQuality.Low:
                    PerformKawaseBlur(cmd, settings.TemporalFiltering ? _spatialHandle : _finalHandle, _blurLoadAction, material, settingsAfterOpaque);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        [Obsolete("Obsolete")]
        private void ExecuteTemporalFilterPass(CommandBuffer cmd, Material material, bool isAfterOpaque)
        {
            RenderBufferLoadAction finalLoadAction = isAfterOpaque ? RenderBufferLoadAction.Load : RenderBufferLoadAction.DontCare;
            
            BlitAoTexture(cmd, ref _spatialHandle, ref _finalHandle,
                finalLoadAction, RenderBufferStoreAction.Store, material,
                isAfterOpaque ? ShaderPasses.TemporalFilterAfterOpaque : ShaderPasses.TemporalFilter);
        }
        
        [Obsolete("Obsolete")]
        private void PerformBilateralBlur(CommandBuffer cmd, RTHandle finalTexture, RenderBufferLoadAction finalLoadAction, Material material, bool settingsAfterOpaque)
        {
            BlitAoTexture(cmd, ref _aoHandle, ref _blurHandle, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store, material, ShaderPasses.BilateralBlurHorizontal);

            BlitAoTexture(cmd, ref _blurHandle, ref _aoHandle,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 
                ShaderPasses.BilateralBlurVertical);

            BlitAoTexture(cmd, ref _aoHandle, ref finalTexture, finalLoadAction,
                RenderBufferStoreAction.Store, material,
                settingsAfterOpaque ? ShaderPasses.BilateralAfterOpaque : ShaderPasses.BilateralBlurFinal);
        }

        [Obsolete("Obsolete")]
        private void PerformGaussianBlur(CommandBuffer cmd, RTHandle finalTexture, RenderBufferLoadAction finalLoadAction, Material material, bool settingsAfterOpaque)
        {
            BlitAoTexture(cmd, ref _aoHandle, ref _blurHandle,
                RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, material, 
                ShaderPasses.GaussianBlurHorizontal);

            BlitAoTexture(cmd, ref _blurHandle, ref finalTexture, finalLoadAction,
                RenderBufferStoreAction.Store, material,
                settingsAfterOpaque ? ShaderPasses.GaussianAfterOpaque : ShaderPasses.GaussianBlurVertical);
        }

        [Obsolete("Obsolete")]
        private void PerformKawaseBlur(CommandBuffer cmd, RTHandle finalTexture, RenderBufferLoadAction finalLoadAction, Material material, bool settingsAfterOpaque) =>
            BlitAoTexture(cmd, ref _aoHandle, ref finalTexture, finalLoadAction,
                RenderBufferStoreAction.Store, material,
                settingsAfterOpaque ? ShaderPasses.KawaseAfterOpaque : ShaderPasses.KawaseBlur);

        [Obsolete("Obsolete")]
        private void BlitAoTexture(CommandBuffer cmd, ref RTHandle source, ref RTHandle destination, 
            RenderBufferLoadAction loadAction, RenderBufferStoreAction storeAction, Material material, ShaderPasses pass)
        { 
            if (source.rt == null)
            {
                // Obsolete usage of RTHandle aliasing a RenderTargetIdentifier
                Vector2 viewportScale = source.useScaling
                    ? new Vector2(source.rtHandleProperties.rtHandleScale.x,
                        source.rtHandleProperties.rtHandleScale.y)
                    : Vector2.one;

                // Will set the correct camera viewport as well.
                CoreUtils.SetRenderTarget(cmd, destination);
                Blitter.BlitTexture(cmd, source.nameID, viewportScale, material, (int)pass);
            }
            else
                Blitter.BlitCameraTexture(cmd, source, destination, loadAction, storeAction, material, (int)pass);
        }
    }
}
