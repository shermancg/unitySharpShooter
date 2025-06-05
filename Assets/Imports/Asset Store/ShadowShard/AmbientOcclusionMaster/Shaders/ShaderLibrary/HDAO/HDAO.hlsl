#ifndef SHADOWSHARD_AO_MASTER_HDAO_INCLUDED
#define SHADOWSHARD_AO_MASTER_HDAO_INCLUDED

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

#include "ShaderLibrary/Utils/AOM_Constants.hlsl"
#include "ShaderLibrary/Utils/AOM_Parameters.hlsl"
#include "ShaderLibrary/Utils/AOM_Samplers.hlsl"
#include "ShaderLibrary/Utils/AOM_Functions.hlsl"
#include "ShaderLibrary/Utils/AOM_Noises.hlsl"

#include "ShaderLibrary/Utils/DepthUtils.hlsl"
#include "ShaderLibrary/Utils/ViewPositionReconstruction.hlsl"
#include "ShaderLibrary/Utils/NormalReconstruction.hlsl"

#include "HdaoParameters.hlsl"

#define HDAO_ULTRA_SAMPLE_COUNT                    24
#define HDAO_HIGH_SAMPLE_COUNT                     16
#define HDAO_MEDIUM_SAMPLE_COUNT                   8
#define HDAO_LOW_SAMPLE_COUNT                      4

#if defined( _SAMPLE_COUNT_ULTRA )

#define SAMPLE_COUNT                             HDAO_ULTRA_SAMPLE_COUNT
static const int2                                SamplePattern[SAMPLE_COUNT] =
{
    {0, -9}, {4, -9}, {2, -6}, {6, -6},
    {0, -3}, {4, -3}, {8, -3}, {2, 0},
    {6, 0}, {9, 0}, {4, 3}, {8, 3},
    {2, 6}, {6, 6}, {9, 6}, {4, 9},
    {10, 0}, {-12, 12}, {9, -14}, {-8, -6},
    {11, -7}, {-9, 1}, {-2, -13}, {-7, -3},
};

#elif defined( _SAMPLE_COUNT_HIGH )

#define SAMPLE_COUNT                             HDAO_HIGH_SAMPLE_COUNT
static const int2                                SamplePattern[SAMPLE_COUNT] =
{
    {0, -9}, {4, -9}, {2, -6}, {6, -6},
    {0, -3}, {4, -3}, {8, -3}, {2, 0},
    {6, 0}, {9, 0}, {4, 3}, {8, 3},
    {2, 6}, {6, 6}, {9, 6}, {4, 9},
};

#elif defined( _SAMPLE_COUNT_MEDIUM )

#define SAMPLE_COUNT                             HDAO_MEDIUM_SAMPLE_COUNT
static const int2                                SamplePattern[SAMPLE_COUNT] =
{
    {0, -9}, {2, -6}, {0, -3}, {8, -3},
    {6, 0}, {4, 3}, {2, 6}, {9, 6},
};

#else //if defined( _SAMPLE_COUNT_LOW )

#define SAMPLE_COUNT                             HDAO_LOW_SAMPLE_COUNT
static const int2 SamplePattern[SAMPLE_COUNT] =
{
    {0, -6}, {0, 6}, {0, -6}, {6, 0},
};

#endif

half4 HDAO(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    real2 uv = input.texcoord;

    // Early Out for Sky...
    real rawDepth = SampleDepth(uv);
    if (rawDepth < SKY_DEPTH_VALUE)
        return PackAONormal(HALF_ZERO, HALF_ZERO);

    // Early Out for Falloff
    real linearDepth = GetLinearEyeDepth(rawDepth);
    half half_linearDepth = half(linearDepth);
    if (half_linearDepth > FALLOFF)
        return PackAONormal(HALF_ZERO, HALF_ZERO);

    float2 pixelDensity = float2(1.0f, 1.0f);
    #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER) {
        pixelDensity = RemapFoveatedRenderingDensity(RemapFoveatedRenderingNonUniformToLinear(uv));
    }
    #endif

    real3 viewPositionWS = ReconstructViewPositionWS(uv, linearDepth);
    real centerDistance = length(viewPositionWS);
    real3 centerNormal = SampleNormal(uv, linearDepth, viewPositionWS, pixelDensity);

    // Camera transform parameters
    half3 camTransform000102 = half3(_CameraViewProjections[unity_eyeIndex]._m00,
                                     _CameraViewProjections[unity_eyeIndex]._m01,
                                     _CameraViewProjections[unity_eyeIndex]._m02);
    half3 camTransform101112 = half3(_CameraViewProjections[unity_eyeIndex]._m10,
                                     _CameraViewProjections[unity_eyeIndex]._m11,
                                     _CameraViewProjections[unity_eyeIndex]._m12);
        
    half2 screenPos = half2(
        camTransform000102.x * viewPositionWS.x + camTransform000102.y * viewPositionWS.y + camTransform000102.z * viewPositionWS.z,
        camTransform101112.x * viewPositionWS.x + camTransform101112.y * viewPositionWS.y + camTransform101112.z * viewPositionWS.z);
        
    half zDist = HALF_ZERO;
    #if defined(_ORTHOGRAPHIC_PROJECTION)
        zDist = half_linearDepth;
        half2 uv_sample = saturate((screenPos + HALF_ONE) * HALF_HALF);
    #else
        zDist = half(-dot(UNITY_MATRIX_V[2].xyz, viewPositionWS));
        half2 uv_sample = saturate(half2(screenPos * rcp(zDist) + HALF_ONE) * HALF_HALF);
    #endif

    #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        uv_sample = RemapFoveatedRenderingLinearToNonUniform(uv_sample);
    }
    #endif
    
    const half noise = GetNoiseMethod(uv, uv_sample * _SourceSize.xy * _Downsample) * OFFSET_CORRECTION; // noise * aspectRatio
    
    half ao = HALF_ZERO;
    half2 acceptRadius = half2(ACCEPT_RADIUS, ACCEPT_RADIUS);
    half2 rejectRadius = half2(REJECT_RADIUS, REJECT_RADIUS);

    const half rcp_samples_count = half(rcp(SAMPLE_COUNT));

    UNITY_UNROLL
    for (uint s = 0; s < SAMPLE_COUNT; ++s)
    {
        real2 samplePattern = SamplePattern[s].xy * noise * _SourceSize.zw;
        
        // Sample depth positions
        real2 uv_s0 = uv_sample + samplePattern;
        real2 uv_s1 = uv_sample - samplePattern;

        real3 positionX = ReconstructViewPositionWS(uv_s0);
        real3 positionY = ReconstructViewPositionWS(uv_s1);

        real distanceX = length(positionX);
        real distanceY = length(positionY);

        // Detect valleys
        real2 distanceDelta = centerDistance.xx - float2(distanceX, distanceY);
        real2 compare = saturate(rejectRadius - distanceDelta); // removed * 6.0f
        compare = distanceDelta > acceptRadius ? compare : 0.0h;

        // Compute dot product, to scale occlusion based on depth position
        real3 directionX = normalize(positionX - viewPositionWS);
        real3 directionY = normalize(positionY - viewPositionWS);
        real directionDot = saturate(dot(directionX, directionY) + 0.9h) * 1.2h;

        // Accumulate weighted occlusion
        ao += compare.x * compare.y * directionDot; // removed pow(directionDot, 3) for optimization
    }
    
    half falloff = CalculateDepthFalloff(half_linearDepth, FALLOFF);
    ao = PositivePow(ao * rcp_samples_count * INTENSITY * falloff, kContrast);

    // Return the packed ao + normals
    return PackAONormal(ao, centerNormal);
}

#endif
