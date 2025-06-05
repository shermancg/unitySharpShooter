#ifndef SHADOWSHARD_AO_TEMPORAL_FILTER_INCLUDED
#define SHADOWSHARD_AO_TEMPORAL_FILTER_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScalingClamping.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

#include "ShaderLibrary/Utils/AOM_Parameters.hlsl"
#include "ShaderLibrary/Utils/AOM_Samplers.hlsl"
#include "ShaderLibrary/Utils/AOM_Functions.hlsl"

#ifndef AA_VARIANCE
    #define AA_VARIANCE 1
#endif

#ifndef AA_Filter
    #define AA_Filter 1
#endif

TEXTURE2D_HALF(_AOM_HistoryTexture);
half _AOM_TemporalScale;
half _AOM_TemporalResponse;

TEXTURE2D_X(_MotionVectorTexture);
float4 _MotionVectorTexture_TexelSize;

float2 SampleMotionVectors(float2 uv)
{
    uv = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(uv), _MotionVectorTexture_TexelSize.xy);

    return SAMPLE_TEXTURE2D_X(_MotionVectorTexture, sampler_LinearClamp, uv).xy;
}

inline half Luma4(half3 Color)
{
    return Color.g * 2.0h + (Color.r + Color.b);
}

inline half HdrWeight4(half3 Color, half Exposure)
{
    return rcp(Luma4(Color) * Exposure + 4.0h);
}

inline void ResolverAABB(half Sharpness, half ExposureScale, half AABBScale, half2 uv, half2 screenSize,
    inout half minColor, inout half maxColor, inout half filterColor)
{
    half TopLeft = SAMPLE_BASEMAP_R(uv + (int2(-1, -1) / screenSize));
    half TopCenter = SAMPLE_BASEMAP_R(uv + (int2(0, -1) / screenSize));
    half TopRight = SAMPLE_BASEMAP_R(uv + (int2(1, -1) / screenSize));
    half MiddleLeft = SAMPLE_BASEMAP_R(uv + (int2(-1,  0) / screenSize));
    half MiddleCenter = SAMPLE_BASEMAP_R(uv + (int2(0,  0) / screenSize));
    half MiddleRight = SAMPLE_BASEMAP_R(uv + (int2(1,  0) / screenSize));
    half BottomLeft = SAMPLE_BASEMAP_R(uv + (int2(-1,  1) / screenSize));
    half BottomCenter = SAMPLE_BASEMAP_R(uv + (int2(0,  1) / screenSize));
    half BottomRight = SAMPLE_BASEMAP_R(uv + (int2(1,  1) / screenSize));
    
    // Resolver filter 
    #if AA_Filter
        half SampleWeights[9];
        SampleWeights[0] = HdrWeight4(TopLeft.rrr, ExposureScale);
        SampleWeights[1] = HdrWeight4(TopCenter.rrr, ExposureScale);
        SampleWeights[2] = HdrWeight4(TopRight.rrr, ExposureScale);
        SampleWeights[3] = HdrWeight4(MiddleLeft.rrr, ExposureScale);
        SampleWeights[4] = HdrWeight4(MiddleCenter.rrr, ExposureScale);
        SampleWeights[5] = HdrWeight4(MiddleRight.rrr, ExposureScale);
        SampleWeights[6] = HdrWeight4(BottomLeft.rrr, ExposureScale);
        SampleWeights[7] = HdrWeight4(BottomCenter.rrr, ExposureScale);
        SampleWeights[8] = HdrWeight4(BottomRight.rrr, ExposureScale);

        half TotalWeight = SampleWeights[0] + SampleWeights[1] + SampleWeights[2] + SampleWeights[3] + SampleWeights[4] + SampleWeights[5] + SampleWeights[6] + SampleWeights[7] + SampleWeights[8];  
        half Filtered = (TopLeft * SampleWeights[0] + TopCenter * SampleWeights[1] + TopRight * SampleWeights[2] + MiddleLeft * SampleWeights[3] + MiddleCenter * SampleWeights[4] + MiddleRight * SampleWeights[5] + BottomLeft * SampleWeights[6] + BottomCenter * SampleWeights[7] + BottomRight * SampleWeights[8]) / TotalWeight;
    #endif

    half m1, m2, mean, stddev;
	#if AA_VARIANCE
        m1 = TopLeft + TopCenter + TopRight + MiddleLeft + MiddleCenter + MiddleRight + BottomLeft + BottomCenter + BottomRight;
        m2 = TopLeft * TopLeft + TopCenter * TopCenter + TopRight * TopRight + MiddleLeft * MiddleLeft + MiddleCenter * MiddleCenter + MiddleRight * MiddleRight + BottomLeft * BottomLeft + BottomCenter * BottomCenter + BottomRight * BottomRight;

        mean = m1 / 9;
        stddev = sqrt(m2 / 9 - mean * mean);
        
        minColor = mean - AABBScale * stddev;
        maxColor = mean + AABBScale * stddev;
    #else 
        minColor = min(TopLeft, min(TopCenter, min(TopRight, min(MiddleLeft, min(MiddleCenter, min(MiddleRight, min(BottomLeft, min(BottomCenter, BottomRight))))))));
        maxColor = max(TopLeft, max(TopCenter, max(TopRight, max(MiddleLeft, max(MiddleCenter, max(MiddleRight, max(BottomLeft, max(BottomCenter, BottomRight))))))));
            
        half center = (minColor + maxColor) * 0.5;
        minColor = (minColor - center) * AABBScale + center;
        maxColor = (maxColor - center) * AABBScale + center;
    #endif

    #if AA_Filter
        filterColor = Filtered;
        minColor = min(minColor, Filtered);
        maxColor = max(maxColor, Filtered);
    #else 
        filterColor = MiddleCenter;
        minColor = min(minColor, MiddleCenter);
        maxColor = max(maxColor, MiddleCenter);
    #endif

    //half4 corners = 4 * (TopLeft + BottomRight) - 2 * filterColor;
    //filterColor += (filterColor - (corners * 0.166667)) * 2.718282 * (Sharpness * 0.25);
}

half4 AOM_TemporalFilter(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;
    half2 velocity = SampleMotionVectors(uv);

    half filterColor = 0, minColor = 0, maxColor = 0;
    ResolverAABB(0, 0, _AOM_TemporalScale, uv, _SourceSize.xy, minColor, maxColor, filterColor);

    half currColor = filterColor;
    half lastColor = SAMPLE_TEXTURE2D_X(_AOM_HistoryTexture, sampler_BlitTexture, UnityStereoTransformScreenSpaceTex(uv - velocity)).r;
    lastColor = clamp(lastColor, minColor, maxColor);
    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
    {
        lastColor = filterColor;
    }
    
    half weight = saturate(clamp(_AOM_TemporalResponse, 0.0h, 0.98h) * (1.0h - length(velocity) * 8.0h));

    return lerp(currColor, lastColor, weight);
}

#endif