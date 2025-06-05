#ifndef SHADOWSHARD_AO_MASTER_NOISES_INCLUDED
#define SHADOWSHARD_AO_MASTER_NOISES_INCLUDED

#include "AOM_Constants.hlsl"
#include "AOM_Parameters.hlsl"
#include "AOM_Samplers.hlsl"
#include "AOM_Functions.hlsl"

//From  Next Generation Post Processing in Call of Duty: Advanced Warfare [Jimenez 2014]
// http://advances.realtimerendering.com/s2014/index.html
float InterleavedGradientNoise(float2 positionSS) //removed frameCount
{
    const float3 magic = float3(0.06711056f, 0.00583715f, 52.9829189f);

    return frac(magic.z * frac(dot(positionSS, magic.xy)));
}

inline float PseudoRandom(float2 positionSS)
{
    return frac(sin(dot(positionSS, float2(12.9898, 78.233))) * 43758.5453123);
}

half GetNoiseMethod(real2 uv, uint2 positionSS)
{
    #if defined(_BLUE_NOISE)
        return SAMPLE_BLUE_NOISE(((uv + BlueNoiseOffset) * BlueNoiseScale));
    #elif defined(_PSEUDO_RANDOM_NOISE)
        return PseudoRandom(uv);
    #else
    return InterleavedGradientNoise(positionSS);
    #endif
}

#endif
