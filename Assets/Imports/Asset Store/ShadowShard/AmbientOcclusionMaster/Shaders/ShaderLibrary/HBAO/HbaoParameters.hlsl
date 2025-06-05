#ifndef SHADOWSHARD_AO_MASTER_HBAO_PARAMETERS_INCLUDED
#define SHADOWSHARD_AO_MASTER_HBAO_PARAMETERS_INCLUDED

half4 _HbaoParameters;
half4 _HbaoParameters2;

#define INTENSITY _HbaoParameters.x
#define RADIUS _HbaoParameters.y
#define INV_SAMPLE_COUNT_PLUS_ONE _HbaoParameters.z
#define FALLOFF _HbaoParameters.w

#define MAX_RADIUS _HbaoParameters2.x
#define ANGLE_BIAS _HbaoParameters2.y
#define FOV_CORRECTION _HbaoParameters2.z
#define INV_RADIUS_SQ _HbaoParameters2.w

#if _DIRECTIONS_2
    #define DIRECTIONS 2
#elif _DIRECTIONS_4
    #define DIRECTIONS 4
#elif _DIRECTIONS_6
    #define DIRECTIONS 6
#else
#define DIRECTIONS 2
#endif

#if _SAMPLES_2
    #define SAMPLES 2
#elif _SAMPLES_4
    #define SAMPLES 4
#elif _SAMPLES_6
    #define SAMPLES 6
#elif _SAMPLES_8
    #define SAMPLES 8
#else
#define SAMPLES 2
#endif

#endif
