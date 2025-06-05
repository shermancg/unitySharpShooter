#ifndef SHADOWSHARD_AO_MASTER_MULTIBOUNCE_INCLUDED
#define SHADOWSHARD_AO_MASTER_MULTIBOUNCE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define GBUFFER0 0
FRAMEBUFFER_INPUT_HALF(GBUFFER0);

real3 MultiBounce(real visibility, real3 albedo)
{
    real3 a =  2.0404 * albedo - 0.3324;
    real3 b = -4.7951 * albedo + 0.6417;
    real3 c =  2.7552 * albedo + 0.6903;

    real x = visibility;
    return max(x, ((x * a + b) * x + c) * x);
}

real3 UniversalMultiBounce(real visibility, real2 uv, real3 positionCS)
{
    real3 albedo = LOAD_FRAMEBUFFER_INPUT(GBUFFER0, positionCS.xy);
    
    return MultiBounce(visibility, albedo);
}
#endif