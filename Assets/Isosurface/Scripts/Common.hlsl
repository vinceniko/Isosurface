#pragma multi_compile SPHERE TORUS PYRAMID OCTAHEDRON BOX NOISE

#include "Noise.hlsl"

#if defined(SPHERE)
 #define FUNCTION Sphere
# elif defined(TORUS)
 #define FUNCTION Torus
# elif defined(PYRAMID)
 #define FUNCTION Pyramid
# elif defined(OCTAHEDRON)
 #define FUNCTION Octahedron
# elif defined(BOX)
 #define FUNCTION Box
# elif defined(NOISE)
 #define FUNCTION SNoise
#endif

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

float Box( float3 p, float3 b)
{
  float3 q = abs(p) - b;
  return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
}

float displacement(float3 p) {
  return sin(20*p.x)*sin(20*p.y)*sin(20*p.z);
}

float opDisplace(float sdf, in float3 p )
{
    float d2 = displacement(p);
    return sdf+d2;
}

float opTwist(in float3 p )
{
    const float k = 10.0; // or some other amount
    float c = cos(k*p.y);
    float s = sin(k*p.y);
    float2x2  m = float2x2(c,-s,s,c);
    float3  q = float3(mul(m, p.xz),p.y);
    return FUNCTION(q, SHAPE_SIZE);
}

float opRep( in float3 p, in float3 c )
{
    float3 q = p+0.5*c % c-0.5*c;
    return FUNCTION( q, SHAPE_SIZE );
}

float4x4 inverse(float4x4 m) {
    float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
    float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
    float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
    float n41 = m[0][3], n42 = m[1][3], n43 = m[2][3], n44 = m[3][3];

    float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
    float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
    float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
    float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;

    float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
    float idet = 1.0f / det;

    float4x4 ret;

    ret[0][0] = t11 * idet;
    ret[0][1] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * idet;
    ret[0][2] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * idet;
    ret[0][3] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * idet;

    ret[1][0] = t12 * idet;
    ret[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * idet;
    ret[1][2] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * idet;
    ret[1][3] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * idet;

    ret[2][0] = t13 * idet;
    ret[2][1] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * idet;
    ret[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * idet;
    ret[2][3] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * idet;

    ret[3][0] = t14 * idet;
    ret[3][1] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * idet;
    ret[3][2] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * idet;
    ret[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * idet;

    return ret;
}

float3 transformPoint(float3 p) {
  return mul(_ShapeToWorld, float4(transformToCenter(p), 1)).xyz;
}

#define EPSILON 0.0001
float3 EstimateNormal(float3 p) {
  // p = mul(_ShapeToWorld, float4(p, 1)).xyz;
  
  float x = FUNCTION(float3(p.x+EPSILON,p.y,p.z), SHAPE_SIZE) - FUNCTION(float3(p.x-EPSILON,p.y,p.z), SHAPE_SIZE); \
  float y = FUNCTION(float3(p.x,p.y+EPSILON,p.z), SHAPE_SIZE) - FUNCTION(float3(p.x,p.y-EPSILON,p.z), SHAPE_SIZE); \
  float z = FUNCTION(float3(p.x,p.y,p.z+EPSILON), SHAPE_SIZE) - FUNCTION(float3(p.x,p.y,p.z-EPSILON), SHAPE_SIZE); 

  return normalize(float3(x,y,z)); 
}