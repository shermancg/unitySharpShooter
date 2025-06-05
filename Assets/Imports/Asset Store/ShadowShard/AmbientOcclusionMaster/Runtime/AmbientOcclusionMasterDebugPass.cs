using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using ShadowShard.AmbientOcclusionMaster.Runtime.Services.Performers;
using ShadowShard.AmbientOcclusionMaster.Runtime.Services.RenderGraphs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ShadowShard.AmbientOcclusionMaster.Runtime
{
    internal class AmbientOcclusionMasterDebugPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler _profilingSampler = new(nameof(AmbientOcclusionMasterDebugPass));
        private readonly AomSettings _defaultSettings = new();
        private readonly AomSettingsService _aomSettingsService = new();

        private Material _material;
        private AomSettings _aomSettings;

        private AomDebugRenderGraph _renderGraph;
        private AomDebugPerformer _performer;

        public bool Setup(ScriptableRenderer renderer, Material material)
        {
            _material = material;
            _aomSettings = _aomSettingsService.GetFromVolumeComponent(_defaultSettings);
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

            _renderGraph = new AomDebugRenderGraph(material);
            _performer = new AomDebugPerformer(renderer, material);

            IAmbientOcclusionSettings aoSettings = _aomSettingsService.GetAmbientOcclusionSettings(_aomSettings);
            return _material != null
                   && aoSettings is { Intensity: > 0.0f, Radius: > 0.0f, Falloff: > 0.0f }
                   && !_aomSettingsService.IsAmbientOcclusionModeNone(_aomSettings)
                   && _aomSettings.DebugMode;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) =>
            _renderGraph.Record(renderGraph, frameData, _profilingSampler);

        [Obsolete(
            "This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.",
            false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) =>
            _performer.OnCameraSetup(ConfigureTarget);

        [Obsolete(
            "This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.",
            false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) =>
            _performer.Execute(context, _profilingSampler);

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));
        }
    }
}