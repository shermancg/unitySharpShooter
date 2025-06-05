#ifndef SHADOWSHARD_AO_MASTER_NORMAL_RECONSTRUCTION_INCLUDED
#define SHADOWSHARD_AO_MASTER_NORMAL_RECONSTRUCTION_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
#include "AOM_Parameters.hlsl"
#include "DepthUtils.hlsl"
#include "ViewPositionReconstruction.hlsl"

// Try reconstructing normal accurately from depth buffer.
// Low:    DDX/DDY on the current pixel
// Medium: 3 taps on each direction | x | * | y |
// High:   5 taps on each direction: | z | x | * | y | w |
// https://atyuwen.github.io/posts/normal-reconstruction/
// https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
half3 ReconstructNormal(float2 uv, float linearDepth, float3 vpos, float2 pixelDensity)
{
    #if defined(_DEPTH_NORMALS_LOW)
        return half3(normalize(cross(ddy(vpos), ddx(vpos))));
    #else
    float2 delta = float2(_SourceSize.zw * 2.0);

    pixelDensity = rcp(pixelDensity);

    // Sample the neighbour fragments
    float2 lUV = float2(-delta.x, 0.0) * pixelDensity;
    float2 rUV = float2(delta.x, 0.0) * pixelDensity;
    float2 uUV = float2(0.0, delta.y) * pixelDensity;
    float2 dUV = float2(0.0, -delta.y) * pixelDensity;

    float3 l1 = float3(uv + lUV, 0.0);
    l1.z = SampleAndGetLinearEyeDepth(l1.xy); // Left1
    float3 r1 = float3(uv + rUV, 0.0);
    r1.z = SampleAndGetLinearEyeDepth(r1.xy); // Right1
    float3 u1 = float3(uv + uUV, 0.0);
    u1.z = SampleAndGetLinearEyeDepth(u1.xy); // Up1
    float3 d1 = float3(uv + dUV, 0.0);
    d1.z = SampleAndGetLinearEyeDepth(d1.xy); // Down1

    // Determine the closest horizontal and vertical pixels...
    // horizontal: left = 0.0 right = 1.0
    // vertical  : down = 0.0    up = 1.0
    #if defined(_DEPTH_NORMALS_MEDIUM)
             uint closest_horizontal = l1.z > r1.z ? 0 : 1;
             uint closest_vertical   = d1.z > u1.z ? 0 : 1;
    #else
    float3 l2 = float3(uv + lUV * 2.0, 0.0);
    l2.z = SampleAndGetLinearEyeDepth(l2.xy); // Left2
    float3 r2 = float3(uv + rUV * 2.0, 0.0);
    r2.z = SampleAndGetLinearEyeDepth(r2.xy); // Right2
    float3 u2 = float3(uv + uUV * 2.0, 0.0);
    u2.z = SampleAndGetLinearEyeDepth(u2.xy); // Up2
    float3 d2 = float3(uv + dUV * 2.0, 0.0);
    d2.z = SampleAndGetLinearEyeDepth(d2.xy); // Down2

    const uint closest_horizontal = abs((2.0 * l1.z - l2.z) - linearDepth) < abs((2.0 * r1.z - r2.z) - linearDepth)
                                        ? 0
                                        : 1;
    const uint closest_vertical = abs((2.0 * d1.z - d2.z) - linearDepth) < abs((2.0 * u1.z - u2.z) - linearDepth)
                                      ? 0
                                      : 1;
    #endif

    // Calculate the triangle, in a counter-clockwize order, to
    // use based on the closest horizontal and vertical depths.
    // h == 0.0 && v == 0.0: p1 = left,  p2 = down
    // h == 1.0 && v == 0.0: p1 = down,  p2 = right
    // h == 1.0 && v == 1.0: p1 = right, p2 = up
    // h == 0.0 && v == 1.0: p1 = up,    p2 = left
    // Calculate the view space positions for the three points...
    half3 P1;
    half3 P2;
    if (closest_vertical == 0)
    {
        P1 = half3(closest_horizontal == 0 ? l1 : d1);
        P2 = half3(closest_horizontal == 0 ? d1 : r1);
    }
    else
    {
        P1 = half3(closest_horizontal == 0 ? u1 : r1);
        P2 = half3(closest_horizontal == 0 ? l1 : u1);
    }

    // Use the cross product to calculate the normal...
    return half3(normalize(cross(ReconstructViewPositionWS(P2.xy, P2.z) - vpos, ReconstructViewPositionWS(P1.xy, P1.z) - vpos)));
    #endif
}

half3 SampleNormal(real2 uv, real linearDepth, float3 vpos, real2 pixelDensity)
{
    #if defined(_DEPTH_NORMALS_PREPASS)
    return half3(SampleSceneNormals(uv));
    #else
    return ReconstructNormal(uv, linearDepth, vpos, pixelDensity);
    #endif
}

half3 SampleNormal(real2 uv, real linearDepth, real2 pixelDensity)
{
    #if defined(_DEPTH_NORMALS_PREPASS)
    return half3(SampleSceneNormals(uv));
    #else
    real3 vpos = ReconstructViewPositionWS(uv, linearDepth);

    return ReconstructNormal(uv, linearDepth, vpos, pixelDensity);
    #endif
}

half3 SampleNormal(float2 uv, float2 pixelDensity)
{
    real linearDepth = SampleAndGetLinearEyeDepth(uv);

    return SampleNormal(uv, linearDepth, pixelDensity);
}

float3 GetNormalVS(float3 normal)
{
    float3 normalVS = normalize(mul((real3x3)UNITY_MATRIX_V, normal));

    return real3(normalVS.xy, normalVS.z);
}

#endif
