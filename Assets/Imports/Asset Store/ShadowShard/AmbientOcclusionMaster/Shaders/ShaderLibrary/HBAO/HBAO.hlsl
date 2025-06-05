#ifndef SHADOWSHARD_AO_MASTER_HBAO_INCLUDED
#define SHADOWSHARD_AO_MASTER_HBAO_INCLUDED

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

#include "HbaoParameters.hlsl"

#define TEMPORAL_ROTATION defined(_TEMPORAL_FILTERING)
#define ENABLE_TEMPORAL_OFFSET defined(_TEMPORAL_FILTERING)

inline half RadiusFalloff(real dist)
{
    return saturate(1.0 - dist * INV_RADIUS_SQ);
}

inline real HbaoSample(real3 viewPosition, real3 stepViewPosition, real3 normal, inout half angleBias)
{
    real3 H = stepViewPosition - viewPosition;
    real dist = length(H);

    // Ensure we don't divide by zero in the sinBlock calculation
    real dist_inv = rcp(max(dist, 1e-6));
    real sinBlock = dot(normal, H) * dist_inv;

    real diff = max(sinBlock - angleBias, 0);
    angleBias = saturate(max(sinBlock, angleBias)); // Clamp to prevent overestimation

    return diff * RadiusFalloff(dist);
}

real2 GetDirection(real alpha, real noise, int d)
{
    #if TEMPORAL_ROTATION
    real angle = alpha * (d + noise + _AOM_TemporalRotation);
    #else
    real angle = alpha * (d + noise);
    #endif
    
    real sin, cos;
    sincos(angle, sin, cos);

    return real2(cos, sin);
}

half4 HBAO(Varyings input) : SV_Target
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
    real3 normalWS = SampleNormal(uv, linearDepth, viewPositionWS, pixelDensity);

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
    
    const real fovCorrectedradiusSS = clamp(RADIUS * FOV_CORRECTION * rcp(linearDepth), SAMPLES, MAX_RADIUS);
    const real stepSize = max(1, fovCorrectedradiusSS * INV_SAMPLE_COUNT_PLUS_ONE);

    const int dirCount = DIRECTIONS;
    
    const real2 positionSS = GetScreenSpacePosition(uv_sample);
    const half noise = GetNoiseMethod(uv_sample, positionSS);
    const half alpha = TWO_PI / dirCount;
    const half rcp_directions_count = half(rcp(dirCount));
    
    half ao = HALF_ZERO;

    UNITY_UNROLL
    for (int d = 0; d < dirCount; ++d)
    {
        real2 direction = GetDirection(alpha, noise, d);

        real rayPixel = 1.0;
        real angleBias = ANGLE_BIAS;

        UNITY_UNROLL
        for (int s = 0; s < SAMPLES; ++s)
        {
            #if ENABLE_TEMPORAL_OFFSET
            float offsets[] = { 0.0, 0.5, 0.25, 0.75 };
            rayPixel += offsets[_AOM_TemporalOffset];
            #endif
            
            real2 step_uv = uv_sample + round(rayPixel * direction) * _SourceSize.zw;

            #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
            UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
            {
                step_uv = RemapFoveatedRenderingResolve(step_uv);
            }
            #endif

            real3 stepViewPositionWS = ReconstructViewPositionWS(step_uv);

            ao += HbaoSample(viewPositionWS, stepViewPositionWS, normalWS, angleBias);
            rayPixel += stepSize;
        }
    }

    half falloff = CalculateDepthFalloff(half_linearDepth, FALLOFF);
    ao = PositivePow(ao * rcp_directions_count * INTENSITY * falloff, kContrast);

    // Return the packed ao + normals
    return PackAONormal(ao, normalWS);
}

#endif
