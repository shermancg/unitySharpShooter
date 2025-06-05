using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Data.Passes
{
    internal class AoPassData
    {
        internal Material Material;
        internal TextureHandle CameraColor;
        internal TextureHandle CameraNormalsTexture;
        internal TextureHandle MotionVectorsTexture;
        internal TextureHandle AOTexture;
        internal TextureHandle BlurTexture;
        internal TextureHandle SpatialTexture;
        internal TextureHandle FinalTexture;
    }
}