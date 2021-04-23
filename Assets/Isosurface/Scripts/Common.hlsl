uint _Resolution;
float4x4 _GridToWorld;
float _Step;
RWStructuredBuffer<float> _IsoVals;
RWStructuredBuffer<float4> _SurfacePoints;
float4x4 _ShapeToWorld;

struct Edge {
    float3 start;
    float3 end;
};

float3 transformToCenter(float3 pos) {
    return (pos - _Resolution * 0.5) * _Step + _Step * 0.5;
}

float3 inverseTransformToCenter(float3 pos) {
    return ((-pos - 0.5 * _Resolution * _Step) + 0.5 * _Step) / _Step;
}

float3 GetPosSurface(uint3 id)
{
    return mul(_GridToWorld, transformToCenter(id.zyx)).xyz;
    // return mul(_GridToWorld, float4(id.zyx * _Step, 1.0)).xyz;
}

float3 GetPos(uint3 id) {
    return (float3(id.xyz) - _Resolution * 0.5 /*shift to origin*/) * _Step + _Step * 0.5;
}

uint3 oneDToThreeD(uint i) 
{
	uint zDirection = i % _Resolution;
	uint yDirection = (i / _Resolution) % _Resolution;
	uint xDirection = i / (_Resolution * _Resolution);

	return uint3(xDirection, yDirection, zDirection);
}

uint threeDToOneD(uint3 idPos) {
	return idPos.x + _Resolution * (idPos.y + _Resolution * idPos.z);
}

float3 linInterpolate(float3 p1, float3 p2, float d1, float d2) {
    return (1-d1/(d1-d2))*p1 + d1/(d1-d2)*p2;
}

void SetIsoVal(uint3 id, float val) 
{
	// id = mul(_ShapeToWorld, float4(id.zyx, 1.0)).xyz;
	if (id.x < _Resolution && id.y < _Resolution && id.z < _Resolution) 
    {
		_IsoVals[threeDToOneD(id)] = val;
	}
}

void SetSurfacePoint(uint3 id, float3 val) 
{
	// id = mul(_ShapeToWorld, float4(id.zyx, 1.0)).xyz;
	if (id.x < _Resolution-1 && id.y < _Resolution-1 && id.z < _Resolution-1 && 
    id.x >= 0 && id.y >= 0 && id.z >= 0 &&
    val.x < _Resolution && val.y < _Resolution && val.z < _Resolution &&
    val.x >= 0 && val.y >= 0 && val.z >= 0) 
    {
        val = transformToCenter(val);
		_SurfacePoints[threeDToOneD(id)] = float4(val, 1.0);
	} else {
        val = transformToCenter(id);
        _SurfacePoints[threeDToOneD(id)] = float4(val, 0.0);
    }
}
