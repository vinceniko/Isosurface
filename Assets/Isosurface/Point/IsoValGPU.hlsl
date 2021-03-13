#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<float> _IsoVals;
#endif

float _Step;
int _Resolution;
float3 _Color;
float4x4 _GridToWorld;
float4x4 _ShapeToWorld;
float3 _ViewDir;
float _PointBrightness;
float _PointSize;

float3 oneDToThreeD(int i) 
{
	int zDirection = i % _Resolution;
	int yDirection = (i / _Resolution) % _Resolution;
	int xDirection = i / (_Resolution * _Resolution);

	return float3(xDirection, yDirection, zDirection);
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

void ConfigureProcedural () {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		unity_ObjectToWorld = 0.0;
		float4 pos = float4((oneDToThreeD(unity_InstanceID) - _Resolution * 0.5) * _Step + _Step * 0.5, 1.0);
		unity_ObjectToWorld._m03_m13_m23_m33 = mul(_GridToWorld, pos);
		// unity_ObjectToWorld._m03_m13_m23_m33 = float4((oneDToThreeD(unity_InstanceID) - _Resolution * 0.5) * _Step + _Step * 0.5, 1.0);
		unity_ObjectToWorld._m00_m11_m22 = _Step / (4.0 - _PointSize);

		unity_ObjectToWorld = mul(unity_ObjectToWorld, look_at_matrix(-_ViewDir * 1000, pos.xyz, float3(0.0, 1.0, 0.0)));

		_Color = float3(-_IsoVals[unity_InstanceID] / (1.0 - _PointBrightness), 0.0, 0.0);
		// _Color = float3(_IsoVals[unity_InstanceID] < 0.0 ? 1.0 : 0.0, 0.0, 0.0);
		
		// float3 threeD = mul(_ShapeToWorld, float4(oneDToThreeD(unity_InstanceID), 1.0)).xyz;
		// uint transformedIdx = uint(threeD.x + _Resolution * (threeD.y + _Resolution * threeD.z));
		// if (transformedIdx >= 0 && transformedIdx < _Resolution) 
		// {
		// 	_Color = float3(_IsoVals[transformedIdx] < 0.0 ? 1.0 : 0.0, 0.0, 0.0);
		// }∂
	#endif
}

void ShaderGraphFunction_float (float3 In, out float3 Out) {
	Out = In;
}

void ShaderGraphFunction_half (half3 In, out half3 Out) {
	Out = In;
}

void getColor_float(out float3 Out) {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		Out.xyz = _Color;
	#endif
}

void clipInside_float(float3 In, out float3 Out) {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		if (_IsoVals[unity_InstanceID] < 0.0) discard;
	#endif
	Out = In;
}

void clipOutside_float(float3 In, out float3 Out) {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		if (_IsoVals[unity_InstanceID] > 0.0) discard;
	#endif
	Out = In;
}

void getAlpha_float(float3 In, out float Out) {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		Out = In.r == 1.0 ? 1.0 : 0.5;
	#endif
}