using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using UnityEngine;
using RenderingPath = ShadowShard.AmbientOcclusionMaster.Runtime.Enums.RenderingPath;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings
{
    [Serializable]
    public class AomSettings
    {
        [SerializeField] public AmbientOcclusionMode AmbientOcclusionMode;

        [SerializeField] public SsaoSettings SsaoSettings = new();
        [SerializeField] public HdaoSettings HdaoSettings = new();
        [SerializeField] public HbaoSettings HbaoSettings = new();
        [SerializeField] public GtaoSettings GtaoSettings = new();

        [SerializeField] public bool MultiBounce;
        [SerializeField] public float DirectLightingStrength = 0.25f;
        [SerializeField] public NoiseMethod NoiseMethod = NoiseMethod.InterleavedGradient;
        [SerializeField] public BlurQuality BlurQuality = BlurQuality.High;

        [SerializeField] public bool DebugMode;
        [SerializeField] public RenderingPath RenderingPath = RenderingPath.Forward;
        [SerializeField] public bool AfterOpaque = true;
        [SerializeField] public bool Downsample;
        [SerializeField] public DepthSource DepthSource = DepthSource.Depth;
        [SerializeField] public NormalQuality NormalQuality = NormalQuality.Medium;
        
        [SerializeField] public bool TemporalFiltering;
        [SerializeField] public float TemporalScale = 1;
        [SerializeField] public float TemporalResponse = 1;
    }
}