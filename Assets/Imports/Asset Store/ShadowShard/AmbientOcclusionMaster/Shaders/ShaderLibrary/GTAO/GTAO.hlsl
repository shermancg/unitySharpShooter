#ifndef SHADOWSHARD_AO_MASTER_GTAO_INCLUDED
#define SHADOWSHARD_AO_MASTER_GTAO_INCLUDED

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

#include "GtaoParameters.hlsl"

#define TEMPORAL_ROTATION defined(_TEMPORAL_FILTERING)
#define ENABLE_TEMPORAL_OFFSET defined(_TEMPORAL_FILTERING)

// --------------------------------------------------
// Helper Functions
// --------------------------------------------------
real IntegrateArcCosWeighted(real horizon1, real horizon2, real N, real cosN)
{
    real h1 = horizon1 * 2.0;
    real h2 = horizon2 * 2.0;
    real sinN = sin(N);

    return 0.25 * ((-cos(h1 - N) + cosN + h1 * sinN) + (-cos(h2 - N) + cosN + h2 * sinN));
}

real GTAOFastAcos(real x)
{
    real outVal = -0.156583 * abs(x) + HALF_PI;
    outVal *= sqrt(1.0 - abs(x));

    return x >= 0 ? outVal : PI - outVal;
}

// --------------------------------------------
// Get sample start offset
// --------------------------------------------
real2 GetDirection(real2 uv, uint2 positionSS, int offset)
{
    half noise = GetNoiseMethod(uv, positionSS);
    real rotations[] = {60.0, 300.0, 180.0, 240.0, 120.0, 0.0};
    
    #if TEMPORAL_ROTATION
        float rotation = _AOM_TemporalRotation;
    #else
        float rotation = rotations[offset] / 360.0;
    #endif
    
    noise += rotation;
    noise *= PI;

    return real2(cos(noise), sin(noise));
}

inline real GetOffset(uint2 positionSS)
{
    real offset = 0.25 * ((positionSS.y - positionSS.x) & 0x3);

    #if ENABLE_TEMPORAL_OFFSET
        float offsets[] = { 0.0, 0.5, 0.25, 0.75 };
        offset += offsets[_AOM_TemporalOffset];
    #endif
    return frac(offset);
}

// --------------------------------------------
// Input generation functions
// --------------------------------------------
real UpdateHorizon(real maxH, real candidateH, real distSq)
{
    real falloff = saturate(1.0 - distSq * INV_RADIUS_SQ);

    return (candidateH > maxH) ? lerp(maxH, candidateH, falloff) : lerp(maxH, candidateH, 0.03f);
    // TODO: Thickness heuristic here.
}

real HorizonLoop(real3 positionVS, real3 V, real2 rayStart, real2 rayDir, real rayOffset, real rayStep)
{
    real maxHorizon = -1.0f; // cos(pi)
    real t = rayOffset * rayStep + rayStep;

    UNITY_UNROLL
    for (uint i = 0; i < SAMPLES; i++)
    {
        // Calculate the screen-space position from the UV coordinates and the current ray distance.
        real2 samplePos = max(2, min(rayStart + t * rayDir, _SourceSize.xy - 2));

        // Convert the screen-space position back to normalized UV coordinates.
        real2 uvSamplePos = samplePos * _SourceSize.zw;

        #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
        UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
        {
            uvSamplePos = RemapFoveatedRenderingResolve(uvSamplePos);
        }
        #endif

        real3 samplePosVS = WorldToViewSpaceVec(ReconstructViewPositionWS(uvSamplePos));
        
        real3 deltaPos = samplePosVS - positionVS;
        real deltaLenSq = dot(deltaPos, deltaPos);

        real currHorizon = dot(deltaPos, V) * rsqrt(deltaLenSq);
        maxHorizon = UpdateHorizon(maxHorizon, currHorizon, deltaLenSq);

        t += rayStep;
    }

    return maxHorizon;
}

half4 GTAO(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    real2 uv = input.texcoord;

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
    real3 positionVS = WorldToViewSpaceVec(viewPositionWS);
    real3 viewDirectionVS = normalize(-positionVS);
    
    real3 normalWS = SampleNormal(uv, linearDepth, viewPositionWS, pixelDensity);
    real3 normalVS = WorldToViewSpaceVec(normalWS);

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

    const real fovCorrectedRadiusSS = clamp(RADIUS * FOV_CORRECTION * rcp(linearDepth), SAMPLES, MAX_RADIUS);
    const real stepSize = max(1, fovCorrectedRadiusSS * INV_SAMPLE_COUNT_PLUS_ONE);

    real2 rayStart = uv_sample * SCREEN_PARAMS.xy;
    half offset = GetOffset(rayStart);

    half ao = HALF_ZERO;

    #ifdef _TEMPORAL_FILTERING
    const int dirCount = 1;
    #else
    const int dirCount = DIRECTIONS;
    #endif
    
    const half rcp_directions_count = half(rcp(dirCount));

    UNITY_UNROLL
    for (int i = 0; i < dirCount; i++)
    {
        real2 direction = GetDirection(uv, rayStart, i);
        real2 negativeDirection = -direction + 1e-30;

        // Find horizons
        real2 maxHorizons;
        maxHorizons.x = HorizonLoop(positionVS, viewDirectionVS, rayStart, direction, offset, stepSize);
        maxHorizons.y = HorizonLoop(positionVS, viewDirectionVS, rayStart, negativeDirection, offset, stepSize);

        // Integrate horizons
        real3 planeNormal = normalize(cross(real3(direction.xy, 0.0f), viewDirectionVS));
        real3 projectedNormal = normalVS - planeNormal * dot(normalVS, planeNormal);
        real projectedNormalLength = length(projectedNormal);
        real cosN = dot(projectedNormal / projectedNormalLength, viewDirectionVS);

        real3 T = cross(viewDirectionVS, planeNormal);
        real N = -sign(dot(projectedNormal, T)) * GTAOFastAcos(cosN);

        // Now we find the actual horizon angles
        maxHorizons.x = -GTAOFastAcos(maxHorizons.x);
        maxHorizons.y = GTAOFastAcos(maxHorizons.y);
        maxHorizons.x = N + max(maxHorizons.x - N, -HALF_PI);
        maxHorizons.y = N + min(maxHorizons.y - N, HALF_PI);
        ao += AnyIsNaN(maxHorizons) ? 1 : IntegrateArcCosWeighted(maxHorizons.x, maxHorizons.y, N, cosN);
    }

    half falloff = CalculateDepthFalloff(half_linearDepth, FALLOFF);
    ao = HALF_ONE - saturate(ao * rcp_directions_count);
    ao = PositivePow(ao * INTENSITY * falloff, kContrast);

    // Return the packed ao + normals
    return PackAONormal(ao, normalWS);
}

#endif
