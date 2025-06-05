using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using UnityEngine;
using RenderingPath = ShadowShard.AmbientOcclusionMaster.Runtime.Enums.RenderingPath;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data.Parameters
{
    internal readonly struct GeneralParameters
    {
        internal readonly bool MultiBounce;
        internal readonly bool NoiseMethodInterleavedGradient;
        internal readonly bool NoiseMethodPseudoRandom;
        internal readonly bool NoiseMethodBlueNoise;
        
        internal readonly bool TemporalFiltering;

        internal readonly bool OrthographicCamera;
        internal readonly bool Downsample;

        internal readonly bool SourceDepthNormals;
        internal readonly bool SourceDepthHigh;
        internal readonly bool SourceDepthMedium;
        internal readonly bool SourceDepthLow;

        internal readonly bool DebugMode;
        internal readonly bool IsDeferredRendering;
        internal readonly bool IsAfterOpaque;

        internal GeneralParameters(AomSettings settings, bool isOrthographic)
        {
            MultiBounce = settings.MultiBounce;
                
            NoiseMethodInterleavedGradient = settings.NoiseMethod == NoiseMethod.InterleavedGradient;
            NoiseMethodPseudoRandom = settings.NoiseMethod == NoiseMethod.PseudoRandom;
            NoiseMethodBlueNoise = settings.NoiseMethod == NoiseMethod.BlueNoise;

            TemporalFiltering = settings.TemporalFiltering;

            OrthographicCamera = isOrthographic;
            Downsample = settings.Downsample;

            bool isUsingDepthNormals = settings.DepthSource == DepthSource.DepthNormals;
            SourceDepthNormals = isUsingDepthNormals;
            SourceDepthHigh = !isUsingDepthNormals && settings.NormalQuality == NormalQuality.High;
            SourceDepthMedium = !isUsingDepthNormals && settings.NormalQuality == NormalQuality.Medium;
            SourceDepthLow = !isUsingDepthNormals && settings.NormalQuality == NormalQuality.Low;

            DebugMode = settings.DebugMode;
            IsDeferredRendering = settings.RenderingPath == RenderingPath.Deferred;
            IsAfterOpaque = settings.AfterOpaque;
        }

        internal bool Equals(GeneralParameters other)
        {
            return MultiBounce == other.MultiBounce
                   && NoiseMethodInterleavedGradient == other.NoiseMethodInterleavedGradient
                   && NoiseMethodPseudoRandom == other.NoiseMethodPseudoRandom
                   && NoiseMethodBlueNoise == other.NoiseMethodBlueNoise
                   && TemporalFiltering == other.TemporalFiltering
                   && OrthographicCamera == other.OrthographicCamera
                   && Downsample == other.Downsample
                   && SourceDepthNormals == other.SourceDepthNormals
                   && SourceDepthHigh == other.SourceDepthHigh
                   && SourceDepthMedium == other.SourceDepthMedium
                   && SourceDepthLow == other.SourceDepthLow
                   && DebugMode == other.DebugMode
                   && IsDeferredRendering == other.IsDeferredRendering
                   && IsAfterOpaque == other.IsAfterOpaque;
        }
    }
}