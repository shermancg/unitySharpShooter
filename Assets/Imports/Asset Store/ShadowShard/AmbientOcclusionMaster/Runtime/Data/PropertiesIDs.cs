using UnityEngine;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data
{
    internal static class PropertiesIDs
    {
        internal static readonly int AoColor = Shader.PropertyToID("_AoColor");

        internal static readonly int SsaoParameters = Shader.PropertyToID("_SsaoParameters");

        internal static readonly int HdaoParameters = Shader.PropertyToID("_HdaoParameters");
        internal static readonly int HdaoParameters2 = Shader.PropertyToID("_HdaoParameters2");

        internal static readonly int HbaoParameters = Shader.PropertyToID("_HbaoParameters");
        internal static readonly int HbaoParameters2 = Shader.PropertyToID("_HbaoParameters2");

        internal static readonly int GtaoParameters = Shader.PropertyToID("_GtaoParameters");
        internal static readonly int GtaoParameters2 = Shader.PropertyToID("_GtaoParameters2");

        internal static readonly int CameraNormalsTexture = Shader.PropertyToID("_CameraNormalsTexture");
        internal static readonly int MotionVectorTexture = Shader.PropertyToID("_MotionVectorTexture");
        internal static readonly int AomHistoryTexture = Shader.PropertyToID("_AOM_HistoryTexture");
        internal static readonly int AoFinalTexture = Shader.PropertyToID(AmbientOcclusionConstants.AoTextureName);

        internal static readonly int AomBlueNoiseParameters = Shader.PropertyToID("_AomBlueNoiseParameters");
        internal static readonly int BlueNoiseTexture = Shader.PropertyToID("_BlueNoiseTexture");
        
        internal static readonly int AomTemporalScale = Shader.PropertyToID("_AOM_TemporalScale");
        internal static readonly int AomTemporalResponse = Shader.PropertyToID("_AOM_TemporalResponse");
        internal static readonly int AomTemporalRotation = Shader.PropertyToID("_AOM_TemporalRotation");
        internal static readonly int AomTemporalOffset = Shader.PropertyToID("_AOM_TemporalOffset");

        internal static readonly int SourceSize = Shader.PropertyToID("_SourceSize");
        internal static readonly int Downsample = Shader.PropertyToID("_Downsample");
            
        internal static readonly int CameraViewXExtent = Shader.PropertyToID("_CameraViewXExtent");
        internal static readonly int CameraViewYExtent = Shader.PropertyToID("_CameraViewYExtent");
        internal static readonly int CameraViewZExtent = Shader.PropertyToID("_CameraViewZExtent");
        internal static readonly int ProjectionParams2 = Shader.PropertyToID("_ProjectionParams2");
        internal static readonly int CameraViewProjections = Shader.PropertyToID("_CameraViewProjections");
        internal static readonly int CameraViewTopLeftCorner = Shader.PropertyToID("_CameraViewTopLeftCorner");
    }
}