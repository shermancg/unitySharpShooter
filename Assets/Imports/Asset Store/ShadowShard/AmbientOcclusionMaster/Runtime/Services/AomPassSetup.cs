using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using UnityEngine.Rendering.Universal;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Services
{
    internal class AomPassSetup
    {
        internal void SetRenderPassEventAndNormalsSource(AomSettings settings, bool isAfterOpaque, ref DepthSource depthSource,
            out RenderPassEvent renderPassEvent)
        {
            if (settings.RenderingPath == RenderingPath.Deferred)
                renderPassEvent = isAfterOpaque
                    ? RenderPassEvent.AfterRenderingOpaques
                    : RenderPassEvent.AfterRenderingGbuffer;
            else
                renderPassEvent = isAfterOpaque
                    ? RenderPassEvent.BeforeRenderingTransparents
                    : RenderPassEvent.AfterRenderingPrePasses + 1;

            if (settings.RenderingPath == RenderingPath.Deferred)
                depthSource = DepthSource.DepthNormals;
            
            if(settings.AmbientOcclusionMode == AmbientOcclusionMode.HDAO)
                depthSource = DepthSource.Depth;
        }

        internal void ConfigureRenderPassInputs(AomSettings settings, Action<ScriptableRenderPassInput> configureInput)
        {
            ScriptableRenderPassInput input = settings.DepthSource switch
            {
                DepthSource.Depth => ScriptableRenderPassInput.Depth,
                DepthSource.DepthNormals => ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (settings.TemporalFiltering) 
                input |= ScriptableRenderPassInput.Motion;

            configureInput(input);
        }

        internal ShaderPasses GetAmbientOcclusionPass(AmbientOcclusionMode mode)
        {
            return mode switch
            {
                AmbientOcclusionMode.None => ShaderPasses.SSAO,
                AmbientOcclusionMode.SSAO => ShaderPasses.SSAO,
                AmbientOcclusionMode.HDAO => ShaderPasses.HDAO,
                AmbientOcclusionMode.HBAO => ShaderPasses.HBAO,
                AmbientOcclusionMode.GTAO => ShaderPasses.GTAO,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        internal bool IsAfterOpaque(bool afterOpaque, bool debugMode) =>
            afterOpaque && !debugMode;
    }
}