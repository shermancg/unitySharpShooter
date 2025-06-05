using UnityEngine;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data
{
    internal static class AmbientOcclusionConstants
    {
        internal const string AoTextureName = "_ScreenSpaceOcclusionTexture";
        internal const string AmbientOcclusionParamName = "_AmbientOcclusionParam";

        internal static readonly bool SupportsR8RenderTextureFormat =
            SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);
    }
}