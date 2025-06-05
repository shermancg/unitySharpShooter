#ifndef SHADOWSHARD_AO_MASTER_SSAO_INCLUDED
#define SHADOWSHARD_AO_MASTER_SSAO_INCLUDED

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

#include "SsaoParameters.hlsl"

half3 PickSsaoSamplePoint(float2 uv, int sampleIndex, half sampleIndexHalf, half rcpSampleCount, half radius,
                          half3 normal, float2 pixelDensity)
{
    #if defined(_BLUE_NOISE)
        const half lerpVal = sampleIndexHalf * rcpSampleCount;
        const half noise = SAMPLE_BLUE_NOISE(((uv + BlueNoiseOffset) * BlueNoiseScale) + lerpVal);
        const half u = frac(GetSsaoRandomVal(HALF_ZERO, sampleIndexHalf).x + noise) * HALF_TWO - HALF_ONE;
        const half theta = (GetSsaoRandomVal(HALF_ONE, sampleIndexHalf).x + noise) * HALF_TWO_PI * HALF_HUNDRED;
        const half u2 = half(sqrt(HALF_ONE - u * u));
    
        half3 v = half3(u2 * cos(theta), u2 * sin(theta), u);
        v *= (dot(normal, v) >= HALF_ZERO) * HALF_TWO - HALF_ONE;
        v *= lerp(0.1, 1.0, lerpVal * lerpVal);
    #else
    const float2 positionSS = GetScreenSpacePosition(uv);

    #if defined(_PSEUDO_RANDOM_NOISE)
        const half noise = half(PseudoRandom(positionSS));
    #else
    const half noise = half(InterleavedGradientNoise(positionSS, sampleIndex));
    #endif

    const half u = frac(GetSsaoRandomVal(HALF_ZERO, sampleIndex) + noise) * HALF_TWO - HALF_ONE;
    const half theta = (GetSsaoRandomVal(HALF_ONE, sampleIndex) + noise) * HALF_TWO_PI;
    const half u2 = half(sqrt(HALF_ONE - u * u));

    half3 v = half3(u2 * cos(theta), u2 * sin(theta), u);
    v *= sqrt((sampleIndexHalf + HALF_ONE) * rcpSampleCount);
    v = faceforward(v, -normal, v);
    #endif

    v *= radius;
    v.xy *= pixelDensity;

    return v;
}

// Distance-based AO estimator based on Morgan 2011
// "Alchemy screen-space ambient obscurance algorithm"
// http://graphics.cs.williams.edu/papers/AlchemyHPG11/
half4 SSAO(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord;
    
    // Early Out for Sky...
    float rawDepth = SampleDepth(uv);
    if (rawDepth < SKY_DEPTH_VALUE)
        return PackAONormal(HALF_ZERO, HALF_ZERO);

    // Early Out for Falloff
    float linearDepth = GetLinearEyeDepth(rawDepth);
    half half_linearDepth = half(linearDepth);
    if (half_linearDepth > FALLOFF)
        return PackAONormal(HALF_ZERO, HALF_ZERO);

    float2 pixelDensity = float2(1.0f, 1.0f);
    #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER) {
        pixelDensity = RemapFoveatedRenderingDensity(RemapFoveatedRenderingNonUniformToLinear(uv));
    }
    #endif

    float3 vpos_o = ReconstructViewPositionWS(uv, linearDepth);
    half3 normalWS = SampleNormal(uv, linearDepth, vpos_o, pixelDensity);

    // Parameters used in coordinate conversion
    half3 camTransform000102 = half3(_CameraViewProjections[unity_eyeIndex]._m00,
                                     _CameraViewProjections[unity_eyeIndex]._m01,
                                     _CameraViewProjections[unity_eyeIndex]._m02);
    half3 camTransform101112 = half3(_CameraViewProjections[unity_eyeIndex]._m10,
                                     _CameraViewProjections[unity_eyeIndex]._m11,
                                     _CameraViewProjections[unity_eyeIndex]._m12);

    const half rcpSampleCount = half(rcp(SAMPLE_COUNT));
    half ao = HALF_ZERO;
    half sHalf = HALF_MINUS_ONE;
    
    UNITY_UNROLL
    for (int s = 0; s < SAMPLE_COUNT; s++)
    {
        sHalf += HALF_ONE;

        // Sample point
        half3 v_s1 = PickSsaoSamplePoint(uv, s, sHalf, rcpSampleCount, RADIUS, normalWS, pixelDensity);
        half3 vpos_s1 = half3(vpos_o + v_s1);
        half2 screenPos = half2(
            camTransform000102.x * vpos_s1.x + camTransform000102.y * vpos_s1.y + camTransform000102.z * vpos_s1.z,
            camTransform101112.x * vpos_s1.x + camTransform101112.y * vpos_s1.y + camTransform101112.z * vpos_s1.z
        );

        half zDist = HALF_ZERO;
        #if defined(_ORTHOGRAPHIC_PROJECTION)
            zDist = half_linearDepth;
            half2 uv_sample = saturate((screenPos + HALF_ONE) * HALF_HALF);
        #else
            zDist = half(-dot(UNITY_MATRIX_V[2].xyz, vpos_s1));
            half2 uv_sample = saturate(half2(screenPos * rcp(zDist) + HALF_ONE) * HALF_HALF);
        #endif

        #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
        UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
        {
            uv_sample = RemapFoveatedRenderingLinearToNonUniform(uv_sample);
        }
        #endif

        // Relative depth of the sample point
        float rawDepth_s = SampleDepth(uv_sample);
        float linearDepth_s = GetLinearEyeDepth(rawDepth_s);

        // We need to make sure we not use the AO value if the sample point it's outside the radius or if it's the sky...
        half halfLinearDepth_s = half(linearDepth_s);
        half isInsideRadius = abs(zDist - halfLinearDepth_s) < RADIUS ? 1.0 : 0.0;
        isInsideRadius *= rawDepth_s > SKY_DEPTH_VALUE ? 1.0 : 0.0;

        // Relative position of the sample point
        half3 v_s2 = half3(ReconstructViewPositionWS(uv_sample, linearDepth_s) - vpos_o);

        // Estimate the obscurance value
        half dotVal = dot(v_s2, normalWS) - kBeta * half_linearDepth;
        half a1 = max(dotVal, HALF_ZERO);
        half a2 = dot(v_s2, v_s2) + kEpsilon;
        ao += a1 * rcp(a2) * isInsideRadius;
    }

    half falloff = CalculateDepthFalloff(half_linearDepth, FALLOFF);
    ao *= RADIUS;
    ao = PositivePow(saturate(ao * INTENSITY * falloff * rcpSampleCount), kContrast);

    // Return the packed ao + normals
    return PackAONormal(ao, normalWS);
}

#endif //UNIVERSAL_SSAO_INCLUDED
