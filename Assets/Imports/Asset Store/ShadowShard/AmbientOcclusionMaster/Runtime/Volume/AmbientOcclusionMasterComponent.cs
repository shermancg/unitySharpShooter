using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums.Samples;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RenderingPath = ShadowShard.AmbientOcclusionMaster.Runtime.Enums.RenderingPath;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Volume
{
    [Serializable, VolumeComponentMenu("Lighting/ShadowShard/Ambient Occlusion Master")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public class AmbientOcclusionMasterComponent : VolumeComponent
    {
        private AmbientOcclusionMasterComponent() =>
            displayName = "ShadowShard Ambient Occlusion Master";

        public EnumParameter<AmbientOcclusionMode> Mode = new(AmbientOcclusionMode.None);

        //SSAO Settings
        public MinFloatParameter SsaoIntensity = new(3.0f, 0.0f);
        public MinFloatParameter SsaoRadius = new(0.1f, 0.0f);
        public MinFloatParameter SsaoFalloff = new(100.0f, 0.0f);
        public EnumParameter<SsaoSamples> SsaoSamplesCount = new(SsaoSamples.Medium);

        //HDAO Settings
        public MinFloatParameter HdaoIntensity = new(0.6f, 0.0f);
        public FloatParameter HdaoRejectRadius = new(2.0f);
        public FloatParameter HdaoAcceptRadius = new(0.003f);
        public MinFloatParameter HdaoFalloff = new(100.0f, 0.0f);
        public EnumParameter<HdaoSamples> HdaoSamples = new(Enums.Samples.HdaoSamples.Low);

        //HBAO Settings
        public MinFloatParameter HbaoIntensity = new(3.0f, 0.0f);
        public MinFloatParameter HbaoRadius = new(0.3f, 0.0f);
        public ClampedIntParameter HbaoMaxRadiusInPixels = new(40, 4, 256);
        public ClampedFloatParameter HbaoAngleBias = new(0.1f, 0.0f, 0.9f);
        public MinFloatParameter HbaoFalloff = new(100.0f, 0.0f);
        public EnumParameter<HbaoDirections> HbaoDirections = new(Enums.Samples.HbaoDirections.Directions2);
        public EnumParameter<HbaoSamples> HbaoSamples = new(Enums.Samples.HbaoSamples.Samples4);

        //GTAO Settings
        public MinFloatParameter GtaoIntensity = new(3.0f, 0.0f);
        public MinFloatParameter GtaoRadius = new(0.3f, 0.0f);
        public ClampedIntParameter GtaoMaxRadiusInPixels = new(40, 4, 256);
        public MinFloatParameter GtaoFalloff = new(100.0f, 0.0f);
        public ClampedIntParameter GtaoDirections = new(2, 1, 6);
        public EnumParameter<GtaoSamples> GtaoSamples = new(Enums.Samples.GtaoSamples.Samples4);

        // General AOM settings
        public BoolParameter MultiBounce = new(false);
        public ClampedFloatParameter DirectLightingStrength = new(0.25f, 0.0f, 1.0f);
        public EnumParameter<NoiseMethod> NoiseType = new(NoiseMethod.BlueNoise);
        public EnumParameter<BlurQuality> BlurMode = new(BlurQuality.High);
        public BoolParameter TemporalFiltering = new(false);
        public ClampedFloatParameter TemporalScale = new(1.0f, 1.0f, 5.0f);
        public ClampedFloatParameter TemporalResponse = new(1.0f, 0.0f, 1.0f);

        // Rendering AOM settings
        public BoolParameter DebugMode = new(false);
        public EnumParameter<RenderingPath> RenderPath = new(RenderingPath.Forward);
        public BoolParameter AfterOpaque = new(true);
        public BoolParameter Downsample = new(false);
        public EnumParameter<DepthSource> Source = new(DepthSource.Depth);
        public EnumParameter<NormalQuality> NormalsQuality = new(NormalQuality.Medium);

        public static AmbientOcclusionMasterComponent GetAmbientOcclusionMasterComponent() =>
            VolumeManager.instance.stack.GetComponent<AmbientOcclusionMasterComponent>();
    }
}