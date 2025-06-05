#ifndef SHADOWSHARD_BILATERAL_BLUR_AO_INCLUDED
#define SHADOWSHARD_BILATERAL_BLUR_AO_INCLUDED

#include "ShaderLibrary/Utils/AOM_Constants.hlsl"
#include "ShaderLibrary/Utils/AOM_Parameters.hlsl"
#include "ShaderLibrary/Utils/AOM_Samplers.hlsl"
#include "ShaderLibrary/Utils/AOM_Functions.hlsl"

// ------------------------------------------------------------------
// Bilateral Blur
// ------------------------------------------------------------------

// Geometry-aware separable bilateral filter
half4 Blur(const float2 uv, const float2 delta) : SV_Target
{
    half4 p0 = SAMPLE_BASEMAP(uv);
    half4 p1a = SAMPLE_BASEMAP(uv - delta * 1.3846153846);
    half4 p1b = SAMPLE_BASEMAP(uv + delta * 1.3846153846);
    half4 p2a = SAMPLE_BASEMAP(uv - delta * 3.2307692308);
    half4 p2b = SAMPLE_BASEMAP(uv + delta * 3.2307692308);

    half3 n0 = GetPackedNormal(p0);

    half w0 = half(0.2270270270);
    half w1a = CompareNormal(n0, GetPackedNormal(p1a)) * half(0.3162162162);
    half w1b = CompareNormal(n0, GetPackedNormal(p1b)) * half(0.3162162162);
    half w2a = CompareNormal(n0, GetPackedNormal(p2a)) * half(0.0702702703);
    half w2b = CompareNormal(n0, GetPackedNormal(p2b)) * half(0.0702702703);

    half s = half(0.0);
    s += GetPackedAO(p0) * w0;
    s += GetPackedAO(p1a) * w1a;
    s += GetPackedAO(p1b) * w1b;
    s += GetPackedAO(p2a) * w2a;
    s += GetPackedAO(p2b) * w2b;
    s *= rcp(w0 + w1a + w1b + w2a + w2b);

    return PackAONormal(s, n0);
}

// Geometry-aware bilateral filter (single pass/small kernel)
half BlurSmall(const float2 uv, const float2 delta)
{
    half4 p0 = SAMPLE_BASEMAP(uv);
    half4 p1 = SAMPLE_BASEMAP(uv + float2(-delta.x, -delta.y));
    half4 p2 = SAMPLE_BASEMAP(uv + float2(delta.x, -delta.y));
    half4 p3 = SAMPLE_BASEMAP(uv + float2(-delta.x, delta.y));
    half4 p4 = SAMPLE_BASEMAP(uv + float2(delta.x, delta.y));

    half3 n0 = GetPackedNormal(p0);

    half w0 = HALF_ONE;
    half w1 = CompareNormal(n0, GetPackedNormal(p1));
    half w2 = CompareNormal(n0, GetPackedNormal(p2));
    half w3 = CompareNormal(n0, GetPackedNormal(p3));
    half w4 = CompareNormal(n0, GetPackedNormal(p4));

    half s = HALF_ZERO;
    s += GetPackedAO(p0) * w0;
    s += GetPackedAO(p1) * w1;
    s += GetPackedAO(p2) * w2;
    s += GetPackedAO(p3) * w3;
    s += GetPackedAO(p4) * w4;

    return s *= rcp(w0 + w1 + w2 + w3 + w4);
}

half4 HorizontalBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    const float2 uv = input.texcoord;
    const float2 delta = float2(_SourceSize.z * rcp(_Downsample), 0.0);
    return Blur(uv, delta);
}

half4 VerticalBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    const float2 uv = input.texcoord;
    const float2 delta = float2(0.0, _SourceSize.w * rcp(_Downsample));
    return Blur(uv, delta);
}

half4 FinalBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    const float2 uv = input.texcoord;
    const float2 delta = _SourceSize.zw * rcp(_Downsample);
    return HALF_ONE - BlurSmall(uv, delta);
}

#endif
