#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<float> _IsoVals;
#endif

float _Step;
int _Resolution;
float3 _Color;
float4x4 _ModelToWorld;

float3 oneDToThreeD(int i) 
{
	int zDirection = i % _Resolution;
	int yDirection = (i / _Resolution) % _Resolution;
	int xDirection = i / (_Resolution * _Resolution);

	return float3(xDirection, yDirection, zDirection);
}

void ConfigureProcedural () {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		unity_ObjectToWorld = 0.0;
		unity_ObjectToWorld._m03_m13_m23_m33 = mul(_ModelToWorld, float4((oneDToThreeD(unity_InstanceID) - _Resolution * 0.5) * _Step, 1.0));
		unity_ObjectToWorld._m00_m11_m22 = _Step / 2.0;

		_Color = float3(_IsoVals[unity_InstanceID] < 0.0 ? 1.0 : 0.0, 0.0, 0.0);
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
		Out = _Color;
	#endif
}