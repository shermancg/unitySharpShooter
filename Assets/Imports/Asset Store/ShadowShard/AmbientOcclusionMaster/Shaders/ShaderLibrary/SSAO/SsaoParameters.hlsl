#ifndef SHADOWSHARD_AO_MASTER_SSAO_PARAMETERS_INCLUDED
#define SHADOWSHARD_AO_MASTER_SSAO_PARAMETERS_INCLUDED

half4 _SsaoParameters;

#define INTENSITY _SsaoParameters.x
#define RADIUS _SsaoParameters.y
#define FALLOFF _SsaoParameters.z

#if defined(_SAMPLE_COUNT_HIGH)
    static const int SAMPLE_COUNT = 12;
#elif defined(_SAMPLE_COUNT_MEDIUM)
    static const int SAMPLE_COUNT = 8;
#else
static const int SAMPLE_COUNT = 4;
#endif

#endif
