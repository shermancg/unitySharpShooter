#ifndef SHADOWSHARD_AO_MASTER_PARAMETERS_INCLUDED
#define SHADOWSHARD_AO_MASTER_PARAMETERS_INCLUDED

float4 _CameraViewTopLeftCorner[2];
float4x4 _CameraViewProjections[2];
// This is different from UNITY_MATRIX_VP (platform-agnostic projection matrix is used). Handle both non-XR and XR modes.

float4 _SourceSize;
float4 _ProjectionParams2;
float4 _CameraViewXExtent[2];
float4 _CameraViewYExtent[2];
float4 _CameraViewZExtent[2];

half _AOM_TemporalRotation;
half _AOM_TemporalOffset;

half _Downsample;

#if defined(_BLUE_NOISE)
half4 _AomBlueNoiseParameters;
#define BlueNoiseScale          _AomBlueNoiseParameters.xy
#define BlueNoiseOffset         _AomBlueNoiseParameters.zw
#endif

#endif
