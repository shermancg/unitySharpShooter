#ifndef SHADOWSHARD_AO_MASTER_GTAO_PARAMETERS_INCLUDED
#define SHADOWSHARD_AO_MASTER_GTAO_PARAMETERS_INCLUDED

half4 _GtaoParameters;
half4 _GtaoParameters2;

#define INTENSITY _GtaoParameters.x
#define RADIUS _GtaoParameters.y
#define INV_SAMPLE_COUNT_PLUS_ONE _GtaoParameters.z
#define FALLOFF _GtaoParameters.w

#define MAX_RADIUS _GtaoParameters2.x
#define INV_RADIUS_SQ _GtaoParameters2.y
#define FOV_CORRECTION _GtaoParameters2.z
#define DIRECTIONS _GtaoParameters2.w

#if _SAMPLES_2
    #define SAMPLES 2
#elif _SAMPLES_4
    #define SAMPLES 4
#elif _SAMPLES_6
    #define SAMPLES 6
#elif _SAMPLES_8
    #define SAMPLES 8
#elif _SAMPLES_12
    #define SAMPLES 12
#elif _SAMPLES_16
    #define SAMPLES 16
#else
#define SAMPLES 2
#endif

#endif
