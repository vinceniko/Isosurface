uint _Resolution;
float4x4 _GridToWorld;
float _Step;
RWStructuredBuffer<float> _IsoVals;
RWStructuredBuffer<float4> _Normals;
RWStructuredBuffer<float4> _SurfacePoints;
float4x4 _ShapeToWorld;

struct Edge {
    float3 start;
    float3 end;
};

float opUnion( float d1, float d2 ) { return min(d1,d2); }

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

#define SHAPE_SIZE 0.5

float Sphere(float3 p, float s)
{
	return length(p) - s;
}

float Torus(float3 p, float s)
{
	float2 q = float2(length(p.xz)-s * 2.5,p.y);
  	return length(q)-s;
}

float Pyramid(float3 p, float h)
{
  float m2 = h*h + 0.25;
    
  p.xz = abs(p.xz);
  p.xz = (p.z>p.x) ? p.zx : p.xz;
  p.xz -= 0.5;

  float3 q = float3( p.z, h*p.y - 0.5*p.x, h*p.x + 0.5*p.y);
   
  float s = max(-q.x,0.0);
  float t = clamp( (q.y-0.5*p.z)/(m2+0.25), 0.0, 1.0 );
    
  float a = m2*(q.x+s)*(q.x+s) + q.y*q.y;
  float b = m2*(q.x+0.5*t)*(q.x+0.5*t) + (q.y-m2*t)*(q.y-m2*t);
    
  float d2 = min(q.y,-q.x*m2-q.y*0.5) > 0.0 ? 0.0 : min(a,b);
    
  return sqrt( (d2+q.z*q.z)/m2 ) * sign(max(q.z,-p.y));
}

float Octahedron(float3 p, float s)
{
  p = abs(p);
  float m = p.x+p.y+p.z-s;
  float3 q;
       if( 3.0*p.x < m ) q = p.xyz;
  else if( 3.0*p.y < m ) q = p.yzx;
  else if( 3.0*p.z < m ) q = p.zxy;
  else return m*0.57735027;
    
  float k = clamp(0.5*(q.z-q.y+s),0.0,s); 
  return length(float3(q.x,q.y-s+k,q.z-k)); 
}

#define epsilon 0.01
#define KERNEL_FUNCTION(function) \
  float3 function##EstimateNormal(float3 p) { \
    float x = function(float3(p.x+epsilon,p.y,p.z), SHAPE_SIZE) - function(float3(p.x-epsilon,p.y,p.z), SHAPE_SIZE); \
    float y = function(float3(p.x,p.y+epsilon,p.z), SHAPE_SIZE) - function(float3(p.x,p.y-epsilon,p.z), SHAPE_SIZE); \
    float z = function(float3(p.x,p.y,p.z+epsilon), SHAPE_SIZE) - function(float3(p.x,p.y,p.z-epsilon), SHAPE_SIZE); \
    return normalize(float3(x,y,z)); \
  }

KERNEL_FUNCTION(Sphere)
KERNEL_FUNCTION(Torus)
KERNEL_FUNCTION(Pyramid)
KERNEL_FUNCTION(Octahedron)
