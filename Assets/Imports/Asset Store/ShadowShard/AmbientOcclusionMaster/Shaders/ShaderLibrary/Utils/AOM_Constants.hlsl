#ifndef SHADOWSHARD_AO_MASTER_CONSTANTS_INCLUDED
#define SHADOWSHARD_AO_MASTER_CONSTANTS_INCLUDED

// Constants
// kContrast determines the contrast of occlusion. This allows users to control over/under
// occlusion. At the moment, this is not exposed to the editor because it's rarely useful.
// The range is between 0 and 1.
static const half kContrast = half(0.6);

// The constant below controls the geometry-awareness of the bilateral
// filter. The higher value, the more sensitive it is.
static const half kGeometryCoeff = half(0.8);

// The constants below are used in the AO estimator. Beta is mainly used for suppressing
// self-shadowing noise, and Epsilon is used to prevent calculation underflow. See the paper
// (Morgan 2011 https://casual-effects.com/research/McGuire2011AlchemyAO/index.html)
// for further details of these constants.
static const half kBeta = half(0.004);
static const half kEpsilon = half(0.0001);

static const float SKY_DEPTH_VALUE = 0.00001;
static const half HALF_POINT_ONE = half(0.1);
static const half HALF_MINUS_ONE = half(-1.0);
static const half HALF_ZERO = half(0.0);
static const half HALF_HALF = half(0.5);
static const half HALF_ONE = half(1.0);
static const half4 HALF4_ONE = half4(1.0, 1.0, 1.0, 1.0);
static const half HALF_TWO = half(2.0);
static const half HALF_TWO_PI = half(6.28318530717958647693);
static const half HALF_FOUR = half(4.0);
static const half HALF_NINE = half(9.0);
static const half HALF_HUNDRED = half(100.0);

#if defined(USING_STEREO_MATRICES)
    #define unity_eyeIndex unity_StereoEyeIndex
#else
#define unity_eyeIndex 0
#endif

// Hardcoded random UV values that improves performance.
// The values were taken from this function:
// r = frac(43758.5453 * sin( dot(float2(12.9898, 78.233), uv)) ));
// Indices  0 to 19 are for u = 0.0
// Indices 20 to 39 are for u = 1.0
static half SSAORandomUV[40] =
{
    0.00000000, // 00
    0.33984375, // 01
    0.75390625, // 02
    0.56640625, // 03
    0.98437500, // 04
    0.07421875, // 05
    0.23828125, // 06
    0.64062500, // 07
    0.35937500, // 08
    0.50781250, // 09
    0.38281250, // 10
    0.98437500, // 11
    0.17578125, // 12
    0.53906250, // 13
    0.28515625, // 14
    0.23137260, // 15
    0.45882360, // 16
    0.54117650, // 17
    0.12941180, // 18
    0.64313730, // 19

    0.92968750, // 20
    0.76171875, // 21
    0.13333330, // 22
    0.01562500, // 23
    0.00000000, // 24
    0.10546875, // 25
    0.64062500, // 26
    0.74609375, // 27
    0.67968750, // 28
    0.35156250, // 29
    0.49218750, // 30
    0.12500000, // 31
    0.26562500, // 32
    0.62500000, // 33
    0.44531250, // 34
    0.17647060, // 35
    0.44705890, // 36
    0.93333340, // 37
    0.87058830, // 38
    0.56862750, // 39
};

#endif
