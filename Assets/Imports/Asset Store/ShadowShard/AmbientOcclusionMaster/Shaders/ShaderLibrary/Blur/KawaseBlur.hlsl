#ifndef SHADOWSHARD_KAWASE_BLUR_AO_INCLUDED
#define SHADOWSHARD_KAWASE_BLUR_AO_INCLUDED

#include "ShaderLibrary/Utils/AOM_Constants.hlsl"
#include "ShaderLibrary/Utils/AOM_Parameters.hlsl"
#include "ShaderLibrary/Utils/AOM_Samplers.hlsl"
#include "ShaderLibrary/Utils/AOM_Functions.hlsl"

// ------------------------------------------------------------------
// Kawase Blur
// ------------------------------------------------------------------

///////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Developed by Masaki Kawase, Bunkasha Games
// Used in DOUBLE-S.T.E.A.L. (aka Wreckless)
// From his GDC2003 Presentation: Frame Buffer Postprocessing Effects in  DOUBLE-S.T.E.A.L (Wreckless)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
half KawaseBlurFilter(half2 texCoord, half2 pixelSize, half iteration)
{
    half2 texCoordSample;
    half2 halfPixelSize = pixelSize * HALF_HALF;
    half2 dUV = (pixelSize.xy * half2(iteration, iteration)) + halfPixelSize.xy;

    half cOut;

    // Sample top left pixel
    texCoordSample.x = texCoord.x - dUV.x;
    texCoordSample.y = texCoord.y + dUV.y;

    cOut = SAMPLE_BASEMAP_R(texCoordSample);

    // Sample top right pixel
    texCoordSample.x = texCoord.x + dUV.x;
    texCoordSample.y = texCoord.y + dUV.y;

    cOut += SAMPLE_BASEMAP_R(texCoordSample);

    // Sample bottom right pixel
    texCoordSample.x = texCoord.x + dUV.x;
    texCoordSample.y = texCoord.y - dUV.y;
    cOut += SAMPLE_BASEMAP_R(texCoordSample);

    // Sample bottom left pixel
    texCoordSample.x = texCoord.x - dUV.x;
    texCoordSample.y = texCoord.y - dUV.y;

    cOut += SAMPLE_BASEMAP_R(texCoordSample);

    // Average
    cOut *= half(0.25);

    return cOut;
}

half KawaseBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.texcoord;
    half2 texelSize = _SourceSize.zw * rcp(_Downsample);

    half col = KawaseBlurFilter(uv, texelSize, 0);
    col = HALF_ONE - col;

    return col;
}

#endif
