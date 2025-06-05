using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using ShadowShard.AmbientOcclusionMaster.Runtime.Services;
using ShadowShard.AmbientOcclusionMaster.Runtime.Services.Performers;
using ShadowShard.AmbientOcclusionMaster.Runtime.Services.RenderGraphs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ShadowShard.AmbientOcclusionMaster.Runtime
{
    internal class AmbientOcclusionMasterPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler _profilingSampler = new(nameof(AmbientOcclusionMasterPass));
        private readonly AomPassSetup _passSetup = new();
        private readonly AomPerformer _aomPerformer = new();
        private readonly AomRenderGraph _aomRenderGraph = new();
        private readonly AomSettingsService _aomSettingsService = new();
        
        private ScriptableRenderer _renderer;
        private AomSettings _aomSettings;
        private Material _material;
        
        private RenderBufferLoadAction _blurLoadAction;

        private ShaderPasses _aoPass;
        private bool _isAfterOpaque;

        internal bool Setup(ScriptableRenderer renderer, AomSettings defaultAomSettings, Material material, Texture2D[] blueNoiseTextures)
        {
            _renderer = renderer;
            _material = material;
            _aomSettings = _aomSettingsService.GetFromVolumeComponent(defaultAomSettings);

            _aomPerformer.InitBlueNoise(blueNoiseTextures);
            _aomRenderGraph.InitBlueNoise(blueNoiseTextures);

            ConfigurePass();

            IAmbientOcclusionSettings aoSettings = _aomSettingsService.GetAmbientOcclusionSettings(_aomSettings);
            return _material != null && 
                   aoSettings is { Intensity: > 0.0f, Radius: > 0.0f, Falloff: > 0.0f } &&
                   !_aomSettingsService.IsAmbientOcclusionModeNone(_aomSettings);
        }

        private void ConfigurePass()
        {
            _isAfterOpaque = _passSetup.IsAfterOpaque(_aomSettings.AfterOpaque, _aomSettings.DebugMode);
            _passSetup.SetRenderPassEventAndNormalsSource(_aomSettings, _isAfterOpaque, ref _aomSettings.DepthSource,
                out RenderPassEvent passEvent);
            renderPassEvent = passEvent;

            _passSetup.ConfigureRenderPassInputs(_aomSettings, ConfigureInput);
            
            _aoPass = _passSetup.GetAmbientOcclusionPass(_aomSettings.AmbientOcclusionMode);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) =>
            _aomRenderGraph.Record(renderGraph, frameData, _profilingSampler, _aomSettings, _material, _aoPass, _isAfterOpaque);

        [Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) => 
            _aomPerformer.OnCameraSetup(cmd, _renderer, renderingData, _aomSettings, _material, _isAfterOpaque, ConfigureTargetAndClear);

        [Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) => 
            _aomPerformer.Execute(
                context, 
                _profilingSampler, 
                renderingData.cameraData, 
                _aomSettings, 
                _material, 
                _aoPass, 
                _isAfterOpaque, 
                _aomSettingsService.IsAmbientOcclusionModeNone(_aomSettings));

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));

            if (!_isAfterOpaque)
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, false);
        }

        internal void Dispose() => 
            _aomPerformer?.Dispose();

        private void ConfigureTargetAndClear(RTHandle finalTextureHandle)
        {
#pragma warning disable CS0618
            ConfigureTarget(_isAfterOpaque
                ? _renderer.cameraColorTargetHandle
                : finalTextureHandle);
            ConfigureClear(ClearFlag.None, Color.white);
#pragma warning restore CS0618
        }
    }
}