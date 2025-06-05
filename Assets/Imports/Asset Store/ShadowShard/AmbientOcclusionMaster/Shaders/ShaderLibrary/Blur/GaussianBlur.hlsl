#ifndef SHADOWSHARD_GAUSSIAN_BLUR_AO_INCLUDED
#define SHADOWSHARD_GAUSSIAN_BLUR_AO_INCLUDED

#include "ShaderLibrary/Utils/AOM_Constants.hlsl"
#include "ShaderLibrary/Utils/AOM_Parameters.hlsl"
#include "ShaderLibrary/Utils/AOM_Samplers.hlsl"
#include "ShaderLibrary/Utils/AOM_Functions.hlsl"

// ------------------------------------------------------------------
// Gaussian Blur
// ------------------------------------------------------------------

// https://software.intel.com/content/www/us/en/develop/blogs/an-investigation-of-fast-real-time-gpu-based-image-blur-algorithms.html
half GaussianBlur(half2 uv, half2 pixelOffset)
{
    half colOut = 0;

    // Kernel width 7 x 7
    const int stepCount = 2;

    const half gWeights[stepCount] = {
        0.44908,
        0.05092
    };
    const half gOffsets[stepCount] = {
        0.53805,
        2.06278
    };

    UNITY_UNROLL
    for (int i = 0; i < stepCount; i++)
    {
        half2 texCoordOffset = gOffsets[i] * pixelOffset;
        half4 p1 = SAMPLE_BASEMAP(uv + texCoordOffset);
        half4 p2 = SAMPLE_BASEMAP(uv - texCoordOffset);
        half col = p1.r + p2.r;
        colOut += gWeights[i] * col;
    }

    return colOut;
}

half HorizontalGaussianBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.texcoord;
    half2 delta = half2(_SourceSize.z * rcp(_Downsample), HALF_ZERO);

    return GaussianBlur(uv, delta);
}

half VerticalGaussianBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.texcoord;
    half2 delta = half2(HALF_ZERO, _SourceSize.w * rcp(_Downsample));

    return HALF_ONE - GaussianBlur(uv, delta);
}

#endif
