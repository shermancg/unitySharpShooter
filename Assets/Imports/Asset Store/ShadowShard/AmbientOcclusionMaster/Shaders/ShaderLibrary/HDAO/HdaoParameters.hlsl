#ifndef SHADOWSHARD_AO_MASTER_HBAO_PARAMETERS_INCLUDED
#define SHADOWSHARD_AO_MASTER_HBAO_PARAMETERS_INCLUDED

half4 _HdaoParameters;
half4 _HdaoParameters2;

#define INTENSITY _HdaoParameters.x
#define REJECT_RADIUS _HdaoParameters.y
#define ACCEPT_RADIUS _HdaoParameters.z
#define FALLOFF _HdaoParameters.w

#define OFFSET_CORRECTION _HdaoParameters2.x

#endif
