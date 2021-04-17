#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<float4> _SurfacePoints;
#endif

float _Step;
int _Resolution;
float _Alpha;
float4x4 _GridToWorld;
float4x4 _ShapeToWorld;
float3 _ViewDir;
float _PointBrightness;
float _PointSize;

uint3 oneDToThreeD(uint i) 
{
	uint zDirection = i % _Resolution;
	uint yDirection = (i / _Resolution) % _Resolution;
	uint xDirection = i / (_Resolution * _Resolution);

	return uint3(xDirection, yDirection, zDirection);
}

float4x4 axis_matrix(float3 right, float3 up, float3 forward)
{
    float3 xaxis = right;
    float3 yaxis = up;
    float3 zaxis = forward;
    return float4x4(
		xaxis.x, yaxis.x, zaxis.x, 0,
		xaxis.y, yaxis.y, zaxis.y, 0,
		xaxis.z, yaxis.z, zaxis.z, 0,
		0, 0, 0, 1
	);
}

float4x4 look_at_matrix(float3 at, float3 eye, float3 up)
{
    float3 zaxis = normalize(at - eye);
    float3 xaxis = normalize(cross(up, zaxis));
    float3 yaxis = cross(zaxis, xaxis);
    return axis_matrix(xaxis, yaxis, zaxis);
}

#define BASE_POINTSIZE 4.0
#define BASE_BRIGHTNESS 1.0

void ConfigureProcedural () {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		unity_ObjectToWorld = 0.0;
		float4 pos = mul(_GridToWorld, float4(_SurfacePoints[unity_InstanceID].xyz, 1.0));
		_Alpha = _SurfacePoints[unity_InstanceID].w;
		unity_ObjectToWorld._m03_m13_m23_m33 = float4(pos.xyz, 1.0);
		// float3 test = oneDToThreeD(unity_InstanceID);
		// unity_ObjectToWorld._m03_m13_m23_m33 = float4(test, 1.0);
		unity_ObjectToWorld._m00_m11_m22 = _Step / (BASE_POINTSIZE - _PointSize);

		unity_ObjectToWorld = mul(unity_ObjectToWorld, look_at_matrix(-_ViewDir * 1000 /* high enough constant to look towards view */, pos.xyz, float3(0.0, 1.0, 0.0)));
	#endif
}

void ShaderGraphFunction_float (float3 In, out float3 Out) {
	Out = In;
}

void ShaderGraphFunction_half (half3 In, out half3 Out) {
	Out = In;
}

void clip_float(float3 In, out float3 Out) {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		if (_Alpha < 1.0) discard;
	#endif
	Out = In;
}