using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums.Samples;
using UnityEngine;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data.Parameters
{
    internal struct HdaoMaterialParameters
    {
        internal Vector4 HdaoParameters;
        internal Vector4 HdaoParameters2;

        internal readonly bool SampleCountLow;
        internal readonly bool SampleCountMedium;
        internal readonly bool SampleCountHigh;
        internal readonly bool SampleCountUltra;

        internal HdaoMaterialParameters(AomSettings aomSettings, Camera camera)
        {
            HdaoSettings settings = aomSettings.HdaoSettings;

            HdaoParameters = new Vector4(
                settings.Intensity,
                settings.Radius,
                settings.AcceptRadius,
                settings.Falloff
            );

            HdaoParameters2 = new Vector4(GetOffsetCorrection(camera.pixelWidth, camera.pixelHeight), 0.0f, 0.0f,0.0f);

            SampleCountLow = settings.Samples == HdaoSamples.Low;
            SampleCountMedium = settings.Samples == HdaoSamples.Medium;
            SampleCountHigh = settings.Samples == HdaoSamples.High;
            SampleCountUltra = settings.Samples == HdaoSamples.Ultra;
        }

        internal bool Equals(HdaoMaterialParameters other)
        {
            return HdaoParameters == other.HdaoParameters
                   && HdaoParameters2 == other.HdaoParameters2
                   && SampleCountLow == other.SampleCountLow
                   && SampleCountMedium == other.SampleCountMedium
                   && SampleCountHigh == other.SampleCountHigh
                   && SampleCountUltra == other.SampleCountUltra;
        }

        private static float GetOffsetCorrection(int pixelWidth, int pixelHeight)
        {
            float aspectRatio = (float)pixelWidth * pixelHeight;
            const float referenceAspectRatio = 540.0f * 960.0f;
            float aspectRatioRatio = aspectRatio / referenceAspectRatio;

            return Mathf.Max(4, 4 * Mathf.Sqrt(aspectRatioRatio));
        }
    }
}