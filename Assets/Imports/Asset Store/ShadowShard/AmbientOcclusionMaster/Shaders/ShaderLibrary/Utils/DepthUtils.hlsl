#ifndef SHADOWSHARD_AO_MASTER_DEPTH_UTILS_INCLUDED
#define SHADOWSHARD_AO_MASTER_DEPTH_UTILS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "AOM_Constants.hlsl"
#include "AOM_Parameters.hlsl"

// For Downsampled AO we need to adjust the UV coordinates
// so it hits the center of the pixel inside the depth texture.
// The texelSize multiplier is 1.0 when DOWNSAMPLE is enabled, otherwise 0.0
#define ADJUSTED_DEPTH_UV(uv) uv.xy + ((_CameraDepthTexture_TexelSize.xy * 0.5) * (1.0 - (_Downsample - 0.5) * 2.0))
#define MIN_DEPTH_GATHERED_FOR_CENTRAL 0

float SampleDepth(float2 uv)
{
    return SampleSceneDepth(ADJUSTED_DEPTH_UV(uv.xy));
}

float GetLinearEyeDepth(float rawDepth)
{
    #if defined(_ORTHOGRAPHIC_PROJECTION)
        return LinearDepthToEyeDepth(rawDepth);
    #else
        return LinearEyeDepth(rawDepth, _ZBufferParams);
    #endif
}

float SampleAndGetLinearEyeDepth(float2 uv)
{
    const float rawDepth = SampleDepth(uv);

    return GetLinearEyeDepth(rawDepth);
}

half CalculateDepthFalloff(half linearDepth, half distance)
{
    half falloff = HALF_ONE - linearDepth * half(rcp(distance));

    return falloff * falloff;
}

#endif
