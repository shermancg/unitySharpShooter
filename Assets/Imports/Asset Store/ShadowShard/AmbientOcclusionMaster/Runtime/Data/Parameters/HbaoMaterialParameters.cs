using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums.Samples;
using UnityEngine;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data.Parameters
{
    internal struct HbaoMaterialParameters
    {
        internal Vector4 HbaoParameters;
        internal Vector4 HbaoParameters2;

        internal readonly bool DirectionCountTwo;
        internal readonly bool DirectionCountFour;
        internal readonly bool DirectionCountSix;

        internal readonly bool SampleCountTwo;
        internal readonly bool SampleCountFour;
        internal readonly bool SampleCountSix;
        internal readonly bool SampleCountEight;

        internal HbaoMaterialParameters(AomSettings aomSettings, Camera camera)
        {
            HbaoSettings settings = aomSettings.HbaoSettings;

            HbaoParameters = new Vector4(
                settings.Intensity,
                settings.Radius,
                1.0f / ((int)settings.HbaoSamples + 1.0f), // Inverted Samples + One
                settings.Falloff
            );

            HbaoParameters2 = new Vector4(
                SetMaxRadius(camera.pixelWidth, camera.pixelHeight, settings.MaxRadiusPixel),
                settings.AngleBias,
                SetFovCorrection(camera.fieldOfView, camera.pixelHeight),
                1.0f / (HbaoParameters.y * HbaoParameters.y)
            );

            DirectionCountSix = settings.HbaoDirections == HbaoDirections.Directions6;
            DirectionCountFour = settings.HbaoDirections == HbaoDirections.Directions4;
            DirectionCountTwo = settings.HbaoDirections == HbaoDirections.Directions2;

            SampleCountEight = settings.HbaoSamples == HbaoSamples.Samples8;
            SampleCountSix = settings.HbaoSamples == HbaoSamples.Samples6;
            SampleCountFour = settings.HbaoSamples == HbaoSamples.Samples4;
            SampleCountTwo = settings.HbaoSamples == HbaoSamples.Samples2;
        }

        internal bool Equals(HbaoMaterialParameters other)
        {
            return HbaoParameters == other.HbaoParameters
                   && HbaoParameters2 == other.HbaoParameters2
                   && DirectionCountSix == other.DirectionCountSix
                   && DirectionCountFour == other.DirectionCountFour
                   && DirectionCountTwo == other.DirectionCountTwo
                   && SampleCountEight == other.SampleCountEight
                   && SampleCountSix == other.SampleCountSix
                   && SampleCountFour == other.SampleCountFour
                   && SampleCountTwo == other.SampleCountTwo;
        }

        private static float SetMaxRadius(int pixelWidth, int pixelHeight, int maxRadiusPixels)
        {
            float aspectRatio = (float)pixelWidth * pixelHeight;
            const float referenceAspectRatio = 540.0f * 960.0f;

            float aspectRatioRatio = aspectRatio / referenceAspectRatio;

            return Mathf.Max(4, maxRadiusPixels * Mathf.Sqrt(aspectRatioRatio));
        }

        private static float SetFovCorrection(float fieldOfView, int pixelHeight)
        {
            float fovRad = fieldOfView * Mathf.Deg2Rad;
            float invHalfTanFOV = 1 / Mathf.Tan(fovRad * 0.5f);

            return pixelHeight * invHalfTanFOV * 0.25f;
        }
    }
}