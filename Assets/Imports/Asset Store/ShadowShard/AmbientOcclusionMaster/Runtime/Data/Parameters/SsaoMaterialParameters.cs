using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums.Samples;
using UnityEngine;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data.Parameters
{
    internal struct SsaoMaterialParameters
    {
        internal Vector4 SsaoParameters;

        internal readonly bool SampleCountLow;
        internal readonly bool SampleCountMedium;
        internal readonly bool SampleCountHigh;

        internal SsaoMaterialParameters(AomSettings aomSettings)
        {
            SsaoSettings settings = aomSettings.SsaoSettings;
            float radiusMultiplier = aomSettings.NoiseMethod == NoiseMethod.BlueNoise ? 1.5f : 1.0f;

            SsaoParameters = new Vector4(
                settings.Intensity,
                settings.Radius * radiusMultiplier,
                settings.Falloff,
                0.0f
            );

            SampleCountLow = settings.SamplesCount == SsaoSamples.Low;
            SampleCountMedium = settings.SamplesCount == SsaoSamples.Medium;
            SampleCountHigh = settings.SamplesCount == SsaoSamples.High;
        }

        internal bool Equals(SsaoMaterialParameters other)
        {
            return SsaoParameters == other.SsaoParameters
                   && SampleCountLow == other.SampleCountLow
                   && SampleCountMedium == other.SampleCountMedium
                   && SampleCountHigh == other.SampleCountHigh;
        }
    }
}